# Xbox Mode CLI
## Introduction
Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode.

## Background
Windows 11 introduced a mode that is more friendly when using a gamepad, handhelds, or on a big screen known as XBOX
mode (formerly Full Screen Experience). Unfortunately, there's no documented way to automate switching in and out of
XBOX mode. This can be useful when game streaming to a mobile device or when using an existing desktop PC on
a TV part-time. This project aims provide a way to non-interactively control XBOX mode.

## Features
- Check whether the XBOX mode is currently active.
- Switch in and out of XBOX mode deterministically with retries.
- Pre-compiled releases are published with .NET Native AOT for performance benefits.

> **Note:** This does not include a way to enable the _availability_ of XBOX mode on a system. As it is a gradual
rollout, everyone should eventually get it on supported versions of Windows 11. There are also tools available that
can be used to forcibly enable it.

## Usage
```
XboxModeCli [-?|-h|--help]
XboxModeCli [--version]
```
#### Options
- `-?|-h|--help`

  Show help and usage information.

- `--version`

  Show version information.

### Status Command
Shows the current status of XBOX mode.
```
XboxModeCli status
```

### Activate Command
Activates XBOX mode.

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

### Deactivate Command
Deactivates XBOX mode.

```
XboxModeCli deactivate [-m|--movePointer] [-w|--waitForSessionUnlock] [-t|--waitTimeout <seconds>]
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
Get an XBOX handheld-like experience by setting up a game streaming service that loads directly into XBOX mode. This
can achieved with a mobile device, a telescoping controller, and Sunshine configured with prep commands to activate
XBOX mode automatically while streaming.

1. Download a copy of the XboxModeCli executable from the releases and save it somewhere.
2. In Sunshine (or ideally, one of its forks with virtual displays), create a new application and call it "XBOX Mode".
3. Add a new set of prep commands for this application. Update `<path_to>` to point it to the location of the
executable saved in step 1.

   | Action | Command |
   |--------|---------|
   | Do     | `cmd /c "start """" ""<path_to>\XboxModeCli.exe"" activate -m -w"` |
   | Undo   | `cmd /c "start """" ""<path_to>\XboxModeCli.exe"" deactivate -e -w"` |

3. Open Moonlight (or a compatible client), connect to the PC, and select the new "XBOX Mode" application. The stream
should start and XBOX mode should automatically be activated. After disconnecting and quitting, the PC should
automatically revert back to desktop mode.

> **Note:** Sunshine waits for these prep commands to complete successfully before continuing. This causes it to appear
to hang when the `-w` option is used since it's waiting for the lock screen to be dismissed. To get around this, the
prep commands execute with `cmd /c "start """"...` in order to not wait for command completion.


## Building
### Prerequisites
- Visual Studio 2026
- .NET 10 SDK

### Build and Run
1. Open the [`XboxModeCli.slnx`](/XboxModeCli.slnx) solution.
2. If not already set, set the `XboxModeCli` project as the startup project.
3. Choose a launch profile for a specific command.
4. Start debugging.
