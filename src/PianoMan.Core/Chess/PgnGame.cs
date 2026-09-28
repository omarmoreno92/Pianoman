namespace PianoMan.Core.Chess;

public sealed record PgnGame(
    Dictionary<string, string> Headers,
    List<string> Moves)
{
    public string? Header(string name) => Headers.GetValueOrDefault(name);

    public Position CreateInitialPosition() =>
        Header("SetUp") == "1" && Header("FEN") is { Length: > 0 } fen
            ? Fen.Parse(fen)
            : Position.Initial;
}
