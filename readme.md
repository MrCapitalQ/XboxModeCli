# Xbox Mode CLI
## Introduction
Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode.

## Background
Windows 11 introduced a mode that is more friendly when using a gamepad, handhelds, or on a big screen known as XBOX
mode (formerly Full Screen Experience). Unfortunately, there's no documented way to automate switching in and out of
XBOX mode. This can be useful when game streaming to a mobile device or when using an existing desktop PC part-time on
a TV. This project aims provide a way to fill that gap without having to through the UI or requring user input.

## Commands
```
XboxModeCli [-?|-h|--help]
XboxModeCli [--version]
```
#### Options
- `-?|-h|--help`

  Show help and usage information.

- `--version`

  Show version information.

### Activate
```
XboxModeCli activate [-m|--movePointer] [-w|--waitForSessionUnlock] [-t|--waitTimeout <seconds>]
```
#### Options
- `-m|--movePointer`

  Move the mouse pointer offscreen if XBOX mode activation is successful. This is useful in avoiding having the mouse
  pointer being stuck in the middle of the screen when primarily using gamepad inputs.

- `-w|--waitForSessionUnlock`

  Waits for the current user session to be unlocked first otherwise the command will fail if the session is currently
  locked.

- `-t|--waitTimeout <seconds>`

  Sets how many seconds to wait for the session to be unlocked. Default is 300 when not set.

- `--closeSettingsApp`

  Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can
  sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in
  this CLI and is likely to be removed in the future.

### Activate
```
XboxModeCli activate [-m|--movePointer] [-w|--waitForSessionUnlock] [-t|--waitTimeout <seconds>]
```
#### Options
- `-e|--exit`

  Exit and close the XBOX app if XBOX mode deactivation is successful.

- `-w|--waitForSessionUnlock`

  Waits for the current user session to be unlocked first otherwise the command will fail if the session is currently
  locked.

- `-t|--waitTimeout <seconds>`

  Sets how many seconds to wait for the session to be unlocked. Default is 300 when not set.

- `--closeSettingsApp`

  Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can
  sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in
  this CLI and is likely to be removed in the future.

## Examples

### Moonlight & Sunshine Game Streaming
Set up Sunshine (or ideally one of its forks that automates virtual displays) with a dedicated XBOX Mode application so
it can start directly in XBOX mode and revert to desktop mode upon disconnect. Using a telescoping controller and my
mobile phone, I can get an excellent handheld gaming experience.

1. Add a new application in Sunshine and call it "XBOX Mode".
2. Add prep commands for this new application.

   | Action | Command |
   |--------|---------|
   | Do     | `cmd /c "start """" ""<path_to>\XboxModeCli.exe"" activate -m -w` |
   | Undo   | `cmd /c "start """" ""<path_to>\XboxModeCli.exe"" deactivate -e -w` |

3. Open Moonlight (or a compatible client), connect to the PC, and select "XBOX Mode". The stream should start and XBOX
mode should automatically be activated. After disconnecting, the PC should be back to desktop mode.

In this example, we're passing the option to wait for the session to be unlocked first, giving us a chance to unlock
the PC after starting the stream instead of immediately failing. But because Sunshine waits for the commands to
complete before connecting, we're runing the command without waiting for it to finish with `cmd /c "start """" ...`
instead of directly.

## Building
### Prerequisites
- Visual Studio 2026
- .NET 10 SDK

### Build and Run
1. Open the [`XboxModeCli.slnx`](/XboxModeCli.slnx) solution.
2. If not already set, set the `XboxModeCli` project as the startup project.
3. Choose a launch profile for a specific command.
4. Start debugging.
