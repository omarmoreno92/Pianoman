using PianoMan.Core.Chess;
namespace PianoMan.Core.Theory;
public static class PackedMove
{
    public static ushort Pack(Move move){var promo=move.Promotion switch{PieceType.None=>0,PieceType.Knight=>1,PieceType.Bishop=>2,PieceType.Rook=>3,PieceType.Queen=>4,_=>throw new ArgumentOutOfRangeException(nameof(move))};return (ushort)(move.From|(move.To<<6)|(promo<<12));}
    public static Move Resolve(Position position,ushort packed){var from=packed&0x3F;var to=(packed>>6)&0x3F;var promo=(packed>>12)&0x7;var pt=promo switch{0=>PieceType.None,1=>PieceType.Knight,2=>PieceType.Bishop,3=>PieceType.Rook,4=>PieceType.Queen,_=>PieceType.None};return MoveGenerator.GenerateLegalMoves(position).Single(m=>m.From==from&&m.To==to&&m.Promotion==pt);}
}
