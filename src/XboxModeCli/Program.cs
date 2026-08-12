using MrCapitalQ.XboxModeCli;
using System.CommandLine;

var rootCommand = new RootCommand("Xbox Mode CLI is a command line interface used to interact with Windows 11's XBOX mode.");

var statusCommand = new Command("status", "Shows the current status of XBOX mode.");
statusCommand.SetAction(static async (parseResult, cancellationToken) =>
{
    var isActive = await XboxModeHelper.IsXboxModeAsync(cancellationToken);
    Console.WriteLine($"XBOX mode: {(isActive ? "active" : "inactive")}");
    return 0;
});

var activateCommand = new Command("activate", "Activates XBOX mode.")
{
    CommandOptions.MovePointerOption,
    CommandOptions.WaitForSessionUnlockOption,
    CommandOptions.CloseSettingsAppOption
};
activateCommand.SetAction(static (parseResult, cancellationToken) => CommandActions.SetXboxModeActionAsync(parseResult,
    true,
    cancellationToken));

var deactivateCommand = new Command("deactivate", "Deactivates XBOX mode.")
{
    CommandOptions.ExitXboxAppOption,
    CommandOptions.WaitForSessionUnlockOption,
    CommandOptions.CloseSettingsAppOption
};
deactivateCommand.SetAction(static (parseResult, cancellationToken) => CommandActions.SetXboxModeActionAsync(parseResult,
    false,
    cancellationToken));

rootCommand.Subcommands.Add(statusCommand);
rootCommand.Subcommands.Add(activateCommand);
rootCommand.Subcommands.Add(deactivateCommand);

await rootCommand.Parse(args).InvokeAsync();
