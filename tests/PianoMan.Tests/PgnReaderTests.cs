using PianoMan.Core.Analysis;using PianoMan.Core.Chess;
namespace PianoMan.Tests;
public sealed class PgnReaderTests
{
    [Fact] public void ReaderKeepsMainLineAndIgnoresCommentsAndVariations(){const string pgn="""
[Event "Test"]
[White "Ada"]
[Black "Grace"]
[Result "1-0"]

1. e4 {King pawn} e5 (1... c5) 2. Bc4 Nc6 3. Qh5 Nf6?? $4 4. Qxf7# 1-0
""";var game=PgnReader.Read(pgn);var analysis=GameAnalyzer.Analyze(game,null);Assert.Equal(7,game.Moves.Count);Assert.Equal(8,analysis.Timeline.Count);Assert.Equal("Qxf7#",analysis.Timeline[^1].San);}
    [Fact] public void ReadsMultipleGames(){const string pgn="""
[Event "One"]
[Result "*"]
1. e4 e5 *

[Event "Two"]
[Result "*"]
1. d4 d5 *
""";var games=PgnReader.ReadMany(pgn);Assert.Equal(2,games.Count);Assert.Equal("One",games[0].Header("Event"));Assert.Equal("Two",games[1].Header("Event"));}

    [Fact]
    public void ImmortalGameProcessesThroughCheckmate()
    {
        const string pgn = """
[Event "London"]
[White "Adolf Anderssen"]
[Black "Lionel Kieseritzky"]
[Result "1-0"]

1. e4 e5 2. f4 exf4 3. Bc4 Qh4+ 4. Kf1 b5 5. Bxb5 Nf6 6. Nf3 Qh6
7. d3 Nh5 8. Nh4 Qg5 9. Nf5 c6 10. g4 Nf6 11. Rg1 cxb5 12. h4 Qg6
13. h5 Qg5 14. Qf3 Ng8 15. Bxf4 Qf6 16. Nc3 Bc5 17. Nd5 Qxb2
18. Bd6 Bxg1 19. e5 Qxa1+ 20. Ke2 Na6 21. Nxg7+ Kd8 22. Qf6+ Nxf6
23. Be7# 1-0
""";
        var game = PgnReader.Read(pgn);
        var analysis = GameAnalyzer.Analyze(game, PianoMan.Core.Theory.TheoryBook.LoadEmbedded());
        Assert.Equal(45, game.Moves.Count);
        Assert.Equal(PositionStatus.Checkmate, analysis.Timeline[^1].Harmony.Status);
        Assert.Equal(Color.White, analysis.Timeline[^1].Harmony.Winner);
    }
}
