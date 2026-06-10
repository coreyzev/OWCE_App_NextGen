# ADR-003: Ride Data Storage and SQLite Schema

**Status:** Accepted  
**Date:** 2026-06-10

## Context

The original app stored ride data as protobuf binary files with minimal metadata in a SQLite table (`StartTime`, `EndTime`, `DataFileName`, `BoardSerial`). Top speed, average speed, and distance were not persisted. GPS was not recorded.

## Decision

Replace the protobuf binary approach with a fully relational SQLite schema using `sqlite-net-pcl`.

### Schema

**`RideSession`** — one row per completed ride.

| Column | Type | Notes |
|---|---|---|
| `Id` | TEXT (PK) | GUID |
| `StartTime` | DATETIME | UTC |
| `EndTime` | DATETIME | UTC, nullable |
| `TopSpeedMph` | REAL | Tracked in-memory, persisted on EndRide |
| `AvgSpeedMph` | REAL | Computed from data points |
| `DistanceMiles` | REAL | From TripOdometer at EndRide |
| `BoardSerial` | TEXT | |
| `BoardType` | INTEGER | `OWBoardType` enum value |

**`RideDataPoint`** — sampled at 1 Hz during recording.

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER (PK, autoincrement) | |
| `RideSessionId` | TEXT (FK) | |
| `Timestamp` | DATETIME | UTC |
| `SpeedMph` | REAL | |
| `BatteryPercent` | INTEGER | |
| `LatitudeDeg` | REAL | nullable — GPS may not be available |
| `LongitudeDeg` | REAL | nullable |

## Top Speed Logic

`TopSpeedMph` is tracked in-memory in `RideService` during an active ride. On every `IBoardStateService.StateUpdated` event, if `state.SpeedMph > TopSpeedThisRide`, the value is updated. On `EndRideAsync`, the value is persisted to `RideSession.TopSpeedMph`.

`AllTimeTopSpeed` is computed as `MAX(TopSpeedMph)` across all `RideSession` rows.

## GPS

GPS recording uses `Microsoft.Maui.Devices.Sensors.Geolocation`. It is best-effort — if the user denies location permission, ride recording continues without GPS data. `LatitudeDeg` and `LongitudeDeg` are nullable for this reason.

## Consequences

- No more binary protobuf ride files. All data is queryable SQL.
- `IRideService` is the only class that touches SQLite.
- `RideService` must be thread-safe: BLE events arrive on a background thread.
