# ADR-001: MAUI 9 Layered Architecture

**Status:** Accepted  
**Date:** 2026-06-10  
**Deciders:** Corey (Owner), Manus AI (Architect)

## Context

The original OWCE app was built on Xamarin.Forms (EOL May 2024) with a single God Class (`OWBoard.cs`, 1,600+ lines) handling BLE, handshake, ride recording, UI navigation, and HTTP. This made the app fragile, untestable, and difficult to extend.

## Decision

Rewrite the app in .NET MAUI 9 using a strict layered architecture:

1. **`OWCE.Contracts`** — Interfaces and shared models only. No implementation. No MAUI references.
2. **`OWCE.App/Services`** — Concrete service implementations. No UI references.
3. **`OWCE.App/ViewModels`** — `CommunityToolkit.Mvvm` ViewModels. No direct service calls from UI.
4. **`OWCE.App/Views`** — XAML pages and controls. No business logic.
5. **`OWCE.App/Platforms`** — Platform-specific implementations (BLE, Watch Sync).
6. **`OWCE.Tests`** — xUnit tests. References `OWCE.Contracts` only.

## Consequences

- Services are independently testable by mocking `OWCE.Contracts` interfaces.
- No layer may reference a layer above it.
- `App.Current` may only be referenced in `App.xaml.cs`.
- All navigation is via Shell routes defined in `AppShell.xaml.cs`.
