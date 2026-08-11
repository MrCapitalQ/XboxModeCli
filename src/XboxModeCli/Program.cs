using MrCapitalQ.XboxModeCli;
using System.CommandLine;
using System.Diagnostics;
using Windows.Win32;

const long DefaultWaitTimeoutMs = 300000;
const long DefaultPostCommandDelayMs = 1000;


var rootCommand = new RootCommand("Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode.");

var waitForSessionUnlockOption = new Option<bool>("--waitForSessionUnlock", "-w")
{
    Description = "Waits for the current user session to be unlocked first otherwise the command will fail if the session is currently locked."
};

var waitForSessionUnlockTimeoutOption = new Option<long>("--waitTimeout", "-t")
{
    Description = $"Sets how many milliseconds to wait for the session to be unlocked. Default is {DefaultWaitTimeoutMs} (5 minutes) when not set.",
    HelpName = "milliseconds",
    DefaultValueFactory = _ => DefaultWaitTimeoutMs
};
waitForSessionUnlockTimeoutOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<long>() < 0)
    {
        result.AddError("Wait for session unlock timeout must be non-negative");
    }
});

var movePointerOption = new Option<bool>("--movePointer", "-m")
{
    Description = "Move the mouse pointer offscreen if XBOX mode activation is successful. This is useful in avoiding having the mouse pointer being stuck in the middle of the screen when primarily using gamepad inputs."
};

var exitXboxAppOption = new Option<bool>("--exit", "-e")
{
    Description = "Exit and close the XBOX app if XBOX mode deactivation is successful."
};

var closeSettingsAppOption = new Option<bool>("--closeSettingsApp")
{
    Description = "Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in this CLI and is likely to be removed in the future."
};

var postCommandDelayOption = new Option<long>("--postCommandActionDelay")
{
    Description = $"Sets how many milliseconds to wait before executing post-command actions like moving the mouse pointer or closing the XBOX app. Default is {DefaultPostCommandDelayMs} (1 second) when not set.",
    HelpName = "milliseconds",
    DefaultValueFactory = _ => DefaultPostCommandDelayMs
};
postCommandDelayOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<long>() < 0)
    {
        result.AddError("Post-command action delay must be non-negative");
    }
});

var statusCommand = new Command("status", "Shows the current status of XBOX mode.");
statusCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var isActive = await XboxModeHelper.IsXboxModeAsync(cancellationToken);
    Console.WriteLine($"XBOX mode: {(isActive ? "active" : "inactive")}");
    return 0;
});

var activateCommand = new Command("activate", "Activates XBOX mode.")
{
    movePointerOption,
    waitForSessionUnlockOption,
    waitForSessionUnlockTimeoutOption,
    closeSettingsAppOption,
    postCommandDelayOption
};
activateCommand.SetAction(async (parseResult, cancellationToken) =>
{
    Console.WriteLine("Activating XBOX mode...");
    return await SetXboxModeAction(parseResult, true, cancellationToken);
});

var deactivateCommand = new Command("deactivate", "Deactivates XBOX mode.")
{
    exitXboxAppOption,
    waitForSessionUnlockOption,
    waitForSessionUnlockTimeoutOption,
    closeSettingsAppOption,
    postCommandDelayOption
};
deactivateCommand.SetAction(async (parseResult, cancellationToken) =>
{
    Console.WriteLine("Deactivating XBOX mode...");
    return await SetXboxModeAction(parseResult, false, cancellationToken);
});

rootCommand.Subcommands.Add(statusCommand);
rootCommand.Subcommands.Add(activateCommand);
rootCommand.Subcommands.Add(deactivateCommand);

await rootCommand.Parse(args).InvokeAsync();

async Task<int> SetXboxModeAction(ParseResult parseResult, bool isActive, CancellationToken cancellationToken)
{
    if (isActive == await XboxModeHelper.IsXboxModeAsync(cancellationToken))
    {
        Console.WriteLine($"XBOX mode is already {(isActive ? "activated" : "deactivated")}. No action needed.");
        return 0;
    }

    if (parseResult.GetValue(waitForSessionUnlockOption))
    {
        Console.WriteLine("Checking if session is locked.");
        var timeout = TimeSpan.FromSeconds(parseResult.GetRequiredValue(waitForSessionUnlockTimeoutOption));

        try
        {
            await SessionHelper.UnlockedAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Wait for session to unlock timed out after {timeout}. Giving up.");
            return 1;
        }
    }

    var isSuccessful = await XboxModeHelper.SetXboxModeAsync(isActive, cancellationToken);

    if (!isSuccessful)
        return 1;

    var shouldMoveMouse = isActive && parseResult.GetValue(movePointerOption);
    var shouldCloseXboxApp = !isActive && parseResult.GetValue(exitXboxAppOption);
    var shouldCloseSettingsApp = parseResult.GetValue(closeSettingsAppOption);

    if (shouldMoveMouse || shouldCloseXboxApp || shouldCloseSettingsApp)
    {
        var postCommandDelay = TimeSpan.FromMilliseconds(parseResult.GetRequiredValue(postCommandDelayOption));
        Console.WriteLine($"Waiting {postCommandDelay.TotalSeconds} seconds before post command actions.");
        await Task.Delay(postCommandDelay, cancellationToken);
    }

    if (shouldMoveMouse && await XboxModeHelper.GetXboxModeInfoAsync(cancellationToken) is { } xboxModeInfo)
    {
        Console.WriteLine("Moving mouse pointer offscreen.");
        PInvoke.SetCursorPos(xboxModeInfo.MonitorSize.Width, xboxModeInfo.MonitorSize.Height);
    }

    if (shouldCloseXboxApp)
    {
        Console.WriteLine("Closing the XBOX app...");
        foreach (var process in Process.GetProcessesByName("XboxPcApp"))
        {
            process.CloseMainWindow();
        }
    }

    if (shouldCloseSettingsApp)
    {
        Console.WriteLine("Closing the Settings app...");
        foreach (var process in Process.GetProcessesByName("SystemSettings"))
        {
            process.Kill();
        }
    }

    return 0;
}
