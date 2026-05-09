# Stardew Audio Device Refresh 0.2.0

Version 0.2.0 adds experimental automatic audio device refreshing when the default Windows output device changes.

## Added

- Added automatic audio device refresh when the default Windows output device changes.
- Automatic refresh is disabled by default and can be enabled through Generic Mod Config Menu or `config.json`.
- Added a configurable delay before automatic refresh.
- Added an option to play a test sound after automatic refresh.
- The manual `F8` refresh hotkey is still available as a fallback.
- Release archive now includes the required NAudio DLL dependencies.

## Tested

The automatic refresh feature has been tested in the following scenarios:

- Automatic refresh is disabled by default.
- Manual refresh with `F8` continues to work.
- The mod does not crash when Generic Mod Config Menu is not installed.
- Normal output device switching works consistently.
- Rapid device switching does not cause errors.
- Disconnecting the current default output device correctly switches audio to another available device, such as laptop speakers.
- All required DLL files are included in the release archive.

## How to use

1. Install the mod into your `Mods` folder.
2. Launch Stardew Valley through SMAPI.
3. Open Generic Mod Config Menu.
4. Enable automatic audio device refresh.
5. When you change the Windows output device, the mod should automatically reopen Stardew Valley's audio device.

The manual `F8` refresh hotkey is still available if automatic refresh does not work for any reason.

## Requirements

- Stardew Valley 1.6.x
- SMAPI 4.0.0 or newer
- Windows
- Generic Mod Config Menu is optional, but recommended.

## Important

Automatic refresh is Windows-only because it uses NAudio / Windows CoreAudio to detect default output device changes.

The release archive should include:

- `StardewAudioDeviceRefresh.dll`
- `NAudio.Core.dll`
- `NAudio.Wasapi.dll`
- `manifest.json`
