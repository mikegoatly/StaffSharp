using Avalonia.Platform.Storage;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using StaffSharp.Abc.Exporting;
using StaffSharp.Abc.Importing;
using StaffSharp.Audio;
using StaffSharp.Audio.Diagnostics;
using StaffSharp.Demo.Services;
using StaffSharp.Demo.Services.Audio;
using StaffSharp.Json;
using StaffSharp.Notation;
using StaffSharp.Synthesis;

namespace StaffSharp.Demo.ViewModels;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable", Justification = "Cleanup method is called explicitly")]
public partial class MainViewModel : ViewModelBase
{
    private static readonly SvgScoreExporter _svgExporter = new();

    private const int RecordingSampleRate = 44100;

    private readonly ConversionService _conversionService;
    private readonly IAudioService _audioService;
    private readonly ClipboardService _clipboardService;
    private CancellationTokenSource? _conversionCts;
    private CancellationTokenSource? _abcParseCts;

    [ObservableProperty]
    public partial SettingsFlyoutViewModel SettingsFlyout { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecording))]
    public partial AudioBuffer? RecordedSamples { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready. Open a file to begin.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertFileCommand))]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertFileCommand))]
    public partial string? InputFilePath { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayScoreCommand))]
    public partial NotationScore? Score { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayAudioCommand))]
    public partial AudioBuffer? WaveformSamples { get; set; }

    [ObservableProperty]
    public partial AudioBuffer? SynthesizedSamples { get; set; }

    [ObservableProperty]
    public partial double PlaybackPosition { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayAudioCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopPlaybackCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayScoreCommand))]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayScoreCommand))]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial bool HasResult { get; set; }

    [ObservableProperty]
    public partial bool HasProcessedAudio { get; set; }

    [ObservableProperty]
    public partial string? AbcText { get; set; }

    [ObservableProperty]
    public partial string? SvgContent { get; set; }

    [ObservableProperty]
    public partial string? ScoreTitle { get; set; }

    [ObservableProperty]
    public partial int DetectedTempo { get; set; }

    [ObservableProperty]
    public partial TimeSpan RecordingDuration { get; set; }

    // ML Diagnostics
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[]? NormalizedWaveform { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[]? ResampledWaveform { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[,]? MelSpectrogram { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[,]? OnsetProbabilities { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[,]? FrameProbabilities { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial float[,]? OffsetProbabilities { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMlDiagnostics))]
    public partial IReadOnlyList<NoteEvent>? DecodedNoteEvents { get; set; }

    [ObservableProperty]
    public partial double MlFrameRate { get; set; }

    public bool HasMlDiagnostics =>
        NormalizedWaveform != null ||
        ResampledWaveform != null ||
        MelSpectrogram != null ||
        OnsetProbabilities != null;

    // Required for file picker
    public IStorageProvider? StorageProvider { get; set; }

    public bool HasRecording => RecordedSamples is not null;

    public MainViewModel()
    {
        _clipboardService = ClipboardService.Instance;
        _conversionService = new ConversionService();
        _conversionService.StatusChanged += OnConversionStatusChanged;

        _audioService = AudioService.Instance;
        _audioService.PlaybackStateChanged += OnPlaybackStateChanged;

        _audioService.PositionChanged += OnPositionChanged;

        SettingsFlyout = new SettingsFlyoutViewModel();
        SettingsFlyout.SettingsApplied += OnSettingsApplied;
    }

    private void OnPlaybackStateChanged(PlaybackState state)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            IsPlaying = state == PlaybackState.Playing;
            IsPaused = state == PlaybackState.Paused;
        });
    }

    private void OnPositionChanged(TimeSpan position)
    {
        if (_audioService.Duration.TotalSeconds > 0)
        {
            Dispatcher.UIThread.Post(() =>
                PlaybackPosition = position.TotalSeconds / _audioService.Duration.TotalSeconds);
        }
    }

    private void OnSettingsApplied(ProcessingOptions options)
    {
        // Re-process existing content if available
        if (RecordedSamples is not null)
        {
            _ = AnalyzeRecordingAsync(RecordedSamples);
        }
        else if (!string.IsNullOrEmpty(InputFilePath) && WaveformSamples is not null)
        {
            _ = ConvertFileAsync();
        }
    }

    private void OnConversionStatusChanged(ImportProgress status)
    {
        Dispatcher.UIThread.Post(() => StatusMessage = $"[{status.StepName}] {status.Message}");
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (StorageProvider == null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Audio or Notation File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio Files") { Patterns = ["*.wav"] },
                new FilePickerFileType("ABC Notation") { Patterns = ["*.abc"] },
                new FilePickerFileType("All Supported") { Patterns = ["*.wav", "*.abc"] }
            ]
        });

        if (files.Count > 0)
        {
            var file = files[0];
            InputFilePath = file.Path.LocalPath;
            await ConvertFileAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertFileAsync()
    {
        if (string.IsNullOrEmpty(InputFilePath))
        {
            return;
        }

        await ConvertAsync(ct => _conversionService.ConvertAsync(InputFilePath, SettingsFlyout.Options, ct));
    }

    async partial void OnAbcTextChanged(string? value)
    {
        // Cancel any previous parse operation
        _abcParseCts?.Cancel();
        _abcParseCts?.Dispose();
        _abcParseCts = new CancellationTokenSource();
        var ct = _abcParseCts.Token;

        if (IsProcessing || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        ClearCurrentResult();

        // Convert the ABC text to a score
        try
        {
            // Debounce: small delay to avoid parsing on every keystroke
            await Task.Delay(250, ct);

            ct.ThrowIfCancellationRequested();

            Score = AbcParser.Parse(value);
            ScoreTitle = Score.Metadata.Title ?? "Untitled";

            SvgContent = await _svgExporter.ExportToStringAsync(
                Score,
                SettingsFlyout.Options.ExportOptions.ToDictionary(),
                ct);

            HasResult = true;
        }
        catch (OperationCanceledException)
        {
            // Ignore cancelled parse
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error parsing ABC text: {ex.Message}";
            Score = null;
            HasResult = false;
        }
    }

    private void ClearCurrentResult()
    {
        Score = null;
        WaveformSamples = null;
        SvgContent = null;
        HasResult = false;
        HasProcessedAudio = false;
    }

    private async Task AnalyzeRecordingAsync(AudioBuffer samples)
    {
        // Set the waveform samples for display
        WaveformSamples = samples;

        await ConvertAsync(ct => _conversionService.ConvertAsync(samples, SettingsFlyout.Options, ct));
    }

    private async Task ConvertAsync(Func<CancellationToken, Task<ConversionResult>> convertAsync)
    {
        var cts = await CreateNewCancellationTokenAsync();
        IsProcessing = true;
        ClearCurrentResult();

        // Clear ML diagnostics
        NormalizedWaveform = null;
        ResampledWaveform = null;
        MelSpectrogram = null;
        OnsetProbabilities = null;
        FrameProbabilities = null;
        OffsetProbabilities = null;
        DecodedNoteEvents = null;
        MlFrameRate = 0;

        try
        {
            var result = await convertAsync(cts.Token);

            if (result.Success)
            {
                Score = result.Score;
                ScoreTitle = result.Score.Metadata.Title ?? Path.GetFileNameWithoutExtension(InputFilePath);
                DetectedTempo = result.Score.Metadata.Tempo;

                ExtractMlDiagnostics(result.Diagnostics);

                if (result.SourceAudio != null)
                {
                    WaveformSamples = result.SourceAudio;
                }

                HasResult = true;
                var svg = Task.Run(() =>
                {
                    return _svgExporter.ExportToStringAsync(
                        result.Score,
                        SettingsFlyout.Options.ExportOptions.ToDictionary());
                });

                var abc = Task.Run(() =>
                {
                    var abcExporter = new AbcScoreExporter();
                    return abcExporter.ExportToStringAsync(
                        result.Score,
                        SettingsFlyout.Options.ExportOptions.ToDictionary());
                });

                HasProcessedAudio = true;
                SvgContent = await svg;
                AbcText = await abc;

                StatusMessage = $"Conversion complete! Title: {ScoreTitle}, Tempo: {DetectedTempo} BPM";
            }
            else
            {
                StatusMessage = "Conversion failed. Check diagnostics for details.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Conversion cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task<CancellationTokenSource> CreateNewCancellationTokenAsync()
    {
        if (_conversionCts is not null)
        {
            await _conversionCts.CancelAsync();
        }

        _conversionCts = new CancellationTokenSource();

        return _conversionCts;
    }

    private bool CanConvert() => !string.IsNullOrEmpty(InputFilePath) && !IsProcessing;

    [RelayCommand(CanExecute = nameof(CanPlayAudio))]
    private void PlayAudio()
    {
        if (WaveformSamples is not null)
        {
            if (IsPlaying)
            {
                _audioService.PausePlayback();
            }
            else if (IsPaused)
            {
                _audioService.ResumePlayback();
            }
            else
            {
                _audioService.PlayAudioBuffer(WaveformSamples);
            }
        }
    }

    private bool CanPlayAudio() => WaveformSamples != null && !IsRecording;

    [RelayCommand(CanExecute = nameof(CanStopPlayback))]
    private void StopPlayback()
    {
        _audioService.StopPlayback();
    }

    private bool CanStopPlayback() => IsPlaying;

    [RelayCommand(CanExecute = nameof(CanPlayScore))]
    private void PlayScore()
    {
        if (Score == null)
        {
            return;
        }

        try
        {
            StatusMessage = "Synthesizing score...";

            var synthesizer = new ScoreSynthesizer();
            var audioBuffer = synthesizer.Synthesize(Score, sampleRate: 44100);
            SynthesizedSamples = audioBuffer;

            StatusMessage = "Playing synthesized score...";
            _audioService.PlayAudioBuffer(audioBuffer);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error synthesizing score: {ex.Message}";
        }
    }

    private bool CanPlayScore() => Score != null && !IsRecording && !IsPlaying;

    [RelayCommand(CanExecute = nameof(CanRecord))]
    private void Record()
    {
        RecordedSamples = null;
        IsRecording = true;
        StatusMessage = "Recording... Press Stop Recording when done.";
        _audioService.StartRecording(RecordingSampleRate, 1);
    }

    private bool CanRecord() => !IsRecording && !IsPlaying;

    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private async Task StopRecordingAsync()
    {
        var buffer = _audioService.StopRecording();

        IsRecording = false;

        if (buffer != null && buffer.SampleCount > 0)
        {
            RecordedSamples = buffer;
            RecordingDuration = TimeSpan.FromSeconds(buffer.DurationSeconds);
            StatusMessage = $"Recording complete! Duration: {RecordingDuration.TotalSeconds:F1}s - Analyzing...";

            // Automatically analyze the recorded audio
            await AnalyzeRecordingAsync(buffer);
        }
        else
        {
            StatusMessage = "Recording stopped (no audio captured).";
        }
    }

    private bool CanStopRecording() => IsRecording;

    [RelayCommand]
    private async Task SaveRecordingAsync()
    {
        if (StorageProvider == null || RecordedSamples is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Recording as WAV",
            SuggestedFileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
            FileTypeChoices =
            [
                new FilePickerFileType("WAV Audio") { Patterns = ["*.wav"] }
            ]
        });

        if (file != null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                RecordedSamples.Save(stream);
                StatusMessage = $"Saved recording to {file.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving recording: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task SaveSvgAsync()
    {
        if (StorageProvider == null || Score == null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save SVG File",
            SuggestedFileName = $"{ScoreTitle ?? "output"}.svg",
            FileTypeChoices =
            [
                new FilePickerFileType("SVG Image") { Patterns = ["*.svg"] }
            ]
        });

        if (file != null)
        {
            try
            {
                StatusMessage = "Generating SVG...";
                await _conversionService.ExportAsync(file.Path.LocalPath, Score, SettingsFlyout.Options);
                StatusMessage = $"Saved SVG to {file.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving SVG: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task SaveAbcAsync()
    {
        if (StorageProvider == null || Score == null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions()
            {
                DefaultExtension = ".abc",
                FileTypeChoices =
            [
                new FilePickerFileType("ABC Notation") { Patterns = ["*.abc"] }
            ]
            });

        if (file != null)
        {
            using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(AbcText);
            StatusMessage = $"Saved ABC to {file.Name}";
        }
    }

    [RelayCommand]
    private async Task SaveMidiAsync()
    {
        if (StorageProvider == null || Score == null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save MIDI File",
            SuggestedFileName = $"{ScoreTitle ?? "output"}.mid",
            FileTypeChoices =
            [
                new FilePickerFileType("MIDI File") { Patterns = ["*.mid", "*.midi"] }
            ]
        });

        if (file != null)
        {
            StatusMessage = "Generating MIDI...";
            await _conversionService.ExportAsync(file.Path.LocalPath, Score, SettingsFlyout.Options);
            StatusMessage = $"Saved MIDI to {file.Name}";
        }
    }

    [RelayCommand]
    private async Task SaveJsonAsync()
    {
        if (StorageProvider == null || Score == null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save JSON File",
            SuggestedFileName = $"{ScoreTitle ?? "output"}.json",
            FileTypeChoices =
            [
                new FilePickerFileType("JSON File") { Patterns = ["*.json"] }
            ]
        });

        if (file != null)
        {
            StatusMessage = "Generating JSON...";
            await _conversionService.ExportAsync(file.Path.LocalPath, Score, SettingsFlyout.Options);
            StatusMessage = $"Saved JSON to {file.Name}";
        }
    }


    [RelayCommand]
    private async Task CopySvgAsync()
    {
        if (Score == null)
        {
            return;
        }

        try
        {
            if (!_clipboardService.IsAvailable)
            {
                StatusMessage = "Clipboard not available";
                return;
            }

            StatusMessage = "Generating SVG...";

            // Generate SVG to memory stream
            using var memoryStream = new MemoryStream();
            await _svgExporter.ExportAsync(Score, memoryStream, SettingsFlyout.Options.ExportOptions.ToDictionary());

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            var svgContent = await reader.ReadToEndAsync();

            await _clipboardService.SetTextAsync(svgContent);
            StatusMessage = "SVG copied to clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying SVG: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyAbcAsync()
    {
        if (this.AbcText is not null)
        {
            await _clipboardService.SetTextAsync(this.AbcText);
        }
    }

    [RelayCommand]
    private async Task CopyJsonAsync()
    {
        if (Score == null)
        {
            return;
        }

        try
        {
            if (!_clipboardService.IsAvailable)
            {
                StatusMessage = "Clipboard not available";
                return;
            }

            StatusMessage = "Generating JSON...";

            // Generate JSON to memory stream
            using var memoryStream = new MemoryStream();
            var jsonExporter = new JsonScoreExporter();
            await jsonExporter.ExportAsync(Score, memoryStream, SettingsFlyout.Options.ExportOptions.ToDictionary());

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            var jsonContent = await reader.ReadToEndAsync();

            await _clipboardService.SetTextAsync(jsonContent);
            StatusMessage = "JSON copied to clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying JSON: {ex.Message}";
        }
    }

    private void ExtractMlDiagnostics(InMemoryDiagnosticsCollector diagnostics)
    {
        // Reset
        NormalizedWaveform = diagnostics.GetDiagnostic<float[]>("NormalizedWaveform");
        ResampledWaveform = diagnostics.GetDiagnostic<float[]>("ResampledWaveform");
        ResampledWaveform = diagnostics.GetDiagnostic<float[]>("ResampledWaveform");
        MelSpectrogram = diagnostics.GetDiagnostic<float[,]>("MelSpectrogram");
        OnsetProbabilities = diagnostics.GetDiagnostic<float[,]>("OnsetProbabilities");
        FrameProbabilities = diagnostics.GetDiagnostic<float[,]>("FrameProbabilities");
        OffsetProbabilities = diagnostics.GetDiagnostic<float[,]>("OffsetProbabilities");
        DecodedNoteEvents = diagnostics.GetDiagnostic<IReadOnlyList<NoteEvent>>("DecodedNoteEvents");
        MlFrameRate = diagnostics.GetDiagnostic<double>("Frame rate (Hz)");
    }

    public void Cleanup()
    {
        _conversionCts?.Cancel();
        _conversionCts?.Dispose();
        _audioService.Dispose();
    }
}