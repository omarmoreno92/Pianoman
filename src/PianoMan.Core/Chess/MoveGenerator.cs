namespace PianoMan.Core.Chess;

public static class MoveGenerator
{
    private static readonly (int File,int Rank)[] KnightOffsets=[(1,2),(2,1),(2,-1),(1,-2),(-1,-2),(-2,-1),(-2,1),(-1,2)];
    private static readonly (int File,int Rank)[] BishopDirections=[(1,1),(1,-1),(-1,1),(-1,-1)];
    private static readonly (int File,int Rank)[] RookDirections=[(1,0),(-1,0),(0,1),(0,-1)];
    private static readonly (int File,int Rank)[] QueenDirections=[(1,1),(1,-1),(-1,1),(-1,-1),(1,0),(-1,0),(0,1),(0,-1)];
    private static readonly PieceType[] PromotionPieces=[PieceType.Queen,PieceType.Rook,PieceType.Bishop,PieceType.Knight];

    public static IReadOnlyList<Move> GenerateLegalMoves(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);var color=position.SideToMove;var pseudo=GeneratePseudoLegalMoves(position);var legal=new List<Move>(pseudo.Count);
        foreach(var move in pseudo){var next=position.Clone();next.Apply(move);if(!IsKingInCheck(next,color))legal.Add(move);}return legal;
    }
    public static bool IsKingInCheck(Position position,Color color){var king=position.FindKing(color);return king>=0&&IsSquareAttacked(position,king,color.Opposite());}
    public static bool IsCheckmate(Position position)=>IsKingInCheck(position,position.SideToMove)&&GenerateLegalMoves(position).Count==0;
    public static bool IsStalemate(Position position)=>!IsKingInCheck(position,position.SideToMove)&&GenerateLegalMoves(position).Count==0;
    public static bool IsSquareAttacked(Position position,int target,Color byColor){foreach(var(from,piece)in position.Pieces())if(piece.Color==byColor&&PieceAttacksSquare(position,from,piece,target))return true;return false;}
    public static int CountAttackedSquares(Position position,Color color){var count=0;for(var square=0;square<64;square++)if(IsSquareAttacked(position,square,color))count++;return count;}

    private static List<Move> GeneratePseudoLegalMoves(Position position)
    {
        var moves=new List<Move>(48);foreach(var(square,piece)in position.Pieces()){if(piece.Color!=position.SideToMove)continue;switch(piece.Type){case PieceType.Pawn:AddPawnMoves(position,square,piece.Color,moves);break;case PieceType.Knight:AddJumpMoves(position,square,piece.Color,KnightOffsets,moves);break;case PieceType.Bishop:AddSlidingMoves(position,square,piece.Color,BishopDirections,moves);break;case PieceType.Rook:AddSlidingMoves(position,square,piece.Color,RookDirections,moves);break;case PieceType.Queen:AddSlidingMoves(position,square,piece.Color,QueenDirections,moves);break;case PieceType.King:AddKingMoves(position,square,piece.Color,moves);break;}}return moves;
    }
    private static void AddPawnMoves(Position position,int from,Color color,List<Move> moves)
    {
        var file=Square.File(from);var rank=Square.Rank(from);var dir=color==Color.White?1:-1;var start=color==Color.White?1:6;var promo=color==Color.White?7:0;var nr=rank+dir;
        if(Square.IsValid(file,nr)){var one=Square.FromCoordinates(file,nr);if(position[one].IsNone){AddPawnMove(from,one,nr==promo,MoveFlags.None,moves);if(rank==start){var two=Square.FromCoordinates(file,rank+2*dir);if(position[two].IsNone)moves.Add(new Move(from,two,Flags:MoveFlags.DoublePawnPush));}}}
        foreach(var off in new[]{-1,1}){var tf=file+off;if(!Square.IsValid(tf,nr))continue;var target=Square.FromCoordinates(tf,nr);var p=position[target];if(!p.IsNone&&p.Color!=color&&p.Type!=PieceType.King)AddPawnMove(from,target,nr==promo,MoveFlags.Capture,moves);else if(position.EnPassantSquare==target)moves.Add(new Move(from,target,Flags:MoveFlags.Capture|MoveFlags.EnPassant));}
    }
    private static void AddPawnMove(int from,int to,bool promotion,MoveFlags flags,List<Move> moves){if(!promotion){moves.Add(new Move(from,to,Flags:flags));return;}foreach(var p in PromotionPieces)moves.Add(new Move(from,to,p,flags|MoveFlags.Promotion));}
    private static void AddJumpMoves(Position position,int from,Color color,IReadOnlyList<(int File,int Rank)> offsets,List<Move> moves){var ff=Square.File(from);var fr=Square.Rank(from);foreach(var(df,dr)in offsets){var f=ff+df;var r=fr+dr;if(Square.IsValid(f,r))AddMoveIfAvailable(position,from,Square.FromCoordinates(f,r),color,moves);}}
    private static void AddSlidingMoves(Position position,int from,Color color,IReadOnlyList<(int File,int Rank)> dirs,List<Move> moves){var ff=Square.File(from);var fr=Square.Rank(from);foreach(var(df,dr)in dirs){var f=ff+df;var r=fr+dr;while(Square.IsValid(f,r)){var target=Square.FromCoordinates(f,r);var p=position[target];if(p.IsNone)moves.Add(new Move(from,target));else{if(p.Color!=color&&p.Type!=PieceType.King)moves.Add(new Move(from,target,Flags:MoveFlags.Capture));break;}f+=df;r+=dr;}}}
    private static void AddKingMoves(Position p,int from,Color color,List<Move> moves){AddJumpMoves(p,from,color,QueenDirections,moves);AddCastlingMoves(p,from,color,moves);}
    private static void AddCastlingMoves(Position p,int kingSquare,Color color,List<Move> moves)
    {
        var rank=color==Color.White?0:7;if(kingSquare!=Square.FromCoordinates(4,rank)||IsKingInCheck(p,color))return;var opp=color.Opposite();var ks=color==Color.White?CastlingRights.WhiteKingSide:CastlingRights.BlackKingSide;var qs=color==Color.White?CastlingRights.WhiteQueenSide:CastlingRights.BlackQueenSide;
        if((p.CastlingRights&ks)!=0&&HasRook(p,color,7,rank)&&p[Square.FromCoordinates(5,rank)].IsNone&&p[Square.FromCoordinates(6,rank)].IsNone&&!IsSquareAttacked(p,Square.FromCoordinates(5,rank),opp)&&!IsSquareAttacked(p,Square.FromCoordinates(6,rank),opp))moves.Add(new Move(kingSquare,Square.FromCoordinates(6,rank),Flags:MoveFlags.CastleKingSide));
        if((p.CastlingRights&qs)!=0&&HasRook(p,color,0,rank)&&p[Square.FromCoordinates(1,rank)].IsNone&&p[Square.FromCoordinates(2,rank)].IsNone&&p[Square.FromCoordinates(3,rank)].IsNone&&!IsSquareAttacked(p,Square.FromCoordinates(3,rank),opp)&&!IsSquareAttacked(p,Square.FromCoordinates(2,rank),opp))moves.Add(new Move(kingSquare,Square.FromCoordinates(2,rank),Flags:MoveFlags.CastleQueenSide));
    }
    private static bool HasRook(Position p,Color c,int f,int r)=>p[Square.FromCoordinates(f,r)]==new Piece(c,PieceType.Rook);
    private static void AddMoveIfAvailable(Position p,int from,int target,Color color,List<Move> moves){var t=p[target];if(t.IsNone)moves.Add(new Move(from,target));else if(t.Color!=color&&t.Type!=PieceType.King)moves.Add(new Move(from,target,Flags:MoveFlags.Capture));}
    private static bool PieceAttacksSquare(Position p,int from,Piece piece,int target)
    {
        if(from==target)return false;var df=Square.File(target)-Square.File(from);var dr=Square.Rank(target)-Square.Rank(from);return piece.Type switch{PieceType.Pawn=>Math.Abs(df)==1&&dr==(piece.Color==Color.White?1:-1),PieceType.Knight=>(Math.Abs(df)==1&&Math.Abs(dr)==2)||(Math.Abs(df)==2&&Math.Abs(dr)==1),PieceType.Bishop=>Math.Abs(df)==Math.Abs(dr)&&IsRayClear(p,from,target,Math.Sign(df),Math.Sign(dr)),PieceType.Rook=>(df==0||dr==0)&&IsRayClear(p,from,target,Math.Sign(df),Math.Sign(dr)),PieceType.Queen=>(df==0||dr==0||Math.Abs(df)==Math.Abs(dr))&&IsRayClear(p,from,target,Math.Sign(df),Math.Sign(dr)),PieceType.King=>Math.Abs(df)<=1&&Math.Abs(dr)<=1,_=>false};
    }
    private static bool IsRayClear(Position p,int from,int target,int df,int dr){var f=Square.File(from)+df;var r=Square.Rank(from)+dr;while(Square.IsValid(f,r)){var s=Square.FromCoordinates(f,r);if(s==target)return true;if(!p[s].IsNone)return false;f+=df;r+=dr;}return false;}
}
