namespace PianoMan.Core.Chess;

public sealed class Position
{
    private readonly Piece[] _board;

    public Position(
        Piece[] board,
        Color sideToMove,
        CastlingRights castlingRights,
        int? enPassantSquare,
        int halfmoveClock,
        int fullmoveNumber)
    {
        if (board.Length != 64)
        {
            throw new ArgumentException("A chess board must contain exactly 64 squares.", nameof(board));
        }

        _board = (Piece[])board.Clone();
        SideToMove = sideToMove;
        CastlingRights = castlingRights;
        EnPassantSquare = enPassantSquare;
        HalfmoveClock = halfmoveClock;
        FullmoveNumber = fullmoveNumber;
    }

    public static Position Initial => Fen.Parse(Fen.InitialPosition);

    public Color SideToMove { get; private set; }

    public CastlingRights CastlingRights { get; private set; }

    public int? EnPassantSquare { get; private set; }

    public int HalfmoveClock { get; private set; }

    public int FullmoveNumber { get; private set; }

    public Piece this[int square]
    {
        get
        {
            if (!Square.IsValid(square))
            {
                throw new ArgumentOutOfRangeException(nameof(square));
            }

            return _board[square];
        }
    }

    public Position Clone() => new(
        _board,
        SideToMove,
        CastlingRights,
        EnPassantSquare,
        HalfmoveClock,
        FullmoveNumber);

    public Position WithSideToMove(Color color)
    {
        var clone = Clone();
        clone.SideToMove = color;
        return clone;
    }

    public IEnumerable<(int Square, Piece Piece)> Pieces()
    {
        for (var square = 0; square < 64; square++)
        {
            if (!_board[square].IsNone)
            {
                yield return (square, _board[square]);
            }
        }
    }

    public int FindKing(Color color)
    {
        for (var square = 0; square < 64; square++)
        {
            if (_board[square] == new Piece(color, PieceType.King))
            {
                return square;
            }
        }

        return -1;
    }

    public void Apply(Move move)
    {
        if (!Square.IsValid(move.From) || !Square.IsValid(move.To))
        {
            throw new ArgumentOutOfRangeException(nameof(move));
        }

        var movingPiece = _board[move.From];
        if (movingPiece.IsNone || movingPiece.Color != SideToMove)
        {
            throw new InvalidOperationException($"No {SideToMove} piece can move from {Square.Name(move.From)}.");
        }

        var capturedSquare = move.To;
        if ((move.Flags & MoveFlags.EnPassant) != 0)
        {
            capturedSquare = move.To + (movingPiece.Color == Color.White ? -8 : 8);
        }

        var capturedPiece = _board[capturedSquare];
        _board[move.From] = Piece.None;
        _board[capturedSquare] = Piece.None;

        var placedPiece = move.Promotion == PieceType.None
            ? movingPiece
            : new Piece(movingPiece.Color, move.Promotion);
        _board[move.To] = placedPiece;

        if ((move.Flags & MoveFlags.CastleKingSide) != 0)
        {
            MoveRookForCastle(movingPiece.Color, kingSide: true);
        }
        else if ((move.Flags & MoveFlags.CastleQueenSide) != 0)
        {
            MoveRookForCastle(movingPiece.Color, kingSide: false);
        }

        UpdateCastlingRights(movingPiece, move.From, capturedPiece, capturedSquare);

        EnPassantSquare = (move.Flags & MoveFlags.DoublePawnPush) != 0
            ? move.From + (movingPiece.Color == Color.White ? 8 : -8)
            : null;

        HalfmoveClock = movingPiece.Type == PieceType.Pawn || !capturedPiece.IsNone
            ? 0
            : HalfmoveClock + 1;

        if (SideToMove == Color.Black)
        {
            FullmoveNumber++;
        }

        SideToMove = SideToMove.Opposite();
    }

    public string ToFen() => Fen.Serialize(this);

    private void MoveRookForCastle(Color color, bool kingSide)
    {
        var rank = color == Color.White ? 0 : 7;
        var rookFrom = Square.FromCoordinates(kingSide ? 7 : 0, rank);
        var rookTo = Square.FromCoordinates(kingSide ? 5 : 3, rank);
        _board[rookTo] = _board[rookFrom];
        _board[rookFrom] = Piece.None;
    }

    private void UpdateCastlingRights(
        Piece movingPiece,
        int from,
        Piece capturedPiece,
        int capturedSquare)
    {
        if (movingPiece.Type == PieceType.King)
        {
            CastlingRights &= movingPiece.Color == Color.White
                ? ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide)
                : ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);
        }

        if (movingPiece.Type == PieceType.Rook)
        {
            RemoveRookCastlingRight(movingPiece.Color, from);
        }

        if (capturedPiece.Type == PieceType.Rook)
        {
            RemoveRookCastlingRight(capturedPiece.Color, capturedSquare);
        }
    }

    private void RemoveRookCastlingRight(Color color, int rookSquare)
    {
        CastlingRights right = (color, rookSquare) switch
        {
            (Color.White, 0) => CastlingRights.WhiteQueenSide,
            (Color.White, 7) => CastlingRights.WhiteKingSide,
            (Color.Black, 56) => CastlingRights.BlackQueenSide,
            (Color.Black, 63) => CastlingRights.BlackKingSide,
            _ => CastlingRights.None
        };

        CastlingRights &= ~right;
    }
}
