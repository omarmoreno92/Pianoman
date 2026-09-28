namespace PianoMan.Core.Chess;

public static class MoveGenerator
{
    private static readonly (int File, int Rank)[] KnightOffsets =
    [
        (1, 2), (2, 1), (2, -1), (1, -2),
        (-1, -2), (-2, -1), (-2, 1), (-1, 2)
    ];

    private static readonly (int File, int Rank)[] BishopDirections =
    [
        (1, 1), (1, -1), (-1, 1), (-1, -1)
    ];

    private static readonly (int File, int Rank)[] RookDirections =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1)
    ];

    private static readonly (int File, int Rank)[] QueenDirections =
    [
        (1, 1), (1, -1), (-1, 1), (-1, -1),
        (1, 0), (-1, 0), (0, 1), (0, -1)
    ];

    private static readonly PieceType[] PromotionPieces =
    [
        PieceType.Queen,
        PieceType.Rook,
        PieceType.Bishop,
        PieceType.Knight
    ];

    public static IReadOnlyList<Move> GenerateLegalMoves(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        var movingColor = position.SideToMove;
        var pseudoLegal = GeneratePseudoLegalMoves(position);
        var legal = new List<Move>(pseudoLegal.Count);

        foreach (var move in pseudoLegal)
        {
            var next = position.Clone();
            next.Apply(move);
            if (!IsKingInCheck(next, movingColor))
            {
                legal.Add(move);
            }
        }

        return legal;
    }

    public static bool IsKingInCheck(Position position, Color color)
    {
        var kingSquare = position.FindKing(color);
        return kingSquare >= 0 && IsSquareAttacked(position, kingSquare, color.Opposite());
    }

    public static bool IsCheckmate(Position position) =>
        IsKingInCheck(position, position.SideToMove) && GenerateLegalMoves(position).Count == 0;

    public static bool IsStalemate(Position position) =>
        !IsKingInCheck(position, position.SideToMove) && GenerateLegalMoves(position).Count == 0;

    public static bool IsSquareAttacked(Position position, int target, Color byColor)
    {
        ArgumentNullException.ThrowIfNull(position);

        foreach (var (from, piece) in position.Pieces())
        {
            if (piece.Color == byColor && PieceAttacksSquare(position, from, piece, target))
            {
                return true;
            }
        }

        return false;
    }

    public static int CountAttackedSquares(Position position, Color color)
    {
        var count = 0;
        for (var square = 0; square < 64; square++)
        {
            if (IsSquareAttacked(position, square, color))
            {
                count++;
            }
        }

        return count;
    }

    private static List<Move> GeneratePseudoLegalMoves(Position position)
    {
        var moves = new List<Move>(48);
        foreach (var (square, piece) in position.Pieces())
        {
            if (piece.Color != position.SideToMove)
            {
                continue;
            }

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    AddPawnMoves(position, square, piece.Color, moves);
                    break;
                case PieceType.Knight:
                    AddJumpMoves(position, square, piece.Color, KnightOffsets, moves);
                    break;
                case PieceType.Bishop:
                    AddSlidingMoves(position, square, piece.Color, BishopDirections, moves);
                    break;
                case PieceType.Rook:
                    AddSlidingMoves(position, square, piece.Color, RookDirections, moves);
                    break;
                case PieceType.Queen:
                    AddSlidingMoves(position, square, piece.Color, QueenDirections, moves);
                    break;
                case PieceType.King:
                    AddKingMoves(position, square, piece.Color, moves);
                    break;
            }
        }

        return moves;
    }

    private static void AddPawnMoves(Position position, int from, Color color, List<Move> moves)
    {
        var file = Square.File(from);
        var rank = Square.Rank(from);
        var direction = color == Color.White ? 1 : -1;
        var startRank = color == Color.White ? 1 : 6;
        var promotionRank = color == Color.White ? 7 : 0;

        var nextRank = rank + direction;
        if (Square.IsValid(file, nextRank))
        {
            var oneStep = Square.FromCoordinates(file, nextRank);
            if (position[oneStep].IsNone)
            {
                AddPawnMove(from, oneStep, nextRank == promotionRank, MoveFlags.None, moves);

                var twoStepRank = rank + (2 * direction);
                if (rank == startRank)
                {
                    var twoStep = Square.FromCoordinates(file, twoStepRank);
                    if (position[twoStep].IsNone)
                    {
                        moves.Add(new Move(from, twoStep, Flags: MoveFlags.DoublePawnPush));
                    }
                }
            }
        }

        foreach (var fileOffset in new[] { -1, 1 })
        {
            var targetFile = file + fileOffset;
            if (!Square.IsValid(targetFile, nextRank))
            {
                continue;
            }

            var target = Square.FromCoordinates(targetFile, nextRank);
            var targetPiece = position[target];
            if (!targetPiece.IsNone && targetPiece.Color != color && targetPiece.Type != PieceType.King)
            {
                AddPawnMove(from, target, nextRank == promotionRank, MoveFlags.Capture, moves);
            }
            else if (position.EnPassantSquare == target)
            {
                moves.Add(new Move(from, target, Flags: MoveFlags.Capture | MoveFlags.EnPassant));
            }
        }
    }

    private static void AddPawnMove(
        int from,
        int to,
        bool isPromotion,
        MoveFlags flags,
        List<Move> moves)
    {
        if (!isPromotion)
        {
            moves.Add(new Move(from, to, Flags: flags));
            return;
        }

        foreach (var promotion in PromotionPieces)
        {
            moves.Add(new Move(from, to, promotion, flags | MoveFlags.Promotion));
        }
    }

    private static void AddJumpMoves(
        Position position,
        int from,
        Color color,
        IReadOnlyList<(int File, int Rank)> offsets,
        List<Move> moves)
    {
        var fromFile = Square.File(from);
        var fromRank = Square.Rank(from);
        foreach (var (fileOffset, rankOffset) in offsets)
        {
            var file = fromFile + fileOffset;
            var rank = fromRank + rankOffset;
            if (!Square.IsValid(file, rank))
            {
                continue;
            }

            AddMoveIfAvailable(position, from, Square.FromCoordinates(file, rank), color, moves);
        }
    }

    private static void AddSlidingMoves(
        Position position,
        int from,
        Color color,
        IReadOnlyList<(int File, int Rank)> directions,
        List<Move> moves)
    {
        var fromFile = Square.File(from);
        var fromRank = Square.Rank(from);
        foreach (var (fileDirection, rankDirection) in directions)
        {
            var file = fromFile + fileDirection;
            var rank = fromRank + rankDirection;
            while (Square.IsValid(file, rank))
            {
                var target = Square.FromCoordinates(file, rank);
                var targetPiece = position[target];
                if (targetPiece.IsNone)
                {
                    moves.Add(new Move(from, target));
                }
                else
                {
                    if (targetPiece.Color != color && targetPiece.Type != PieceType.King)
                    {
                        moves.Add(new Move(from, target, Flags: MoveFlags.Capture));
                    }

                    break;
                }

                file += fileDirection;
                rank += rankDirection;
            }
        }
    }

    private static void AddKingMoves(Position position, int from, Color color, List<Move> moves)
    {
        AddJumpMoves(position, from, color, QueenDirections, moves);
        AddCastlingMoves(position, from, color, moves);
    }

    private static void AddCastlingMoves(Position position, int kingSquare, Color color, List<Move> moves)
    {
        var homeRank = color == Color.White ? 0 : 7;
        var expectedKingSquare = Square.FromCoordinates(4, homeRank);
        if (kingSquare != expectedKingSquare || IsKingInCheck(position, color))
        {
            return;
        }

        var opponent = color.Opposite();
        var kingSideRight = color == Color.White
            ? CastlingRights.WhiteKingSide
            : CastlingRights.BlackKingSide;
        var queenSideRight = color == Color.White
            ? CastlingRights.WhiteQueenSide
            : CastlingRights.BlackQueenSide;

        if ((position.CastlingRights & kingSideRight) != 0 &&
            HasRook(position, color, 7, homeRank) &&
            position[Square.FromCoordinates(5, homeRank)].IsNone &&
            position[Square.FromCoordinates(6, homeRank)].IsNone &&
            !IsSquareAttacked(position, Square.FromCoordinates(5, homeRank), opponent) &&
            !IsSquareAttacked(position, Square.FromCoordinates(6, homeRank), opponent))
        {
            moves.Add(new Move(
                kingSquare,
                Square.FromCoordinates(6, homeRank),
                Flags: MoveFlags.CastleKingSide));
        }

        if ((position.CastlingRights & queenSideRight) != 0 &&
            HasRook(position, color, 0, homeRank) &&
            position[Square.FromCoordinates(1, homeRank)].IsNone &&
            position[Square.FromCoordinates(2, homeRank)].IsNone &&
            position[Square.FromCoordinates(3, homeRank)].IsNone &&
            !IsSquareAttacked(position, Square.FromCoordinates(3, homeRank), opponent) &&
            !IsSquareAttacked(position, Square.FromCoordinates(2, homeRank), opponent))
        {
            moves.Add(new Move(
                kingSquare,
                Square.FromCoordinates(2, homeRank),
                Flags: MoveFlags.CastleQueenSide));
        }
    }

    private static bool HasRook(Position position, Color color, int file, int rank) =>
        position[Square.FromCoordinates(file, rank)] == new Piece(color, PieceType.Rook);

    private static void AddMoveIfAvailable(
        Position position,
        int from,
        int target,
        Color color,
        List<Move> moves)
    {
        var targetPiece = position[target];
        if (targetPiece.IsNone)
        {
            moves.Add(new Move(from, target));
        }
        else if (targetPiece.Color != color && targetPiece.Type != PieceType.King)
        {
            moves.Add(new Move(from, target, Flags: MoveFlags.Capture));
        }
    }

    private static bool PieceAttacksSquare(Position position, int from, Piece piece, int target)
    {
        if (from == target)
        {
            return false;
        }

        var fileDifference = Square.File(target) - Square.File(from);
        var rankDifference = Square.Rank(target) - Square.Rank(from);

        return piece.Type switch
        {
            PieceType.Pawn =>
                Math.Abs(fileDifference) == 1 &&
                rankDifference == (piece.Color == Color.White ? 1 : -1),
            PieceType.Knight =>
                (Math.Abs(fileDifference) == 1 && Math.Abs(rankDifference) == 2) ||
                (Math.Abs(fileDifference) == 2 && Math.Abs(rankDifference) == 1),
            PieceType.Bishop =>
                Math.Abs(fileDifference) == Math.Abs(rankDifference) &&
                IsRayClear(position, from, target, Math.Sign(fileDifference), Math.Sign(rankDifference)),
            PieceType.Rook =>
                (fileDifference == 0 || rankDifference == 0) &&
                IsRayClear(position, from, target, Math.Sign(fileDifference), Math.Sign(rankDifference)),
            PieceType.Queen =>
                (fileDifference == 0 || rankDifference == 0 ||
                 Math.Abs(fileDifference) == Math.Abs(rankDifference)) &&
                IsRayClear(position, from, target, Math.Sign(fileDifference), Math.Sign(rankDifference)),
            PieceType.King => Math.Abs(fileDifference) <= 1 && Math.Abs(rankDifference) <= 1,
            _ => false
        };
    }

    private static bool IsRayClear(
        Position position,
        int from,
        int target,
        int fileDirection,
        int rankDirection)
    {
        var file = Square.File(from) + fileDirection;
        var rank = Square.Rank(from) + rankDirection;
        while (Square.IsValid(file, rank))
        {
            var square = Square.FromCoordinates(file, rank);
            if (square == target)
            {
                return true;
            }

            if (!position[square].IsNone)
            {
                return false;
            }

            file += fileDirection;
            rank += rankDirection;
        }

        return false;
    }
}
