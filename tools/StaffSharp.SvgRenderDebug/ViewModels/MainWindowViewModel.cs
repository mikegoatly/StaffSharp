using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using StaffSharp.Importers.Abc;
using StaffSharp.Notation;
using StaffSharp.Svg;

namespace StaffSharp.SvgRenderDebug.ViewModels;

public class AbcExample
{
    public string Name { get; init; } = string.Empty;
    public string AbcContent { get; init; } = string.Empty;
}

public class MainWindowViewModel : INotifyPropertyChanged
{
    private string _abcText = @"X:1
T:Simple Scale
M:4/4
L:1/4
K:C
C D E F | G A B c |]";

    private string? _selectedLayoutPass;
    private string? _svgContent;
    private string? _errorMessage;
    private double _controlWidth = 800;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string AbcText
    {
        get => _abcText;
        set
        {
            if (_abcText != value)
            {
                _abcText = value;
                OnPropertyChanged();
                UpdateSvg();
            }
        }
    }

    public ReadOnlyCollection<string> LayoutPasses { get; }

    public ReadOnlyCollection<AbcExample> Examples { get; }

    public string? SelectedLayoutPass
    {
        get => _selectedLayoutPass;
        set
        {
            if (_selectedLayoutPass != value)
            {
                _selectedLayoutPass = value;
                OnPropertyChanged();
                UpdateSvg();
            }
        }
    }

    public double ControlWidth
    {
        get => _controlWidth;
        set
        {
            if (Math.Abs(_controlWidth - value) > 0.5)
            {
                _controlWidth = value;
                OnPropertyChanged();
                UpdateSvg();
            }
        }
    }

    public string? SvgContent
    {
        get => _svgContent;
        private set
        {
            if (_svgContent != value)
            {
                _svgContent = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        // Populate layout passes
        var passes = LayoutEngine.LayoutPasses
            .Select(pass => pass.GetType().Name)
            .ToList();
        LayoutPasses = new ReadOnlyCollection<string>(passes);

        // Populate examples
        var examples = new List<AbcExample>
        {
            new() { Name = "Simple Scale", AbcContent = @"X:1
T:Simple Scale
M:4/4
L:1/4
K:C
C D E F | G A B c |]" },

            new() { Name = "Multiple Measures", AbcContent = @"X:2
T:Longer Piece
M:4/4
L:1/8
K:C
C2 D2 E2 F2 | G2 A2 B2 c2 | d2 c2 B2 A2 | G2 F2 E2 D2 | C4 z4 |]" },

            new() { Name = "With Ties", AbcContent = @"X:3
T:Tied Notes
M:4/4
L:1/4
K:C
C2-C D | E2-E F | G2-G2 | c4 |]" },

            new() { Name = "Key: G Major (1 Sharp)", AbcContent = @"X:4
T:G Major Scale
M:4/4
L:1/4
K:G
G A B c | d e f g |]" },

            new() { Name = "Key: D Major (2 Sharps)", AbcContent = @"X:5
T:D Major Scale
M:4/4
L:1/4
K:D
D E ^F G | A B ^c d |]" },

            new() { Name = "Key: F# Major (6 Sharps)", AbcContent = @"X:6
T:F# Major Scale
M:4/4
L:1/4
K:F#
^F ^G ^A B | ^c ^d ^e ^f |]" },

            new() { Name = "Key: F Major (1 Flat)", AbcContent = @"X:7
T:F Major Scale
M:4/4
L:1/4
K:F
F G A _B | c d e f |]" },

            new() { Name = "Key: Bb Major (2 Flats)", AbcContent = @"X:8
T:Bb Major Scale
M:4/4
L:1/4
K:Bb
_B c d _e | f g a _b |]" },

            new() { Name = "Key: Db Major (5 Flats)", AbcContent = @"X:9
T:Db Major Scale
M:4/4
L:1/4
K:Db
_D _E F _G | _A _B c _d |]" },

            new() { Name = "Chords", AbcContent = @"X:10
T:Chord Progression
M:4/4
L:1/4
K:C
[CEG] [DFA] | [EGB] [FAc] | [GBd] [Ace] | [C2E2G2] z2 |]" },

            new() { Name = "Chords with Stems", AbcContent = @"X:11
T:Chord Stems Test
M:4/4
L:1/8
K:C
[C2E2G2] [D2F2A2] | [E2G2B2] [F2A2c2] | [G,2B,2D2] [A,2C2E2] | [C4E4G4] |]" },

            new() { Name = "Long Score (System Breaks)", AbcContent = @"X:12
T:Long Melody
M:4/4
L:1/8
K:C
C2 D2 E2 F2 | G2 A2 B2 c2 | d2 e2 f2 g2 | a2 g2 f2 e2 |
d2 c2 B2 A2 | G2 F2 E2 D2 | C2 D2 E2 F2 | G4 E4 |
c2 B2 A2 G2 | F2 E2 D2 C2 | D2 E2 F2 G2 | A4 F4 |
G2 A2 B2 c2 | d2 c2 B2 A2 | G2 F2 E2 D2 | C8 |]" }
        };
        Examples = new ReadOnlyCollection<AbcExample>(examples);

        // Select the last pass by default (run all passes)
        _selectedLayoutPass = LayoutPasses.LastOrDefault();

        // Initial render
        UpdateSvg();
    }

    public void LoadExample(AbcExample example)
    {
        AbcText = example.AbcContent;
    }

    private void UpdateSvg()
    {
#pragma warning disable CA1031 // Do not catch general exception types - this is a debug tool
        try
        {
            ErrorMessage = null;

            // Parse ABC
            var score = AbcParser.Parse(AbcText);

            // Create SVG context with optional bail-after-pass
            var options = new Dictionary<string, string>
            {
                ["maxWidth"] = ControlWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };

            if (SelectedLayoutPass != null)
            {
                options["bailAfterPass"] = SelectedLayoutPass;
            }

            // Export to SVG
            var exporter = new SvgScoreExporter();
            using var stream = new MemoryStream();
            exporter.ExportAsync(score, stream, options).Wait();

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            SvgContent = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            SvgContent = null;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
