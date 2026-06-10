# OWCE NextGen — Progress Tracker

> **How to use this file:** Update task status as work is completed. Each task maps to a commit or PR.
> When a Jira/Linear integration is set up, this file can be retired.
>
> Status key: `[x]` done · `[-]` in progress · `[ ]` not started · `[~]` deferred to BACKLOG.md

---

## Phase 0 — Foundation & Blueprint
**Goal:** Shared contracts, rules, and architecture decisions that all agents reference.

- [x] Create `legacy-xamarin` branch preserving original Xamarin codebase
- [x] Write `AGENTS.md` — 12 Core Rules + shared agent memory
- [x] Write `BACKLOG.md` — deferred features with context
- [x] Create `OWCE.Contracts` project with all interfaces, enums, models, `BLEUuids`
- [x] Write ADR-001 — Overall architecture overview
- [x] Write ADR-002 — BLE service design
- [x] Write ADR-003 — Ride data / SQLite schema
- [x] Write ADR-004 — Watch sync architecture
- [x] Write `docs/MILESTONE_CHECKLIST.md`
- [x] Write `docs/DISTRIBUTION.md`

---

## Phase 1 — MAUI Solution Scaffold
**Goal:** Compilable solution skeleton with DI, navigation, and folder structure.

- [x] Create `OWCE.sln` with all project references
- [x] Create `OWCE.App.csproj` targeting iOS + Android (net9.0)
- [x] Create `OWCE.Tests.csproj` (xUnit + FluentAssertions + Moq)
- [x] Write `MauiProgram.cs` with full DI container wiring
- [x] Write `AppShell.xaml` with all routes defined
- [x] Write `App.xaml` / `App.xaml.cs`
- [x] Write `AppSettingsService.cs`
- [x] Create placeholder pages for all 5 screens
- [x] Create `Messages/Messages.cs` (WeakReferenceMessenger types)
- [x] Write `Resources/Styles/Colors.xaml` and `Styles.xaml`
- [x] Write `Platforms/iOS/Info.plist` (BLE + location permissions)
- [x] Write `Platforms/Android/AndroidManifest.xml` (BLE + Wearable permissions)
- [ ] **Verify solution compiles** (`dotnet build`) — *requires Mac + .NET 9 MAUI workload*

---

## Phase 2 — BLE Service Layer
**Goal:** Full BLE abstraction, board identification, and handshake for all board types.

- [x] Write `IBLEService` interface in `OWCE.Contracts`
- [x] Write `BLEService.cs` — **stub only, needs Plugin.BLE implementation**
- [x] Write `BoardStateService.cs` — full BLE byte parser (RPM→speed, all UUIDs, board type detection)
- [x] Write `HandshakeService.cs` — all strategies (V1/Plus/XR=none, Pint/GT=Gemini, GT-S=Polaris 6215)
- [x] Write `BoardConnectionService.cs` — full connect→identify→handshake→subscribe orchestration
- [x] Write `HandshakeServiceTests.cs`
- [x] Write `BoardStateServiceTests.cs`
- [ ] **Implement `BLEService.cs`** — wrap `Plugin.BLE` (reference `legacy-xamarin/OWCE.iOS/OWBLE.cs`)
- [ ] **Integration test on real hardware** — connect to Onewheel, verify telemetry flows

---

## Phase 3 — Ride Data Layer
**Goal:** SQLite persistence, ride lifecycle, top speed tracking, GPS data points.

- [x] Write `IRideService` interface in `OWCE.Contracts`
- [x] Write `RideService.cs` — ride recording, top speed, 1Hz SQLite data points, GPS
- [x] Write `RideModels.cs` — SQLite entity classes
- [x] Write `RideServiceTests.cs`
- [ ] **Add SQLite attributes** to `RideModels.cs` (`[Table]`, `[PrimaryKey]`, `[AutoIncrement]`)
- [ ] **Integration test** — start/end ride, verify rows in SQLite

---

## Phase 4 — ViewModels
**Goal:** Full CommunityToolkit.Mvvm ViewModel layer.

- [x] Write `BaseViewModel.cs`
- [x] Write `BoardListViewModel.cs` — BLE scan, connect via `IBoardConnectionService`
- [x] Write `BoardViewModel.cs` — live telemetry, top speed, watch sync at 1Hz, light controls stub
- [x] Write `AppSettingsViewModel.cs` — speed unit toggle, temp unit toggle, auto-record
- [x] Write `RideHistoryViewModel.cs` — ride list from SQLite
- [x] Write `BoardDetailsViewModel.cs` — board info, lifetime odometer
- [x] Write `ViewModelTests.cs`
- [ ] **Verify ViewModel bindings** after compile fixes in Phase 2

---

## Phase 5 — UI Migration
**Goal:** All XAML pages fully implemented and bound to ViewModels.

