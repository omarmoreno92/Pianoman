namespace PianoMan.Core.Chess;

public readonly record struct Piece(Color Color, PieceType Type)
{
    public static Piece None => default;

    public bool IsNone => Type == PieceType.None;

    public char ToFenChar()
    {
        var value = Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new InvalidOperationException("An empty square has no FEN character.")
        };

        return Color == Color.White ? char.ToUpperInvariant(value) : value;
    }

    public static Piece FromFenChar(char value)
    {
        var type = char.ToLowerInvariant(value) switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => throw new FormatException($"Invalid FEN piece: '{value}'.")
        };

        return new Piece(char.IsUpper(value) ? Color.White : Color.Black, type);
    }
}
