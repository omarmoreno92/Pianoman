namespace PianoMan.Core.Chess;

public static class SanParser
{
    public static Move Parse(Position position, string san)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentException.ThrowIfNullOrWhiteSpace(san);

        var normalized = Normalize(san);
        var legalMoves = MoveGenerator.GenerateLegalMoves(position);

        if (TryParseUci(normalized, legalMoves, out var uciMove))
        {
            return uciMove;
        }

        if (normalized is "O-O" or "O-O-O")
        {
            var requiredFlag = normalized == "O-O"
                ? MoveFlags.CastleKingSide
                : MoveFlags.CastleQueenSide;
            var castleMatches = legalMoves
                .Where(move => (move.Flags & requiredFlag) != 0)
                .ToArray();
            return SingleMatch(san, castleMatches);
        }

        var destinationIndex = FindDestinationIndex(normalized);
        if (destinationIndex < 0)
        {
            throw new FormatException($"SAN move '{san}' does not contain a destination square.");
        }

        var destination = Square.Parse(normalized.AsSpan(destinationIndex, 2));
        var pieceType = PieceType.Pawn;
        var prefixStart = 0;
        if (TryParsePieceType(normalized[0], out var parsedPieceType))
        {
            pieceType = parsedPieceType;
            prefixStart = 1;
        }

        var promotion = ParsePromotion(normalized);
        var expectsCapture = normalized.Contains('x');
        var disambiguation = normalized[prefixStart..destinationIndex]
            .Replace("x", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var matches = legalMoves.Where(move =>
        {
            var movingPiece = position[move.From];
            if (movingPiece.Type != pieceType || move.To != destination)
            {
                return false;
            }

            if (move.IsCapture != expectsCapture || move.Promotion != promotion)
            {
                return false;
            }

            foreach (var symbol in disambiguation)
            {
                if (symbol is >= 'a' and <= 'h' && Square.File(move.From) != symbol - 'a')
                {
                    return false;
                }

                if (symbol is >= '1' and <= '8' && Square.Rank(move.From) != symbol - '1')
                {
                    return false;
                }
            }

            return true;
        }).ToArray();

        return SingleMatch(san, matches);
    }

    private static string Normalize(string san)
    {
        var value = san.Trim().Replace('0', 'O');
        while (value.Length > 0 && value[^1] is '+' or '#' or '!' or '?')
        {
            value = value[..^1];
        }

        return value.EndsWith("e.p.", StringComparison.OrdinalIgnoreCase)
            ? value[..^4].TrimEnd()
            : value;
    }

    private static bool TryParseUci(
        string value,
        IReadOnlyList<Move> legalMoves,
        out Move result)
    {
        result = default;
        if (value.Length is not (4 or 5) ||
            !Square.TryParse(value.AsSpan(0, 2), out var from) ||
            !Square.TryParse(value.AsSpan(2, 2), out var to))
        {
            return false;
        }

        var promotion = value.Length == 5 ? ParsePromotionPiece(value[4]) : PieceType.None;
        var matches = legalMoves
            .Where(move => move.From == from && move.To == to && move.Promotion == promotion)
            .ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        result = matches[0];
        return true;
    }

    private static int FindDestinationIndex(string value)
    {
        for (var index = value.Length - 2; index >= 0; index--)
        {
            if (Square.TryParse(value.AsSpan(index, 2), out _))
            {
                return index;
            }
        }

        return -1;
    }

    private static PieceType ParsePromotion(string value)
    {
        var equalsIndex = value.IndexOf('=');
        if (equalsIndex < 0)
        {
            return PieceType.None;
        }

        if (equalsIndex + 1 >= value.Length)
        {
            throw new FormatException("A SAN promotion must name the promoted piece.");
        }

        return ParsePromotionPiece(value[equalsIndex + 1]);
    }

    private static PieceType ParsePromotionPiece(char value) => char.ToUpperInvariant(value) switch
    {
        'Q' => PieceType.Queen,
        'R' => PieceType.Rook,
        'B' => PieceType.Bishop,
        'N' => PieceType.Knight,
        _ => throw new FormatException($"Invalid promotion piece: '{value}'.")
    };

    private static bool TryParsePieceType(char value, out PieceType pieceType)
    {
        pieceType = value switch
        {
            'N' => PieceType.Knight,
            'B' => PieceType.Bishop,
            'R' => PieceType.Rook,
            'Q' => PieceType.Queen,
            'K' => PieceType.King,
            _ => PieceType.None
        };

        return pieceType != PieceType.None;
    }

    private static Move SingleMatch(string san, Move[] matches) => matches.Length switch
    {
        1 => matches[0],
        0 => throw new InvalidOperationException($"SAN move '{san}' is not legal in the current position."),
        _ => throw new InvalidOperationException($"SAN move '{san}' is ambiguous in the current position.")
    };
}
