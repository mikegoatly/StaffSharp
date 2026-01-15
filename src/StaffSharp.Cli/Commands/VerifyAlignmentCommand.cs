namespace StaffSharp.Cli.Commands;

using System.CommandLine;

using Spectre.Console;

using StaffSharp.MachineLearning.ML.Training;

/// <summary>
/// Verify alignment between MIDI files, audio files, and extracted training features.
/// 
/// Usage:
///   verify-alignment --audio audio.wav --midi audio.midi
///   verify-alignment --maestro-dir /path/to/maestro-v3.0.0 --sample-size 10
/// </summary>
internal static class VerifyAlignmentCommand
{
    public static Command Create()
    {
        var command = new Command("verify-alignment", "Verify MIDI-to-spectrogram alignment");

        var audioOption = new Option<FileInfo?>("--audio")
        {
            Description = "Audio file to verify (.wav)",
            Arity = ArgumentArity.ZeroOrOne,
            Aliases = { "-a" }
        };

        var midiOption = new Option<FileInfo?>("--midi")
        {
            Description = "MIDI file to verify (.mid or .midi)",
            Arity = ArgumentArity.ZeroOrOne,
            Aliases = { "-m" }
        };

        var maestroOption = new Option<DirectoryInfo?>("--maestro-dir")
        {
            Description = "MAESTRO dataset directory for batch verification",
            Arity = ArgumentArity.ZeroOrOne
        };

        var sampleSizeOption = new Option<int>("--sample-size")
        {
            Description = "Number of random files to verify from MAESTRO dataset",
            Aliases = { "-s" },
            DefaultValueFactory = (r) => 10
        };

        var sampleSeed = new Option<int>("--sample-seed")
        {
            Description = "Random seed for sampling files from MAESTRO dataset",
            DefaultValueFactory = (r) => 42
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show detailed information for each file",
            Aliases = { "-v" }
        };

        command.Options.Add(audioOption);
        command.Options.Add(midiOption);
        command.Options.Add(maestroOption);
        command.Options.Add(sampleSizeOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var audioFile = parseResult.GetValue(audioOption);
            var midiFile = parseResult.GetValue(midiOption);
            var maestroDir = parseResult.GetValue(maestroOption);
            var sampleSize = parseResult.GetValue(sampleSizeOption);
            var verbose = parseResult.GetValue(verboseOption);
            var sampleSeedValue = parseResult.GetValue(sampleSeed);

            return await ExecuteAsync(audioFile, midiFile, maestroDir, sampleSize, sampleSeedValue, verbose);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        FileInfo? audioFile,
        FileInfo? midiFile,
        DirectoryInfo? maestroDir,
        int sampleSize,
        int sampleSeed,
        bool verbose)
    {
        try
        {
            if (audioFile != null && midiFile != null)
            {
                // Single file verification
                return await VerifySingleFileAsync(audioFile.FullName, midiFile.FullName, verbose);
            }
            else if (maestroDir != null)
            {
                // Batch verification
                return await VerifyMaestroDatasetAsync(maestroDir.FullName, sampleSize, sampleSeed, verbose);
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Specify either --audio and --midi, or --maestro-dir");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (verbose)
            {
                AnsiConsole.WriteException(ex);
            }

            return 1;
        }
    }

    private static async Task<int> VerifySingleFileAsync(
        string audioPath,
        string midiPath,
        bool verbose)
    {
        if (!File.Exists(audioPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Audio file not found: {audioPath}");
            return 1;
        }

        if (!File.Exists(midiPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] MIDI file not found: {midiPath}");
            return 1;
        }

        AnsiConsole.MarkupLine("Verifying alignment...");
        AnsiConsole.MarkupLine($"  Audio: [cyan]{Path.GetFileName(audioPath)}[/]");
        AnsiConsole.MarkupLine($"  MIDI:  [cyan]{Path.GetFileName(midiPath)}[/]");
        AnsiConsole.MarkupLine(string.Empty);

        var processor = new MaestroDatasetProcessor();
        var verifier = new TrainingDataAlignmentVerifier();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (trainingData, result) = await ProcessAndVerifyFileAsync(audioPath, midiPath, verbose, processor, verifier);
        stopwatch.Stop();

        AnsiConsole.MarkupLine($"Processing time: [green]{stopwatch.ElapsedMilliseconds}ms[/]");
        AnsiConsole.MarkupLine(
            $"  Frames: [cyan]{trainingData.MelSpectrogram.GetLength(0)}[/], " +
            $"Duration: [cyan]{trainingData.MelSpectrogram.GetLength(0) / 31.25:F2}s[/]");
        AnsiConsole.MarkupLine(string.Empty);

        var summary = result.ToString();
        AnsiConsole.MarkupLine(summary);

        return result.IsValid ? 0 : 1;
    }

    private static async Task<(TrainingDataSample TrainingData, VerificationResult Result)> ProcessAndVerifyFileAsync(
        string audioPath,
        string midiPath,
        bool verbose,
        MaestroDatasetProcessor processor,
        TrainingDataAlignmentVerifier verifier)
    {
        var trainingData = await processor.ProcessFileAsync(audioPath, midiPath);
        var result = verifier.VerifyAlignment(midiPath, audioPath, trainingData);

        if (verbose)
        {
            if (result.IsValid)
            {
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] {audioPath}: [cyan]{result.TotalMidiNotes}[/] notes, " +
                    $"[cyan]{result.TotalFrames}[/] frames");
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]✗[/] {audioPath}: " +
                    $"[red]{result.ErrorCount}[/] errors, [yellow]{result.WarningCount}[/] warnings");

                AnsiConsole.MarkupLine(string.Empty);
                AnsiConsole.MarkupLine("[yellow]Detailed Issues:[/]");
                foreach (var issue in result.Issues.OrderBy(i => i.Severity != "ERROR"))
                {
                    var (icon, color) = issue.Severity switch
                    {
                        "ERROR" => ("✗", "[red]"),
                        "WARNING" => ("⚠", "[yellow]"),
                        _ => ("ℹ", "[blue]")
                    };
                    AnsiConsole.MarkupLine(
                        $"  {color}{icon}[/] [{issue.Category}] {issue.Message}");
                }
            }
        }

