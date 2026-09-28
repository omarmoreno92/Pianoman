using System.Text.Json;
using System.Text.Json.Serialization;
using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;
using PianoMan.Core.Theory;

const int MaxPgnBytes = 2 * 1024 * 1024;
const string DemoPgn = """
[Event "Demo"]
[Site "Piano Man"]
[Date "2026.09.28"]
[Round "1"]
[White "White"]
[Black "Black"]
[Result "*"]

1. e4 e5 2. Nf3 Nc6 *
""";

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
var app = builder.Build();
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
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", version = "0.2" }));
app.MapGet("/api/demo", () => Results.Text(DemoPgn, "application/x-chess-pgn"));
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
        var book = TheoryBook.LoadEmbedded();
        var analyses = games.Select(g => GameAnalyzer.Analyze(g, book)).ToArray();
        return Results.Ok(new { games = analyses, book = book.Metadata, modelNotice = "Las clasificaciones Best/Excellent/Good/Inaccuracy/Mistake/Blunder pertenecen al modelo de Piano Man; no son etiquetas de Stockfish.", theoryNotice = "El libro es un corpus CC0 versionado. Una jugada teórica no tiene por qué sonar consonante: el acorde representa la función matemática actual, no una prueba independiente de corrección." });
    }
    catch (Exception ex) when (ex is FormatException or InvalidOperationException or InvalidDataException)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
app.Run();
public sealed record AnalyzeRequest(string Pgn);