- [x] Write `SpeedArcView.cs` — SkiaSharp custom control (current speed arc + top speed tick)
- [x] Write `BatteryView.xaml` — battery percentage visual
- [x] Write `BoardListPage.xaml` — scan list, connect button
- [x] Write `BoardPage.xaml` — live ride dashboard (speed, battery, stats, lights)
- [x] Write `RideHistoryPage.xaml` — past rides list
- [x] Write `AppSettingsPage.xaml` — unit toggles, auto-record
- [x] Write `BoardDetailsPage.xaml` — board info
- [x] Write all Converters (SpeedConverter, TempConverter, DistanceConverter, etc.)
- [ ] **Wire light controls** — `BoardViewModel.SetLightModeAsync` stub needs `IBLEService.WriteCharacteristicAsync` call (~20 lines)
- [ ] **UI polish pass** — verify SpeedArc renders correctly on device, tune colors/layout
- [ ] **Dark mode verification**

---

## Phase 6 — Smartwatch Integration
**Goal:** Live telemetry on Apple Watch and Wear OS during a ride.

- [x] Write iOS `WatchSyncService.cs` — WCSession phone-side implementation
- [x] Write `OWCE.Watch.iOS/Controllers/InterfaceController.cs` — watchOS UI (speed, top speed, battery, range)
- [x] Write `OWCE.Watch.iOS/OWCE.Watch.iOS.csproj`
- [x] Write Android `WatchSyncService.cs` — DataClient phone-side implementation (DataClient cached)
- [x] Write `OWCE.Watch.Android/src/MainActivity.kt` — Wear OS Compose UI
- [x] Write `OWCE.Watch.Android/src/OWCEDataListenerService.kt` — DataListenerService
- [x] Write `OWCE.Watch.Android/build.gradle.kts`
- [x] Write `OWCE.Watch.Android/AndroidManifest.xml`
- [x] Write `WatchSyncServiceTests.cs`
- [x] Write `Platforms/iOS/MauiProgram.iOS.cs` — registers `IWatchSyncService` on iOS
- [x] Write `Platforms/Android/MauiProgram.Android.cs` — registers `IWatchSyncService` on Android
- [ ] **Xcode: add watchOS extension target** — link `OWCE.Watch.iOS` to iOS app (manual, ~30 min, see `MILESTONE_CHECKLIST.md` M3)
- [ ] **Create `libs.versions.toml`** for Wear OS Gradle version catalog
- [ ] **Build and test on Apple Watch**
- [ ] **Build and test on Wear OS**

---

## Phase 7 — Hardening, CI/CD & Distribution
**Goal:** Stable, signed builds ready for user distribution.

- [x] Write `.github/workflows/ci.yml` — build + test on every push (macOS runner)
- [x] Write `Platforms/iOS/BackgroundBLEHandler.cs` — BGTaskScheduler reconnect fix
- [x] Write `Platforms/iOS/AppDelegate.cs` — background task registration
- [x] Update `Info.plist` — `bluetooth-central` background mode + BGTaskScheduler identifier
- [x] Write `Platforms/Android/MainApplication.cs` — Android 13+ NEARBY_DEVICES permission handling
- [x] Update `AndroidManifest.xml` — Android 12+ BLE permissions
- [ ] **Verify CI passes** — check GitHub Actions tab after next push
- [ ] **Fix any CI failures**
- [ ] **Build signed Android APK** (`dotnet publish -f net9.0-android -c Release`)
- [ ] **Build signed iOS IPA** (`dotnet publish -f net9.0-ios -c Release`)
- [ ] **Submit to F-Droid** (Android open-source store)
- [ ] **Set up AltStore listing** or TestFlight (iOS)
- [~] Strava / GPX export — deferred, see `BACKLOG.md`
- [~] GPS route map on ride detail — deferred, see `BACKLOG.md`

---

## Code Review Gates

| Milestone | Trigger | Reviewer | Status |
|---|---|---|---|
| M1 — Architecture | After Phase 0+1 commits | Claude Sonnet | **Done** — 10 findings, all fixed in commit `4c1ff01` |
| M2 — Services | After BLEService implemented + tests pass | Claude Opus (recommended) | Not started |
| M3 — First device run | After Phase 5 UI verified on hardware | Human (you) | Not started |
| M4 — Watch apps | After watch apps verified on hardware | Human (you) | Not started |
| M5 — Release candidate | Before publishing to F-Droid / AltStore | Human (you) + Claude | Not started |

---

## Known Issues / Watch List

| # | Issue | Severity | Notes |
|---|---|---|---|
| 1 | `BLEService.cs` is a stub | Blocker | Implement before any device testing |
| 2 | SQLite entity attributes missing | Blocker | Add before ride recording works |
| 3 | iOS background BLE disconnect | High | Fix in place (`BackgroundBLEHandler.cs`), needs device verification |
| 4 | Android 13+ crash | High | Fix in place (`MainApplication.cs`), needs device verification |
| 5 | GT board requires Rewheel patch | Medium | Documented in `HandshakeService.cs` — surface clear error to user |
| 6 | GT-S Polaris token may rotate | Low | Strategy pattern in place — swap token in `HandshakeService.cs` if needed |
| 7 | App Store risk (Future Motion) | Medium | Use TestFlight / AltStore, avoid App Store review until community decides |
