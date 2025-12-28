# StaffSharp

StaffSharp is a musical transformation library for .NET. It takes music in one form, transforms it to an internal representation, then outputs it in a different format.

The initial use-case was just WAV -> SVG Score, but it can ultimately take any musical format to any other, e.g.

WAV -> MIDI
ABC -> MIDI
ABC -> SVG
etc.

## Pipeline processing

We will define the pipeline as:

```cs
var pipeline = new ScorePipelineBuilder()
    .FromAudio(audioBuffer) // Audio input
    .WithPitchDetector(new AdaptivePitchDetector()) // What DSP algorithm should be applied
    .WithTempoHint(120) // Tempo hint; otherwise will be inferred.
    .WithQuantization(subdivision: 4) // How notes should be aligned
    .Build();
```

