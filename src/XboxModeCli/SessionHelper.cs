using Microsoft.Win32;
using System.Diagnostics;

namespace MrCapitalQ.XboxModeCli;

internal class SessionHelper
{
    public static bool IsLocked() => Process.GetProcessesByName("LogonUI").Length > 0;

    public static Task UnlockedAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!IsLocked())
        {
            Console.WriteLine("Session is not locked.");
            return Task.CompletedTask;
        }

        Console.WriteLine($"Session is locked. Waiting for session unlock by user with a timeout of {timeout}.");

        var tcs = new TaskCompletionSource();
        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason != SessionSwitchReason.SessionUnlock)
                return;

            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

            if (tcs.TrySetResult())
                Console.WriteLine("Session was unlocked. Detected via session unlock event.");
        }

        var timeoutCts = new CancellationTokenSource(timeout);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        cancellationToken.Register(() =>
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            tcs.SetCanceled(cancellationToken);
        });
        timeoutCts.Token.Register(() =>
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            tcs.SetException(new TimeoutException());
        });

        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

        // To deal with potentially flaky session unlock events or race conditions, also periodically poll for the
        // current lock status as a backup.
        _ = Task.Run(async () =>
        {
            while (!tcs.Task.IsCompleted && IsLocked())
            {
                await Task.Delay(1000);
            }

            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;

            if (tcs.TrySetResult())
                Console.WriteLine("Session was unlocked. Detected via polling current status.");
        }, timeoutCts.Token);

        return tcs.Task;
    }
}
