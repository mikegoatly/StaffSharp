namespace StaffSharp.Core.Tests.Notation;

using StaffSharp.Core.Notation;

public class KeySignatureTests
{
    [Fact]
    public void C_HasNoSharpsOrFlats()
    {
        Assert.Equal(0, KeySignature.C.Sharps);
        Assert.False(KeySignature.C.HasSharps);
        Assert.False(KeySignature.C.HasFlats);
    }

    [Fact]
    public void G_HasOneSha()
    {
        Assert.Equal(1, KeySignature.G.Sharps);
        Assert.True(KeySignature.G.HasSharps);
        Assert.Equal(1, KeySignature.G.SharpCount);
    }

    [Fact]
    public void F_HasOneFlat()
    {
        Assert.Equal(-1, KeySignature.F.Sharps);
        Assert.True(KeySignature.F.HasFlats);
        Assert.Equal(1, KeySignature.F.FlatCount);
    }

    [Fact]
    public void Create_ValidRange_Succeeds()
    {
        var sevenSharps = KeySignature.Create(7);
        var sevenFlats = KeySignature.Create(-7);

        Assert.Equal(7, sevenSharps.SharpCount);
        Assert.Equal(7, sevenFlats.FlatCount);
    }

    [Fact]
    public void Create_OutOfRange_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KeySignature.Create(8));
        Assert.Throws<ArgumentOutOfRangeException>(() => KeySignature.Create(-8));
    }
}
