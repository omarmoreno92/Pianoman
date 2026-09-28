using PianoMan.Core.Chess;
namespace PianoMan.Core.Theory;
public static class ZobristHasher
{
    private static readonly ulong[,] Pieces = BuildPieceKeys();
    private static readonly ulong SideKey = BuildSideKey();
    private static readonly ulong[] CastlingKeys = BuildKeys(16, 0x9E3779B97F4A7C15UL);
    private static readonly ulong[] EnPassantFileKeys = BuildKeys(8, 0xD1B54A32D192ED03UL);

    public static ulong Hash(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);ulong hash=0;
        foreach(var(square,piece) in position.Pieces()) hash ^= Pieces[PieceIndex(piece),square];
        if(position.SideToMove==Color.Black) hash ^= SideKey;
        hash ^= CastlingKeys[(int)position.CastlingRights & 0xF];
        if(CapturableEnPassantFile(position) is int file) hash ^= EnPassantFileKeys[file];
        return hash;
    }
    public static int? CapturableEnPassantFile(Position p)
    {
        if(p.EnPassantSquare is not int ep) return null;var epFile=Square.File(ep);var epRank=Square.Rank(ep);var fromRank=epRank+(p.SideToMove==Color.White?-1:1);if(fromRank is <0 or >7)return null;
        var pawn=new Piece(p.SideToMove,PieceType.Pawn);foreach(var df in new[]{-1,1}){var file=epFile+df;if(file is >=0 and <8&&p[Square.FromCoordinates(file,fromRank)]==pawn)return epFile;}return null;
    }
    private static int PieceIndex(Piece p)=>(p.Color==Color.White?0:6)+(int)p.Type-1;
    private static ulong BuildSideKey(){ulong seed=0xA0761D6478BD642FUL;return Next(ref seed);}
    private static ulong[,] BuildPieceKeys(){var a=new ulong[12,64];ulong s=0x243F6A8885A308D3UL;for(var p=0;p<12;p++)for(var sq=0;sq<64;sq++)a[p,sq]=Next(ref s);return a;}
    private static ulong[] BuildKeys(int n,ulong seed){var a=new ulong[n];for(var i=0;i<n;i++)a[i]=Next(ref seed);return a;}
    private static ulong Next(ref ulong state){state+=0x9E3779B97F4A7C15UL;var z=state;z=(z^(z>>30))*0xBF58476D1CE4E5B9UL;z=(z^(z>>27))*0x94D049BB133111EBUL;return z^(z>>31);}
}
