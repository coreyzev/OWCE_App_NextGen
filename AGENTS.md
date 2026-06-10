# OWCE App — AI Agent Shared Memory

This file serves as the shared context for all AI agents working on the OWCE App modernization project. **Every agent must read this file before writing any code.**

## The 12 Core Rules

These rules are non-negotiable. They exist to prevent AI-generated spaghetti code, context loss, and unmaintainable architectures.

1.  **Interface-first:** No implementation class is written until its interface is defined in `OWCE.Contracts` and reviewed.
2.  **Single Responsibility:** Every class has exactly one reason to change. Reject any class with 2+ unrelated concerns.
3.  **No `App.Current` in non-App code:** Business logic, services, and ViewModels must never reach up to `Application.Current`.
4.  **No UI in Services or Models:** `DisplayAlert`, navigation, and popups are the ViewModel's or Shell's responsibility, surfaced through events or `WeakReferenceMessenger`.
5.  **Dependency Injection for everything:** Nothing `new`s its own service-level dependencies.
6.  **No commented-out code in PRs:** Use `// TODO:` tags or GitHub issues for incomplete features.
7.  **Cancellation tokens everywhere async:** Every public async method that touches I/O accepts a `CancellationToken`.
8.  **Each PR addresses exactly one concern:** Scope work tightly.
9.  **Tests are written in the same phase as the code:** Tests are not a Phase 7 concern.
10. **Architecture review before each phase begins:** Review the plan against the approved architecture before writing code.
11. **Update this file:** `AGENTS.md` is the shared memory. Add patterns, gotchas, and decisions here.
12. **Reflection is required on failure:** Answer "What failed? What specific change would fix it? Am I repeating the same approach?" before retrying.

## Architectural Decisions & Patterns

-   **Framework:** .NET MAUI 9
-   **MVVM:** `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`)
-   **Messaging:** `WeakReferenceMessenger` (replaces `MessagingCenter`)
-   **BLE:** `Plugin.BLE` (abstracted behind `IBLEService`)
-   **Storage:** `SQLite-net-pcl` (for rides), `Microsoft.Maui.Storage.Preferences` (for settings)
-   **Timers:** Use `IDispatcherTimer`, NOT `Device.StartTimer`.
-   **Bindings:** All XAML must use compiled bindings (`x:DataType`) for NativeAOT compatibility.

## Known Gotchas

-   **iOS Background BLE:** iOS requires specific background modes and `CBCentralManager` configuration to keep BLE alive when backgrounded (Issue #111).
-   **Android 13+ BLE:** Requires `BLUETOOTH_SCAN` and `BLUETOOTH_CONNECT` runtime permissions (Issue #97).
-   **Watch Sync:** The watch apps are *passive displays*. They do not initiate BLE connections. The MAUI app pushes data to them.
-   **Speed Property:** `Speed` must be computed from `RPM` inside the model layer (not just the UI converter) so that `TopSpeed` can be accurately tracked during a ride.

## Feature Decisions (v2.1)

-   **Top Speed:** Tracked during ride, persisted to SQLite, and displayed as "All-Time Top Speed" in history.
-   **GPS Tracking:** Enabled using `Microsoft.Maui.Devices.Sensors` to support Strava/GPX export and route mapping.
-   **Unit Toggles:** Separate toggles for Speed (mph/km/h) and Temperature (°F/°C).
-   **Deferred Features (See BACKLOG.md):** Footpad sensors, Pitch/Roll/Yaw (to save BLE subscriptions), Voice Alerts, Ride Submission to server.
