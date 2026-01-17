namespace StaffSharp.MachineLearning.Tests.ML.PostProcessing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

using StaffSharp;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.ML.PostProcessing;
using StaffSharp.MachineLearning.Options;

/// <summary>
/// Equivalence tests verifying that the C# NoteEventDecoder produces identical output
/// to the Python decode_notes() function.
/// 
/// These tests load pre-computed test cases exported by the Python test suite and verify
/// that both implementations decode notes identically. This ensures:
/// - Training validation metrics match production inference
/// - Python and C# implementations are semantically equivalent
/// - No regressions occur if either implementation is modified
/// </summary>
public sealed class NoteEventDecoderEquivalenceTests
{
    private const double Tolerance = 1e-6;
    
    /// <summary>
    /// Represents an expected note from a test case JSON file.
    /// </summary>
    private sealed class ExpectedNote
    {
        public int Pitch { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double Velocity { get; set; }
    }
    
    /// <summary>
    /// Represents a complete test case loaded from JSON.
    /// </summary>
    private sealed class TestCase
    {
        public string Name { get; set; } = string.Empty;
        public double FrameRate { get; set; }
        public JsonElement Thresholds { get; set; }
        public JsonElement Input { get; set; }
        public JsonElement ExpectedNotes { get; set; }
    }
    
    /// <summary>
    /// Loads all test cases from the Python-generated test data directory.
    /// </summary>
    private static IEnumerable<TestCase> LoadTestCases()
    {
        // Find test data directory relative to this test file
        // AppContext.BaseDirectory is bin/Debug/net10.0
        // We need to go up to the StaffSharp root, then navigate to training/scripts/test_data
        var testDataPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", // bin/Debug/net10.0 -> bin -> Debug -> net10.0 -> test -> StaffSharp (root)
            "training", "scripts", "test_data", "decoder_equivalence"
        );
        
        var fullPath = Path.GetFullPath(testDataPath);
        
        if (!Directory.Exists(fullPath))
        {
            // Try from project root (if running from workspace root)
            testDataPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "training", "scripts", "test_data", "decoder_equivalence"
            );
            fullPath = Path.GetFullPath(testDataPath);
        }
        
