using System.Xml.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

using Svg.Skia;

namespace StaffSharp.Avalonia.Controls;

/// <summary>
/// A control that renders SVG content from a string using direct Skia rendering.
/// </summary>
public sealed class SvgContentControl : Control, IDisposable
{
    private SKSvg? _svg;
    private SKSize _svgSize;
    private SvgDrawOperation _svgDrawOperation;

    public static readonly StyledProperty<XElement?> SvgContentProperty =
        AvaloniaProperty.Register<SvgContentControl, XElement?>(nameof(SvgContent));

    public SvgContentControl()
    {
        _svgDrawOperation = new SvgDrawOperation(this);
    }

    public XElement? SvgContent
    {
        get => GetValue(SvgContentProperty);
        set => SetValue(SvgContentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);

        base.OnPropertyChanged(change);

        if (change.Property == SvgContentProperty)
        {
            LoadSvg();
            InvalidateVisual();
        }
    }

    private void LoadSvg()
    {
        _svg?.Dispose();

        if (SvgContent is not { } content)
        {
            return;
        }

        _svgDrawOperation = new SvgDrawOperation(this);

        using var reader = content.CreateReader();
        _svg = SKSvg.CreateFromXmlReader(reader);
        _svgSize = _svg.Picture?.CullRect.Size ?? SKSize.Empty;
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.Render(context);

        context.Custom(_svgDrawOperation);
    }

    public void Dispose()
    {
        _svgDrawOperation?.Dispose();
        _svg?.Dispose();
    }

    private sealed class SvgDrawOperation : ICustomDrawOperation
    {
        private readonly SvgContentControl _parent;

        public SvgDrawOperation(SvgContentControl parent)
        {
            _parent = parent;
        }

        public Rect Bounds => _parent.Bounds;

        public void Dispose()
        {
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var skiaContext = lease?.SkCanvas;
            if (skiaContext is null || _parent._svg is null)
            {
                return;
            }

            var svgSize = _parent._svgSize;
            if (svgSize.Width <= 0 || svgSize.Height <= 0)
            {
                return;
            }

            var width = (float)Bounds.Width;
            var height = (float)Bounds.Height;

            var offsetX = (width - svgSize.Width) / 2;
            var offsetY = (height - svgSize.Height) / 2;

            skiaContext.Save();
            skiaContext.Translate(offsetX, offsetY);

            _parent._svg.Draw(skiaContext);

            skiaContext.Restore();
        }
    }
}
