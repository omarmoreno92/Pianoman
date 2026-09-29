namespace PianoMan.Core.Chess;

public static class UciParser
{
    public static Move Parse(Position position,string uci)
    {
        ArgumentNullException.ThrowIfNull(position);
        if(string.IsNullOrWhiteSpace(uci))throw new FormatException("Se requiere una jugada UCI.");
        var normalized=uci.Trim().ToLowerInvariant();
        var matches=MoveGenerator.GenerateLegalMoves(position)
            .Where(move=>move.ToUci().Equals(normalized,StringComparison.Ordinal))
            .ToArray();
        return matches.Length switch
        {
            1=>matches[0],
            0=>throw new InvalidOperationException($"La jugada '{uci}' no es legal desde esta posición."),
            _=>throw new InvalidOperationException($"La jugada '{uci}' es ambigua.")
        };
    }
}
