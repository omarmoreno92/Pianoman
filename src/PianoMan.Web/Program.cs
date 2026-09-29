using System.Text.Json;
using System.Text.Json.Serialization;
using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;
using PianoMan.Core.Theory;

const int MaxPgnBytes = 2 * 1024 * 1024;
const string DemoPgn = """
[Event "Piano Man Demo"]
[Site "Piano Man"]
[Date "2026.09.28"]
[Round "1"]
[White "White"]
[Black "Black"]
[Result "*"]

1. e4 e5 2. Nf3 Nc6 *
""";

const string EvergreenPgn = """
[Event "La Siempreviva"]
[Site "Berlin"]
[Date "1852.??.??"]
[White "Adolf Anderssen"]
[Black "Jean Dufresne"]
[Result "1-0"]
[ECO "C52"]

1. e4 e5 2. Nf3 Nc6 3. Bc4 Bc5 4. b4 Bxb4 5. c3 Ba5
6. d4 exd4 7. O-O d3 8. Qb3 Qf6 9. e5 Qg6 10. Re1 Nge7
11. Ba3 b5 12. Qxb5 Rb8 13. Qa4 Bb6 14. Nbd2 Bb7 15. Ne4 Qf5
16. Bxd3 Qh5 17. Nf6+ gxf6 18. exf6 Rg8 19. Rad1 Qxf3
20. Rxe7+ Nxe7 21. Qxd7+ Kxd7 22. Bf5+ Ke8 23. Bd7+ Kf8
24. Bxe7# 1-0
""";

const string ImmortalPgn = """
[Event "La Inmortal"]
[Site "London"]
[Date "1851.06.21"]
[White "Adolf Anderssen"]
[Black "Lionel Kieseritzky"]
[Result "1-0"]
[ECO "C33"]

1. e4 e5 2. f4 exf4 3. Bc4 Qh4+ 4. Kf1 b5 5. Bxb5 Nf6
6. Nf3 Qh6 7. d3 Nh5 8. Nh4 Qg5 9. Nf5 c6 10. g4 Nf6
11. Rg1 cxb5 12. h4 Qg6 13. h5 Qg5 14. Qf3 Ng8 15. Bxf4 Qf6
16. Nc3 Bc5 17. Nd5 Qxb2 18. Bd6 Bxg1 19. e5 Qxa1+
20. Ke2 Na6 21. Nxg7+ Kd8 22. Qf6+ Nxf6 23. Be7# 1-0
""";

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
var app = builder.Build();
var theoryBook = TheoryBook.LoadEmbedded();
app.Use(async (context, next) =>
{
    if (context.Request.ContentLength is > MaxPgnBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new { error = "El PGN supera el límite de 2 MB." });
        return;
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "0.3" }));
app.MapGet("/api/demo", () => Results.Text(DemoPgn, "application/x-chess-pgn"));
app.MapGet("/api/demo/evergreen", () => Results.Text(EvergreenPgn, "application/x-chess-pgn"));
app.MapGet("/api/demo/immortal", () => Results.Text(ImmortalPgn, "application/x-chess-pgn"));
app.MapPost("/api/analyze", async (HttpRequest request) =>
{
    AnalyzeRequest? input;
    try { input = await request.ReadFromJsonAsync<AnalyzeRequest>(); }
    catch (JsonException) { return Results.BadRequest(new { error = "JSON inválido." }); }
    if (input is null || string.IsNullOrWhiteSpace(input.Pgn)) return Results.BadRequest(new { error = "Se requiere un PGN." });
    if (System.Text.Encoding.UTF8.GetByteCount(input.Pgn) > MaxPgnBytes) return Results.Json(new { error = "El PGN supera el límite de 2 MB." }, statusCode: 413);
    try
    {
        var games = PgnReader.ReadMany(input.Pgn);
        var analyses = games.Select(g => GameAnalyzer.Analyze(g, theoryBook)).ToArray();
        return Results.Ok(new { games = analyses, book = theoryBook.Metadata, modelNotice = "Las clasificaciones Best/Excellent/Good/Inaccuracy/Mistake/Blunder pertenecen al modelo de Piano Man; no son etiquetas de Stockfish.", theoryNotice = "Dentro del corpus, la teoría conserva consonancia: la agudeza se expresa como energía y la desventaja como incomodidad de cada perspectiva. La disonancia global aparece al apartarse de una continuación correcta." });
    }
    catch (Exception ex) when (ex is FormatException or InvalidOperationException or InvalidDataException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
app.MapPost("/api/move", (MoveRequest input) =>
{
    if(string.IsNullOrWhiteSpace(input.Fen)||string.IsNullOrWhiteSpace(input.Uci))
        return Results.BadRequest(new { error = "Se requieren FEN y jugada UCI." });
    try
    {
        var position=Fen.Parse(input.Fen);
        var move=UciParser.Parse(position,input.Uci);
        var entry=GameAnalyzer.AnalyzeContinuation(position,move,Math.Max(1,input.Ply),theoryBook);
        return Results.Ok(new { entry });
    }
    catch(Exception ex) when(ex is FormatException or InvalidOperationException or InvalidDataException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
app.MapPost("/api/principal-line", (PrincipalLineRequest input) =>
{
    if(string.IsNullOrWhiteSpace(input.Fen))return Results.BadRequest(new { error = "Se requiere una posición FEN." });
    try
    {
        var position=Fen.Parse(input.Fen);
        var line=HarmonicLineAnalyzer.Analyze(position,theoryBook,input.MaxPlies??8);
        return Results.Ok(new { line });
    }
    catch(Exception ex) when(ex is FormatException or InvalidOperationException or InvalidDataException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
app.Run();
public sealed record AnalyzeRequest(string Pgn);
public sealed record MoveRequest(string Fen,string Uci,int Ply);
public sealed record PrincipalLineRequest(string Fen,int? MaxPlies);