        return (trainingData, result);
    }

    private static async Task<int> VerifyMaestroDatasetAsync(
        string sampleDir,
        int sampleSize,
        int sampleSeed,
        bool verbose)
    {
        var sampleFiles = await SelectSampleFiles(sampleDir, sampleSize, sampleSeed);

        AnsiConsole.MarkupLine(string.Empty);
        AnsiConsole.MarkupLine($"Verifying [cyan]{sampleFiles.Count}[/] random files from MAESTRO dataset");
        AnsiConsole.MarkupLine(string.Empty);

        var processor = new MaestroDatasetProcessor();
        var verifier = new TrainingDataAlignmentVerifier();

        var results = new List<(string Path, VerificationResult Result)>();
        var passed = 0;
        var failed = 0;
        var errors = 0;

        // Process files with progress bar
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask(
                    "[green]Verifying files[/]",
                    maxValue: sampleFiles.Count);

                for (int i = 0; i < sampleFiles.Count; i++)
                {
                    var audioPath = sampleFiles[i];
                    var midiPath = GetMidiPathForAudio(audioPath);

                    var fileName = Path.GetFileName(audioPath);
                    progressTask.Description = $"[green]Processing[/] [cyan]{fileName}[/]";

                    try
                    {
                        var (trainingData, result) = await ProcessAndVerifyFileAsync(audioPath, midiPath, verbose, processor, verifier);

                        results.Add((audioPath, result));

                        if (result.IsValid)
                        {
                            passed++;
                        }
                        else
                        {
                            failed++;
                            errors += result.ErrorCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (verbose)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] {fileName}: {ex.Message}");
                        }

                        failed++;
                    }

                    progressTask.Increment(1);
                }

                progressTask.Description = "[green]Verification complete[/]";
            });

        AnsiConsole.MarkupLine(string.Empty);
        // Display summary in a panel
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("Metric").Centered())
            .AddColumn(new TableColumn("Value").Centered());

        summaryTable.AddRow("Total Files", $"[cyan]{sampleFiles.Count}[/]");
        summaryTable.AddRow("Passed", $"[green]{passed}[/]");
        summaryTable.AddRow("Failed", $"[red]{failed}[/]");
        if (errors > 0)
        {
            summaryTable.AddRow("Total Errors", $"[red]{errors}[/]");
        }

        var successRate = sampleFiles.Count > 0 ? (passed * 100.0 / sampleFiles.Count) : 0;
        var rateColor = successRate == 100 ? "green" : successRate >= 80 ? "yellow" : "red";
        summaryTable.AddRow("Success Rate", $"[{rateColor}]{successRate:F1}%[/]");

        var panel = new Panel(summaryTable)
            .Header("[bold]Verification Summary[/]")
            .BorderColor(failed == 0 ? Color.Green : Color.Red);

        AnsiConsole.Write(panel);

        if (failed > 0 && !verbose)
        {
            AnsiConsole.MarkupLine(string.Empty);
            AnsiConsole.MarkupLine("[yellow]Failed files (use --verbose for details):[/]");

            var failedTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn("File")
                .AddColumn("Errors");

            foreach (var (path, result) in results.Where(r => !r.Result.IsValid).Take(10))
            {
                failedTable.AddRow(
                    $"  [dim]{Path.GetFileName(path)}[/]",
                    $"[red]{result.ErrorCount}[/] errors");
            }

            AnsiConsole.Write(failedTable);
        }

        return failed == 0 ? 0 : 1;
    }

    private static string GetMidiPathForAudio(string audioPath)
    {
        var midiPath = Path.ChangeExtension(audioPath, ".midi");

        if (!File.Exists(midiPath))
        {
            midiPath = Path.ChangeExtension(audioPath, ".mid");
        }

        if (!File.Exists(midiPath))
        {
            throw new FileNotFoundException($"MIDI file not found for audio: {audioPath}");
        }

        return midiPath;
    }

    private static async Task<List<string>> SelectSampleFiles(string sampleDir, int sampleSize, int sampleSeed)
    {
        if (!Directory.Exists(sampleDir))
        {
            throw new DirectoryNotFoundException($"Directory not found: {sampleDir}");
        }

        List<string> sampleFiles = [];

        // Find all audio files with status feedback
        await AnsiConsole.Status()
            .StartAsync("Scanning directory for sample files...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                var audioFiles = Directory.GetFiles(sampleDir, "*.wav", SearchOption.AllDirectories)
                    .ToList();

                ctx.Status($"Found [cyan]{audioFiles.Count}[/] audio files");

#pragma warning disable CA5394 // Random is acceptable here for non-security-critical sampling
                var random = new Random(sampleSeed); // Reproducible sampling with fixed seed
                sampleFiles = audioFiles
                    .OrderBy(_ => random.Next())
                    .Take(Math.Min(sampleSize, audioFiles.Count))
                    .ToList();
#pragma warning restore CA5394

                ctx.Status($"Selected [cyan]{sampleFiles.Count}[/] files for verification");

                return Task.CompletedTask;
            });

        if (sampleFiles.Count == 0)
        {
            throw new InvalidOperationException($"No audio files found in {sampleDir}");
        }

        return sampleFiles;
    }
}
