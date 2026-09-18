# Xbox Mode CLI

## Introduction

Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode. This tool can be useful for
those using a desktop Windows PC to [replicate a gaming console or a streaming-based gaming handheld experience](#examples).
Written by a human so any bugs you find are organic, gluten-free, and with no artificial flavors added.

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
> rollout, everyone should eventually get it on supported versions of Windows 11. There are also tools available that
> can be used to forcibly enable it.

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
XboxModeCli activate
    [-m|--movePointer <delayInSeconds>]
    [-w|--waitForSessionUnlock <timeoutInSeconds>]
```

#### Options

- `-m|--movePointer <delayInSeconds>`

  Move the mouse pointer offscreen if XBOX mode activation is successful. If the `<delayInSeconds>` argument is
  omitted, the mouse pointer will be moved immediately after XBOX mode is activated.

- `-w|--waitForSessionUnlock <timeoutInSeconds>`

  Wait for the current user session to be unlocked first before executing the command. If the `<timeoutInSeconds>`
  argument is omitted, a default timeout of 300 seconds (5 minutes) is used.

- `--closeSettingsApp <delayInSeconds>`

  Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can
  sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in
  this CLI and is likely to be removed in the future. If the `<delayInSeconds>` argument is omitted, a default delay of
  3 seconds will be used before closing the Settings app.

### Deactivate Command

Deactivates XBOX mode.

```
XboxModeCli deactivate
    [-e|--exit <delayInSeconds>]
    [-w|--waitForSessionUnlock <timeoutInSeconds>]
```

#### Options

- `-e|--exit <delayInSeconds>`

  Exit and close the XBOX app if XBOX mode deactivation is successful. If the `<delayInSeconds>` argument is omitted,
  a default delay of 3 seconds will be used before closing the XBOX app after XBOX mode is deactivated.

- `-w|--waitForSessionUnlock <timeoutInSeconds>`

  Wait for the current user session to be unlocked first before executing the command. If the `<timeoutInSeconds>`
  argument is omitted, a default timeout of 300 seconds (5 minutes) is used.

- `--closeSettingsApp <delayInSeconds>`

  Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can
  sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in
  this CLI and is likely to be removed in the future. If the `<delayInSeconds>` argument is omitted, a default delay of
  3 seconds will be used before closing the Settings app.

## Examples

### Moonlight & Sunshine Game Streaming

Get an XBOX handheld-like experience by setting up a game streaming service that loads directly into XBOX mode. This
can achieved with a mobile device, a telescoping controller, and Sunshine configured with prep commands to activate
XBOX mode automatically while streaming.

1.  Download the latest release from the releases page. Be careful to choose the correct download that matches your CPU
    architecture (ARM vs x64). Unzip the download, and save the `XboxModeCli.exe` executable somewhere.
2.  In Sunshine (or ideally, one of its forks with virtual displays), create a new application and call it "XBOX Mode".
3.  Add a new set of prep commands for this application. Update `<path_to>` to point it to the location of the
    executable saved in step 1.

    | Action | Command                                                             |
    | ------ | ------------------------------------------------------------------- |
    | Do     | `cmd /c start /min "" "<path_to>\XboxModeCli.exe" activate -m -w`   |
    | Undo   | `cmd /c start /min "" "<path_to>\XboxModeCli.exe" deactivate -e -w` |

    > **Tip:** If Windows is executing these prep commands in Windows Terminal where it can cause some undesirable UI
    > behaviors, you can add `conhost` to the beginning of the commands to force it to be executed in the old command
    > prompt window. The command should look like this: `conhost cmd c/ start /min ...`

    > **Note:** Sunshine waits for these prep commands to complete successfully before continuing. This causes it to appear
    > to hang when the `-w` option is used since it's waiting for the lock screen to be dismissed. To get around this, the
    > prep commands execute with `cmd /c start /min "" ...` in order to execute the tool in separate minimized window.

4.  Open Moonlight (or a compatible client), connect to the PC, and select the new "XBOX Mode" application. The stream
    should start and XBOX mode should automatically be activated. After disconnecting and quitting, the PC should
    automatically revert back to desktop mode.

### XBOX Mode at Startup

For Windows PCs being used as a console-like gaming system connected to a TV, automatically starting XBOX mode on boot
or login would be nice. However, this functionality is normally not available on the desktop version of XBOX mode.
There are methods to enable this option but it's possible to achieve the same thing with the built-in Task Scheduler.

1.  Download the latest release from the releases page. Be careful to choose the correct download that matches your CPU
    architecture (ARM vs x64). Unzip the download, and save the `XboxModeCli.exe` executable somewhere.
2.  Open Window's Task Scheduler and create a new task.
3.  In the "Triggers" tab, add one or more trigger for when you want to activate XBOX mode. For example, I want my
    dedicated couch gaming system to always be in XBOX mode when I start using it so I set up a trigger for "At log on"
    and "On workstation unlock".
4.  In the "Actions" tab, add a new "Start a program" action. For the "Program/script" field, browse to or set the path
    to where the `XboxModeCli.exe` executable was saved in step 1. Then in the "Add arguments (optional)" field, set it
    to `activate -m -w`.
5.  Test the scheduled task to ensure it's working as expected. In my case, I restart the system and after signing in,
    XBOX automatic activates after a few seconds.

## Building

### Prerequisites

- Visual Studio 2026
- .NET 10 SDK

### Build and Run

1. Open the [`XboxModeCli.slnx`](/XboxModeCli.slnx) solution.
2. If not already set, set the `XboxModeCli` project as the startup project.
3. Choose a launch profile for a specific command.
4. Start debugging.
