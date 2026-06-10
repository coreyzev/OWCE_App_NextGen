# OWCE App — Deferred Features Backlog

This document tracks features that were present in the original Xamarin app or requested by the community, but have been explicitly deferred from the initial v2.1 modernization release.

## 1. Footpad Sensors & Diagnostics (Pitch/Roll/Yaw)
- **Status:** Deferred
- **Context:** The original app included `FootpadsView` and `AngleView` (Pitch/Roll/Yaw). However, these were commented out by the original author with the note: *"removed to make way for bluetooth subscription limits on Android."* Android imposes a hard limit on the number of concurrent BLE characteristic subscriptions. Prioritizing core ride metrics (Speed, Battery, Voltage, RPM) required sacrificing these diagnostic metrics.
- **Future Implementation:** If re-introduced, these should be placed behind a "Diagnostics Mode" toggle that unsubscribes from non-essential metrics (like individual battery cells) to stay under the Android subscription limit.

## 2. Voice Alerts
- **Status:** Deferred
- **Context:** A community-requested feature to provide audio warnings for low battery, high speed, and pushback using Text-to-Speech (TTS).
- **Future Implementation:** The architecture supports this via an `IVoiceAlertService` interface. It can be implemented in a future release using `Microsoft.Maui.Media.TextToSpeech`.

## 3. Ride Submission to OWCE Servers
- **Status:** Deferred
- **Context:** The original app allowed users to submit anonymized ride data to `api.owce.app`. We are not affiliated with the original author and do not control that server.
- **Future Implementation:** We would need to stand up our own backend infrastructure to collect and aggregate this data. For now, ride tracking is strictly local (SQLite) and exportable (Strava/GPX).

## 4. Max Recommended Speed (Pushback Indicator)
- **Status:** Deferred
- **Context:** The original app had hardcoded pushback speeds for different ride modes (e.g., Delirium = 20 mph) to show a warning on the speed arc. Future Motion does not expose actual pushback thresholds via BLE; these were community estimates.
- **Future Implementation:** Unless the actual pushback threshold is reverse-engineered from the BLE stream, hardcoded estimates are unreliable across different firmware versions and rider weights. Skip until accurate data is available.
