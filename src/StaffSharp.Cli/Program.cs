using System.CommandLine;

using StaffSharp.Cli.Commands;

var rootCommand = new RootCommand("StaffSharp - Music notation format converter");

// Add convert command
rootCommand.Subcommands.Add(ConvertCommand.Create());

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
