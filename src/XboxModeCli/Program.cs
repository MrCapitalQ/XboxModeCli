using MrCapitalQ.XboxModeCli;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Windows.Win32;

const long DefaultWaitTimeoutSeconds = 300;
const long DefaultMouseMoveDelaySeconds = 0;
const long DefaultAppCloseDelaySeconds = 1;


var rootCommand = new RootCommand("Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode.");

var waitForSessionUnlockOption = new Option<double?>("--waitForSessionUnlock", "-w")
{
    Description = "Wait for the current user session to be unlocked first before executing the command. If the <timeoutInSeconds> argument is omitted, a default timeout of 300 seconds (5 minutes) is used.",
    HelpName = "timeoutInSeconds",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
        ? DefaultWaitTimeoutSeconds
        : null
};
waitForSessionUnlockOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<double?>() < 0)
    {
        result.AddError("Wait for session unlock timeout must be non-negative");
    }
});

var movePointerOption = new Option<double?>("--movePointer", "-m")
{
    Description = "Move the mouse pointer offscreen if XBOX mode activation is successful. If the <delayInSeconds> argument is omitted, the mouse pointer will be moved immediately after XBOX mode is activated.",
    HelpName = "delayInSeconds",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
        ? DefaultMouseMoveDelaySeconds
        : null
};
movePointerOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<double?>() < 0)
    {
        result.AddError("Delay before moving the mouse pointer must be non-negative");
    }
});

var exitXboxAppOption = new Option<double?>("--exit", "-e")
{
    Description = "Exit and close the XBOX app if XBOX mode deactivation is successful. If the <delayInSeconds> argument is omitted, a default delay of 1 second will be used before closing the XBOX app after XBOX mode is deactivated.",
    HelpName = "delayInSeconds",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
        ? DefaultAppCloseDelaySeconds
        : null
};
exitXboxAppOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<double?>() < 0)
    {
        result.AddError("Delay before closing the XBOX app must be non-negative");
    }
});

var closeSettingsAppOption = new Option<double?>("--closeSettingsApp")
{
    Description = "Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in this CLI and is likely to be removed in the future. If the <delayInSeconds> argument is omitted, a default delay of 1 second will be used before closing the Settings app.",
    HelpName = "delayInSeconds",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
        ? DefaultAppCloseDelaySeconds
        : null
};
closeSettingsAppOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<double?>() < 0)
    {
        result.AddError("Delay before closing the Settings app must be non-negative");
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
    closeSettingsAppOption
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
    closeSettingsAppOption
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

    if (parseResult.GetValue(waitForSessionUnlockOption) is { } timeoutSeconds)
    {
        Console.WriteLine("Checking if session is locked.");
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

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

    var postCommandTasks = new List<Task>();

    if (isActive && parseResult.GetValue(movePointerOption) is { } movePointerDelaySeconds)
    {
        var movePointerDelay = TimeSpan.FromSeconds(movePointerDelaySeconds);
        postCommandTasks.Add(ExecuteActionWithDelayAsync(() =>
        {
            Console.WriteLine("Moving mouse pointer offscreen.");
            if (XboxModeHelper.GetXboxModeInfoAsync(cancellationToken).Result is { } xboxModeInfo)
            {
                PInvoke.SetCursorPos(xboxModeInfo.MonitorSize.Width, xboxModeInfo.MonitorSize.Height);
            }
        }, movePointerDelay, $"Waiting {movePointerDelay} before moving mouse pointer offscreen."));
    }

    if (!isActive && parseResult.GetValue(exitXboxAppOption) is { } exitXboxAppDelayMs)
    {
        var exitXboxAppDelay = TimeSpan.FromSeconds(exitXboxAppDelayMs);
        postCommandTasks.Add(ExecuteActionWithDelayAsync(() =>
        {
            Console.WriteLine("Closing the XBOX app...");
            foreach (var process in Process.GetProcessesByName("XboxPcApp"))
            {
                process.CloseMainWindow();
            }
        }, exitXboxAppDelay, $"Waiting {exitXboxAppDelay} before closing the XBOX app."));
    }

    if (parseResult.GetValue(closeSettingsAppOption) is { } exitSettingsAppDelaySeconds)
    {
        var exitSettingsAppDelay = TimeSpan.FromSeconds(exitSettingsAppDelaySeconds);
        postCommandTasks.Add(ExecuteActionWithDelayAsync(() =>
        {
            Console.WriteLine("Closing the Settings app...");
            foreach (var process in Process.GetProcessesByName("SystemSettings"))
            {
                process.Kill();
            }
        }, exitSettingsAppDelay, $"Waiting {exitSettingsAppDelay} before closing the Settings app."));
    }

    await Task.WhenAll(postCommandTasks);

    return 0;
}

async Task ExecuteActionWithDelayAsync(Action action, TimeSpan delay, string delayMessage)
{
    if (delay > TimeSpan.Zero)
    {
        Console.WriteLine(delayMessage);
        await Task.Delay(delay);
    }

    action();
}
