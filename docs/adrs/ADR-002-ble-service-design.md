# ADR-002: BLE Service Design

**Status:** Accepted  
**Date:** 2026-06-10

## Context

The original app used a custom `IOWBLE` abstraction over Xamarin.Forms BLE. The new app must support Plugin.BLE v3.x (MAUI-compatible), handle Android 12+ permission requirements, and keep BLE alive in the iOS background.

## Decision

`IBLEService` is the single BLE abstraction. `BLEService` (in `Services/`) wraps Plugin.BLE. Platform-specific overrides (iOS background mode, Android permission flow) live in `Platforms/iOS/` and `Platforms/Android/`.

`IBoardStateService` is a separate concern from `IBLEService`. It receives raw `byte[]` from `IBLEService.ValueUpdated` and produces typed `BoardState` snapshots. This separation means `IBoardStateService` has zero BLE dependencies and is fully unit-testable with mock byte arrays.

## Board Hardware Revision Ranges

| Board | HW Revision Range | Notes |
|---|---|---|
| V1 | 1–2999 | 16 cells |
| Plus | 3000–3999 | 16 cells |
| XR | 4000–4999 | 15 cells (cell 15 ignored) |
| Pint | 5000–5999 | 15 cells |
| GT | 6000–6999 | 18 cells. Requires Rewheel for BLE handshake patch. |
| Pint X | 7000–7999 | 15 cells |
| GT-S | 8000–8999 | Polaris 6215 protocol. Static 20-byte token + 15s keep-alive. |

## Consequences

- BLE scanning, connection, and characteristic I/O are all async with `CancellationToken`.
- `IDispatcherTimer` replaces `Device.StartTimer` for the keep-alive loop.
- The handshake strategy pattern allows GT-S token rotation without touching `BoardStateService`.
