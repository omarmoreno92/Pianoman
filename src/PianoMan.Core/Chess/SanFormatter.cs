using System.Text;
namespace PianoMan.Core.Chess;
public static class SanFormatter
{
    public static string Format(Position position,Move move)
    {
        ArgumentNullException.ThrowIfNull(position);var legal=MoveGenerator.GenerateLegalMoves(position);if(!legal.Contains(move))throw new InvalidOperationException("Cannot format an illegal move as SAN.");
        if((move.Flags&MoveFlags.CastleKingSide)!=0)return Suffix(position,move,"O-O");if((move.Flags&MoveFlags.CastleQueenSide)!=0)return Suffix(position,move,"O-O-O");
        var piece=position[move.From];var b=new StringBuilder();if(piece.Type!=PieceType.Pawn)b.Append(PieceLetter(piece.Type));
        var peers=legal.Where(m=>m!=move&&m.To==move.To&&position[m.From].Type==piece.Type).ToArray();
        if(piece.Type==PieceType.Pawn&&move.IsCapture)b.Append((char)('a'+Square.File(move.From)));else if(peers.Length>0){var sameFile=peers.Any(m=>Square.File(m.From)==Square.File(move.From));var sameRank=peers.Any(m=>Square.Rank(m.From)==Square.Rank(move.From));if(!sameFile)b.Append((char)('a'+Square.File(move.From)));else if(!sameRank)b.Append((char)('1'+Square.Rank(move.From)));else b.Append(Square.Name(move.From));}
        if(move.IsCapture)b.Append('x');b.Append(Square.Name(move.To));if(move.Promotion!=PieceType.None)b.Append('=').Append(PieceLetter(move.Promotion));return Suffix(position,move,b.ToString());
    }
    private static string Suffix(Position p,Move m,string san){var n=p.Clone();n.Apply(m);return MoveGenerator.IsCheckmate(n)?san+"#":MoveGenerator.IsKingInCheck(n,n.SideToMove)?san+"+":san;}
    private static char PieceLetter(PieceType t)=>t switch{PieceType.Knight=>'N',PieceType.Bishop=>'B',PieceType.Rook=>'R',PieceType.Queen=>'Q',PieceType.King=>'K',_=>throw new ArgumentOutOfRangeException(nameof(t))};
}
