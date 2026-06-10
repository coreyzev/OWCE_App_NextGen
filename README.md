# OWCE App — NextGen

**Onewheel Community Edition App** — A community-built companion app for Onewheel boards.

This is a full modernization of the original [OWCE App](https://github.com/OnewheelCommunityEdition/OWCE_App), rewritten from Xamarin.Forms to **.NET MAUI 9** with a clean layered architecture, smartwatch support, and improved ride tracking.

> The original Xamarin codebase is preserved in the `legacy-xamarin` branch for reference.

> **NOTE:** Onewheel Community Edition app is not endorsed by or affiliated with Future Motion in any way.

## Features

- Live ride dashboard: speed, battery, voltage, temperature, ride mode
- Top speed tracking (current ride and all-time)
- Ride history with distance, average speed, and top speed
- GPS ride recording
- Light controls (front and rear)
- Apple Watch companion app (current speed, top speed, battery, range)
- Wear OS companion app (current speed, top speed, battery, range)
- Supports all Onewheel boards: V1, Plus, XR, Pint, Pint X, GT, GT-S

> **NOTE:** GT support requires patching with [Rewheel](https://github.com/rewheel-app/rewheel). GT-S support uses the Polaris 6215 protocol.

## Architecture

See `docs/adrs/` for Architecture Decision Records.

```
src/
  OWCE.Contracts/   — Interfaces and shared models (no implementation)
  OWCE.App/         — .NET MAUI 9 application
    Services/       — Business logic (BLE, Board State, Ride, Settings)
    ViewModels/     — CommunityToolkit.Mvvm ViewModels
    Views/          — XAML pages and controls
    Platforms/      — Platform-specific implementations
  OWCE.Tests/       — xUnit unit tests
docs/
  adrs/             — Architecture Decision Records
AGENTS.md           — AI agent shared memory and core rules
BACKLOG.md          — Deferred features with context
```

## Development

This project is developed with AI agent assistance. See `AGENTS.md` for the core rules that govern all code contributions.

### Prerequisites

- .NET 9 SDK
- Visual Studio 2022 (Windows) or Visual Studio for Mac / Rider (macOS)
- Xcode 15+ (for iOS and watchOS builds)
- Android SDK API 23+ (for Android builds)

### Building

```bash
dotnet build OWCE.sln
dotnet test src/OWCE.Tests/OWCE.Tests.csproj
```

## Distribution

- **Android:** F-Droid (primary) and direct APK sideloading
- **iOS:** AltStore / SideStore

## Contributing

Please read `AGENTS.md` before contributing. All PRs are reviewed against the AI Review Checklist.
