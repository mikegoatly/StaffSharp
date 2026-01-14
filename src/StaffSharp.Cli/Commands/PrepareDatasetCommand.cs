namespace StaffSharp.Cli.Commands;

using System.CommandLine;
using System.Text.Json;

using Spectre.Console;

using StaffSharp.Audio;
using StaffSharp.Audio.IO;
using StaffSharp.MachineLearning.ML.Features;

/// <summary>
/// Implements the 'prepare-dataset' command for ML training data preparation.
/// </summary>
internal static class PrepareDatasetCommand
{
    public static Command Create()
    {
        var command = new Command(
            "prepare-dataset",
            "Prepare MAESTRO dataset for machine learning training");

        // Arguments
        var maestroDirArg = new Argument<string>("maestro-dir")
        {
            Description = "Path to MAESTRO v3.0.0 directory"
        };

        var outputDirArg = new Argument<string>("output-dir")
        {
            Description = "Output directory for processed data"
        };

        command.Arguments.Add(maestroDirArg);
        command.Arguments.Add(outputDirArg);

        // Options
        var maxFilesOption = new Option<int?>("--max-files")
        {
            Description = "Maximum files per split (for testing). Omit to process all files."
        };

        var parallelOption = new Option<int>("--parallel")
        {
            Description = "Number of parallel processing tasks. Default is half the number of processor cores - for this machine: " +
                          $"{Math.Max(1, Environment.ProcessorCount / 2)}"
        };

        command.Options.Add(maxFilesOption);
        command.Options.Add(parallelOption);

        // Handler
        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var maestroDir = parseResult.GetRequiredValue(maestroDirArg);
            var outputDir = parseResult.GetRequiredValue(outputDirArg);
            var maxFiles = parseResult.GetValue(maxFilesOption);
            var parallel = parseResult.GetValue(parallelOption);
            
            // Apply default if not specified
            if (parallel == 0)
            {
                parallel = Math.Max(1, Environment.ProcessorCount / 2);
            }

            return await ExecuteAsync(maestroDir, outputDir, maxFiles, parallel);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string maestroDir,
        string outputDir,
        int? maxFiles,
        int parallel)
    {
        try
        {
            var processor = new DatasetProcessor(maestroDir, outputDir, maxFiles, parallel);
            await processor.ProcessAsync();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (ex.InnerException != null)
            {
                AnsiConsole.MarkupLine($"[red]  Inner:[/] {ex.InnerException.Message}");
            }
            return 1;
        }
    }
}
