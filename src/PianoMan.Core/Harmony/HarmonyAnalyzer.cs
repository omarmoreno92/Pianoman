using PianoMan.Core.Chess;

namespace PianoMan.Core.Harmony;

public sealed class HarmonyAnalyzer
{
    public static HarmonySnapshot Analyze(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        var white = AnalyzeSide(position, Color.White);
        var black = AnalyzeSide(position, Color.Black);
        var legalMoves = MoveGenerator.GenerateLegalMoves(position);
        var sideToMoveInCheck = MoveGenerator.IsKingInCheck(position, position.SideToMove);
        var status = legalMoves.Count > 0
            ? PositionStatus.Ongoing
            : sideToMoveInCheck
                ? PositionStatus.Checkmate
                : PositionStatus.Stalemate;
        var winner = status == PositionStatus.Checkmate
            ? position.SideToMove.Opposite()
            : (Color?)null;
        var relativeScore = status switch
        {
            PositionStatus.Checkmate when winner == Color.White => 100_000,
            PositionStatus.Checkmate => -100_000,
            PositionStatus.Stalemate => 0,
            _ => white.Total - black.Total
        };
        var tension = status == PositionStatus.Ongoing
            ? CalculateTension(position)
            : 0;

        return new HarmonySnapshot(
            white,
            black,
            relativeScore,
            tension,
            CalculatePhase(position),
            position.SideToMove,
            status,
            winner);
    }

    private static SideHarmony AnalyzeSide(Position position, Color color)
    {
        var material = 0;
        var defendedPieces = 0;
        foreach (var (square, piece) in position.Pieces())
        {
            if (piece.Color != color)
            {
                continue;
            }

            material += PieceValue(piece.Type);
            if (piece.Type != PieceType.King &&
                MoveGenerator.IsSquareAttacked(position, square, color))
            {
                defendedPieces++;
            }
        }

        var attackedSquares = MoveGenerator.CountAttackedSquares(position, color);
        var space = CountSpace(position, color);
        var coordination = defendedPieces * 4;
        var activity = attackedSquares * 2;
        var structure = EvaluatePawnStructure(position, color);
        var kingSafety = EvaluateKingSafety(position, color);
        var pressure = EvaluatePressure(position, color);
        var initiative = position.SideToMove == color ? 8 : 0;

        return new SideHarmony(
            material,
            activity,
            coordination,
            kingSafety,
            space,
            structure,
            pressure,
            initiative);
    }

    private static int CountSpace(Position position, Color color)
    {
        var count = 0;
        var firstRank = color == Color.White ? 4 : 0;
        var lastRank = color == Color.White ? 7 : 3;
        for (var rank = firstRank; rank <= lastRank; rank++)
        {
            for (var file = 0; file < 8; file++)
            {
                if (MoveGenerator.IsSquareAttacked(
                        position,
                        Square.FromCoordinates(file, rank),
                        color))
                {
                    count += 2;
                }
            }
        }

        return count;
    }

    private static int EvaluatePawnStructure(Position position, Color color)
    {
        var pawnsByFile = new List<int>[8];
        for (var file = 0; file < 8; file++)
        {
            pawnsByFile[file] = [];
        }

        foreach (var (square, piece) in position.Pieces())
        {
            if (piece == new Piece(color, PieceType.Pawn))
            {
                pawnsByFile[Square.File(square)].Add(Square.Rank(square));
            }
        }

        var score = 0;
        for (var file = 0; file < 8; file++)
        {
            var ranks = pawnsByFile[file];
            if (ranks.Count > 1)
            {
                score -= (ranks.Count - 1) * 12;
            }

            foreach (var rank in ranks)
            {
                var hasAdjacentPawn = HasPawnInFile(pawnsByFile, file - 1) ||
                                      HasPawnInFile(pawnsByFile, file + 1);
                if (!hasAdjacentPawn)
                {
                    score -= 8;
                }

                if (HasConnectedPawn(pawnsByFile, file, rank))
                {
                    score += 4;
                }

                if (IsPassedPawn(position, color, file, rank))
                {
                    var advance = color == Color.White ? rank - 1 : 6 - rank;
                    score += 5 + (Math.Max(0, advance) * 3);
                }
            }
        }

        return score;
    }

    private static bool HasPawnInFile(List<int>[] pawnsByFile, int file) =>
        file is >= 0 and < 8 && pawnsByFile[file].Count > 0;

