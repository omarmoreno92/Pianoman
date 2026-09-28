namespace PianoMan.Core.Chess;

public static class SanParser
{
    public static Move Parse(Position position,string san)
    {
        ArgumentNullException.ThrowIfNull(position);ArgumentException.ThrowIfNullOrWhiteSpace(san);var normalized=Normalize(san);var legal=MoveGenerator.GenerateLegalMoves(position);
        if(TryParseUci(normalized,legal,out var uci))return uci;
        if(normalized is "O-O" or "O-O-O"){var flag=normalized=="O-O"?MoveFlags.CastleKingSide:MoveFlags.CastleQueenSide;return SingleMatch(san,legal.Where(m=>(m.Flags&flag)!=0).ToArray());}
        var di=FindDestinationIndex(normalized);if(di<0)throw new FormatException($"SAN move '{san}' does not contain a destination square.");var dest=Square.Parse(normalized.AsSpan(di,2));var type=PieceType.Pawn;var start=0;if(TryParsePieceType(normalized[0],out var t)){type=t;start=1;}var promotion=ParsePromotion(normalized);var capture=normalized.Contains('x');var dis=normalized[start..di].Replace("x",string.Empty,StringComparison.Ordinal).Replace("-",string.Empty,StringComparison.Ordinal);
        var matches=legal.Where(m=>{var moving=position[m.From];if(moving.Type!=type||m.To!=dest||m.IsCapture!=capture||m.Promotion!=promotion)return false;foreach(var c in dis){if(c is >= 'a' and <= 'h'&&Square.File(m.From)!=c-'a')return false;if(c is >= '1' and <= '8'&&Square.Rank(m.From)!=c-'1')return false;}return true;}).ToArray();return SingleMatch(san,matches);
    }
    private static string Normalize(string san){var v=san.Trim().Replace('0','O');while(v.Length>0&&v[^1] is '+' or '#' or '!' or '?')v=v[..^1];return v.EndsWith("e.p.",StringComparison.OrdinalIgnoreCase)?v[..^4].TrimEnd():v;}
    private static bool TryParseUci(string value,IReadOnlyList<Move> legal,out Move result){result=default;if(value.Length is not(4 or 5)||!Square.TryParse(value.AsSpan(0,2),out var from)||!Square.TryParse(value.AsSpan(2,2),out var to))return false;var p=value.Length==5?ParsePromotionPiece(value[4]):PieceType.None;var ms=legal.Where(m=>m.From==from&&m.To==to&&m.Promotion==p).ToArray();if(ms.Length!=1)return false;result=ms[0];return true;}
    private static int FindDestinationIndex(string v){for(var i=v.Length-2;i>=0;i--)if(Square.TryParse(v.AsSpan(i,2),out _))return i;return -1;}
    private static PieceType ParsePromotion(string v){var i=v.IndexOf('=');return i<0?PieceType.None:i+1<v.Length?ParsePromotionPiece(v[i+1]):throw new FormatException("A SAN promotion must name the promoted piece.");}
    private static PieceType ParsePromotionPiece(char v)=>char.ToUpperInvariant(v) switch{'Q'=>PieceType.Queen,'R'=>PieceType.Rook,'B'=>PieceType.Bishop,'N'=>PieceType.Knight,_=>throw new FormatException($"Invalid promotion piece: '{v}'.")};
    private static bool TryParsePieceType(char v,out PieceType t){t=v switch{'N'=>PieceType.Knight,'B'=>PieceType.Bishop,'R'=>PieceType.Rook,'Q'=>PieceType.Queen,'K'=>PieceType.King,_=>PieceType.None};return t!=PieceType.None;}
    private static Move SingleMatch(string san,Move[] matches)=>matches.Length switch{1=>matches[0],0=>throw new InvalidOperationException($"SAN move '{san}' is not legal in the current position."),_=>throw new InvalidOperationException($"SAN move '{san}' is ambiguous in the current position.")};
}
