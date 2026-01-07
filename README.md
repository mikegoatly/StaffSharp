# StaffSharp

A music notation library for .NET that converts between musical formats - audio, MIDI, MusicXML, ABC notation, and SVG scores.

## What's this?

StaffSharp transforms music from one representation to another. The original goal was converting audio recordings (WAV files) into readable sheet music (SVG), but the architecture supports any-to-any conversion:

- **Audio** → MIDI, SVG
- **MusicXML** → SVG, MIDI, ABC
- **ABC notation** → SVG, MIDI
- **MIDI** → (planned)

## Early days

This is a very early stage project. The core architecture is in place with dual intermediate representations (performance timeline and notation score), but many features are incomplete or experimental. Expect breaking changes, missing functionality, and rough edges.

What's working:
- Monophonic audio analysis (Using pYIN, though there is a YIN implementation in there)
- MusicXML import
- ABC import
- MIDI export
- Basic score structures
- Score validation - the notation layer has validation logic that verifies it is structurally sound
- Rendering to SVG (Definitely a WIP!)  
  <svg viewBox="0 0 441 110" width="441" height="110" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <path id="trebleClef" d="M 399.0 -266.0 C 408.0 -267.0 418.0 -268.0 428.0 -268.0 C 497.0 -268.0 572.0 -240.0 616.0 -188.0 C 667.0 -133.0 679.0 -68.0 679.0 3.0 C 679.0 96.0 642.0 169.0 555.0 211.0 C 518.0 230.0 488.0 245.0 446.0 249.0 L 462.0 436.0 C 463.0 452.0 464.0 467.0 464.0 482.0 C 464.0 585.0 424.0 639.0 316.0 639.0 C 204.0 639.0 139.0 570.0 139.0 486.0 C 139.0 429.0 184.0 378.0 253.0 378.0 C 315.0 378.0 365.0 432.0 365.0 491.0 C 365.0 550.0 317.0 584.0 255.0 585.0 C 275.0 596.0 298.0 600.0 324.0 600.0 C 375.0 600.0 425.0 573.0 425.0 472.0 C 425.0 463.0 425.0 454.0 424.0 444.0 L 407.0 254.0 C 396.0 255.0 379.0 255.0 368.0 255.0 C 136.0 255.0 0.0 131.0 0.0 -100.0 C 0.0 -188.0 25.0 -277.0 57.0 -334.0 C 83.0 -385.0 164.0 -465.0 207.0 -499.0 C 231.0 -519.0 278.0 -563.0 329.0 -601.0 C 321.0 -705.0 311.0 -787.0 311.0 -852.0 C 311.0 -1052.0 410.0 -1144.0 503.0 -1172.0 C 588.0 -1080.0 592.0 -1005.0 592.0 -873.0 C 592.0 -688.0 539.0 -598.0 380.0 -473.0 L 399.0 -266.0 M 411.0 -142.0 L 442.0 212.0 C 579.0 181.0 613.0 104.0 613.0 28.0 C 613.0 -54.0 533.0 -142.0 425.0 -142.0 C 420.0 -142.0 416.0 -142.0 411.0 -142.0 M 404.0 212.0 L 372.0 -138.0 C 303.0 -125.0 246.0 -83.0 246.0 -16.0 C 246.0 16.0 267.0 55.0 293.0 87.0 C 223.0 70.0 192.0 -1.0 192.0 -73.0 C 192.0 -172.0 270.0 -236.0 361.0 -261.0 L 344.0 -450.0 C 164.0 -306.0 93.0 -226.0 92.0 -82.0 C 95.0 86.0 166.0 216.0 400.0 216.0 C 404.0 216.0 404.0 214.0 404.0 212.0 M 363.0 -822.0 C 360.0 -797.0 359.0 -769.0 359.0 -743.0 C 359.0 -690.0 363.0 -641.0 366.0 -626.0 C 447.0 -681.0 551.0 -785.0 551.0 -890.0 C 551.0 -936.0 539.0 -1003.0 501.0 -1025.0 C 415.0 -1015.0 374.0 -910.0 363.0 -822.0" />
    <path id="sharp" d="M 82.0 -77.0 L 82.0 103.0 L 184.0 75.0 L 184.0 -106.0 L 82.0 -77.0 M 82.0 369.0 L 57.0 369.0 L 57.0 194.0 L 0.0 211.0 L 0.0 126.0 L 57.0 112.0 L 57.0 -71.0 L 0.0 -55.0 L 0.0 -142.0 L 57.0 -156.0 L 57.0 -334.0 L 82.0 -334.0 L 82.0 -164.0 L 184.0 -190.0 L 184.0 -373.0 L 208.0 -373.0 L 208.0 -198.0 L 268.0 -213.0 L 268.0 -130.0 L 208.0 -112.0 L 208.0 68.0 L 268.0 53.0 L 268.0 136.0 L 208.0 151.0 L 208.0 328.0 L 184.0 328.0 L 184.0 158.0 L 82.0 187.0 L 82.0 369.0" />
    <path id="commonTime" d="M 303.0 -164.0 C 311.0 -163.0 315.0 -162.0 321.0 -161.0 C 324.0 -165.0 325.0 -170.0 325.0 -174.0 C 325.0 -202.0 273.0 -228.0 234.0 -228.0 C 173.0 -226.0 119.0 -170.0 119.0 -18.0 C 119.0 58.0 126.0 133.0 158.0 175.0 C 181.0 204.0 207.0 217.0 239.0 217.0 C 265.0 217.0 294.0 207.0 322.0 183.0 C 350.0 159.0 369.0 119.0 392.0 71.0 C 392.0 74.0 410.0 77.0 409.0 80.0 C 376.0 183.0 333.0 244.0 211.0 246.0 C 161.0 246.0 111.0 226.0 73.0 189.0 C 34.0 151.0 13.0 98.0 10.0 30.0 C 10.0 26.0 9.0 -13.0 9.0 -17.0 C 9.0 -185.0 97.0 -248.0 228.0 -249.0 C 280.0 -249.0 325.0 -222.0 345.0 -199.0 C 365.0 -176.0 379.0 -150.0 379.0 -124.0 C 379.0 -77.0 354.0 -30.0 315.0 -30.0 C 270.0 -30.0 242.0 -69.0 242.0 -104.0 C 244.0 -130.0 265.0 -164.0 302.0 -164.0 L 303.0 -164.0" />
    <path id="noteHeadBlack" d="M 0.0 33.0 C 0.0 -55.0 77.0 -109.0 165.0 -132.0 C 182.0 -136.0 197.0 -139.0 212.0 -139.0 C 276.0 -139.0 330.0 -103.0 330.0 -33.0 C 330.0 56.0 255.0 106.0 165.0 132.0 C 147.0 138.0 131.0 140.0 114.0 140.0 C 52.0 140.0 0.0 103.0 0.0 33.0" />
  </defs>
  <rect width="441" height="110" fill="white" />
  <g class="system" transform="translate(0,40)">
    <g class="staff" transform="translate(0,0)">
      <line x1="40" y1="0" x2="401" y2="0" stroke="black" stroke-width="1" />
      <line x1="40" y1="10" x2="401" y2="10" stroke="black" stroke-width="1" />
      <line x1="40" y1="20" x2="401" y2="20" stroke="black" stroke-width="1" />
      <line x1="40" y1="30" x2="401" y2="30" stroke="black" stroke-width="1" />
      <line x1="40" y1="40" x2="401" y2="40" stroke="black" stroke-width="1" />
      <g class="clef" transform="translate(45,30)">
        <use href="#trebleClef" fill="black" transform="scale(0.022087244616234125)" />
      </g>
      <g class="key-signature" transform="translate(72,0)">
        <use href="#sharp" fill="black" transform="translate(0,0) scale(0.026954177897574125)" />
        <use href="#sharp" fill="black" transform="translate(7,15) scale(0.026954177897574125)" />
        <use href="#sharp" fill="black" transform="translate(14,-5) scale(0.026954177897574125)" />
        <use href="#sharp" fill="black" transform="translate(21,10) scale(0.026954177897574125)" />
      </g>
      <g class="time-signature" transform="translate(113,0)">
        <use href="#commonTime" fill="black" transform="translate(0,20) scale(0.020202020202020204)" />
      </g>
      <g class="note" transform="translate(173.5,40)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-51.66666666666667" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(203.5,20)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-35" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(233.5,20)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-38.333333333333336" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(263.5,25)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-46.66666666666667" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(296,20)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="1" y1="0" x2="1" y2="35" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(333.5,40)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-45" stroke="black" stroke-width="1.5" />
      </g>
      <g class="note" transform="translate(363.5,20)">
        <use href="#noteHeadBlack" fill="black" transform="scale(0.035842293906810034)" />
        <line x1="11" y1="0" x2="11" y2="-35" stroke="black" stroke-width="1.5" />
      </g>
      <g>
        <line x1="139.5" y1="0" x2="139.5" y2="40" stroke="black" stroke-width="6" />
        <line x1="142.5" y1="0" x2="142.5" y2="40" stroke="black" stroke-width="2" />
        <circle cx="146.5" cy="25" r="1.25" fill="black" />
        <circle cx="146.5" cy="15" r="1.25" fill="black" />
      </g>
      <g>
        <line x1="386" y1="0" x2="386" y2="40" stroke="black" stroke-width="2" />
      </g>
      <g class="beam-group">
        <polygon points="184.5,-11.666666666666668 274.5,-21.666666666666668 274.5,-16.666666666666668 184.5,-6.666666666666668" fill="black" />
      </g>
      <g class="beam-group">
        <polygon points="344.5,-5 374.5,-15 374.5,-10 344.5,0" fill="black" />
      </g>
    </g>
  </g>
</svg>

What's not:
- Polyphonic audio analysis
- ABC export
- MIDI import
- MusicXML export
- Rendering decorations (trills, etc.) is very ropey
- Realistically even the things above you should expect bugs in!

## Structure

The codebase is organized into focused libraries:

- **StaffSharp.Core** - Core types (`NoteEvent`, `Frequency`, `Rational` timing)
- **StaffSharp.Audio** - DSP pipeline for audio analysis (pitch/onset detection)
- **StaffSharp.MusicXml** - MusicXML parsing (Exporting TODO)
- **StaffSharp.Abc** - ABC parsing (Exporting TODO)
- **StaffSharp.Midi** - MIDI file generation (Parsing TODO)
- **StaffSharp.Svg** - SVG score rendering (very much WIP)
- **StaffSharp.Cli** - Command-line interface

## Architecture

StaffSharp uses two intermediate representations:

1. **Performance Timeline (IR1)** - Flat list of note events with rational timing. Used for audio sources.
2. **Notation Score (IR2)** - Hierarchical structure (parts/voices/measures) with symbolic durations. Required for all outputs.

Most importers go directly to IR2. Audio goes through IR1 first for analysis, then converts to IR2.

See [docs/architecture.md](docs/architecture.md) for details.

