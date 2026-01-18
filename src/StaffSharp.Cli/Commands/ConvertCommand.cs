namespace StaffSharp.Cli.Commands;

using System.CommandLine;
using System.Security.Cryptography.X509Certificates;

using Spectre.Console;

using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.Validation;

/// <summary>
/// Implements the 'convert' command for format conversion.
/// </summary>
internal static class ConvertCommand
{
    private static Dictionary<string, Option> dynamicOptions = [];

    // Arguments
    private static readonly Argument<string> inputArg = new("input")
    {
        Description = "Input file path (use '-' for stdin)"
    };

    private static readonly Argument<string> outputArg = new("output")
    {
        Description = "Output file path (use '-' for stdout)"
    };

    // Options for format override
    private static readonly Option<string?> fromOption = new("--from") { Description = "Override input format detection (e.g., 'abc')" };
    private static readonly Option<string?> toOption = new("--to") { Description = "Override output format detection (e.g., 'midi')" };

    // Verbosity options
    private static readonly Option<bool> quietOption = new("--quiet") { Description = "Suppress all output except errors" };
    private static readonly Option<bool> verboseOption = new("--verbose") { Description = "Show detailed conversion information" };

    // ML note detection options
    private static readonly Option<bool> useMlOption = new("--use-ml") { Description = "Use machine learning for audio note detection (instead of algorithmic)" };
    private static readonly Option<string?> modelPathOption = new("--model-path") { Description = "Path to ONNX model file (optional, uses default if not specified)" };
    private static readonly Option<float?> onsetThresholdOption = new("--onset-threshold") { Description = "ML onset detection threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> frameThresholdOption = new("--frame-threshold") { Description = "ML frame activation threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> offsetThresholdOption = new("--offset-threshold") { Description = "ML offset detection threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> minNoteLengthOption = new("--min-note-length") { Description = "Minimum note length in seconds (default: 0.05)" };

    public static Command Create()
    {
        var command = new Command("convert", "Convert between music notation formats");

        command.Arguments.Add(inputArg);
        command.Arguments.Add(outputArg);

        command.Options.Add(fromOption);
        command.Options.Add(toOption);
        command.Options.Add(quietOption);
        command.Options.Add(verboseOption);
        command.Options.Add(useMlOption);
        command.Options.Add(modelPathOption);
        command.Options.Add(onsetThresholdOption);
        command.Options.Add(frameThresholdOption);
        command.Options.Add(offsetThresholdOption);
        command.Options.Add(minNoteLengthOption);

        // Add format-specific options dynamically
        foreach (var exporter in FormatRegistry.Exporters)
        {
            foreach (var option in exporter.AvailableOptions)
            {
                var cmdOption = new Option<string?>($"--{option.Name}")
                {
                    Description = option.Description,
                    HelpName = option.DefaultValue
                };

                dynamicOptions.Add(option.Name, cmdOption);
                command.Options.Add(cmdOption);
            }
        }

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var input = parseResult.GetRequiredValue(inputArg);
            var output = parseResult.GetRequiredValue(outputArg);
            var from = parseResult.GetValue(fromOption);
            var to = parseResult.GetValue(toOption);
            var quiet = parseResult.GetValue(quietOption);
            var verbose = parseResult.GetValue(verboseOption);

            return await ExecuteAsync(
                input,
                output,
                from,
                to,
                quiet,
                verbose,
                parseResult,
                cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string input,
        string output,
        string? fromFormat,
        string? toFormat,
        bool quiet,
        bool verbose,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine input format
            var importer = GetImporter(input, fromFormat);
            if (importer == null)
            {
                await Console.Error.WriteLineAsync($"Error: Unable to determine input format for '{input}'");
                return 1; // User error
            }

            // Determine output format
            var exporter = GetExporter(output, toFormat);
            if (exporter == null)
            {
                await Console.Error.WriteLineAsync($"Error: Unable to determine output format for '{output}'");
                return 1; // User error
            }

            if (verbose)
            {
                Console.WriteLine($"Converting {importer.FormatName} → {exporter.FormatName}...");
            }

            // Configure audio importer with ML options if applicable
            if (importer is AudioScoreImporter audioImporter)
            {
                var useMl = parseResult.GetValue(useMlOption);
                if (useMl)
                {
                    var modelPath = parseResult.GetValue(modelPathOption);
                    var onsetThreshold = parseResult.GetValue(onsetThresholdOption);
                    var frameThreshold = parseResult.GetValue(frameThresholdOption);
                    var offsetThreshold = parseResult.GetValue(offsetThresholdOption);
                    var minNoteLength = parseResult.GetValue(minNoteLengthOption);

                    audioImporter.ConfigureMLOptions(modelPath, onsetThreshold, frameThreshold, offsetThreshold, minNoteLength);
                }
            }

            // Import
            var progress = verbose ? new Progress<ImportProgress>(m => AnsiConsole.MarkupLine($"[blue]{m.StepName}[/] {m.Message}")) : null;
            NotationScore score;
            if (input == "-")
            {
                // Read from stdin
                using var stdin = Console.OpenStandardInput();
                score = await importer.ImportAsync(stdin, progress, cancellationToken);
            }
            else
            {
                // Read from file
                if (!File.Exists(input))
                {
                    await Console.Error.WriteLineAsync($"Error: File not found: {input}");
                    return 1; // User error
                }

                using var fileStream = File.OpenRead(input);
                score = await importer.ImportAsync(fileStream, progress, cancellationToken);
            }

            if (verbose)
            {
                AnsiConsole.MarkupLine($"[green]Imported[/]: {score.Parts.Count} part(s), [yellow]{score.Metadata.Tempo}[/] BPM, [yellow]{score.Metadata.TimeSignature}[/]");
            }

            // Validate score
            var validationResults = NotationScoreValidator.Validate(score);

            if (validationResults.HasIssues)
            {
                if (validationResults.Errors.Count > 0)
                {
                    await Console.Error.WriteLineAsync("Validation errors:");
                    foreach (var error in validationResults.Errors)
                    {
                        await Console.Error.WriteLineAsync($"  - {error}");
                    }
                    return 1; // User error - invalid input
                }

                if (validationResults.Warnings.Count > 0 && verbose)
                {
                    AnsiConsole.MarkupLine("[yellow]Validation warnings:[/]");
                    foreach (var warning in validationResults.Warnings)
                    {
                        AnsiConsole.MarkupLine($"[yellow]  - {Markup.Escape(warning)}[/]");
                    }
                }
            }

            // Collect format-specific options
            var options = new Dictionary<string, string>();
            foreach (var option in exporter.AvailableOptions)
            {
                if (dynamicOptions.TryGetValue(option.Name, out var opt)
                    && parseResult.GetResult(opt) is { } optionResult
                    && optionResult.GetValueOrDefault<string?>() is { } value)
                {
                    options[option.Name] = value;
                }
            }

            // Export
            if (output == "-")
            {
                // Write to stdout
                using var stdout = Console.OpenStandardOutput();
                await exporter.ExportAsync(score, stdout, options, cancellationToken);
            }
            else
            {
                // Write to file
                using var fileStream = File.Create(output);
                await exporter.ExportAsync(score, fileStream, options, cancellationToken);
            }

            if (!quiet)
            {
                var noteCount = score.Parts
                    .SelectMany(p => p.Voices)
                    .SelectMany(v => v.Measures)
                    .SelectMany(m => m.Events)
                    .Count(e => e is NotationNote or Chord);

                AnsiConsole.MarkupLine($"[green]✓ Converted[/] {input} → {output} ([yellow]{score.Metadata.Tempo}[/] BPM, [yellow]{score.Metadata.TimeSignature}[/], [yellow]{noteCount}[/] note(s))");
            }

            return 0; // Success
        }
        catch (FileNotFoundException ex)
        {
            await Console.Error.WriteLineAsync($"Error: File not found: {ex.FileName}");
            return 1; // User error
        }
        catch (UnauthorizedAccessException ex)
        {
            await Console.Error.WriteLineAsync($"Error: Access denied: {ex.Message}");
            return 1; // User error
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            if (verbose)
            {
                await Console.Error.WriteLineAsync(ex.StackTrace);
            }
            return 2; // System error
        }
    }

    private static IScoreImporter? GetImporter(string inputPath, string? formatOverride)
    {
        if (formatOverride != null)
        {
            var ext = formatOverride.StartsWith('.') ? formatOverride : $".{formatOverride}";
            return FormatRegistry.GetImporter(ext);
        }

        if (inputPath != "-")
        {
            var ext = Path.GetExtension(inputPath);
            if (!string.IsNullOrEmpty(ext))
            {
                return FormatRegistry.GetImporter(ext);
            }
        }

        return null;
    }

    private static IScoreExporter? GetExporter(string outputPath, string? formatOverride)
    {
        if (formatOverride != null)
        {
            var ext = formatOverride.StartsWith('.') ? formatOverride : $".{formatOverride}";
            return FormatRegistry.GetExporter(ext);
        }

        if (outputPath != "-")
        {
            var ext = Path.GetExtension(outputPath);
            if (!string.IsNullOrEmpty(ext))
            {
                return FormatRegistry.GetExporter(ext);
            }
        }

        return null;
    }
}
