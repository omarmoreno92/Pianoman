using System.Globalization; using System.Text;
namespace PianoMan.Core.Chess;
public static class Fen
{
    public const string InitialPosition="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public static Position Parse(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);var f=fen.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(f.Length!=6)throw new FormatException("A FEN position must contain six fields.");
        var board=ParseBoard(f[0]);var side=f[1] switch{"w"=>Color.White,"b"=>Color.Black,_=>throw new FormatException("Invalid active color.")};var rights=ParseCastling(f[2]);int? ep=f[3]=="-"?null:Square.Parse(f[3]);
        if(!int.TryParse(f[4],NumberStyles.None,CultureInfo.InvariantCulture,out var half)||half<0)throw new FormatException("Invalid FEN halfmove clock.");if(!int.TryParse(f[5],NumberStyles.None,CultureInfo.InvariantCulture,out var full)||full<1)throw new FormatException("Invalid FEN fullmove number.");return new Position(board,side,rights,ep,half,full);
    }
    public static string Serialize(Position p)
    {
        var b=new StringBuilder(90);for(var r=7;r>=0;r--){var empty=0;for(var file=0;file<8;file++){var piece=p[Square.FromCoordinates(file,r)];if(piece.IsNone){empty++;continue;}if(empty>0){b.Append(empty);empty=0;}b.Append(piece.ToFenChar());}if(empty>0)b.Append(empty);if(r>0)b.Append('/');}
        b.Append(p.SideToMove==Color.White?" w ":" b ");AppendCastling(b,p.CastlingRights);b.Append(' ').Append(p.EnPassantSquare is{} s?Square.Name(s):"-").Append(' ').Append(p.HalfmoveClock.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(p.FullmoveNumber.ToString(CultureInfo.InvariantCulture));return b.ToString();
    }
    private static Piece[] ParseBoard(string field){var ranks=field.Split('/');if(ranks.Length!=8)throw new FormatException("A FEN board must contain eight ranks.");var board=new Piece[64];for(var fr=0;fr<8;fr++){var file=0;foreach(var c in ranks[fr]){if(char.IsAsciiDigit(c)){var n=c-'0';if(n is <1 or >8)throw new FormatException();file+=n;continue;}if(file>=8)throw new FormatException();board[Square.FromCoordinates(file,7-fr)]=Piece.FromFenChar(c);file++;}if(file!=8)throw new FormatException("Every FEN rank must describe exactly eight squares.");}return board;}
    private static CastlingRights ParseCastling(string v){if(v=="-")return CastlingRights.None;var r=CastlingRights.None;foreach(var c in v)r|=c switch{'K'=>CastlingRights.WhiteKingSide,'Q'=>CastlingRights.WhiteQueenSide,'k'=>CastlingRights.BlackKingSide,'q'=>CastlingRights.BlackQueenSide,_=>throw new FormatException("Invalid castling rights.")};return r;}
    private static void AppendCastling(StringBuilder b,CastlingRights r){if(r==CastlingRights.None){b.Append('-');return;}if((r&CastlingRights.WhiteKingSide)!=0)b.Append('K');if((r&CastlingRights.WhiteQueenSide)!=0)b.Append('Q');if((r&CastlingRights.BlackKingSide)!=0)b.Append('k');if((r&CastlingRights.BlackQueenSide)!=0)b.Append('q');}
}
