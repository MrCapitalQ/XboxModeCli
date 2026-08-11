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
            if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Console.WriteLine("Session was unlocked.");

                SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                tcs.SetResult();
            }
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

        return tcs.Task;
    }
}
