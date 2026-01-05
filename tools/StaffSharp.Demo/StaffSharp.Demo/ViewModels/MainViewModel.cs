using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using StaffSharp.Demo.Services;
using StaffSharp.Midi;
using StaffSharp.Notation;

namespace StaffSharp.Demo.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ConversionService _conversionService = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private NotationScore? _currentScore;

    [ObservableProperty]
    public partial ProcessingOptions Options { get; set; } = new();

    [ObservableProperty]
    public partial string? SvgContent { get; set; }
    
    [ObservableProperty]
    public partial ReadOnlyMemory<float>? WaveformSamples { get; set; }

    [ObservableProperty]
    public partial string? InputFilePath { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial int DetectedTempo { get; set; }

    [RelayCommand]
    private async Task OpenAudioFileAsync()
    {
        try
        {
            if (PickFileAsync == null) return;

            var file = await PickFileAsync(["wav"], "Open Audio File");
            if (file == null) return;

            InputFilePath = file;
            await ProcessAudioAsync(file);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenAbcFileAsync()
    {
        try
        {
            if (PickFileAsync == null) return;

            var file = await PickFileAsync(["abc"], "Open ABC File");
            if (file == null) return;

            InputFilePath = file;
            var content = await File.ReadAllTextAsync(file);
            await ProcessAbcAsync(content);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveScore), AllowConcurrentExecutions = false)]
    private async Task SaveSvgAsync(CancellationToken cancellationToken)
    {
        if (_currentScore == null || string.IsNullOrEmpty(SvgContent)) return;

        try
        {
            if (SaveFileAsync == null)
            {
                return;
            }

            var file = await SaveFileAsync(["svg"], "Save SVG");
            if (file == null) return;

            await File.WriteAllTextAsync(file, SvgContent, cancellationToken);
            StatusMessage = "SVG saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving SVG: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveScore), AllowConcurrentExecutions = false)]
    private async Task SaveMidiAsync(CancellationToken cancellationToken)
    {
        if (_currentScore == null) return;

        try
        {
            if (SaveFileAsync == null) return;

            var file = await SaveFileAsync(["mid", "midi"], "Save MIDI");
            if (file == null) return;

            var exporter = new MidiScoreExporter();
            using var stream = File.Create(file);

            // TODO expose options for MIDI export
            await exporter.ExportAsync(_currentScore, stream, cancellationToken: cancellationToken);

            StatusMessage = "MIDI saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving MIDI: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplySettings()
    {
        if (!string.IsNullOrEmpty(InputFilePath))
        {
            if (InputFilePath.EndsWith(".abc", StringComparison.OrdinalIgnoreCase))
            {
                _ = Task.Run(async () =>
                {
                    var content = await File.ReadAllTextAsync(InputFilePath);
                    await ProcessAbcAsync(content);
                });
            }
            else if (InputFilePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                _ = ProcessAudioAsync(InputFilePath);
            }
        }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        Options.ResetToDefaults();
    }

    private bool CanSaveScore() => _currentScore != null;

    private async Task ProcessAudioAsync(string filePath)
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
        }
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsProcessing = true;
            StatusMessage = "Processing audio...";

            var result = await _conversionService.ConvertAudioAsync(filePath, Options, _cancellationTokenSource.Token);

            _currentScore = result.Score;
            WaveformSamples = result.AudioSamples;
            DetectedTempo = result.DetectedTempo;

            await RenderScoreAsync(_cancellationTokenSource.Token);

            StatusMessage = $"Audio processed successfully (Tempo: {DetectedTempo} BPM)";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Processing cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Processing failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            SaveSvgCommand.NotifyCanExecuteChanged();
            SaveMidiCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task ProcessAbcAsync(string abcContent)
    {
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
        }
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsProcessing = true;
            StatusMessage = "Processing ABC notation...";

            var result = await _conversionService.ConvertAbcAsync(abcContent, _cancellationTokenSource.Token);

            _currentScore = result.Score;
            WaveformSamples = null;
            DetectedTempo = result.DetectedTempo;

            await RenderScoreAsync(_cancellationTokenSource.Token);

            StatusMessage = "ABC processed successfully";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Processing cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Processing failed: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            SaveSvgCommand.NotifyCanExecuteChanged();
            SaveMidiCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RenderScoreAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    // File picker delegates - injected from view
    public Func<string[], string, Task<string?>>? PickFileAsync { get; set; }
    public Func<string[], string, Task<string?>>? SaveFileAsync { get; set; }
}