    private static bool HasConnectedPawn(List<int>[] pawnsByFile, int file, int rank)
    {
        for (var adjacentFile = file - 1; adjacentFile <= file + 1; adjacentFile += 2)
        {
            if (adjacentFile is < 0 or > 7)
            {
                continue;
            }

            if (pawnsByFile[adjacentFile].Any(otherRank => Math.Abs(otherRank - rank) <= 1))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPassedPawn(Position position, Color color, int file, int rank)
    {
        var opponentPawn = new Piece(color.Opposite(), PieceType.Pawn);
        var direction = color == Color.White ? 1 : -1;
        for (var targetRank = rank + direction;
             targetRank is >= 0 and < 8;
             targetRank += direction)
        {
            for (var targetFile = Math.Max(0, file - 1);
                 targetFile <= Math.Min(7, file + 1);
                 targetFile++)
            {
                if (position[Square.FromCoordinates(targetFile, targetRank)] == opponentPawn)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int EvaluateKingSafety(Position position, Color color)
    {
        var king = position.FindKing(color);
        if (king < 0)
        {
            return -500;
        }

        var score = 0;
        var kingFile = Square.File(king);
        var kingRank = Square.Rank(king);
        var shieldRank = kingRank + (color == Color.White ? 1 : -1);
        if (shieldRank is >= 0 and < 8)
        {
            for (var file = Math.Max(0, kingFile - 1); file <= Math.Min(7, kingFile + 1); file++)
            {
                if (position[Square.FromCoordinates(file, shieldRank)] ==
                    new Piece(color, PieceType.Pawn))
                {
                    score += 10;
                }
            }
        }

        var opponent = color.Opposite();
        foreach (var square in KingZone(king))
        {
            if (MoveGenerator.IsSquareAttacked(position, square, opponent))
            {
                score -= 12;
            }
        }

        if (MoveGenerator.IsKingInCheck(position, color))
        {
            score -= 50;
        }

        var homeRank = color == Color.White ? 0 : 7;
        if (kingRank == homeRank && kingFile is 2 or 6)
        {
            score += 12;
        }

        var hasPawnOnKingFile = position.Pieces().Any(entry =>
            entry.Piece == new Piece(color, PieceType.Pawn) &&
            Square.File(entry.Square) == kingFile);
        if (!hasPawnOnKingFile)
        {
            score -= 8;
        }

        return score;
    }

    private static int EvaluatePressure(Position position, Color color)
    {
        var opponent = color.Opposite();
        var score = 0;
        foreach (var (square, piece) in position.Pieces())
        {
            if (piece.Color != opponent ||
                !MoveGenerator.IsSquareAttacked(position, square, color))
            {
                continue;
            }

            score += piece.Type switch
            {
                PieceType.Pawn => 3,
                PieceType.Knight or PieceType.Bishop => 8,
                PieceType.Rook => 12,
                PieceType.Queen => 18,
                PieceType.King => 20,
                _ => 0
            };

            if (piece.Type != PieceType.King &&
                !MoveGenerator.IsSquareAttacked(position, square, opponent))
            {
                score += 10;
            }
        }

        var enemyKing = position.FindKing(opponent);
        if (enemyKing >= 0)
        {
            score += KingZone(enemyKing)
                .Count(square => MoveGenerator.IsSquareAttacked(position, square, color)) * 5;
        }

        return score;
    }

    private static int CalculateTension(Position position)
    {
        var tension = 0;
        if (MoveGenerator.IsKingInCheck(position, Color.White)) tension += 70;
        if (MoveGenerator.IsKingInCheck(position, Color.Black)) tension += 70;

        foreach (var (square, piece) in position.Pieces())
        {
            if (piece.Type == PieceType.King)
            {
                continue;
            }

            var opponent = piece.Color.Opposite();
            if (!MoveGenerator.IsSquareAttacked(position, square, opponent))
            {
                continue;
            }

            var localTension = Math.Max(2, PieceValue(piece.Type) / 40);
            if (!MoveGenerator.IsSquareAttacked(position, square, piece.Color))
            {
                localTension *= 2;
            }

            tension += localTension;
        }

        foreach (var color in new[] { Color.White, Color.Black })
        {
            var view = position.WithSideToMove(color);
            tension += MoveGenerator.GenerateLegalMoves(view).Count(move => move.IsCapture) * 2;
        }

        return Math.Min(255, tension);
    }

    private static int CalculatePhase(Position position)
    {
        var remaining = position.Pieces().Sum(entry => entry.Piece.Type switch
        {
            PieceType.Knight or PieceType.Bishop => 1,
            PieceType.Rook => 2,
            PieceType.Queen => 4,
            _ => 0
        });

        return Math.Clamp((remaining * 100) / 24, 0, 100);
    }

    private static IEnumerable<int> KingZone(int kingSquare)
    {
        var kingFile = Square.File(kingSquare);
        var kingRank = Square.Rank(kingSquare);
        for (var file = kingFile - 1; file <= kingFile + 1; file++)
        {
            for (var rank = kingRank - 1; rank <= kingRank + 1; rank++)
            {
                if (Square.IsValid(file, rank))
                {
                    yield return Square.FromCoordinates(file, rank);
                }
            }
        }
    }

    public static int PieceValue(PieceType pieceType) => pieceType switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 320,
        PieceType.Bishop => 330,
        PieceType.Rook => 500,
        PieceType.Queen => 900,
        _ => 0
    };
}
