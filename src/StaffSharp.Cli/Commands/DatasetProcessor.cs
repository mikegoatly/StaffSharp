namespace StaffSharp.Cli.Commands;

using System.Globalization;
using System.Text.Json;

using Spectre.Console;

using StaffSharp.MachineLearning.ML.Training;

/// <summary>
/// CLI wrapper for MAESTRO dataset processing with progress reporting.
/// </summary>
internal sealed class DatasetProcessor(string maestroDir, string outputDir, int? maxFiles, int parallelTasks)
{
    private readonly MaestroDatasetProcessor _processor = new();

    public async Task ProcessAsync()
    {
        // Load metadata
        var metadataPath = Path.Combine(maestroDir, "maestro-v3.0.0.json");
        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException($"Metadata file not found: {metadataPath}");
        }

        AnsiConsole.MarkupLine($"[cyan]Loading metadata from:[/] {metadataPath}");
        var metadata = await LoadMetadataAsync(metadataPath);

        // Create output directories
        foreach (var split in new[] { "train", "validation", "test" })
        {
            Directory.CreateDirectory(Path.Combine(outputDir, split));
        }

        // Process each split
        var stats = new Dictionary<string, int>
        {
            ["train"] = 0,
            ["validation"] = 0,
            ["test"] = 0,
            ["errors"] = 0
        };

        foreach (var split in new[] { "train", "validation", "test" })
        {
            var entries = metadata.Where(e => e.Split == split).ToList();

            if (maxFiles.HasValue)
            {
                entries = entries.Take(maxFiles.Value).ToList();
            }

            AnsiConsole.MarkupLine($"\n[yellow]Processing {split} split[/] ([cyan]{entries.Count}[/] files)...");

            var splitProcessedCount = 0;
            var errorsInSplit = 0;
            await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"[cyan]{split}[/]", maxValue: entries.Count);

                    await Parallel.ForEachAsync(
                        entries,
                        new ParallelOptions { MaxDegreeOfParallelism = parallelTasks },
                        async (entry, ct) =>
                        {
                            var success = await ProcessSingleFileAsync(entry, split);
                            if (success)
                            {
                                Interlocked.Increment(ref splitProcessedCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref errorsInSplit);
                            }

                            task.Increment(1);
                        });
                });

            stats[split] = splitProcessedCount;
            stats["errors"] += errorsInSplit;
        }

        // Print statistics
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[cyan]Split[/]")
            .AddColumn("[green]Processed[/]")
            .AddRow("Train", stats["train"].ToString(CultureInfo.InvariantCulture))
            .AddRow("Validation", stats["validation"].ToString(CultureInfo.InvariantCulture))
            .AddRow("Test", stats["test"].ToString(CultureInfo.InvariantCulture))
            .AddRow("[red]Errors[/]", $"[red]{stats["errors"]}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[green]✓ Dataset preparation complete![/]");
    }

    private static async Task<List<MaestroEntry>> LoadMetadataAsync(string path)
    {
        using var stream = File.OpenRead(path);
        var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        // MAESTRO JSON format: { "split": { "0": "train", "1": "train", ... }, "audio_filename": { "0": "...", ... } }
        var splitDict = root.GetProperty("split");
        var audioDict = root.GetProperty("audio_filename");
        var midiDict = root.GetProperty("midi_filename");

        var entries = new List<MaestroEntry>();
        var count = splitDict.GetProperty("0").ValueKind != JsonValueKind.Undefined
            ? splitDict.EnumerateObject().Count()
            : 0;

        for (int i = 0; i < splitDict.EnumerateObject().Count(); i++)
        {
            var key = i.ToString(CultureInfo.InvariantCulture);
            entries.Add(new MaestroEntry
            {
                Split = splitDict.GetProperty(key).GetString()!,
                AudioFilename = audioDict.GetProperty(key).GetString()!,
                MidiFilename = midiDict.GetProperty(key).GetString()!
            });
        }

        return entries;
    }

    private async ValueTask<bool> ProcessSingleFileAsync(MaestroEntry entry, string split)
    {
        try
        {
            var audioPath = Path.Combine(maestroDir, entry.AudioFilename);
            var midiPath = Path.Combine(maestroDir, entry.MidiFilename);

            if (!File.Exists(audioPath))
            {
                AnsiConsole.MarkupLine($"[red]Audio file not found:[/] {audioPath}");
                return false;
            }

            if (!File.Exists(midiPath))
            {
                AnsiConsole.MarkupLine($"[red]MIDI file not found:[/] {midiPath}");
                return false;
            }

            var outputFilename = Path.GetFileNameWithoutExtension(entry.AudioFilename) + ".npz";
            var outputPath = Path.Combine(outputDir, split, outputFilename);
            if (File.Exists(outputPath))
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping existing file:[/] {outputPath}");
                return false;
            }

            // Process file using ML library
            var sample = await _processor.ProcessFileAsync(audioPath, midiPath);

            NpzWriter.WriteSample(outputPath, sample);

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error processing {entry.AudioFilename}:[/] {ex.Message}");
            return false;
        }
    }

    private sealed class MaestroEntry
    {
        public required string Split { get; init; }
        public required string AudioFilename { get; init; }
        public required string MidiFilename { get; init; }
    }
}
