using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class PgnReaderTests
{
    [Fact]
    public void ReaderKeepsTheMainLineAndIgnoresCommentsAndVariations()
    {
        const string pgn = """
            [Event "Test"]
            [White "Ada"]
            [Black "Grace"]
            [Result "1-0"]

            1. e4 {King pawn} e5 (1... c5) 2. Bc4 Nc6
            3. Qh5 Nf6?? $4 4. Qxf7# 1-0
            """;

        var game = PgnReader.Read(pgn);
        var analysis = GameAnalyzer.Analyze(game);

        Assert.Equal(7, game.Moves.Count);
        Assert.Equal(8, analysis.Timeline.Count);
        Assert.Equal("Ada vs Grace", analysis.Title);
        Assert.Equal("Qxf7#", analysis.Timeline[^1].San);
        Assert.Equal(PositionStatus.Checkmate, analysis.Timeline[^1].Harmony.Status);
        Assert.Equal(Color.White, analysis.Timeline[^1].Harmony.Winner);
        Assert.Equal(100_000, analysis.Timeline[^1].Harmony.RelativeScore);
    }
}
