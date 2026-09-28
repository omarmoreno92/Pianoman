namespace PianoMan.Core.Chess;

[Flags]
public enum MoveFlags : byte
{
    None = 0,
    Capture = 1,
    EnPassant = 2,
    CastleKingSide = 4,
    CastleQueenSide = 8,
    DoublePawnPush = 16,
    Promotion = 32
}

public readonly record struct Move(
    int From,
    int To,
    PieceType Promotion = PieceType.None,
    MoveFlags Flags = MoveFlags.None)
{
    public bool IsCapture => (Flags & (MoveFlags.Capture | MoveFlags.EnPassant)) != 0;

    public bool IsCastle => (Flags & (MoveFlags.CastleKingSide | MoveFlags.CastleQueenSide)) != 0;

    public string ToUci()
    {
        var promotion = Promotion switch
        {
            PieceType.Queen => "q",
            PieceType.Rook => "r",
            PieceType.Bishop => "b",
            PieceType.Knight => "n",
            _ => string.Empty
        };

        return $"{Square.Name(From)}{Square.Name(To)}{promotion}";
    }

    public override string ToString() => ToUci();
}
