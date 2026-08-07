using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace MrCapitalQ.XboxModeCli;

internal static class WindowHelper
{
    private const string ApplicationFrameHostProcessName = "ApplicationFrameHost";

    public static async Task<IReadOnlyCollection<nint>> GetTopLevelWindowIdsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            List<nint> windowIds = [];
            BOOL EnumerationCallback(HWND hWnd, LPARAM lParam)
            {
                windowIds.Add(hWnd);
                return true;
            }

            PInvoke.EnumWindows(EnumerationCallback, 0);
            return windowIds.AsReadOnly();
        }, cancellationToken);
    }

    public static async Task<IReadOnlyCollection<nint>> GetChildWindowIdsAsync(nint parentWindowHandle, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            List<nint> windowIds = [];
            BOOL EnumerationCallback(HWND hWnd, LPARAM lParam)
            {
                windowIds.Add(hWnd);
                return true;
            }

            PInvoke.EnumChildWindows(new(parentWindowHandle), EnumerationCallback, 0);
            return windowIds.AsReadOnly();
        }, cancellationToken);
    }

    public static async Task<bool> IsWindowRelatedToProcessAsync(nint windowHandle, Process process)
    {
        _ = PInvoke.GetWindowThreadProcessId(new(windowHandle), out var windowProcessId);
        var windowProcess = Process.GetProcessById((int)windowProcessId);
        if (ApplicationFrameHostProcessName.Equals(windowProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            var childWindows = await GetChildWindowIdsAsync(windowHandle);
            foreach (var childWindow in childWindows)
            {
                _ = PInvoke.GetWindowThreadProcessId(new(childWindow), out var childWindowProcessId);
                var childWindowProcess = Process.GetProcessById((int)childWindowProcessId);
                if (!ApplicationFrameHostProcessName.Equals(childWindowProcess.ProcessName, StringComparison.OrdinalIgnoreCase)
                    && process.Id == childWindowProcessId)
                    return true;
            }
        }
        else if (process.Id == windowProcessId)
            return true;

        return false;
    }
}
