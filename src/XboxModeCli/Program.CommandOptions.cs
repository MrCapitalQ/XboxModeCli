using System.CommandLine;
using System.CommandLine.Parsing;

namespace MrCapitalQ.XboxModeCli;

internal static class CommandOptions
{
    public const long DefaultWaitTimeoutSeconds = 300;
    public const long DefaultMouseMoveDelaySeconds = 0;
    public const long DefaultAppCloseDelaySeconds = 3;

    public static Option<double?> WaitForSessionUnlockOption { get; } = new("--waitForSessionUnlock", "-w")
    {
        Description = "Wait for the current user session to be unlocked first before executing the command. If the <timeoutInSeconds> argument is omitted, a default timeout of 300 seconds (5 minutes) is used.",
        HelpName = "timeoutInSeconds",
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
            ? DefaultWaitTimeoutSeconds
            : null,
        Validators =
        {
            result =>
            {
                if (result.GetValueOrDefault<double?>() < 0)
                    result.AddError("Wait for session unlock timeout must be non-negative");
            }
        }
    };

    public static Option<double?> MovePointerOption { get; } = new("--movePointer", "-m")
    {
        Description = "Move the mouse pointer offscreen if XBOX mode activation is successful. If the <delayInSeconds> argument is omitted, the mouse pointer will be moved immediately after XBOX mode is activated.",
        HelpName = "delayInSeconds",
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
            ? DefaultMouseMoveDelaySeconds
            : null,
        Validators =
        {
            result =>
            {
                if (result.GetValueOrDefault<double?>() < 0)
                    result.AddError("Delay before moving the mouse pointer must be non-negative");
            }
        }
    };

    public static Option<double?> ExitXboxAppOption { get; } = new("--exit", "-e")
    {
        Description = "Exit and close the XBOX app if XBOX mode deactivation is successful. If the <delayInSeconds> argument is omitted, a default delay of 3 seconds will be used before closing the XBOX app after XBOX mode is deactivated.",
        HelpName = "delayInSeconds",
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
            ? DefaultAppCloseDelaySeconds
            : null,
        Validators =
        {
            result =>
            {
                if (result.GetValueOrDefault<double?>() < 0)
                    result.AddError("Delay before closing the XBOX app must be non-negative");
            }
        }
    };

    public static Option<double?> CloseSettingsAppOption { get; } = new("--closeSettingsApp")
    {
        Description = "Close the Settings app if it's open. This is a hacky workaround for a bug where switching in and out of XBOX mode can sometimes cause the Settings app to open. This is still being evaluated on whether this should be the included in this CLI and is likely to be removed in the future. If the <delayInSeconds> argument is omitted, a default delay of 3 seconds will be used before closing the Settings app.",
        HelpName = "delayInSeconds",
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = result => result.Parent is OptionResult { IdentifierTokenCount: > 0 }
            ? DefaultAppCloseDelaySeconds
            : null,
        Validators =
        {
            result =>
            {
                if (result.GetValueOrDefault<double?>() < 0)
                    result.AddError("Delay before closing the Settings app must be non-negative");
            }
        }
    };
}
