using System.CommandLine;
using System.Diagnostics;
using Windows.Win32;

namespace MrCapitalQ.XboxModeCli;

internal static class CommandActions
{
    public static async Task<int> SetXboxModeActionAsync(ParseResult parseResult,
        bool isActive,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"{(isActive ? "Activating" : "Deactivating")} XBOX mode...");

        if (isActive == await XboxModeHelper.IsXboxModeAsync(cancellationToken))
        {
            Console.WriteLine($"XBOX mode is already {(isActive ? "activated" : "deactivated")}. No action needed.");
            return 0;
        }

        if (parseResult.GetValue(CommandOptions.WaitForSessionUnlockOption) is { } timeoutSeconds)
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

        if (isActive && parseResult.GetValue(CommandOptions.MovePointerOption) is { } movePointerDelaySeconds)
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

        if (!isActive && parseResult.GetValue(CommandOptions.ExitXboxAppOption) is { } exitXboxAppDelayMs)
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

        if (parseResult.GetValue(CommandOptions.CloseSettingsAppOption) is { } exitSettingsAppDelaySeconds)
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

    private static async Task ExecuteActionWithDelayAsync(Action action, TimeSpan delay, string delayMessage)
    {
        if (delay > TimeSpan.Zero)
        {
            Console.WriteLine(delayMessage);
            await Task.Delay(delay);
        }

        action();
    }
}
