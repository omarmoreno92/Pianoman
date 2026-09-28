using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class HarmonyTests
{
    [Fact]
    public void InitialPositionIsBalancedAndMapsToCMajor()
    {
        var (harmony, chord) = GameAnalyzer.Analyze(Position.Initial);

        Assert.InRange(harmony.RelativeScore, -25, 25);
        Assert.Equal(0, harmony.Tension);
        Assert.Equal("Cmaj", chord.Symbol);
        Assert.True(harmony.IsStable);
    }

    [Fact]
    public void ExtraWhiteQueenCreatesALargePositiveMaterialDelta()
    {
        var position = Fen.Parse("4k3/8/8/8/8/8/8/Q3K3 w - - 0 1");

        var (harmony, chord) = GameAnalyzer.Analyze(position);

        Assert.True(harmony.RelativeScore > 800);
        Assert.True(harmony.White.Material > harmony.Black.Material);
        Assert.EndsWith("maj7", chord.Symbol, StringComparison.Ordinal);
    }
}
