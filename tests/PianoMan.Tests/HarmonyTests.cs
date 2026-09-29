using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;
using PianoMan.Core.Theory;

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
        Assert.Equal(0,p.Global.Tension);
        Assert.Equal(0,p.White.Tension);
        Assert.InRange(p.Black.Tension,1,4);
    }

    [Fact]
    public void InitialPositionAssignsOneConsonantVoiceToEveryPiece()
    {
        var game=PgnReader.Read("1. e4 *");
        var entry=GameAnalyzer.Analyze(game,TheoryBook.LoadEmbedded()).Timeline[0];
        var chordPitchClasses=entry.Perception.Global.Chord.MidiNotes.Select(note=>note%12).ToHashSet();

        Assert.Equal(32,entry.Voicing.Global.Count);
        Assert.Equal(32,entry.Voicing.White.Count);
        Assert.Equal(32,entry.Voicing.Black.Count);
        Assert.All(entry.Voicing.Global,voice=>Assert.Contains(voice.MidiNote%12,chordPitchClasses));
    }

    [Fact]
    public void FirstPlyTheoryDissonanceIsDerivedFromTheRankedCorpusCandidates()
    {
        var book=TheoryBook.LoadEmbedded();
        var lookup=book.Lookup(Position.Initial);
        Assert.NotNull(lookup);

        foreach(var continuation in lookup!.Continuations)
        {
            var move=PackedMove.Resolve(Position.Initial,continuation.PackedMove);
            var entry=GameAnalyzer.AnalyzeContinuation(Position.Initial,move,1,book);
            Assert.Equal(AnalysisMode.Theory,entry.Decision!.Mode);
            var played=Assert.Single(entry.Decision.Candidates,candidate=>candidate.Played);
            var best=entry.Decision.Candidates.Max(candidate=>candidate.HarmonyScore);
            Assert.Equal(best-played.HarmonyScore,entry.Decision.Loss);
            Assert.Equal(0,Assert.Single(entry.Decision.Candidates,candidate=>candidate.Recommended).Loss);
        }
    }

    [Fact]
    public void ABadMoveOutsideTheoryCreatesDissonanceWhileTacticsRemainEnergy()
    {
        var position=Fen.Parse("4k3/8/8/8/8/8/3q4/3QK3 w - - 0 1");
        var move=UciParser.Parse(position,"e1f1");
        var entry=GameAnalyzer.AnalyzeContinuation(position,move,1,TheoryBook.LoadEmbedded());

        Assert.Equal(AnalysisMode.Tuning,entry.Decision!.Mode);
        Assert.True(entry.Decision.Loss>0);
        Assert.True(entry.Perception.Global.Tension>0);
        Assert.Equal(entry.Harmony.Tension,entry.Perception.Global.Energy);
        var moved=Assert.Single(entry.Voicing.Global,voice=>voice.Moved);
        Assert.NotEqual(0,moved.HarmonicOffset);
        Assert.All(entry.Voicing.Global.Where(voice=>!voice.Moved),voice=>Assert.Equal(0,voice.HarmonicOffset));
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
    public void GlobalVoicingChangesWhenSpaceRelationshipChanges()
    {
        var initial=GameAnalyzer.Analyze(Position.Initial).Chord;
        var afterE4=Position.Initial.Clone();
        afterE4.Apply(SanParser.Parse(afterE4,"e4"));
        var moved=GameAnalyzer.Analyze(afterE4).Chord;

        Assert.False(initial.MidiNotes.SequenceEqual(moved.MidiNotes));
    }

    [Fact]
    public void PrincipalLineIsDeterministicAndContainsOnlyOneMovePerPly()
    {
        var book=TheoryBook.LoadEmbedded();
        var first=HarmonicLineAnalyzer.Analyze(Position.Initial,book,4);
        var second=HarmonicLineAnalyzer.Analyze(Position.Initial,book,4);

        Assert.Equal(4,first.Moves.Count);
        Assert.Equal(first.Moves.Select(move=>move.Uci),second.Moves.Select(move=>move.Uci));
        Assert.Equal(Enumerable.Range(1,4),first.Moves.Select(move=>move.Ply));
    }

    [Fact]
    public void PrincipalLineExtendsWhenMaterialChangesSuddenly()
    {
        var position=Fen.Parse("4k3/8/8/8/8/8/3q4/3QK3 w - - 0 1");
        var line=HarmonicLineAnalyzer.Analyze(position,TheoryBook.LoadEmbedded(),1);

        Assert.Single(line.Moves);
        Assert.Equal(1,line.SelectiveExtensions);
        Assert.True(line.Moves[0].SelectiveExtension);
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
