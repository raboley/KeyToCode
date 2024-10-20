# KeyToCode

![KeyToCode](docs/assets/demo_loop.gif)

## Overview

KeyToCode is a tool designed to help you record and playback keyboard scripts. To get started, follow these steps:

1. Download the `KeyToCode.zip` from the [Releases](https://github.com/raboley/KeyToCode/releases) tab for the latest release.
2. Extract the contents of the zip file.
3. Run `KeyToCodeRecorder.exe`.

## Quickstart

1. **Select Program**: Use the dropdown box to select the program you want to record hotkeys for.
2. **Record Keystrokes**: Type out the keystrokes you want to record.
3. **Stop Recording**: Use the stop recording hotkey (F5 by default) to stop recording.

After recording, the tool will:
- Copy the text to your clipboard (this can be toggled in the settings).
- Display the C# code needed to run the hotkeys you just recorded.

To test the recorded script:
- Click the play button to play it back once.
- Click the infinite loop button to play it back on a loop.

## Saving and Opening Scripts

- **Save a Script**: Use the `File -> Save` option to save your script.
- **Open a Script**: Use the `File -> Open` option to open a saved script.

## Customizing Settings

To customize your settings, click the gear icon and edit the `config.json` file. The settings you can change include:

- **Keybindings**:
  - Starting and stopping recording (defaulted to F4)
  - Starting and stopping playing (defaulted to F5)

- **Boolean Settings**:
  - Save the last selected process on startup
  - Copy output to clipboard after recording is done
  - Automatically open the last saved file on startup