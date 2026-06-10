# OWCE App — NextGen

> ## ⚠️ WORK IN PROGRESS — NOT YET FUNCTIONAL
> This rewrite is under active development. **The app does not run yet.**
> See [`PROGRESS.md`](PROGRESS.md) for the full task tracker.

---

**Onewheel Community Edition App** — A community-built companion app for Onewheel boards.

A full modernization of the original [OWCE App](https://github.com/OnewheelCommunityEdition/OWCE_App),
rewritten from Xamarin.Forms to **.NET MAUI 9** with a clean layered architecture, smartwatch support, and improved ride tracking.

> The original Xamarin codebase is preserved in the `legacy-xamarin` branch for reference.

> **Not endorsed by or affiliated with Future Motion in any way.**

---

## Overall Progress

```
Architecture & Contracts  [████████████████████] 100%  ✓ Done
BLE Service Layer         [████████████████░░░░]  80%  ⚠ BLEService.cs is a stub
Ride Data / SQLite        [███████████████░░░░░]  75%  ⚠ SQLite attributes missing
ViewModels                [████████████████████] 100%  ✓ Done (pending compile verify)
UI / XAML Pages           [████████████░░░░░░░░]  60%  ⚠ Needs device verification
Apple Watch               [█████████████░░░░░░░]  65%  ⚠ Xcode wiring step pending
Wear OS                   [████████████░░░░░░░░]  60%  ⚠ Gradle version catalog pending
CI / CD                   [████████████████████] 100%  ✓ Done
Distribution              [░░░░░░░░░░░░░░░░░░░░]   0%  ✗ Not started

Overall                   [████████████░░░░░░░░]  ~35% (architecture done, no device run yet)
```

> **What "35%" means in practice:** The design, contracts, and most service/ViewModel code is written and reviewed.
> The app has never been compiled or run on a device. The next milestone is getting it to build and connect to a board.
> Realistically, it is 2–3 focused sessions away from a first working build.

---

## Planned Features

- Live ride dashboard: speed, battery, voltage, temperature, ride mode
- Top speed tracking (current ride and all-time)
- Ride history with distance, average speed, and top speed
- GPS ride data recording
- Light controls (front and rear)
- Apple Watch companion app (speed, top speed, battery, range)
- Wear OS companion app (speed, top speed, battery, range)
- Supports all Onewheel boards: V1, Plus, XR, Pint, Pint X, GT, GT-S

> GT support requires patching with [Rewheel](https://github.com/rewheel-app/rewheel).
> GT-S support uses the Polaris 6215 protocol (community-documented).

---

## Architecture

```
src/
  OWCE.Contracts/   — Interfaces, enums, BLEUuids, shared models (no implementation)
  OWCE.App/         — .NET MAUI 9 application
    Services/       — BLE, Board State, Handshake, Ride, Settings
    ViewModels/     — CommunityToolkit.Mvvm ViewModels
    Views/          — XAML pages and controls
    Platforms/      — iOS and Android platform-specific code
  OWCE.Watch.iOS/   — watchOS companion app (native Swift/C#)
  OWCE.Watch.Android/ — Wear OS companion app (Kotlin + Compose)
  OWCE.Tests/       — xUnit unit tests
docs/
  adrs/             — Architecture Decision Records (why things are the way they are)
AGENTS.md           — AI agent shared memory and 12 Core Rules
BACKLOG.md          — Deferred features with context
PROGRESS.md         — Full task tracker
```

See `docs/adrs/` for the full Architecture Decision Records.

---

## Development

This project is developed with AI agent assistance (Manus, Claude, Codex).
See [`AGENTS.md`](AGENTS.md) for the 12 Core Rules that govern all contributions.
See [`PROGRESS.md`](PROGRESS.md) for what's done and what's next.

### Prerequisites

- .NET 9 SDK with MAUI workload (`dotnet workload install maui`)
- Visual Studio for Mac or JetBrains Rider (macOS, for iOS builds)
- Xcode 15+ (iOS and watchOS)
- Android SDK API 23+

### Building

```bash
dotnet restore OWCE.sln
dotnet build OWCE.sln
dotnet test src/OWCE.Tests/OWCE.Tests.csproj
```

---

## Distribution (Planned)

- **Android:** F-Droid (primary) and direct APK sideloading
- **iOS:** AltStore / SideStore (no App Store account required for users)

See [`docs/DISTRIBUTION.md`](docs/DISTRIBUTION.md) for full instructions.

---

## Contributing

Read [`AGENTS.md`](AGENTS.md) before contributing. All PRs are reviewed against the 12 Core Rules.
