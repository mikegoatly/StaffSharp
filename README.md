# StaffSharp

A music notation library for .NET that converts between musical formats - audio, MIDI, MusicXML, ABC notation, and SVG scores.

## What's this?

StaffSharp transforms music from one representation to another. The original goal was converting audio recordings (WAV files) into readable sheet music (SVG), but the architecture supports any-to-any conversion:

- **Audio** → MIDI, SVG, ABC
- **MusicXML** → SVG, MIDI, ABC
- **ABC notation** → SVG, MIDI
- **MIDI** → (planned)

## Early days

This is a very early stage project. The core architecture is in place with dual intermediate representations (performance timeline and notation score), but many features are incomplete or experimental. Expect breaking changes, missing functionality, and rough edges.

What's working:
- Monophonic audio analysis (Using pYIN, though there is a YIN implementation in there)
- Polyphonic audio analysis (Using a deep learning model trained with StaffSharp)
- MusicXML import
- ABC import and export
- MIDI export
- Basic score structures
- Score validation - the notation layer has validation logic that verifies it is structurally sound
- Rendering to SVG (Definitely a WIP!)  
  ![Sample SVG output](docs/images/sample.svg)

What's not:
- MIDI import
- MusicXML export

## Structure

The codebase is organized into focused libraries:

- **StaffSharp.Core** - Core types and utilities
- **StaffSharp.Audio** - DSP pipeline for monophonic audio analysis using algorithmic analysis (YIN/pYIN)
- **StaffSharp.MachineLearning** - a deep learning model for polyphonic pitch detection
- **StaffSharp.MusicXml** - MusicXML parsing and exporting
- **StaffSharp.Abc** - ABC parsing and exporting
- **StaffSharp.Json** - JSON parsing and exporting of intermediate representations
- **StaffSharp.Midi** - MIDI file generation
- **StaffSharp.Svg** - SVG score rendering
- **StaffSharp.Synthesis** - Simple audio synthesis from score outputs
- **StaffSharp.Cli** - Command-line interface

Rendering
- **StaffSharp.Avalonia** - An Avalonia-based viewer application

## Architecture

StaffSharp uses two intermediate representations:

1. **Performance Timeline (IR1)** - Flat list of note events with rational timing. Used for audio sources.
2. **Notation Score (IR2)** - Hierarchical structure (parts/voices/measures) with symbolic durations. Required for all outputs.

Most importers go directly to IR2. Audio goes through IR1 first for analysis, then converts to IR2.

See [docs/architecture.md](docs/architecture.md) for details.

