using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using OWCE.Contracts;

namespace OWCE.Services;

/// <summary>
/// Concrete BLE implementation using Plugin.BLE v3.x.
/// This is the only class in the codebase that references Plugin.BLE directly.
/// All other code interacts with IBLEService.
/// </summary>
public sealed class BLEService : IBLEService, IDisposable
{
    // Onewheel BLE Service UUID — all boards advertise this
    private const string OWServiceUuid = "E659F300-EA98-11E3-AC10-0800200C9A66";

    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private readonly Dictionary<string, ICharacteristic> _characteristics = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public bool IsScanning => _adapter.IsScanning;
    public bool IsConnected => _connectedDevice != null;

    public event EventHandler<DiscoveredBoard>? DeviceDiscovered;
    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<(string CharacteristicUuid, byte[] Data)>? ValueUpdated;
    public event EventHandler<int>? RssiUpdated;

    public BLEService()
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;
        _adapter.DeviceDiscovered += OnAdapterDeviceDiscovered;
        _adapter.DeviceConnected += OnAdapterDeviceConnected;
        _adapter.DeviceDisconnected += OnAdapterDeviceDisconnected;
        _adapter.DeviceConnectionLost += OnAdapterDeviceConnectionLost;
    }

    public async Task StartScanAsync(CancellationToken cancellationToken)
    {
        if (_ble.State != BluetoothState.On)
            throw new InvalidOperationException("Bluetooth is not enabled.");

        _adapter.ScanMode = ScanMode.LowLatency;
        await _adapter.StartScanningForDevicesAsync(
            serviceUuids: [Guid.Parse(OWServiceUuid)],
            cancellationToken: cancellationToken);
    }

    public async Task StopScanAsync()
    {
        if (_adapter.IsScanning)
            await _adapter.StopScanningForDevicesAsync();
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        // Find the device from a previous scan
        var device = _adapter.DiscoveredDevices
            .FirstOrDefault(d => d.Id.ToString().Equals(deviceId, StringComparison.OrdinalIgnoreCase));

        if (device is null)
            return false;

        await _adapter.ConnectToDeviceAsync(device, cancellationToken: cancellationToken);
        return true;
    }

    public async Task DisconnectAsync()
    {
        if (_connectedDevice is null) return;
        _characteristics.Clear();
        await _adapter.DisconnectDeviceAsync(_connectedDevice);
        _connectedDevice = null;
    }

    public async Task<byte[]> ReadCharacteristicAsync(string characteristicUuid, CancellationToken cancellationToken)
    {
        var characteristic = await GetOrFetchCharacteristicAsync(characteristicUuid, cancellationToken);
        var (data, _) = await characteristic.ReadAsync(cancellationToken);
        return data ?? [];
    }

    public async Task WriteCharacteristicAsync(string characteristicUuid, byte[] data, CancellationToken cancellationToken)
    {
        var characteristic = await GetOrFetchCharacteristicAsync(characteristicUuid, cancellationToken);
        await characteristic.WriteAsync(data, cancellationToken);
    }

    public async Task SubscribeToCharacteristicAsync(string characteristicUuid, CancellationToken cancellationToken)
    {
        var characteristic = await GetOrFetchCharacteristicAsync(characteristicUuid, cancellationToken);
        if (!characteristic.CanUpdate) return;

        characteristic.ValueUpdated += OnCharacteristicValueUpdated;
        await characteristic.StartUpdatesAsync(cancellationToken);
    }

    private async Task<ICharacteristic> GetOrFetchCharacteristicAsync(string uuid, CancellationToken cancellationToken)
    {
        if (_characteristics.TryGetValue(uuid, out var cached))
            return cached;

        if (_connectedDevice is null)
            throw new InvalidOperationException("No device connected.");

        var services = await _connectedDevice.GetServicesAsync(cancellationToken);
        foreach (var service in services)
        {
            var characteristics = await service.GetCharacteristicsAsync();
            foreach (var c in characteristics)
            {
                _characteristics[c.Id.ToString()] = c;
            }
        }

        if (_characteristics.TryGetValue(uuid, out var found))
            return found;

        throw new KeyNotFoundException($"Characteristic {uuid} not found on connected device.");
    }

    private void OnAdapterDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        DeviceDiscovered?.Invoke(this, new DiscoveredBoard
        {
            DeviceId = e.Device.Id.ToString(),
            Name = e.Device.Name ?? "Onewheel",
            Rssi = e.Device.Rssi
        });
    }

    private void OnAdapterDeviceConnected(object? sender, DeviceEventArgs e)
    {
        _connectedDevice = e.Device;
        ConnectionStateChanged?.Invoke(this, true);
    }

    private void OnAdapterDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        _connectedDevice = null;
        _characteristics.Clear();
        ConnectionStateChanged?.Invoke(this, false);
    }

    private void OnAdapterDeviceConnectionLost(object? sender, DeviceErrorEventArgs e)
    {
        _connectedDevice = null;
        _characteristics.Clear();
        ConnectionStateChanged?.Invoke(this, false);
    }

    private void OnCharacteristicValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        ValueUpdated?.Invoke(this, (e.Characteristic.Id.ToString(), e.Characteristic.Value));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _adapter.DeviceDiscovered -= OnAdapterDeviceDiscovered;
        _adapter.DeviceConnected -= OnAdapterDeviceConnected;
        _adapter.DeviceDisconnected -= OnAdapterDeviceDisconnected;
        _adapter.DeviceConnectionLost -= OnAdapterDeviceConnectionLost;
        _disposed = true;
    }
}
