using StaffSharp.Audio.Numerics;

namespace StaffSharp.Audio.Tests.Numerics;

public class WindowFunctionsTests
{
    [Fact]
    public void CreateHannWindow_InvalidSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateHannWindow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateHannWindow(-1));
    }

    [Fact]
    public void CreateHannWindow_ValidSize_ReturnsCorrectLength()
    {
        var window = WindowFunctions.CreateHannWindow(512);
        Assert.Equal(512, window.Length);
    }

    [Fact]
    public void CreateHannWindow_EndsAreZero()
    {
        var window = WindowFunctions.CreateHannWindow(100);

        // Hann window should start and end at 0
        Assert.Equal(0f, window[0], precision: 5);
        Assert.Equal(0f, window[99], precision: 5);
    }

    [Fact]
    public void CreateHannWindow_PeakAtCenter()
    {
        var window = WindowFunctions.CreateHannWindow(101);

        // Peak should be at center and equal to 1
        Assert.Equal(1f, window[50], precision: 4);

        // Values should be symmetric
        for (int i = 0; i < window.Length / 2; i++)
        {
            Assert.Equal(window[i], window[window.Length - 1 - i], precision: 4);
        }
    }

    [Fact]
    public void CreateHammingWindow_InvalidSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateHammingWindow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateHammingWindow(-1));
    }

    [Fact]
    public void CreateHammingWindow_ValidSize_ReturnsCorrectLength()
    {
        var window = WindowFunctions.CreateHammingWindow(512);
        Assert.Equal(512, window.Length);
    }

    [Fact]
    public void CreateHammingWindow_EndsAreNonZero()
    {
        var window = WindowFunctions.CreateHammingWindow(100);

        // Hamming window should have non-zero endpoints (0.08)
        Assert.Equal(0.08f, window[0], precision: 2);
        Assert.Equal(0.08f, window[99], precision: 2);
    }

    [Fact]
    public void CreateHammingWindow_PeakAtCenter()
    {
        var window = WindowFunctions.CreateHammingWindow(101);

        // Peak should be at center
        Assert.Equal(1f, window[50], precision: 4);

        // Values should be symmetric
        for (int i = 0; i < window.Length / 2; i++)
        {
            Assert.Equal(window[i], window[window.Length - 1 - i], precision: 4);
        }
    }

    [Fact]
    public void CreateBlackmanWindow_InvalidSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateBlackmanWindow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateBlackmanWindow(-1));
    }

    [Fact]
    public void CreateBlackmanWindow_ValidSize_ReturnsCorrectLength()
    {
        var window = WindowFunctions.CreateBlackmanWindow(512);
        Assert.Equal(512, window.Length);
    }

    [Fact]
    public void CreateBlackmanWindow_EndsAreNearZero()
    {
        var window = WindowFunctions.CreateBlackmanWindow(100);

        // Blackman window should have very small endpoints
        Assert.True(window[0] < 0.01f);
        Assert.True(window[99] < 0.01f);
    }

    [Fact]
    public void CreateBlackmanWindow_PeakAtCenter()
    {
        var window = WindowFunctions.CreateBlackmanWindow(101);

        // Peak should be at center
        Assert.Equal(1f, window[50], precision: 5);

        // Values should be symmetric
        for (int i = 0; i < window.Length / 2; i++)
        {
            Assert.Equal(window[i], window[window.Length - 1 - i], precision: 5);
        }
    }

    [Fact]
    public void CreateRectangularWindow_InvalidSize_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateRectangularWindow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowFunctions.CreateRectangularWindow(-1));
    }

    [Fact]
    public void CreateRectangularWindow_AllValuesAreOne()
    {
        var window = WindowFunctions.CreateRectangularWindow(100);

        Assert.Equal(100, window.Length);
        Assert.All(window, val => Assert.Equal(1f, val));
    }

    [Theory]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    [InlineData(2048)]
    public void AllWindows_CommonSizes_CreateSuccessfully(int size)
    {
        var hann = WindowFunctions.CreateHannWindow(size);
        var hamming = WindowFunctions.CreateHammingWindow(size);
        var blackman = WindowFunctions.CreateBlackmanWindow(size);
        var rectangular = WindowFunctions.CreateRectangularWindow(size);

        Assert.Equal(size, hann.Length);
        Assert.Equal(size, hamming.Length);
        Assert.Equal(size, blackman.Length);
        Assert.Equal(size, rectangular.Length);
    }

    [Fact]
    public void WindowFunctions_AreSymmetric()
    {
        var size = 256;
        var hann = WindowFunctions.CreateHannWindow(size);
        var hamming = WindowFunctions.CreateHammingWindow(size);
        var blackman = WindowFunctions.CreateBlackmanWindow(size);

        // All windows should be symmetric
        for (int i = 0; i < size / 2; i++)
        {
            Assert.Equal(hann[i], hann[size - 1 - i], precision: 4);
            Assert.Equal(hamming[i], hamming[size - 1 - i], precision: 4);
            Assert.Equal(blackman[i], blackman[size - 1 - i], precision: 4);
        }
    }
}
