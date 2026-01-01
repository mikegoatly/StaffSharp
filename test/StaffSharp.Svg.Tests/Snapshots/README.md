# Visual Snapshots

This directory contains golden baseline images for visual regression testing.

## How It Works

1. **First Run**: When a snapshot test runs for the first time:
   - Generates a PNG image and saves it to `Snapshots/{testName}.png`
   - **⚠️ TEST FAILS** with message to review the image
   - This ensures you explicitly review and approve new snapshots

2. **Subsequent Runs**: The test renders the SVG again and compares pixel-by-pixel against the golden image.

3. **Mismatch**: If differences exceed the tolerance threshold, the test fails and saves:
   - `Artifacts/{testName}_actual.png` - The newly rendered image
   - `Artifacts/{testName}_diff.png` - A visualization showing differences in red

## Workflow for New Snapshots

When you add a new snapshot test:

1. **Run the test** - it will FAIL on first run
2. **Review** the generated image in `Snapshots/{testName}.png`
   - Open the PNG file
   - Verify it looks visually correct
   - Check that all elements are positioned properly
3. **If correct**: Commit the snapshot image to git
4. **Re-run the test** - should now pass
5. **If incorrect**: Fix the rendering code, delete the snapshot, and repeat

## Updating Snapshots

If you intentionally change rendering behavior:

1. **Delete** the old snapshot: `rm Snapshots/{testName}.png`
2. **Run** the test - it will fail and regenerate the snapshot
3. **Review** the new image carefully
4. **Verify** the changes are what you expected
5. **Commit** the updated snapshot
6. **Re-run** the test - should now pass

## Snapshot Options

Tests can use different tolerance levels:

- **`SnapshotOptions.Strict`**: Exact pixel match (for glyphs)
  - 0% pixel difference allowed
  - 0 max delta per color channel
  - Used for: Individual glyph rendering tests

- **`SnapshotOptions.Default`**: Slight variation allowed (for scores)
  - 0.5% pixel difference threshold
  - Max delta of 5 per color channel
  - Used for: Full score rendering tests

- **`SnapshotOptions.Relaxed`**: More variation allowed (for complex scores)
  - 1.0% pixel difference threshold
  - Max delta of 10 per color channel
  - Used for: Large, complex multi-staff scores

## Troubleshooting

**Test fails with "Visual snapshot mismatch":**
1. Check `Artifacts/{testName}_actual.png` - is this what you expected?
2. Check `Artifacts/{testName}_diff.png` - where are the differences?
3. If the actual is correct, delete the old golden and re-run to update
4. If the actual is wrong, fix the rendering code

**Golden images look different on CI vs local:**
- Ensure SkiaSharp renders consistently across platforms
- May need to adjust pixel difference thresholds
- Consider using `SnapshotOptions.Relaxed` for platform-sensitive tests
