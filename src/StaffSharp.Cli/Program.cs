using System.CommandLine;
using StaffSharp.Cli;
using StaffSharp.Cli.Commands;

var rootCommand = new RootCommand("StaffSharp - Music notation format converter");

// Add convert command
rootCommand.AddCommand(ConvertCommand.Create());

return await rootCommand.InvokeAsync(args);
