# Visual Snapshot Testing Infrastructure

This infrastructure provides a reusable framework for visual regression testing of SVG rendering.

## Quick Start

### 1. Basic Snapshot Test

```csharp
public class MyTests : VisualSnapshotTestBase
{
    [Fact]
    public void MyGlyph_RendersCorrectly()
    {
        // Create SVG content
        var svg = SvgTestHelpers.CreateGlyphTestSvg(
            glyphPath: "M 0,0 L 10,10",
            x: 100,
            y: 100
        );

        // Assert it matches the snapshot
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }
}
```

### 2. Full Score Test

```csharp
[Fact]
public async Task MyScore_RendersCorrectly()
{
    var score = CreateMyScore();
    var exporter = new SvgScoreExporter();

    using var stream = new MemoryStream();
    await exporter.ExportAsync(score, stream);
    var svgContent = Encoding.UTF8.GetString(stream.ToArray());

    AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
}
```

## API Reference

### VisualSnapshotTestBase

Base class that provides snapshot testing functionality.

#### Methods

**`AssertMatchesSnapshot(string svgContent, SnapshotOptions? options = null, [CallerMemberName] string testName = "")`**

Renders SVG and compares against golden snapshot.

- **First run**: Saves golden image to `Snapshots/{testName}.png`
- **Subsequent runs**: Compares against golden with specified tolerance
- **On mismatch**: Saves actual and diff images to `Artifacts/` and fails test

### SnapshotOptions

Configuration for snapshot comparison behavior.

#### Presets

**`SnapshotOptions.Strict`** - Exact pixel matching
```csharp
Width = 200
Height = 200
PixelDifferenceThreshold = 0.0%
MaxPixelDelta = 0
GenerateDiffImage = true
```
Use for: Individual glyph tests where exact rendering is required.

**`SnapshotOptions.Default`** - Standard tolerance
```csharp
Width = 800
Height = 600
PixelDifferenceThreshold = 0.5%
MaxPixelDelta = 5
GenerateDiffImage = true
```
Use for: Full score rendering with minor anti-aliasing variations allowed.

**`SnapshotOptions.Relaxed`** - Permissive tolerance
```csharp
Width = 1200
Height = 800
PixelDifferenceThreshold = 1.0%
MaxPixelDelta = 10
GenerateDiffImage = true
```
Use for: Complex multi-staff scores where more variation is acceptable.

#### Custom Options

```csharp
var customOptions = new SnapshotOptions
{
    Width = 600,
    Height = 400,
    PixelDifferenceThreshold = 0.3,
    MaxPixelDelta = 3,
    GenerateDiffImage = true
};
```

### SvgTestHelpers

Utility methods for creating test SVG content.

#### Methods

**`CreateSvgWrapper(XElement content, int width, int height, string? viewBox = null)`**

Wraps content in a complete SVG document.

```csharp
var path = new XElement("path", new XAttribute("d", "M 0,0 L 10,10"));
var svg = SvgTestHelpers.CreateSvgWrapper(path, 200, 200);
```

**`CreateGlyphTestSvg(string glyphPath, double x, double y, double scale, bool fill)`**

Creates an SVG containing a single glyph at specified position.

```csharp
var svg = SvgTestHelpers.CreateGlyphTestSvg(
    glyphPath: "M 0,0 m -4,0 a 4,4 0 1,0 8,0",
    x: 100,
    y: 100,
    scale: 1.5,
    fill: true
);
```

**`CreateStaffTestSvg(int staffSpace, int width, int height, XElement? additionalContent)`**

Creates an SVG with a 5-line musical staff, optionally with symbols.

```csharp
var notehead = SvgTestHelpers.CreateNotehead(100, 90, filled: true);
var svg = SvgTestHelpers.CreateStaffTestSvg(
    staffSpace: 10,
    width: 400,
    height: 200,
    additionalContent: notehead
);
```

**`CreateNotehead(double x, double y, bool filled, double radius)`**

Creates a circle notehead element.

```csharp
var notehead = SvgTestHelpers.CreateNotehead(
    x: 100,
    y: 90,
    filled: true,
    radius: 4
);
```

**`CreateStem(double x, double yStart, double yEnd, double width)`**

Creates a stem line element.

```csharp
var stem = SvgTestHelpers.CreateStem(
    x: 104,
    yStart: 90,
    yEnd: 55,
    width: 1.2
);
```

## Best Practices

### Test Organization

1. **Glyph Tests**: Create separate test class for individual symbol rendering
   ```csharp
   public class GlyphSnapshotTests : VisualSnapshotTestBase
   ```

2. **Integration Tests**: Test full score exports in main test class
   ```csharp
   public class SvgExporterTests : VisualSnapshotTestBase
   ```

### Naming Conventions

- Test methods: `{Component}_{Scenario}_RendersCorrectly`
  - `QuarterNotehead_RendersCorrectly`
  - `Export_SimpleScale_RendersCorrectly`
  - `Staff_WithMultipleVoices_RendersCorrectly`

- Snapshot files are auto-named from test method: `{TestMethodName}.png`

### Choosing Tolerance

**Use Strict** when:
- Testing individual glyphs/symbols
- Exact pixel-perfect rendering is required
- Test content is simple and deterministic

**Use Default** when:
- Testing full score rendering
- Minor anti-aliasing variations are acceptable
- Cross-platform consistency may have small variations

**Use Relaxed** when:
- Testing very large/complex scores
- Multiple layout passes introduce accumulated variation
- Font rendering may differ slightly across environments

### Debugging Failed Tests

When a test fails with "Visual snapshot mismatch":

1. **Check the actual output**:
   ```
   Artifacts/{TestName}_actual.png
   ```
   Does it look correct? If yes, update the golden.

2. **Check the diff visualization**:
   ```
   Artifacts/{TestName}_diff.png
   ```
   Red pixels show differences. Is it just anti-aliasing or a real problem?

3. **Update golden if intentional**:
   ```bash
   rm Snapshots/{TestName}.png
   dotnet test --filter {TestName}
   # Review new snapshot
   git add Snapshots/{TestName}.png
   ```

### Platform Considerations

SkiaSharp should render consistently across platforms, but if you encounter issues:

- Use slightly higher tolerance thresholds
- Consider separate golden sets per platform (advanced)
- Verify SkiaSharp versions are consistent

## Implementation Details

### How It Works

1. **SVG → Bitmap**: Uses `Svg.Skia` to load SVG and `SKCanvas` to render to `SKBitmap`
2. **Pixel Comparison**: Iterates all pixels, calculates max color channel delta
3. **Diff Generation**: Creates red overlay on grayscale original for differences
4. **Tolerance Check**: Fails if `differencePercentage > threshold` OR `maxDelta > maxPixelDelta`

### File Locations

```
test/StaffSharp.Svg.Tests/
├── Infrastructure/
│   ├── VisualSnapshotTestBase.cs  # Core testing framework
│   ├── SvgTestHelpers.cs          # SVG creation utilities
│   └── README.md                  # This file
├── Snapshots/                      # Golden images (committed to git)
│   ├── QuarterNotehead_RendersCorrectly.png
│   ├── Export_EmptyScore_ProducesValidSvg.png
│   └── ...
└── Artifacts/                      # Failure artifacts (gitignored)
    ├── MyTest_actual.png
    ├── MyTest_diff.png
    └── ...
```

## Examples

See:
- `GlyphSnapshotTests.cs` - Individual glyph rendering tests
- `SvgExporterTests.cs` - Full score integration tests
