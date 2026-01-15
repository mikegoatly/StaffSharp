using System.CommandLine;

using StaffSharp.Cli.Commands;

var rootCommand = new RootCommand("StaffSharp - Music notation format converter");

// Add convert command
rootCommand.Subcommands.Add(ConvertCommand.Create());

// TODO - make ML commands a plugin or separate CLI app, or at least under a separate subcommand called `ml`
// Add prepare-dataset command
rootCommand.Subcommands.Add(PrepareDatasetCommand.Create());

// Add verify-alignment command
rootCommand.Subcommands.Add(VerifyAlignmentCommand.Create());

var parsedResult = rootCommand.Parse(args);

if (parsedResult.Errors.Count > 0)
{
    foreach (var error in parsedResult.Errors)
    {
        await Console.Error.WriteLineAsync($"Error: {error.Message}");
    }

    return 1;
}

return await parsedResult.InvokeAsync();
