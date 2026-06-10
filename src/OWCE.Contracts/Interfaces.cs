using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OWCE.Contracts
{
    public enum SpeedUnit { Mph, KmH }
    public enum TempUnit { Fahrenheit, Celsius }

    public class WatchPayload
    {
        public float CurrentSpeedMph { get; set; }
        public float TopSpeedMph { get; set; }
        public int BatteryPercent { get; set; }
        public float EstimatedRangeMiles { get; set; }
        public SpeedUnit SpeedUnit { get; set; }
        public bool IsRiding { get; set; }
    }

    public class RideSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public float TopSpeedMph { get; set; }
        public float DistanceMiles { get; set; }
        public string BoardSerial { get; set; } = string.Empty;
    }

    public interface IBLEService
    {
        bool IsScanning { get; }
        Task StartScanAsync(CancellationToken cancellationToken);
        Task StopScanAsync();
        Task ConnectAsync(string deviceId, CancellationToken cancellationToken);
        Task DisconnectAsync(string deviceId);
        
        event EventHandler<string> DeviceDiscovered;
        event EventHandler<bool> ConnectionStateChanged;
        event EventHandler<(string CharacteristicUuid, byte[] Data)> ValueUpdated;
    }

    public interface IBoardStateService
    {
        float CurrentSpeedMph { get; }
        int BatteryPercent { get; }
        float BatteryVoltage { get; }
        int Rpm { get; }
        
        event EventHandler StateUpdated;
    }

    public interface IRideService
    {
        RideSession CurrentRide { get; }
        bool IsRecording { get; }
        
        Task StartRideAsync();
        Task EndRideAsync();
        Task<IEnumerable<RideSession>> GetRideHistoryAsync();
    }

    public interface IWatchSyncService
    {
        Task InitializeAsync();
        Task SyncPayloadAsync(WatchPayload payload);
    }
}