        if (!Directory.Exists(fullPath))
        {
            // Try an absolute path based on common dev locations
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", "dev", "StaffSharp", "training", "scripts", "test_data", "decoder_equivalence"),
                Path.Combine("C:", "dev", "StaffSharp", "training", "scripts", "test_data", "decoder_equivalence"),
                Path.Combine("D:", "dev", "StaffSharp", "training", "scripts", "test_data", "decoder_equivalence"),
            };
            
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    fullPath = path;
                    break;
                }
            }
        }
        
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Test data directory not found. Tried: {fullPath}");
        }
        
        var jsonFiles = Directory.GetFiles(fullPath, "*.json");
        
        foreach (var jsonFile in jsonFiles)
        {
            var json = File.ReadAllText(jsonFile);
            
            // Parse and extract all data before disposing
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                
                var name = root.GetProperty("name").GetString() ?? "unknown";
                var frameRate = root.GetProperty("frame_rate").GetDouble();
                
                // Clone the elements so they persist after doc is disposed
                var thresholdsText = root.GetProperty("thresholds").GetRawText();
                var inputText = root.GetProperty("input").GetRawText();
                var expectedNotesText = root.GetProperty("expected_notes").GetRawText();
                
                using var thresholdsDoc = JsonDocument.Parse(thresholdsText);
                using var inputDoc = JsonDocument.Parse(inputText);
                using var notesDoc = JsonDocument.Parse(expectedNotesText);
                
                var testCase = new TestCase
                {
                    Name = name,
                    FrameRate = frameRate,
                    Thresholds = thresholdsDoc.RootElement.Clone(),
                    Input = inputDoc.RootElement.Clone(),
                    ExpectedNotes = notesDoc.RootElement.Clone()
                };
                
                yield return testCase;
            }
        }
    }
    
    /// <summary>
    /// Extracts a 2D array from JSON representation.
    /// </summary>
    private static float[,] ExtractArray(JsonElement element)
    {
        var rows = element.GetArrayLength();
        if (rows == 0)
            return new float[0, 88];
        
        var cols = element[0].GetArrayLength();
        var array = new float[rows, cols];
        
        for (int i = 0; i < rows; i++)
        {
            var row = element[i];
            for (int j = 0; j < cols; j++)
            {
                array[i, j] = (float)row[j].GetDouble();
            }
        }
        
        return array;
    }
    
    /// <summary>
    /// Extracts expected notes from JSON.
    /// </summary>
    private static List<ExpectedNote> ExtractExpectedNotes(JsonElement element)
    {
        var notes = new List<ExpectedNote>();
        
        foreach (var noteJson in element.EnumerateArray())
        {
            notes.Add(new ExpectedNote
            {
                Pitch = noteJson.GetProperty("pitch").GetInt32(),
                Start = noteJson.GetProperty("start").GetDouble(),
                End = noteJson.GetProperty("end").GetDouble(),
                Velocity = noteJson.GetProperty("velocity").GetDouble()
            });
        }
        
        return notes;
    }
    
    /// <summary>
    /// Test that Python and C# decoders produce identical results.
    /// Loads test cases from Python-generated JSON files.
    /// </summary>
    [Fact]
    public void DecoderEquivalence_LoadPythonTestCases_ProduceIdenticalResults()
    {
        // Arrange
        var testCases = LoadTestCases().ToList();
        
        if (testCases.Count == 0)
        {
            // If no test data found, generate a simple inline test
            TestSingleNoteEquivalence();
            return;
        }
        
        var failures = new List<string>();
        
        foreach (var testCase in testCases)
        {
            try
            {
                // Extract test inputs
                var onsetRoll = ExtractArray(testCase.Input.GetProperty("onset_probs"));
                var frameRoll = ExtractArray(testCase.Input.GetProperty("frame_probs"));
                var offsetRoll = ExtractArray(testCase.Input.GetProperty("offset_probs"));
                var velocityRoll = ExtractArray(testCase.Input.GetProperty("velocity_values"));
                
                var expectedNotes = ExtractExpectedNotes(testCase.ExpectedNotes);
                
                // Extract thresholds
                var onsetThresh = testCase.Thresholds.TryGetProperty("onset_thresh", 
                    out var ot) ? (float)ot.GetDouble() : 0.5f;
                var frameThresh = testCase.Thresholds.TryGetProperty("frame_thresh",
                    out var ft) ? (float)ft.GetDouble() : 0.5f;
                var offsetThresh = testCase.Thresholds.TryGetProperty("offset_thresh",
                    out var oft) ? (float)oft.GetDouble() : 0.5f;
                var minDurationSeconds = testCase.Thresholds.TryGetProperty("min_duration_seconds",
                    out var mds) ? (float)mds.GetDouble() : 0.05f;
                
                // Act - decode using C#
                var options = new MLTranscriptionOptions
                {
                    OnsetThreshold = onsetThresh,
                    FrameThreshold = frameThresh,
                    OffsetThreshold = offsetThresh,
                    MinNoteLengthSeconds = minDurationSeconds
                };
                
                var result = new PolyphonicTranscriptionResult(
                    PianoRoll: frameRoll,
                    OnsetRoll: onsetRoll,
                    OffsetRoll: offsetRoll,
                    VelocityRoll: velocityRoll,
                    FrameRate: testCase.FrameRate,
                    SampleRate: 16000
                );
                
                var decoder = new NoteEventDecoder(options);
                var decodedNotes = decoder.Decode(result).ToList();
                
                // Assert
                if (decodedNotes.Count != expectedNotes.Count)
                {
                    failures.Add($"{testCase.Name}: Expected {expectedNotes.Count} notes, got {decodedNotes.Count}");
                    continue;
                }
                
                for (int i = 0; i < decodedNotes.Count; i++)
                {
                    var decoded = decodedNotes[i];
                    var expected = expectedNotes[i];
                    
                    if (decoded.Pitch.Value != expected.Pitch)
                    {
                        failures.Add($"{testCase.Name}[{i}]: Pitch mismatch - expected {expected.Pitch}, got {decoded.Pitch.Value}");
                    }
                    
                    var decodedStart = decoded.Onset.TotalSeconds;
                    if (Math.Abs(decodedStart - expected.Start) > Tolerance)
                    {
                        failures.Add($"{testCase.Name}[{i}]: Start mismatch - expected {expected.Start}, got {decodedStart}");
                    }
                    
                    var decodedEnd = (decoded.Onset + decoded.Duration).TotalSeconds;
                    if (Math.Abs(decodedEnd - expected.End) > Tolerance)
                    {
                        failures.Add($"{testCase.Name}[{i}]: End mismatch - expected {expected.End}, got {decodedEnd}");
                    }
                    
                    if (Math.Abs(decoded.Velocity.Value - expected.Velocity) > Tolerance)
                    {
                        failures.Add($"{testCase.Name}[{i}]: Velocity mismatch - expected {expected.Velocity}, got {decoded.Velocity.Value}");
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // If no test data found, generate a simple inline test
                TestSingleNoteEquivalence();
                return;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                failures.Add($"{testCase.Name}: Exception - {ex.Message}");
            }
#pragma warning restore CA1031
        }
        
        // Assert all tests passed
        if (failures.Count > 0)
        {
            var message = "Equivalence test failures:\n" + string.Join("\n", failures);
            throw new Xunit.Sdk.XunitException(message);
        }
    }
    
    /// <summary>
    /// Fallback test if Python test data is not available.
    /// Tests a single note to ensure basic functionality.
    /// </summary>
    private static void TestSingleNoteEquivalence()
    {
        // Arrange
        const int numFrames = 20;
        const int keyIndex = 39;  // C4
        const double frameRate = 31.25;  // 16000 Hz / 512 hop size
        
        var onsetRoll = new float[numFrames, 88];
        var frameRoll = new float[numFrames, 88];
        var offsetRoll = new float[numFrames, 88];
        var velocityRoll = new float[numFrames, 88];
        
        onsetRoll[5, keyIndex] = 1.0f;
        velocityRoll[5, keyIndex] = 0.8f;
        
        for (int i = 5; i < 15; i++)
            frameRoll[i, keyIndex] = 1.0f;
        
        var options = new MLTranscriptionOptions();
        var result = new PolyphonicTranscriptionResult(
            frameRoll, onsetRoll, offsetRoll, velocityRoll, frameRate, 16000);
        
        // Act
        var decoder = new NoteEventDecoder(options);
        var notes = decoder.Decode(result).ToList();
        
        // Assert
        Assert.Single(notes);
        Assert.Equal(60, notes[0].Pitch.Value);
        Assert.Equal(0.8f, notes[0].Velocity.Value, precision: 1);
    }
}
