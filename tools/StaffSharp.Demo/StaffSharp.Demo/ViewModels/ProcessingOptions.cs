using CommunityToolkit.Mvvm.ComponentModel;

using StaffSharp.Audio.Analysis;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning;
using StaffSharp.MachineLearning.Options;

namespace StaffSharp.Demo.ViewModels;

/// <summary>
/// Configuration options for audio-to-score processing.
/// </summary>
public partial class ProcessingOptions : ObservableObject
{
    // SVG Rendering
    [ObservableProperty]
    public partial SvgExportOptions ExportOptions { get; set; } = new SvgExportOptions();

    // Note Detection
    [ObservableProperty]
    public partial bool UseMachineLearning { get; set; } = true;

    [ObservableProperty]
    public partial string ModelPath { get; set; }

    /// <summary>
    /// Resets all options to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        ExportOptions = new SvgExportOptions();
        UseMachineLearning = true;
        ModelPath = string.Empty;
    }

    public AudioPipelineOptions CreateAudioPipelineOptions(IDiagnosticsCollector diagnosticsCollector)
    {
        INoteDetector noteDetector = UseMachineLearning
            ? MLNoteDetector.Create(CreateMLTranscriptionOptions())
            : AlgorithmicNoteDetector.Create(); // TODO pass options as AlgorithmicNoteDetectorOptions when available

        return new AudioPipelineOptions
        {
            NoteDetector = noteDetector,
            DiagnosticsCollector = diagnosticsCollector
        };
    }

    private MLTranscriptionOptions CreateMLTranscriptionOptions()
    {
        // TODO other options
        return new()
        {
            ModelPath = string.IsNullOrWhiteSpace(ModelPath) ? null : ModelPath,
        };
    }
}
