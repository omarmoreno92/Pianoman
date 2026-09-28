using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class HarmonyTests
{
    [Fact]
    public void InitialPositionIsBalancedAndMapsToCMajor()
    {
        var(h,c)=GameAnalyzer.Analyze(Position.Initial);
        Assert.InRange(h.RelativeScore,-25,25);
        Assert.Equal(0,h.Tension);
        Assert.Equal("Cmaj",c.Symbol);
    }

    [Fact]
    public void InitialPositionHasThreeStablePerceptions()
    {
        var(h,_)=GameAnalyzer.Analyze(Position.Initial);
        var p=PerceptionAnalyzer.Analyze(h);
        Assert.Equal(-Math.Abs(h.RelativeScore),p.Global.Score);
        Assert.Equal(h.RelativeScore,p.White.Score);
        Assert.Equal(-h.RelativeScore,p.Black.Score);
        Assert.Equal("Cmaj",p.Global.Chord.Symbol);
        Assert.Equal("Cmaj",p.White.Chord.Symbol);
        Assert.Equal("Cmaj",p.Black.Chord.Symbol);
    }

    [Fact]
    public void WhiteAndBlackPerceptionsMirrorRelativeScore()
    {
        var position=Fen.Parse("4k3/8/8/8/8/8/4Q3/4K3 w - - 0 1");
        var(h,_)=GameAnalyzer.Analyze(position);
        var p=PerceptionAnalyzer.Analyze(h);
        Assert.Equal(h.RelativeScore,p.White.Score);
        Assert.Equal(-h.RelativeScore,p.Black.Score);
        Assert.Equal(-Math.Abs(h.RelativeScore),p.Global.Score);
    }

    [Fact]
    public void LossClassificationUsesPianoManThresholds()
    {
        Assert.Equal(PianoMoveClass.Best,GameAnalyzer.Classify(0));
        Assert.Equal(PianoMoveClass.Excellent,GameAnalyzer.Classify(15));
        Assert.Equal(PianoMoveClass.Good,GameAnalyzer.Classify(40));
        Assert.Equal(PianoMoveClass.Inaccuracy,GameAnalyzer.Classify(80));
        Assert.Equal(PianoMoveClass.Mistake,GameAnalyzer.Classify(160));
        Assert.Equal(PianoMoveClass.Blunder,GameAnalyzer.Classify(161));
    }
}
