# OWCE NextGen — Distribution Guide

This document explains how to install the OWCE NextGen app on your iPhone or Android phone,
and how to install the companion watch app on your Apple Watch or Wear OS watch.

OWCE is a community-built app that is not affiliated with Future Motion.
Because it communicates with Onewheel hardware via reverse-engineered BLE protocols,
it cannot be distributed through the Apple App Store or Google Play Store.
The installation methods below are the standard approach for community apps of this type.

---

## Android Phone App

### Method 1: Direct APK Sideload (Recommended)

This is the simplest method and works on all Android phones.

1. On your Android phone, go to **Settings → Apps → Special App Access → Install Unknown Apps**.
2. Enable "Install Unknown Apps" for your browser or file manager.
3. Download the latest `OWCE-android.apk` from the [GitHub Releases page](https://github.com/coreyzev/OWCE_App_NextGen/releases).
4. Open the downloaded APK file and tap **Install**.
5. Open OWCE from your app drawer.

### Method 2: F-Droid (Coming Soon)

The app will be submitted to [F-Droid](https://f-droid.org), the open-source Android app repository.
Once listed, you can install and update it directly through the F-Droid app without enabling unknown sources.

---

## Wear OS Watch App

The Wear OS watch app is a separate APK that must be installed on your watch.

### Prerequisites

- Your phone and watch must be paired via the Wear OS companion app.
- Enable **Developer Options** on your watch: go to **Settings → System → About** and tap the build number 7 times.
- Enable **ADB Debugging** in Developer Options.

### Installation via ADB

1. Find your watch's IP address: **Settings → Developer Options → ADB Debugging → Wireless Debugging**.
2. On your computer, run:
   ```
   adb connect <watch-ip-address>:5555
   adb install OWCE-wear.apk
   ```
3. The OWCE watch app will appear in your watch's app drawer.

### Automatic Installation (Future)

Once the Android phone app is published to F-Droid or an alternative store that supports
Wear OS companion app bundling, the watch app will install automatically alongside the phone app.

---

## iPhone App

### Method 1: AltStore / SideStore (Recommended)

[AltStore](https://altstore.io) and [SideStore](https://sidestore.io) are community tools
that allow you to sideload apps on iPhone without a jailbreak.

**Setup (one-time):**

1. Install AltStore on your computer from [altstore.io](https://altstore.io).
2. Connect your iPhone to your computer and install AltStore onto your iPhone via the AltStore desktop app.
3. Trust the developer certificate on your iPhone: **Settings → General → VPN & Device Management**.

**Installing OWCE:**

1. Download the latest `OWCE-ios.ipa` from the [GitHub Releases page](https://github.com/coreyzev/OWCE_App_NextGen/releases).
2. Open AltStore on your iPhone and tap the **+** button.
3. Select the downloaded `.ipa` file.
4. OWCE will install and appear on your home screen.

**Important:** Free Apple Developer accounts require re-signing every 7 days.
Connect your phone to your computer with AltStore running to refresh automatically,
or use SideStore which can refresh over Wi-Fi without a computer.

### Method 2: TestFlight (For Beta Testers)

If you have been invited to the beta program, you can install OWCE via TestFlight.
This provides automatic updates and does not require re-signing.
Contact the project maintainer for a TestFlight invitation.

### Method 3: EU App Marketplace (Future)

Under the EU Digital Markets Act (DMA), Apple is required to allow alternative app marketplaces
in the European Union. OWCE may be listed on an alternative marketplace in the future,
which would allow EU users to install it without AltStore.

---

## Apple Watch App

The Apple Watch app is bundled inside the iPhone app and installs automatically
when you install OWCE on your iPhone.

**To verify installation:**

1. Open the Watch app on your iPhone.
2. Scroll down to find OWCE in the list of available apps.
3. Tap **Install** if it has not installed automatically.
4. The OWCE watch face will appear in your watch's app list.

---

## Troubleshooting

| Issue | Solution |
|---|---|
| Android: "App not installed" | Enable "Install Unknown Apps" for your file manager in Android Settings |
| Android: App crashes on startup | Ensure you are running Android 8.0 (API 26) or higher |
| iOS: App expires after 7 days | Refresh via AltStore or use SideStore for wireless refresh |
| iOS: BLE not connecting | Ensure Bluetooth permission is granted in iOS Settings → Privacy → Bluetooth |
| iOS: App disconnects when locked | Ensure "Background App Refresh" is enabled for OWCE in iOS Settings |
| Watch: No data showing | Ensure the phone app is connected to the board and a ride is active |
| Wear OS: Watch app not found | Manually install via ADB as described above |
