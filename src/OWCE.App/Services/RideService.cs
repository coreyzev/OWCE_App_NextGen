using SQLite;
using OWCE.Contracts;
using OWCE.Models;

namespace OWCE.Services;

/// <summary>
/// Manages ride session recording, top speed tracking, and SQLite persistence.
///
/// This is the only class in the codebase that reads or writes to the SQLite database.
/// All other code interacts with IRideService.
///
/// Thread safety: BLE state updates arrive on background threads.
/// All mutable state is protected by a lock. SQLite operations are async.
/// </summary>
public sealed class RideService : IRideService, IAsyncDisposable
{
    private readonly IBoardStateService _boardState;
    private readonly IDispatcher _dispatcher;
    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _dbLock = new(1, 1);

    // ── In-memory ride state ──────────────────────────────────────────────────
    private readonly object _rideLock = new();
    private RideSessionEntity? _currentRideEntity;
    private float _topSpeedThisRide;
    private float _speedAccumulator;
    private int _speedSampleCount;
    private IDispatcherTimer? _sampleTimer;
    private CancellationTokenSource? _rideCts;

    public RideSession? CurrentRide => _currentRideEntity?.ToContract();
    public bool IsRecording => _currentRideEntity != null;
    public float TopSpeedThisRide => _topSpeedThisRide;

    public event EventHandler<RideSession>? RideEnded;

    public RideService(IBoardStateService boardState, IDispatcher dispatcher)
    {
        _boardState = boardState;
        _dispatcher = dispatcher;
        _boardState.StateUpdated += OnStateUpdated;
    }

    private async Task EnsureDbAsync()
    {
        if (_db != null) return;

        await _dbLock.WaitAsync();
        try
        {
            if (_db != null) return;
            var dbPath = Path.Combine(
                FileSystem.AppDataDirectory, "owce_rides.db3");
            _db = new SQLiteAsyncConnection(dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _db.CreateTableAsync<RideSessionEntity>();
            await _db.CreateTableAsync<RideDataPointEntity>();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task StartRideAsync(
        string boardSerial,
        OWBoardType boardType,
        CancellationToken cancellationToken)
    {
        await EnsureDbAsync();

        lock (_rideLock)
        {
            if (_currentRideEntity != null) return; // Already recording

            _topSpeedThisRide = 0;
            _speedAccumulator = 0;
            _speedSampleCount = 0;
            _rideCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _currentRideEntity = new RideSessionEntity
            {
                StartTime   = DateTime.UtcNow,
                BoardSerial = boardSerial,
                BoardType   = boardType,
            };
        }

        await _db!.InsertAsync(_currentRideEntity);

        // Start the 1-Hz data point sampling timer via injected IDispatcher (not Application.Current)
        _dispatcher.Dispatch(() =>
        {
            _sampleTimer = _dispatcher.CreateTimer();
            _sampleTimer.Interval = TimeSpan.FromSeconds(1);
            _sampleTimer.Tick += OnSampleTimerTick;
            _sampleTimer.Start();
        });
    }

    public async Task EndRideAsync()
    {
        if (_currentRideEntity is null) return;

        _sampleTimer?.Stop();
        _rideCts?.Cancel();

        RideSessionEntity completed;
        lock (_rideLock)
        {
            completed = _currentRideEntity;
            completed.EndTime = DateTime.UtcNow;
            completed.TopSpeedMph = _topSpeedThisRide;
            completed.AvgSpeedMph = _speedSampleCount > 0
                ? _speedAccumulator / _speedSampleCount
                : 0;

            // Distance from the board's trip odometer at end of ride
            completed.DistanceMiles = _boardState.CurrentState?.TripOdometerMiles ?? 0;

            _currentRideEntity = null;
            _topSpeedThisRide = 0;
        }

        await EnsureDbAsync();
        await _db!.UpdateAsync(completed);

        RideEnded?.Invoke(this, completed.ToContract());
    }

    public async Task<IReadOnlyList<RideSession>> GetRideHistoryAsync()
    {
        await EnsureDbAsync();
        var entities = await _db!.Table<RideSessionEntity>()
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();
        return entities.Select(e => e.ToContract()).ToList();
    }

    public async Task<float> GetAllTimeTopSpeedAsync()
    {
        await EnsureDbAsync();
        var max = await _db!.ExecuteScalarAsync<float>(
            "SELECT MAX(TopSpeedMph) FROM RideSessions");
        return max;
    }

    /// <summary>
    /// Simple linear range estimate based on battery percentage and board type.
    /// Lives here (not in the ViewModel) so it can be tested and reused by watch sync.
    /// A more accurate model would use trip amp-hours and current voltage.
    /// </summary>
    public float EstimateRangeMiles(OWBoardType boardType, int batteryPercent)
    {
        float maxRangeMiles = boardType switch
        {
            OWBoardType.XR    => 18f,
            OWBoardType.GT    => 20f,
            OWBoardType.GTS   => 20f,
            OWBoardType.PintX => 12f,
            OWBoardType.Pint  => 8f,
            OWBoardType.Plus  => 7f,
            OWBoardType.V1    => 6f,
            _                 => 10f,
        };
        return maxRangeMiles * (batteryPercent / 100f);
    }

    private void OnStateUpdated(object? sender, BoardState state)
    {
        if (_currentRideEntity is null) return;

        lock (_rideLock)
        {
            if (state.SpeedMph > _topSpeedThisRide)
                _topSpeedThisRide = state.SpeedMph;

            _speedAccumulator += state.SpeedMph;
            _speedSampleCount++;
        }
    }

    private async void OnSampleTimerTick(object? sender, EventArgs e)
    {
        if (_currentRideEntity is null || _boardState.CurrentState is null) return;

        var state = _boardState.CurrentState;
        var dataPoint = new RideDataPointEntity
        {
            RideSessionId  = _currentRideEntity.Id,
            Timestamp      = DateTime.UtcNow,
            SpeedMph       = state.SpeedMph,
            BatteryPercent = state.BatteryPercent,
        };

        // Best-effort GPS — does not fail the sample if location is unavailable
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location != null)
            {
                dataPoint.LatitudeDeg    = location.Latitude;
                dataPoint.LongitudeDeg   = location.Longitude;
                dataPoint.AltitudeMeters = location.Altitude;
            }
        }
        catch
        {
            // Location permission denied or unavailable — continue without GPS
        }

        try
        {
            await EnsureDbAsync();
            await _db!.InsertAsync(dataPoint);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OWCE] Failed to save data point: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _sampleTimer?.Stop();
        _boardState.StateUpdated -= OnStateUpdated;
        if (_db != null)
            await _db.CloseAsync();
    }
}
