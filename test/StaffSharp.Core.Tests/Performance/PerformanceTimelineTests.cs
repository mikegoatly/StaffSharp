using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Tests.Performance;

public class PerformanceTimelineTests
{
    private static TempoMap CreateSimpleTempoMap()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        return new TempoMap(tempos, timeSigs);
    }

    [Fact]
    public void PerformanceTimeline_SortsEventsByOnset()
    {
        var tempoMap = CreateSimpleTempoMap();

        // Create events out of order
        var events = SymbolicNoteEventBuilder.Create()
            .WithVelocity(Velocity.Create(0.8f))
            .WithDuration(1, 1)  // Whole note
            .AddNoteAt(Rational.Create(4, 1), MidiNote.Create(64))  // E4 at beat 4
            .AddNoteAt(Rational.Zero, MidiNote.C4)                  // C4 at beat 0
            .AddNoteAt(Rational.Create(2, 1), MidiNote.Create(62))  // D4 at beat 2
            .Build();

        var timeline = new PerformanceTimeline(tempoMap, events);

        Assert.Equal(Rational.Zero, timeline.Events[0].OnsetBeats);
        Assert.Equal(Rational.Create(2, 1), timeline.Events[1].OnsetBeats);
        Assert.Equal(Rational.Create(4, 1), timeline.Events[2].OnsetBeats);
    }

    [Fact]
    public void EventsInRange_ReturnsEventsWithinRange()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = SymbolicNoteEventBuilder.Create()
            .WithVelocity(Velocity.Create(0.8f))
            .WithDuration(1, 1)  // Whole note
            .AddNoteAt(Rational.Create(0, 1), MidiNote.C4)          // C4 at beat 0
            .AddNoteAt(Rational.Create(2, 1), MidiNote.Create(62))  // D4 at beat 2
            .AddNoteAt(Rational.Create(4, 1), MidiNote.E4)          // E4 at beat 4
            .AddNoteAt(Rational.Create(6, 1), MidiNote.Create(65))  // F4 at beat 6
            .Build();

        var timeline = new PerformanceTimeline(tempoMap, events);

        var inRange = timeline.EventsInRange(Rational.Create(2, 1), Rational.Create(5, 1)).ToList();

        Assert.Equal(2, inRange.Count);
        Assert.Equal(Rational.Create(2, 1), inRange[0].OnsetBeats);
        Assert.Equal(Rational.Create(4, 1), inRange[1].OnsetBeats);
    }

    [Fact]
    public void EventsAt_ReturnsActiveEvents()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = SymbolicNoteEventBuilder.Create()
            .WithVelocity(Velocity.Create(0.8f))
            // Event from beat 0 to 2 (whole note = 2 beats)
            .AddNoteAt(Rational.Create(0, 1), MidiNote.C4, Rational.Create(2, 1))
            // Event from beat 1 to 3 (whole note = 2 beats)
            .AddNoteAt(Rational.Create(1, 1), MidiNote.Create(62), Rational.Create(2, 1))
            // Event from beat 4 to 5 (whole note = 1 beat)
            .AddNoteAt(Rational.Create(4, 1), MidiNote.E4, Rational.Create(1, 1))
            .Build();

        var timeline = new PerformanceTimeline(tempoMap, events);

        // At beat 1.5, first two events should be active
        var activeAt1_5 = timeline.EventsAt(Rational.Create(3, 2)).ToList();
        Assert.Equal(2, activeAt1_5.Count);

        // At beat 4.5, only third event should be active
        var activeAt4_5 = timeline.EventsAt(Rational.Create(9, 2)).ToList();
        Assert.Single(activeAt4_5);
        Assert.Equal(Rational.Create(4, 1), activeAt4_5[0].OnsetBeats);
    }

    [Fact]
    public void TotalDurationBeats_ReturnsLatestOffset()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = SymbolicNoteEventBuilder.Create()
            .WithVelocity(Velocity.Create(0.8f))
            .AddNoteAt(Rational.Create(0, 1), MidiNote.C4, Rational.Create(2, 1))   // 2 beats duration
            .AddNoteAt(Rational.Create(2, 1), MidiNote.Create(62), Rational.Create(3, 1))  // 3 beats duration, ends at beat 5
            .Build();

        var timeline = new PerformanceTimeline(tempoMap, events);

        Assert.Equal(Rational.Create(5, 1), timeline.TotalDurationBeats);
    }

    [Fact]
    public void TotalDurationSeconds_ConvertsCorrectly()
    {
        var tempoMap = CreateSimpleTempoMap(); // 120 BPM = 2 beats per second

        var events = SymbolicNoteEventBuilder.Create()
            .WithVelocity(Velocity.Create(0.8f))
            .AddNoteAt(Rational.Create(0, 1), MidiNote.C4, Rational.Create(4, 1))  // 4 beats
            .Build();

        var timeline = new PerformanceTimeline(tempoMap, events);

        // 4 beats at 120 BPM = 2 seconds
        Assert.Equal(2.0, timeline.TotalDurationSeconds, precision: 5);
    }

    [Fact]
    public void PerformanceTimeline_EmptyEvents_HasZeroDuration()
    {
        var tempoMap = CreateSimpleTempoMap();
        var timeline = new PerformanceTimeline(tempoMap, Array.Empty<IPerformanceEvent>());

        Assert.Equal(Rational.Zero, timeline.TotalDurationBeats);
        Assert.Equal(0.0, timeline.TotalDurationSeconds);
    }
}
