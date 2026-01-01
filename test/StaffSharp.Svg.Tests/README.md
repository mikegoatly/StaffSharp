# StaffSharp.Svg.Tests

Visual snapshot testing infrastructure for SVG rendering.

## Overview

This test project provides comprehensive visual regression testing for the SVG score exporter. It uses **SkiaSharp** (only - no ImageSharp dependency) to render SVG to PNG and compare against golden baseline images.

## Key Features

- **Fail-on-new-snapshot**: New snapshots fail the test, forcing explicit review and approval
- **Pixel-perfect comparison**: Configurable tolerance for different test scenarios
- **Diff visualization**: Red overlay showing exact pixel differences
- **Pure SkiaSharp**: No additional image processing dependencies needed
- **Reusable infrastructure**: Easy to add new snapshot tests

## Quick Start

### 1. Write a Snapshot Test

```csharp
public class MyTests : VisualSnapshotTestBase
{
    [Fact]
    public void MyFeature_RendersCorrectly()
    {
        var svg = CreateMySvgContent();
        AssertMatchesSnapshot(svg, SnapshotOptions.Default);
    }
}
```

### 2. Run the Test

First run will **fail** and create a golden image:

```
❌ FAILED: MyFeature_RendersCorrectly
⚠️ NEW SNAPSHOT: Golden image created at 'Snapshots/MyFeature_RendersCorrectly.png'.

Please:
1. Review the image visually
2. If correct, commit it to source control
3. Re-run the test
```

### 3. Review and Approve

Open `Snapshots/MyFeature_RendersCorrectly.png` and verify it looks correct.

### 4. Commit and Re-run

```bash
git add Snapshots/MyFeature_RendersCorrectly.png
git commit -m "Add snapshot for MyFeature"
dotnet test --filter MyFeature_RendersCorrectly  # Should pass now
```

## Test Types

### Glyph Tests (`GlyphSnapshotTests.cs`)

Test individual musical symbols in isolation:
- Noteheads (quarter, half, whole)
- Rests (various durations)
- Clefs, accidentals, etc.
- **Use**: `SnapshotOptions.Strict` (exact pixel matching)

### Integration Tests (`SvgExporterTests.cs`)

Test full score rendering end-to-end:
- Complete NotationScore → SVG pipeline
- Multiple measures, voices, staves
- Layout and spacing
- **Use**: `SnapshotOptions.Default` or `Relaxed`

## Snapshot Options

| Option | Width | Height | Pixel % | Max Delta | Use Case |
|--------|-------|--------|---------|-----------|----------|
| `Strict` | 200 | 200 | 0.0% | 0 | Individual glyphs |
| `Default` | 800 | 600 | 0.5% | 5 | Full scores |
| `Relaxed` | 1200 | 800 | 1.0% | 10 | Complex multi-staff |

## Helpers

### SvgTestHelpers

Utilities for creating test SVG content:

```csharp
// Create a glyph in isolation
var svg = SvgTestHelpers.CreateGlyphTestSvg(
    glyphPath: "M 0,0 L 10,10",
    x: 100, y: 100
);

// Create a staff with symbols
var note = SvgTestHelpers.CreateNotehead(100, 90, filled: true);
var svg = SvgTestHelpers.CreateStaffTestSvg(
    staffSpace: 10,
    additionalContent: note
);
```

## Directory Structure

```
StaffSharp.Svg.Tests/
├── Infrastructure/
│   ├── VisualSnapshotTestBase.cs    # Core testing framework
│   ├── SvgTestHelpers.cs            # SVG creation helpers
│   └── README.md                    # Detailed API docs
├── Snapshots/                        # Golden images (committed)
│   ├── QuarterNotehead_RendersCorrectly.png
│   └── ...
├── Artifacts/                        # Test failures (gitignored)
│   ├── MyTest_actual.png
│   ├── MyTest_diff.png
│   └── ...
├── GlyphSnapshotTests.cs            # Individual symbol tests
├── SvgExporterTests.cs              # Full score integration tests
└── README.md                        # This file
```

## When Tests Fail

### New Snapshot

```
⚠️ NEW SNAPSHOT: Golden image created at 'Snapshots/Test.png'.
```
**Action**: Review image, commit if correct, re-run test.

### Dimension Mismatch

```
❌ DIMENSION MISMATCH:
Golden: 800x600
Actual: 1000x600
```
**Action**: Check if dimensions changed intentionally. Update test options or fix code.

### Pixel Mismatch

```
❌ SNAPSHOT MISMATCH:
  Different pixels: 1,523 / 480,000 (0.32%)
  Max pixel delta: 12 (threshold: 5)

Files saved:
  Actual:  Artifacts/Test_actual.png
  Diff:    Artifacts/Test_diff.png
```
**Action**:
1. Open `Artifacts/Test_diff.png` to see differences (red pixels)
2. Open `Artifacts/Test_actual.png` to see new rendering
3. If correct: Delete golden and re-run to update
4. If incorrect: Fix rendering code

## CI/CD Considerations

- **Golden images must be committed** to source control
- Tests will fail if rendering changes unintentionally
- SkiaSharp should render consistently across platforms
- If platform differences occur, consider slightly higher tolerance

## Examples

See:
- **`GlyphSnapshotTests.cs`** - How to test individual symbols
- **`SvgExporterTests.cs`** - How to test full score export
- **`Infrastructure/README.md`** - Complete API documentation
- **`Snapshots/README.md`** - Snapshot management workflow

## Dependencies

- **SkiaSharp** - SVG rendering to bitmap
- **Svg.Skia** - SVG parsing and rendering engine
- **xUnit** - Test framework

No ImageSharp or other image processing libraries required!
