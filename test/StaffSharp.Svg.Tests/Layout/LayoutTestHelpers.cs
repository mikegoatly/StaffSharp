namespace StaffSharp.Svg.Tests.Layout;

using System;

using StaffSharp.Notation;
using StaffSharp.Svg.Layout;

/// <summary>
/// Helper methods for creating layout objects in tests.
/// </summary>
public static class LayoutTestHelpers
{
    /// <summary>
    /// Creates a NoteLayoutSymbol with basic default values.
    /// </summary>
    public static NoteLayoutSymbol CreateNoteSymbol(
        PitchClass pitchClass = PitchClass.C,
        int octave = 4,
        SymbolicDuration? duration = null,
        double x = 10.0,
        double y = 50.0,
        double width = 5.0,
        double height = 10.0)
    {
        return new NoteLayoutSymbol
        {
            Note = new NotationNote(new Pitch(pitchClass, octave), duration ?? SymbolicDuration.Quarter),
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a ChordLayoutSymbol with basic default values.
    /// </summary>
    public static ChordLayoutSymbol CreateChordSymbol(
        Pitch[] notes,
        SymbolicDuration duration,
        double x = 10.0,
        double y = 50.0,
        double width = 5.0,
        double height = 10.0)
    {
        return new ChordLayoutSymbol
        {
            Chord = new Chord(notes, duration),
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a LayoutStaff with basic default values.
    /// </summary>
    public static LayoutStaff CreateStaff(
        double x = 0.0,
        double y = 0.0,
        double width = 100.0,
        double height = 40.0)
    {
        return new LayoutStaff
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a LayoutMeasure with basic default values.
    /// </summary>
    public static LayoutMeasure CreateMeasure(
        double x = 0.0,
        double y = 0.0,
        double width = 100.0,
        double height = 40.0)
    {
        return new LayoutMeasure
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a LayoutSystem with basic default values.
    /// </summary>
    public static LayoutSystem CreateSystem(
        double x = 0.0,
        double y = 0.0,
        double width = 100.0,
        double height = 100.0)
    {
        return new LayoutSystem
        {
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a RestLayoutSymbol with basic default values.
    /// </summary>
    public static LayoutSymbol CreateRestSymbol(
        SymbolicDuration quarter, 
        double x, 
        double y,
        double width = 5.0,
        double height = 10.0)
    {
        return new RestLayoutSymbol
        {
            Rest = new Rest(quarter),
            Y = y,
            X = x,
            Width = width,
            Height = height
        };
    }
}
