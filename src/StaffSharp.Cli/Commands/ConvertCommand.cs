namespace StaffSharp.Cli.Commands;

using System.CommandLine;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

using Spectre.Console;

using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.Validation;

/// <summary>
/// Implements the 'convert' command for format conversion.
/// </summary>
internal static partial class ConvertCommand
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
    private static readonly Option<bool> verboseOption = new("--verbose") { Description = "Show detailed conversion information including diagnostics" };

    // ML note detection options
    private static readonly Option<bool> useMlOption = new("--use-ml") { Description = "Use machine learning for audio note detection (instead of algorithmic)" };
    private static readonly Option<string?> modelPathOption = new("--model-path") { Description = "Path to ONNX model file (optional, uses default if not specified)" };
    private static readonly Option<float?> onsetThresholdOption = new("--onset-threshold") { Description = "ML onset detection threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> frameThresholdOption = new("--frame-threshold") { Description = "ML frame activation threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> offsetThresholdOption = new("--offset-threshold") { Description = "ML offset detection threshold (0.0-1.0, default: 0.5)" };
    private static readonly Option<float?> minNoteLengthOption = new("--min-note-length") { Description = "Minimum note length in seconds (default: 0.05)" };
    private static readonly Option<float?> minGapSecondsOption = new("--min-gap-seconds") { Description = "Maximum gap duration in seconds to tolerate before ending a note (default: 0.05)" };
    private static readonly Option<float?> minVelocityOption = new("--min-velocity") { Description = "Minimum velocity threshold for onset detection (0.0-1.0, default: 0.1)" };
    private static readonly Option<float?> minFrameForOnsetOption = new("--min-frame-for-onset") { Description = "Minimum frame probability required for onset validation (0.0-1.0, default: 0.3)" };
    private static readonly Option<bool> treatPolyphonyAsChordsOption = new("--treat-polyphony-as-chords")
    {
        DefaultValueFactory = (_) => true,
        Description = "Force overlapping notes into chords sharing a single stem/voice (default: true)"
    };

    // Tempo detection options
    private static readonly Option<string?> tempoDetectorOption = new("--tempo-detector") { Description = "Tempo detection algorithm: 'comb-filter' (default, robust) or 'inter-onset' (simple, faster)" };

    // Diagnostic options
    private static readonly Option<int> maxDiagnosticArrayLengthOption = new("--max-diagnostic-array-length")
    {
        DefaultValueFactory = (o) => 20,
        Description = "Maximum length of arrays printed in diagnostics"
    };

    private static readonly Option<string[]> explicitDiagnosticsRangeExtractionOption = new("--diagnostic-range-extraction")
    {
        Arity = ArgumentArity.OneOrMore,
        Description = "Configure the extraction of specific ranges for diagnostics for a keyed value. Must be in the form <key>:<start>-<end>."
    };

    private static readonly Option<string[]> diagnosticFiltersOption = new("--diagnostic-filter")
    {
        Arity = ArgumentArity.OneOrMore,
        Description = "Filters diagnostics to only include specified keys (e.g., 'onset', 'tempo') - useful if you're debugging a certaing part of the audio pipeline."
    };

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
        command.Options.Add(minGapSecondsOption);
        command.Options.Add(minVelocityOption);
        command.Options.Add(minFrameForOnsetOption);
        command.Options.Add(treatPolyphonyAsChordsOption);
        command.Options.Add(tempoDetectorOption);

        command.Options.Add(maxDiagnosticArrayLengthOption);
        command.Options.Add(explicitDiagnosticsRangeExtractionOption);
        command.Options.Add(diagnosticFiltersOption);

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

            ConfigureDiagnostics(parseResult);

            // Configure audio importer with ML options if applicable
            if (importer is AudioScoreImporter audioImporter)
            {
                ConfigureAudioScoreImporter(parseResult, audioImporter);
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

    private static void ConfigureDiagnostics(ParseResult parseResult)
    {
        var diagnosticsCollector = CliDiagnosticsCollector.Instance;

        var maxArrayLength = parseResult.GetValue(maxDiagnosticArrayLengthOption);
        diagnosticsCollector.MaxArrayLengthForDisplay = maxArrayLength;

        var rangeExtraction = parseResult.GetValue(explicitDiagnosticsRangeExtractionOption);
        foreach (var range in rangeExtraction ?? [])
        {
            var parseRegex = diagnosticsRangeSpecification();
            var match = parseRegex.Match(range);
            if (match.Success)
            {
                var key = match.Groups["key"].Value;
                var start = int.Parse(match.Groups["start"].Value, CultureInfo.InvariantCulture);
                var end = int.Parse(match.Groups["end"].Value, CultureInfo.InvariantCulture);
                diagnosticsCollector.ArrayOffsetsByKey[key] = (start, end - start);
            }
            else
            {
                throw new ArgumentException("Invalid format for --diagnostic-range-extraction. Expected format: <key>:<start>-<end>.");
            }
        }

        var filters = parseResult.GetValue(diagnosticFiltersOption);
        if (filters != null && filters.Length > 0)
        {
            foreach (var filter in filters)
            {
                diagnosticsCollector.Filters.Add(filter);
            }
        }
    }

    private static void ConfigureAudioScoreImporter(ParseResult parseResult, AudioScoreImporter audioImporter)
    {
        // Configure tempo detector
        var tempoDetector = parseResult.GetValue(tempoDetectorOption);
        if (!string.IsNullOrEmpty(tempoDetector))
        {
            audioImporter.ConfigureTempoDetector(tempoDetector);
        }

        // Configure ML note detection if requested
        var useMl = parseResult.GetValue(useMlOption);
        if (useMl)
        {
            audioImporter.ConfigureMLOptions(
                parseResult.GetValue(modelPathOption),
                parseResult.GetValue(onsetThresholdOption),
                parseResult.GetValue(frameThresholdOption),
                parseResult.GetValue(offsetThresholdOption),
                parseResult.GetValue(minNoteLengthOption),
                parseResult.GetValue(minGapSecondsOption),
                parseResult.GetValue(minVelocityOption),
                parseResult.GetValue(minFrameForOnsetOption),
                parseResult.GetValue(treatPolyphonyAsChordsOption));
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

    [System.Text.RegularExpressions.GeneratedRegex(@"^(?<key>.+):(?<start>\d+)-(?<end>\d+)$")]
    private static partial System.Text.RegularExpressions.Regex diagnosticsRangeSpecification();
}
