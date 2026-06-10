# OWCE NextGen — Milestone Review Checklist

This document defines the human review gates for the OWCE modernization project.
Each milestone must be reviewed and signed off before the next phase begins.

---

## Milestone M1 — Foundation Complete
**Triggered after:** Phase 0 + Phase 1 commits merged to `main`

### Automated Checks (CI must pass)
- [ ] All unit tests pass (`dotnet test`)
- [ ] Android Debug build succeeds
- [ ] iOS Simulator build succeeds

### Human Review
- [ ] Review `AGENTS.md` — do the 12 Core Rules make sense for this project?
- [ ] Review `BACKLOG.md` — are the deferred features correctly documented?
- [ ] Review `src/OWCE.Contracts/` — do the interfaces cover all required functionality?
- [ ] Review `docs/adrs/` — do the ADRs reflect the decisions we discussed?
- [ ] Confirm the solution structure looks clean and navigable

**Sign-off:** _____________________ Date: _____________

---

## Milestone M2 — Core Services Complete
**Triggered after:** Phase 2 + Phase 3 + Phase 4 commits merged to `main`

### Automated Checks (CI must pass)
- [ ] All unit tests pass (target: >70% coverage on Services layer)
- [ ] Android Debug build succeeds
- [ ] iOS Simulator build succeeds

### Human Review
- [ ] Review `BLEService.cs` — does the scan/connect/subscribe flow look correct?
- [ ] Review `HandshakeService.cs` — are all board types handled (Pint, GT, GT-S)?
- [ ] Review `BoardStateService.cs` — is the RPM→speed conversion correct?
- [ ] Review `RideService.cs` — does top speed tracking work as expected?
- [ ] Review `BoardViewModel.cs` — is the 1 Hz watch sync wired correctly?
- [ ] **Physical device test:** Connect to a real Onewheel board and verify:
  - [ ] Board appears in scan list
  - [ ] Connection and handshake succeed
  - [ ] Speed, battery, and temperature values update in real time
  - [ ] Top speed is tracked correctly during a ride

**Sign-off:** _____________________ Date: _____________

---

## Milestone M3 — UI + Watch Complete
**Triggered after:** Phase 5 + Phase 6 commits merged to `main`

### Automated Checks (CI must pass)
- [ ] All unit tests pass
- [ ] Android Debug build succeeds
- [ ] iOS Simulator build succeeds
- [ ] Wear OS APK builds successfully

### Human Review
- [ ] Review `BoardPage.xaml` — does the live dashboard look correct?
- [ ] Review `SpeedArcView.cs` — does the speed arc render correctly?
- [ ] Review `RideHistoryPage.xaml` — does the ride history display correctly?
- [ ] **iOS Watch setup (human required):**
  - [ ] Open Xcode and add the `OWCE.Watch.iOS` project as a watchOS extension target
  - [ ] Set the bundle ID to `app.owce.maui.watchkitapp`
  - [ ] Verify WCSession activates when the watch app is installed
- [ ] **Physical device test (iOS):**
  - [ ] Install app via AltStore or TestFlight
  - [ ] Verify Apple Watch shows speed, top speed, battery, range during a ride
  - [ ] Verify app stays connected when phone screen is locked
- [ ] **Physical device test (Android):**
  - [ ] Install app via sideload (APK)
  - [ ] Sideload Wear OS APK to watch
  - [ ] Verify Wear OS watch shows speed, top speed, battery, range during a ride
  - [ ] Verify app stays connected when phone screen is locked

**Sign-off:** _____________________ Date: _____________

---

## Milestone M4 — Beta Release
**Triggered after:** Phase 7 commits merged to `main`

### Automated Checks (CI must pass)
- [ ] All unit tests pass (target: >80% coverage)
- [ ] Android Release build succeeds
- [ ] iOS Release build succeeds (requires signing certificate)

### Human Review
- [ ] Review `ci.yml` — does the CI pipeline cover all required checks?
- [ ] Review `DISTRIBUTION.md` — is the sideloading guide accurate and complete?
- [ ] **Full ride test:**
  - [ ] Complete a 30-minute ride with the app running
  - [ ] Verify ride is saved to history with correct stats
  - [ ] Verify top speed is correctly recorded
  - [ ] Verify watch displays correct data throughout the ride
  - [ ] Verify app does not disconnect when phone is locked
- [ ] Share beta APK/IPA with 2–3 community testers for feedback
- [ ] Address any critical bugs found by beta testers

**Sign-off:** _____________________ Date: _____________

---

## Milestone M5 — Public Release
**Triggered after:** Beta feedback addressed

### Checklist
- [ ] All M4 sign-off items complete
- [ ] Beta tester feedback addressed
- [ ] `DISTRIBUTION.md` updated with final installation instructions
- [ ] GitHub Release created with APK, IPA, and Wear OS APK attached
- [ ] Community announcement posted (Reddit r/onewheel, Discord)

**Sign-off:** _____________________ Date: _____________
