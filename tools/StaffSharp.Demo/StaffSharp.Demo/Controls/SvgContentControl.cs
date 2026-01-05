using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Svg.Skia;

namespace StaffSharp.Demo.Controls;

/// <summary>
/// A control that renders SVG content from a string.
/// </summary>
public sealed class SvgContentControl : Control, IDisposable
{
    private SKSvg? _svg;
    private SKPicture? _picture;
    private WriteableBitmap? _bitmap;

    public static readonly StyledProperty<string?> SvgContentProperty =
        AvaloniaProperty.Register<SvgContentControl, string?>(nameof(SvgContent));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<SvgContentControl, Stretch>(nameof(Stretch), Stretch.Uniform);

    public string? SvgContent
    {
        get => GetValue(SvgContentProperty);
        set => SetValue(SvgContentProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    static SvgContentControl()
    {
        AffectsRender<SvgContentControl>(SvgContentProperty, StretchProperty);
        AffectsMeasure<SvgContentControl>(SvgContentProperty, StretchProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SvgContentProperty)
        {
            LoadSvg();
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    private void LoadSvg()
    {
        _picture?.Dispose();
        _svg?.Dispose();
        _bitmap?.Dispose();
        _picture = null;
        _svg = null;
        _bitmap = null;

        var content = SvgContent;
        if (string.IsNullOrEmpty(content))
            return;

        try
        {
            _svg = new SKSvg();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            _picture = _svg.Load(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load SVG: {ex.Message}");
            _svg?.Dispose();
            _svg = null;
            _picture = null;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_picture == null)
        {
            return base.MeasureOverride(availableSize);
        }

        var bounds = _picture.CullRect;
        var pictureSize = new Size(bounds.Width, bounds.Height);

        if (double.IsInfinity(availableSize.Width) && double.IsInfinity(availableSize.Height))
        {
            return pictureSize;
        }

        return CalculateScaledSize(pictureSize, availableSize, Stretch);
    }

    private static Size CalculateScaledSize(Size pictureSize, Size availableSize, Stretch stretch)
    {
        if (pictureSize.Width <= 0 || pictureSize.Height <= 0)
            return new Size(0, 0);

        var scaleX = double.IsInfinity(availableSize.Width) ? 1.0 : availableSize.Width / pictureSize.Width;
        var scaleY = double.IsInfinity(availableSize.Height) ? 1.0 : availableSize.Height / pictureSize.Height;

        switch (stretch)
        {
            case Stretch.None:
                return pictureSize;
            case Stretch.Fill:
                return new Size(
                    double.IsInfinity(availableSize.Width) ? pictureSize.Width : availableSize.Width,
                    double.IsInfinity(availableSize.Height) ? pictureSize.Height : availableSize.Height);
            case Stretch.Uniform:
                var uniformScale = Math.Min(scaleX, scaleY);
                return new Size(pictureSize.Width * uniformScale, pictureSize.Height * uniformScale);
            case Stretch.UniformToFill:
                var fillScale = Math.Max(scaleX, scaleY);
                return new Size(pictureSize.Width * fillScale, pictureSize.Height * fillScale);
            default:
                return pictureSize;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_picture == null)
            return;

        var bounds = _picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var controlSize = Bounds.Size;
        if (controlSize.Width <= 0 || controlSize.Height <= 0)
            return;

        // Render SVG to bitmap if needed
        var targetWidth = (int)controlSize.Width;
        var targetHeight = (int)controlSize.Height;

        if (_bitmap == null || _bitmap.PixelSize.Width != targetWidth || _bitmap.PixelSize.Height != targetHeight)
        {
            _bitmap?.Dispose();
            _bitmap = RenderToBitmap(targetWidth, targetHeight);
        }

        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, new Rect(0, 0, controlSize.Width, controlSize.Height));
        }
    }

    private WriteableBitmap? RenderToBitmap(int width, int height)
    {
        if (_picture == null || width <= 0 || height <= 0)
            return null;

        var bounds = _picture.CullRect;
        var pictureWidth = bounds.Width;
        var pictureHeight = bounds.Height;

        if (pictureWidth <= 0 || pictureHeight <= 0)
            return null;

        // Calculate scale based on Stretch
        float scaleX, scaleY, offsetX = 0, offsetY = 0;

        switch (Stretch)
        {
            case Stretch.None:
                scaleX = scaleY = 1.0f;
                offsetX = (width - pictureWidth) / 2;
                offsetY = (height - pictureHeight) / 2;
                break;
            case Stretch.Fill:
                scaleX = width / pictureWidth;
                scaleY = height / pictureHeight;
                break;
            case Stretch.Uniform:
                var uniformScale = Math.Min(width / pictureWidth, height / pictureHeight);
                scaleX = scaleY = uniformScale;
                offsetX = (width - pictureWidth * uniformScale) / 2;
                offsetY = (height - pictureHeight * uniformScale) / 2;
                break;
            case Stretch.UniformToFill:
                var fillScale = Math.Max(width / pictureWidth, height / pictureHeight);
                scaleX = scaleY = fillScale;
                offsetX = (width - pictureWidth * fillScale) / 2;
                offsetY = (height - pictureHeight * fillScale) / 2;
                break;
            default:
                scaleX = scaleY = 1.0f;
                break;
        }

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);

        using var framebuffer = bitmap.Lock();
        using var surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scaleX, scaleY);
        canvas.DrawPicture(_picture);

        return bitmap;
    }

    public void Dispose()
    {
        _svg?.Dispose();
        _svg = null;
        _picture?.Dispose();
        _picture = null;
        _bitmap?.Dispose();
        _bitmap = null;

    }
}
