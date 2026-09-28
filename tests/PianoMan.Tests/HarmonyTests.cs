using PianoMan.Core.Analysis;using PianoMan.Core.Chess;
namespace PianoMan.Tests;
public sealed class HarmonyTests
{
    [Fact] public void InitialPositionIsBalancedAndMapsToCMajor(){var(h,c)=GameAnalyzer.Analyze(Position.Initial);Assert.InRange(h.RelativeScore,-25,25);Assert.Equal(0,h.Tension);Assert.Equal("Cmaj",c.Symbol);}
    [Fact] public void LossClassificationUsesPianoManThresholds(){Assert.Equal(PianoMoveClass.Best,GameAnalyzer.Classify(0));Assert.Equal(PianoMoveClass.Excellent,GameAnalyzer.Classify(15));Assert.Equal(PianoMoveClass.Good,GameAnalyzer.Classify(40));Assert.Equal(PianoMoveClass.Inaccuracy,GameAnalyzer.Classify(80));Assert.Equal(PianoMoveClass.Mistake,GameAnalyzer.Classify(160));Assert.Equal(PianoMoveClass.Blunder,GameAnalyzer.Classify(161));}
}
