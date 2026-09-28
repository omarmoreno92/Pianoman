using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class FenTests
{
    [Fact]
    public void InitialPositionRoundTripsWithoutLoss()
    {
        var position = Fen.Parse(Fen.InitialPosition);

        Assert.Equal(Fen.InitialPosition, position.ToFen());
        Assert.Equal(32, position.Pieces().Count());
        Assert.Equal(Color.White, position.SideToMove);
        Assert.Equal(CastlingRights.All, position.CastlingRights);
    }

    [Theory]
    [InlineData("8/8/8/3k4/8/4K3/8/8 w - - 17 42")]
    [InlineData("r3k2r/8/8/8/8/8/8/R3K2R b KQkq - 0 1")]
    [InlineData("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 15")]
    public void ArbitraryPositionRoundTripsWithoutLoss(string fen)
    {
        Assert.Equal(fen, Fen.Parse(fen).ToFen());
    }
}
