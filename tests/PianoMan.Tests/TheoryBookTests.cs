using PianoMan.Core.Analysis;using PianoMan.Core.Chess;using PianoMan.Core.Theory;
namespace PianoMan.Tests;
public sealed class TheoryBookTests
{
    [Fact] public void ExpectedV1CorpusMetadataIsPinned(){Assert.Equal(251_274,TheoryBookExpectedMetadata.UniquePositions);Assert.Equal(420_150,TheoryBookExpectedMetadata.Continuations);Assert.Equal(3_329,TheoryBookExpectedMetadata.OpeningIdentities);Assert.Equal(36,TheoryBookExpectedMetadata.MaxPlies);Assert.Equal(7_586_743,TheoryBookExpectedMetadata.RawBytes);Assert.Equal(2_910_309,TheoryBookExpectedMetadata.CompressedBytes);Assert.Equal("36cd056f0fdea527c25fa40b184ae26ef0f5eafb9722a796be9cae0d36927d47",TheoryBookExpectedMetadata.CompressedSha256);}
    [Fact] public void EmbeddedBrotliResourceLoadsCompletely(){var book=TheoryBook.LoadEmbedded();Assert.NotNull(book);Assert.True(book.Metadata.RawBytes>0);Assert.True(book.Metadata.CompressedBytes>0);}
    [Fact] public void InitialPositionContainsE4AndD4(){var book=TheoryBook.LoadEmbedded();var lookup=book.Lookup(Position.Initial);Assert.NotNull(lookup);var ucis=lookup!.Continuations.Select(c=>PackedMove.Resolve(Position.Initial,c.PackedMove).ToUci()).ToArray();Assert.Contains("e2e4",ucis);Assert.Contains("d2d4",ucis);}
    [Fact] public void KnownMoveIsMarkedTheory(){var p=Position.Initial;var move=SanParser.Parse(p,"e4");var result=GameAnalyzer.AnalyzeMove(p,move,TheoryBook.LoadEmbedded());Assert.Equal(AnalysisMode.Theory,result.Mode);Assert.Equal("TEORÍA · SIN BÚSQUEDA",result.Label);Assert.Null(result.Loss);}
    [Fact] public void PositionOutsideBookEvaluatesEveryLegalMove(){var p=Position.Initial;p.Apply(SanParser.Parse(p,"e4"));var played=SanParser.Parse(p,"e5");var legal=MoveGenerator.GenerateLegalMoves(p).Count;var result=GameAnalyzer.AnalyzeMove(p,played,TheoryBook.LoadEmbedded());Assert.Equal(AnalysisMode.Tuning,result.Mode);Assert.Equal(legal,result.EvaluatedMoveCount);Assert.InRange(result.Candidates.Count,1,11);}
}
