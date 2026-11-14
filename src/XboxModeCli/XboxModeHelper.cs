using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace MrCapitalQ.XboxModeCli;

internal class XboxModeHelper
{
    // XboxPcApp is the process name of the original UWP-based XBOX app when XBOX mode was introduced.
    // XboxPcAppCE is the process name of the new webview-based XBOX app experience replacing the previous version.
    private static readonly HashSet<string> s_xboxProcessNames = ["XboxPcApp", "XboxPcAppCE"];

    public static async Task<bool> SetXboxModeAsync(bool isActive, CancellationToken cancellationToken = default)
    {
        if (await IsXboxModeAsync(cancellationToken) == isActive)
        {
            Console.WriteLine($"XBOX mode is already {(isActive ? "activated" : "deactivated")}. No action needed.");
            return true;
        }

        var maxAttempts = 5;
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"{(isActive ? "Activating" : "Deactivating")} XBOX mode (attempt {attempt + 1} of {maxAttempts}).");
            await ToggleXboxModeAsync(cancellationToken);

            await Task.Delay(250, cancellationToken);

            if (await IsXboxModeAsync(cancellationToken) == isActive)
            {
                Console.WriteLine($"XBOX mode {(isActive ? "activated" : "deactivated")}.");
                return true;
            }
            else if (++attempt < maxAttempts)
                Console.WriteLine($"Attempt {attempt} of {maxAttempts} failed. Trying again.");
            else
                break;
        }

        Console.WriteLine($"XBOX mode failed to {(isActive ? "activate" : "deactivate")} after {maxAttempts} attempts.");
        return false;
    }

    public static async Task<bool> IsXboxModeAsync(CancellationToken cancellationToken = default)
        => await GetXboxModeInfoAsync(cancellationToken) is not null;

    public static async Task ToggleXboxModeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Toggling Xbox mode (Win+F11)...");

        PInvoke.keybd_event((byte)VIRTUAL_KEY.VK_LWIN, 0, 0, 0);
        PInvoke.keybd_event((byte)VIRTUAL_KEY.VK_F11, 0, 0, 0);

        await Task.Delay(250, cancellationToken);

        PInvoke.keybd_event((byte)VIRTUAL_KEY.VK_F11, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, 0);
        PInvoke.keybd_event((byte)VIRTUAL_KEY.VK_LWIN, 0, KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP, 0);
    }

    public static async Task<XboxModeInfo?> GetXboxModeInfoAsync(CancellationToken cancellationToken = default)
    {
        var windows = await GetXboxAppWindowsAsync(cancellationToken);
        foreach (var windowHandle in windows)
        {
            // If the XBOX window is the same size as the monitor it's on, then assume it to be in XBOX mode. This does
            // not result in a false positive for the UWP-based app when it's in fullscreen mode (via regular F11)
            // because it doesn't seem to be a top-level window.
            if (PInvoke.GetWindowRect(new(windowHandle), out var windowRect))
            {
                var monitorHandle = PInvoke.MonitorFromWindow(new(windowHandle), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
                var monitorSize = GetMonitorSize(monitorHandle);

                if (monitorSize.HasValue && windowRect.Size == monitorSize)
                    return new XboxModeInfo(windowHandle, monitorHandle, monitorSize.Value);
            }
        }

        return null;
    }

    public static async Task<IEnumerable<nint>> GetXboxAppWindowsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<nint>();

        foreach (var processName in s_xboxProcessNames)
        {
            // The main UI window of the XBOX app can be either the main window of the process or a window presented by the
            // ApplicationFrameHost process but should always relate back to the XboxPcApp process.
            foreach (var process in Process.GetProcessesByName(processName))
            {
                // Get all top-level windows and check if any of them are related to the XboxPcApp process and are in
                // full-screen mode by comparing the size.
                var windows = await WindowHelper.GetTopLevelWindowIdsAsync(cancellationToken);
                foreach (var windowHandle in windows)
                {
                    if (!await WindowHelper.IsWindowRelatedToProcessAsync(windowHandle, process, cancellationToken)
                        || !PInvoke.IsWindowVisible(new(windowHandle)))
                        continue;

                    result.Add(windowHandle);
                }
            }
        }

        return result.AsReadOnly();
    }

    private unsafe static Size? GetMonitorSize(nint monitorHandle)
    {
        var monitorInfoEx = new MONITORINFOEXW();
        monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();
        if (PInvoke.GetMonitorInfo(new(monitorHandle), (MONITORINFO*)&monitorInfoEx) == 0)
            return null;

        return monitorInfoEx.monitorInfo.rcMonitor.Size;
    }

    public record XboxModeInfo(nint WindowHandle, nint MonitorHandle, Size MonitorSize);
}
