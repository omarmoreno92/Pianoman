namespace PianoMan.Core.Chess;

public sealed class Position
{
    private readonly Piece[] _board;
    public Position(Piece[] board, Color sideToMove, CastlingRights castlingRights, int? enPassantSquare, int halfmoveClock, int fullmoveNumber)
    {
        if (board.Length != 64) throw new ArgumentException("A chess board must contain exactly 64 squares.", nameof(board));
        _board=(Piece[])board.Clone(); SideToMove=sideToMove; CastlingRights=castlingRights; EnPassantSquare=enPassantSquare; HalfmoveClock=halfmoveClock; FullmoveNumber=fullmoveNumber;
    }
    public static Position Initial=>Fen.Parse(Fen.InitialPosition);
    public Color SideToMove{get;private set;} public CastlingRights CastlingRights{get;private set;} public int? EnPassantSquare{get;private set;} public int HalfmoveClock{get;private set;} public int FullmoveNumber{get;private set;}
    public Piece this[int square]=>Square.IsValid(square)?_board[square]:throw new ArgumentOutOfRangeException(nameof(square));
    public Position Clone()=>new(_board,SideToMove,CastlingRights,EnPassantSquare,HalfmoveClock,FullmoveNumber);
    public Position WithSideToMove(Color color){var clone=Clone();clone.SideToMove=color;return clone;}
    public IEnumerable<(int Square,Piece Piece)> Pieces(){for(var s=0;s<64;s++)if(!_board[s].IsNone)yield return(s,_board[s]);}
    public int FindKing(Color color){for(var s=0;s<64;s++)if(_board[s]==new Piece(color,PieceType.King))return s;return -1;}
    public void Apply(Move move)
    {
        if(!Square.IsValid(move.From)||!Square.IsValid(move.To))throw new ArgumentOutOfRangeException(nameof(move));
        var moving=_board[move.From]; if(moving.IsNone||moving.Color!=SideToMove)throw new InvalidOperationException($"No {SideToMove} piece can move from {Square.Name(move.From)}.");
        var capturedSquare=(move.Flags&MoveFlags.EnPassant)!=0?move.To+(moving.Color==Color.White?-8:8):move.To;
        var captured=_board[capturedSquare]; _board[move.From]=Piece.None; _board[capturedSquare]=Piece.None;
        _board[move.To]=move.Promotion==PieceType.None?moving:new Piece(moving.Color,move.Promotion);
        if((move.Flags&MoveFlags.CastleKingSide)!=0)MoveRookForCastle(moving.Color,true); else if((move.Flags&MoveFlags.CastleQueenSide)!=0)MoveRookForCastle(moving.Color,false);
        UpdateCastlingRights(moving,move.From,captured,capturedSquare);
        EnPassantSquare=(move.Flags&MoveFlags.DoublePawnPush)!=0?move.From+(moving.Color==Color.White?8:-8):null;
        HalfmoveClock=moving.Type==PieceType.Pawn||!captured.IsNone?0:HalfmoveClock+1;
        if(SideToMove==Color.Black)FullmoveNumber++; SideToMove=SideToMove.Opposite();
    }
    public string ToFen()=>Fen.Serialize(this);
    private void MoveRookForCastle(Color color,bool kingSide){var rank=color==Color.White?0:7;var from=Square.FromCoordinates(kingSide?7:0,rank);var to=Square.FromCoordinates(kingSide?5:3,rank);_board[to]=_board[from];_board[from]=Piece.None;}
    private void UpdateCastlingRights(Piece moving,int from,Piece captured,int capturedSquare)
    {
        if(moving.Type==PieceType.King)CastlingRights&=moving.Color==Color.White?~(CastlingRights.WhiteKingSide|CastlingRights.WhiteQueenSide):~(CastlingRights.BlackKingSide|CastlingRights.BlackQueenSide);
        if(moving.Type==PieceType.Rook)RemoveRookCastlingRight(moving.Color,from); if(captured.Type==PieceType.Rook)RemoveRookCastlingRight(captured.Color,capturedSquare);
    }
    private void RemoveRookCastlingRight(Color color,int square){CastlingRights right=(color,square) switch{(Color.White,0)=>CastlingRights.WhiteQueenSide,(Color.White,7)=>CastlingRights.WhiteKingSide,(Color.Black,56)=>CastlingRights.BlackQueenSide,(Color.Black,63)=>CastlingRights.BlackKingSide,_=>CastlingRights.None};CastlingRights&=~right;}
}
