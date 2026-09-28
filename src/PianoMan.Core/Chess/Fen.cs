using System.Globalization;
using System.Text;

namespace PianoMan.Core.Chess;

public static class Fen
{
    public const string InitialPosition =
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static Position Parse(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);

        var fields = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 6)
        {
            throw new FormatException("A FEN position must contain six fields.");
        }

        var board = ParseBoard(fields[0]);
        var sideToMove = fields[1] switch
        {
            "w" => Color.White,
            "b" => Color.Black,
            _ => throw new FormatException("The FEN active-color field must be 'w' or 'b'.")
        };

        var castlingRights = ParseCastlingRights(fields[2]);
        int? enPassantSquare = fields[3] == "-" ? null : Square.Parse(fields[3]);

        if (!int.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var halfmoveClock) ||
            halfmoveClock < 0)
        {
            throw new FormatException("Invalid FEN halfmove clock.");
        }

        if (!int.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out var fullmoveNumber) ||
            fullmoveNumber < 1)
        {
            throw new FormatException("Invalid FEN fullmove number.");
        }

        return new Position(
            board,
            sideToMove,
            castlingRights,
            enPassantSquare,
            halfmoveClock,
            fullmoveNumber);
    }

    public static string Serialize(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        var builder = new StringBuilder(90);
        for (var rank = 7; rank >= 0; rank--)
        {
            var empty = 0;
            for (var file = 0; file < 8; file++)
            {
                var piece = position[Square.FromCoordinates(file, rank)];
                if (piece.IsNone)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    builder.Append(empty);
                    empty = 0;
                }

                builder.Append(piece.ToFenChar());
            }

            if (empty > 0)
            {
                builder.Append(empty);
            }

            if (rank > 0)
            {
                builder.Append('/');
            }
        }

        builder.Append(position.SideToMove == Color.White ? " w " : " b ");
        AppendCastlingRights(builder, position.CastlingRights);
        builder.Append(' ');
        builder.Append(position.EnPassantSquare is { } square ? Square.Name(square) : "-");
        builder.Append(' ');
        builder.Append(position.HalfmoveClock.ToString(CultureInfo.InvariantCulture));
        builder.Append(' ');
        builder.Append(position.FullmoveNumber.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static Piece[] ParseBoard(string boardField)
    {
        var ranks = boardField.Split('/');
        if (ranks.Length != 8)
        {
            throw new FormatException("A FEN board must contain eight ranks.");
        }

        var board = new Piece[64];
        for (var fenRank = 0; fenRank < 8; fenRank++)
        {
            var file = 0;
            foreach (var value in ranks[fenRank])
            {
                if (char.IsAsciiDigit(value))
                {
                    var emptySquares = value - '0';
                    if (emptySquares is < 1 or > 8)
                    {
                        throw new FormatException("FEN empty-square counts must be between 1 and 8.");
                    }

                    file += emptySquares;
                    continue;
                }

                if (file >= 8)
                {
                    throw new FormatException("A FEN rank contains more than eight squares.");
                }

                var boardRank = 7 - fenRank;
                board[Square.FromCoordinates(file, boardRank)] = Piece.FromFenChar(value);
                file++;
            }

            if (file != 8)
            {
                throw new FormatException("Every FEN rank must describe exactly eight squares.");
            }
        }

        return board;
    }

    private static CastlingRights ParseCastlingRights(string value)
    {
        if (value == "-")
        {
            return CastlingRights.None;
        }

        var rights = CastlingRights.None;
        foreach (var symbol in value)
        {
            rights |= symbol switch
            {
                'K' => CastlingRights.WhiteKingSide,
                'Q' => CastlingRights.WhiteQueenSide,
                'k' => CastlingRights.BlackKingSide,
                'q' => CastlingRights.BlackQueenSide,
                _ => throw new FormatException($"Invalid FEN castling symbol: '{symbol}'.")
            };
        }

        return rights;
    }

    private static void AppendCastlingRights(StringBuilder builder, CastlingRights rights)
    {
        if (rights == CastlingRights.None)
        {
            builder.Append('-');
            return;
        }

        if ((rights & CastlingRights.WhiteKingSide) != 0) builder.Append('K');
        if ((rights & CastlingRights.WhiteQueenSide) != 0) builder.Append('Q');
        if ((rights & CastlingRights.BlackKingSide) != 0) builder.Append('k');
        if ((rights & CastlingRights.BlackQueenSide) != 0) builder.Append('q');
    }
}
