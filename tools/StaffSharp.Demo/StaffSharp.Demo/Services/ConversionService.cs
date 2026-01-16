using StaffSharp.Abc.Importing;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Demo.ViewModels;
using StaffSharp.Midi;
using StaffSharp.MusicXml;
using StaffSharp.Notation;

namespace StaffSharp.Demo.Services;

/// <summary>
/// Service for converting between audio, ABC, and notation formats using StaffSharp's audio pipeline.
/// </summary>
internal sealed class ConversionService : IConversionService, IProgress<ImportProgress>
{
    private static readonly Dictionary<string, IScoreImporter> _importers = BuildImporters();
    private static readonly Dictionary<string, IScoreExporter> _exporters = BuildExporters();

    public Action<ImportProgress>? StatusChanged { get; set; }

    private static Dictionary<string, IScoreImporter> BuildImporters()
    {
        var importers = new IScoreImporter[]
        {
            new AbcScoreImporter(),
            new MusicXmlScoreImporter(),
            new AudioScoreImporter()
        };

        var dict = new Dictionary<string, IScoreImporter>(StringComparer.OrdinalIgnoreCase);
        foreach (var importer in importers)
        {
            foreach (var ext in importer.SupportedExtensions)
            {
                dict[ext] = importer;
            }
        }

        return dict;
    }

    void IProgress<ImportProgress>.Report(ImportProgress value)
    {
        StatusChanged?.Invoke(value);
    }

    private static Dictionary<string, IScoreExporter> BuildExporters()
    {
        var exporters = new IScoreExporter[]
        {
            new MidiScoreExporter(),
            new SvgScoreExporter()
        };

        var dict = new Dictionary<string, IScoreExporter>(StringComparer.OrdinalIgnoreCase);
        foreach (var exporter in exporters)
        {
            foreach (var ext in exporter.SupportedExtensions)
            {
                dict[ext] = exporter;
            }
        }

        return dict;
    }

    public async Task<ConversionResult> ConvertAsync(
        AudioBuffer audioBuffer,
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var diagnosticsCollector = new InMemoryDiagnosticsCollector();

        try
        {
            var score = await AudioPipeline.FromAudioBufferAsync(
                audioBuffer,
                CreateAudioPipelineOptions(diagnosticsCollector, options),
                cancellationToken);

            return ConversionResult.Successful(
                score,
                audioBuffer,
                diagnosticsCollector.GetDiagnostics());
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(
                new ImportProgress
                {
                    StepName = "Import",
                    Message = $"Failed: {ex.Message}",
                });

            return ConversionResult.Failure(diagnosticsCollector.GetDiagnostics());
        }
    }

    /// <summary>
    /// Converts an audio file to a notation score.
    /// </summary>
    public async Task<ConversionResult> ConvertAsync(
        string filePath,
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var diagnosticsCollector = new InMemoryDiagnosticsCollector();

        try
        {
            var extension = Path.GetExtension(filePath)
                ?? throw new ArgumentException("File must have an extension to determine format.", nameof(filePath));

            var importer = _importers.GetValueOrDefault(extension)
                ?? throw new ArgumentException("No importer found for the given file extension.", nameof(filePath));

            using var fileStream = File.OpenRead(filePath);
            if (importer is AudioScoreImporter audioImporter)
            {
                audioImporter.Options = CreateAudioPipelineOptions(diagnosticsCollector, options);
            }

            var score = await importer.ImportAsync(fileStream, this, cancellationToken);

            return ConversionResult.Successful(
                score,
                (importer as AudioScoreImporter)?.LastAudioBuffer,
                diagnosticsCollector.GetDiagnostics());
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(
                new ImportProgress
                {
                    StepName = "Import",
                    Message = $"Failed: {ex.Message}",
                });

            return ConversionResult.Failure(diagnosticsCollector.GetDiagnostics());
        }
    }

    private static AudioPipelineOptions CreateAudioPipelineOptions(InMemoryDiagnosticsCollector diagnosticsCollector, ProcessingOptions options)
    {
        return new AudioPipelineOptions
        {
            // TODO : Map other options for each options in ProcessingOptions
            DiagnosticsCollector = diagnosticsCollector
        };
    }

    public async Task ExportAsync(
        string fileName,
        NotationScore score,
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = Path.GetExtension(fileName)
                ?? throw new ArgumentException("File must have an extension to determine format.", nameof(fileName));

            var exporter = _exporters.GetValueOrDefault(extension)
                ?? throw new ArgumentException("No exporter found for the given file extension.", nameof(fileName));

            using var fileStream = File.Create(fileName);
            await exporter.ExportAsync(
                score,
                fileStream,
                options.ExportOptions.ToDictionary(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(
                new ImportProgress
                {
                    StepName = "Export",
                    Message = $"Failed: {ex.Message}",
                });
        }
    }
}