using StaffSharp.Notation;

namespace StaffSharp.Core.Tests.Notation;

/// <summary>
/// Tests for Part class, focusing on backward compatibility and grand staff support.
/// </summary>
public sealed class PartTests
{
    [Fact]
    public void Part_LegacyConstructor_CreatesStaffCorrectly()
    {
        // Arrange
        var voices = new List<Voice>
        {
            new Voice(1, []),
            new Voice(2, [])
        };

        // Act
        var part = new Part("Violin", Clef.Treble, voices);

        // Assert
        Assert.Equal("Violin", part.Name);
        Assert.Single(part.Staves);
        Assert.Equal(1, part.Staves[0].Number);
        Assert.Equal(Clef.Treble, part.Staves[0].Clef);
        Assert.Equal(2, part.Staves[0].Voices.Count);
        Assert.False(part.IsGrandStaff);
    }

    [Fact]
    public void Part_LegacyConstructor_BackwardCompatibleProperties()
    {
        // Arrange
        var voices = new List<Voice>
        {
            new Voice(1, []),
            new Voice(2, [])
        };

        // Act
        var part = new Part("Cello", Clef.Bass, voices);

        // Assert - backward compatible properties
        Assert.Equal(Clef.Bass, part.Clef); // Should return first staff's clef
        Assert.Equal(2, part.Voices.Count); // Should return all voices
        Assert.Same(voices[0], part.Voices[0]);
        Assert.Same(voices[1], part.Voices[1]);
    }

    [Fact]
    public void Part_MultiStaffConstructor_CreatesGrandStaff()
    {
        // Arrange
        var trebleVoices = new List<Voice>
        {
            new Voice(1, [])
        };
        var bassVoices = new List<Voice>
        {
            new Voice(1, [])
        };

        var staves = new List<Staff>
        {
            new Staff(1, Clef.Treble, trebleVoices),
            new Staff(2, Clef.Bass, bassVoices)
        };

        // Act
        var part = new Part("Piano", staves);

        // Assert
        Assert.Equal("Piano", part.Name);
        Assert.True(part.IsGrandStaff);
        Assert.Equal(2, part.Staves.Count);
        Assert.Equal(Clef.Treble, part.Staves[0].Clef);
        Assert.Equal(Clef.Bass, part.Staves[1].Clef);
    }

    [Fact]
    public void Part_GrandStaff_BackwardCompatiblePropertiesReturnFirstStaffData()
    {
        // Arrange
        var trebleVoices = new List<Voice>
        {
            new Voice(1, []),
            new Voice(2, [])
        };
        var bassVoices = new List<Voice>
        {
            new Voice(1, [])
        };

        var staves = new List<Staff>
        {
            new Staff(1, Clef.Treble, trebleVoices),
            new Staff(2, Clef.Bass, bassVoices)
        };

        // Act
        var part = new Part("Piano", staves);

        // Assert - backward compatible properties
        Assert.Equal(Clef.Treble, part.Clef); // Returns first staff's clef
        Assert.Equal(3, part.Voices.Count); // Returns all voices from all staves (2 + 1)
    }

    [Fact]
    public void Part_MultiStaffConstructor_ThrowsWhenEmptyStaves()
    {
        // Arrange
        var emptyStaves = new List<Staff>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Part("Invalid", emptyStaves));
        Assert.Contains("at least one staff", exception.Message);
    }

    [Fact]
    public void Part_MultiStaffConstructor_ThrowsWhenNullStaves()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Part("Invalid", (IReadOnlyList<Staff>)null!));
        Assert.Contains("at least one staff", exception.Message);
    }

    [Fact]
    public void Staff_Constructor_ValidatesStaffNumber()
    {
        // Arrange
        var voices = new List<Voice> { new Voice(1, []) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Staff(0, Clef.Treble, voices));
        Assert.Contains("must be >= 1", exception.Message);
    }

    [Fact]
    public void Staff_Constructor_ThrowsWhenNullVoices()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Staff(1, Clef.Treble, null!));
    }

    [Fact]
    public void Staff_Constructor_CreatesStaffCorrectly()
    {
        // Arrange
        var voices = new List<Voice>
        {
            new Voice(1, []),
            new Voice(2, [])
        };

        // Act
        var staff = new Staff(1, Clef.Alto, voices);

        // Assert
        Assert.Equal(1, staff.Number);
        Assert.Equal(Clef.Alto, staff.Clef);
        Assert.Equal(2, staff.Voices.Count);
        Assert.Same(voices, staff.Voices);
    }
}
