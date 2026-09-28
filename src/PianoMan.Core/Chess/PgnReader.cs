using System.Text;

namespace PianoMan.Core.Chess;

public static class PgnReader
{
    private static readonly HashSet<string> Results =
        new(StringComparer.Ordinal) { "1-0", "0-1", "1/2-1/2", "*" };

    public static PgnGame Read(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var moveText = new StringBuilder(content.Length);

        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                ParseHeader(trimmed, headers);
            }
            else
            {
                moveText.AppendLine(line);
            }
        }

        var cleaned = RemoveCommentsAndVariations(moveText.ToString());
        var moves = new List<string>();
        foreach (var rawToken in cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var token = StripMoveNumber(rawToken);
            if (token.Length == 0 ||
                token == "..." ||
                token.StartsWith('$') ||
                Results.Contains(token) ||
                token.Equals("e.p.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            moves.Add(token);
        }

        if (moves.Count == 0)
        {
            throw new FormatException("The PGN does not contain any main-line moves.");
        }

        return new PgnGame(headers, moves);
    }

    public static PgnGame ReadFile(string path) => Read(File.ReadAllText(path));

    private static void ParseHeader(string line, Dictionary<string, string> headers)
    {
        var separator = line.IndexOf(' ');
        var firstQuote = line.IndexOf('"');
        var lastQuote = line.LastIndexOf('"');
        if (separator <= 1 || firstQuote < separator || lastQuote <= firstQuote)
        {
            throw new FormatException($"Invalid PGN header: {line}");
        }

        var name = line[1..separator];
        var value = line[(firstQuote + 1)..lastQuote]
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
        headers[name] = value;
    }

    private static string RemoveCommentsAndVariations(string value)
    {
        var result = new StringBuilder(value.Length);
        var braceDepth = 0;
        var variationDepth = 0;
        var lineComment = false;

        foreach (var symbol in value)
        {
            if (lineComment)
            {
                if (symbol == '\n')
                {
                    lineComment = false;
                    result.Append(' ');
                }

                continue;
            }

            if (symbol == ';' && braceDepth == 0 && variationDepth == 0)
            {
                lineComment = true;
                continue;
            }

            if (symbol == '{')
            {
                braceDepth++;
                continue;
            }

            if (symbol == '}' && braceDepth > 0)
            {
                braceDepth--;
                if (braceDepth == 0) result.Append(' ');
                continue;
            }

            if (braceDepth > 0)
            {
                continue;
            }

            if (symbol == '(')
            {
                variationDepth++;
                continue;
            }

            if (symbol == ')' && variationDepth > 0)
            {
                variationDepth--;
                if (variationDepth == 0) result.Append(' ');
                continue;
            }

            if (variationDepth == 0)
            {
                result.Append(symbol);
            }
        }

        if (braceDepth != 0 || variationDepth != 0)
        {
            throw new FormatException("The PGN contains an unterminated comment or variation.");
        }

        return result.ToString();
    }

    private static string StripMoveNumber(string token)
    {
        var lastDot = token.LastIndexOf('.');
        if (lastDot < 0)
        {
            return token;
        }

        var prefix = token.AsSpan(0, lastDot + 1);
        foreach (var symbol in prefix)
        {
            if (!char.IsAsciiDigit(symbol) && symbol != '.')
            {
                return token;
            }
        }

        return token[(lastDot + 1)..];
    }
}
