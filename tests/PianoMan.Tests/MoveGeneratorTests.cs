using PianoMan.Core.Chess;

namespace PianoMan.Tests;

public sealed class MoveGeneratorTests
{
    [Fact]
    public void InitialPositionHasTwentyLegalMoves()
    {
        Assert.Equal(20, MoveGenerator.GenerateLegalMoves(Position.Initial).Count);
    }

    [Fact]
    public void SanParserReachesScholarsMate()
    {
        var position = Position.Initial;
        foreach (var san in new[] { "e4", "e5", "Bc4", "Nc6", "Qh5", "Nf6??", "Qxf7#" })
        {
            position.Apply(SanParser.Parse(position, san));
        }

        Assert.True(MoveGenerator.IsCheckmate(position));
        Assert.Empty(MoveGenerator.GenerateLegalMoves(position));
    }

    [Fact]
    public void CastlingMovesTheKingAndRook()
    {
        var position = Fen.Parse("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");

        position.Apply(SanParser.Parse(position, "O-O"));

        Assert.Equal(new Piece(Color.White, PieceType.King), position[Square.Parse("g1")]);
        Assert.Equal(new Piece(Color.White, PieceType.Rook), position[Square.Parse("f1")]);
        Assert.Equal(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide, position.CastlingRights);
    }
}
