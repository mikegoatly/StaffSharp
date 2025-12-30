namespace StaffSharp.Cli.Commands;

using System.CommandLine;
using System.CommandLine.Invocation;
using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.Validation;

/// <summary>
/// Implements the 'convert' command for format conversion.
/// </summary>
internal static class ConvertCommand
{
    public static Command Create()
    {
        var command = new Command("convert", "Convert between music notation formats");

        // Arguments
        var inputArg = new Argument<string>(
            "input",
            "Input file path (use '-' for stdin)");

        var outputArg = new Argument<string>(
            "output",
            "Output file path (use '-' for stdout)");

        command.AddArgument(inputArg);
        command.AddArgument(outputArg);

        // Options for format override
        var fromOption = new Option<string?>(
            "--from",
            "Override input format detection (e.g., 'abc')");

        var toOption = new Option<string?>(
            "--to",
            "Override output format detection (e.g., 'midi')");

        command.AddOption(fromOption);
        command.AddOption(toOption);

        // Verbosity options
        var quietOption = new Option<bool>(
            "--quiet",
            "Suppress all output except errors");

        var verboseOption = new Option<bool>(
            "--verbose",
            "Show detailed conversion information");

        command.AddOption(quietOption);
        command.AddOption(verboseOption);

        // Add format-specific options dynamically
        foreach (var exporter in FormatRegistry.Exporters)
        {
            foreach (var option in exporter.AvailableOptions)
            {
                var cmdOption = new Option<string?>(
                    $"--{option.Name}",
                    option.Description)
                {
                    ArgumentHelpName = option.DefaultValue
                };
                command.AddOption(cmdOption);
            }
        }

        command.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForArgument(inputArg);
            var output = context.ParseResult.GetValueForArgument(outputArg);
            var from = context.ParseResult.GetValueForOption(fromOption);
            var to = context.ParseResult.GetValueForOption(toOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var exitCode = await ExecuteAsync(
                input,
                output,
                from,
                to,
                quiet,
                verbose,
                context.ParseResult,
                context.GetCancellationToken());

            context.ExitCode = exitCode;
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
        System.CommandLine.Parsing.ParseResult parseResult,
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

            // Import
            NotationScore score;
            if (input == "-")
            {
                // Read from stdin
                using var stdin = Console.OpenStandardInput();
                score = await importer.ImportAsync(stdin, cancellationToken);
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
                score = await importer.ImportAsync(fileStream, cancellationToken);
            }

            if (verbose)
            {
                Console.WriteLine($"Imported: {score.Parts.Count} part(s), {score.Metadata.Tempo} BPM, {score.Metadata.TimeSignature}");
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
                    Console.WriteLine("Validation warnings:");
                    foreach (var warning in validationResults.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }
            }

            // Collect format-specific options
            var options = new Dictionary<string, string>();
            foreach (var option in exporter.AvailableOptions)
            {
                var optionResult = parseResult.FindResultFor(new Option<string?>($"--{option.Name}"));
                if (optionResult != null)
                {
                    var value = optionResult.GetValueOrDefault<string?>();
                    if (value != null)
                    {
                        options[option.Name] = value;
                    }
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

                Console.WriteLine($"✓ Converted {input} → {output} ({score.Metadata.Tempo} BPM, {score.Metadata.TimeSignature}, {noteCount} note(s))");
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
