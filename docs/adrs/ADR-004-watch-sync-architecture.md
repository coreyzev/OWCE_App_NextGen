# ADR-004: Smartwatch Sync Architecture

**Status:** Accepted  
**Date:** 2026-06-10

## Context

The original app had a partial `WatchSyncEventHandler.cs` for iOS only. Android watch support was empty stubs. The new app must support Apple Watch (watchOS) and Wear OS with a unified interface.

## Decision

`IWatchSyncService` is the single abstraction. Platform implementations are registered in `MauiProgram.cs` via conditional compilation or `#if` platform directives.

### iOS — WCSession (WatchConnectivity)

- Native watchOS app built in Xcode (SwiftUI). .NET MAUI does not support watchOS targets.
- The MAUI iOS app implements `IWatchSyncService` using `WatchConnectivity.WCSession`.
- Data is sent via `WCSession.SendMessage` (when watch is reachable) or `UpdateApplicationContext` (background sync).
- Updates are throttled to 1 Hz to preserve watch battery.
- The watchOS app is bundled inside the MAUI iOS `.ipa` via the Xcode project reference pattern documented in `MauiWithWatchApps`.

### Android — Wearable Data Layer API

- Native Wear OS app built in Android Studio (Kotlin + Jetpack Compose for Wear OS).
- The MAUI Android app implements `IWatchSyncService` using `Wearable.DataClient`.
- `WatchPayload` is serialized to a `DataItem` at path `/owce/ridedata`.
- `Wearable.MessageClient` is used for immediate events (ride started/stopped).
- The Wear OS `.apk` is bundled within the Android `.aab` via the standard `wearApp` Gradle dependency.

## WatchPayload Fields

- `CurrentSpeedMph` (float)
- `TopSpeedMph` (float)
- `BatteryPercent` (int)
- `EstimatedRangeMiles` (float)
- `SpeedUnit` (enum: Mph / KmH) — so the watch can display in the user's preferred unit
- `IsRiding` (bool) — watch shows "Not Riding" state when false

## Consequences

- Watch apps are passive displays only. They never initiate BLE connections.
- Phase 6 requires human assistance for initial Xcode project setup (watchOS bundle).
- Wear OS tile is a Phase 6 stretch goal; the full Wear OS app is the primary deliverable.
