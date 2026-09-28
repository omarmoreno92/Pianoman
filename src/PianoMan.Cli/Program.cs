using System.Globalization;
using System.Text.Json;
using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;
using PianoMan.Core.Harmony;

namespace PianoMan.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            return args.FirstOrDefault()?.ToLowerInvariant() switch
            {
                "analyze" => AnalyzePosition(args[1..]),
                "pgn" => AnalyzePgn(args[1..]),
                "demo" => RunDemo(),
                "help" or "--help" or "-h" or null => ShowHelp(),
                _ => UnknownCommand(args[0])
            };
        }
        catch (Exception exception) when (exception is
            ArgumentException or
            FormatException or
            IOException or
            InvalidOperationException)
        {
            Console.Error.WriteLine($"Piano Man: {exception.Message}");
            return 1;
        }
    }

    private static int AnalyzePosition(string[] args)
    {
        var fen = Option(args, "--fen") ?? Fen.InitialPosition;
        var position = Fen.Parse(fen);
        var (harmony, chord) = GameAnalyzer.Analyze(position);

        Console.WriteLine("Piano Man — position analysis");
        Console.WriteLine($"FEN:       {position.ToFen()}");
        Console.WriteLine($"Turn:      {position.SideToMove}");
        Console.WriteLine($"Status:    {harmony.Status}");
        if (harmony.Winner is { } winner) Console.WriteLine($"Winner:    {winner}");
        Console.WriteLine($"Score:     {Signed(harmony.RelativeScore)}");
        Console.WriteLine($"Tension:   {harmony.Tension}/255 ({chord.TensionClass})");
        Console.WriteLine($"Phase:     {harmony.Phase}%");
        Console.WriteLine($"Chord:     {chord.Symbol}");
        Console.WriteLine($"MIDI:      {string.Join(", ", chord.MidiNotes)}");
        Console.WriteLine();
        PrintFeatureTable(harmony);
        return 0;
    }

    private static int AnalyzePgn(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-'))
        {
            throw new ArgumentException("Usage: pianoman pgn <file.pgn> [--json <output.json>]");
        }

        var game = PgnReader.ReadFile(args[0]);
        var analysis = GameAnalyzer.Analyze(game);
        PrintTimeline(analysis);

        if (Option(args, "--json") is { } outputPath)
        {
            WriteJson(analysis, outputPath);
            Console.WriteLine();
            Console.WriteLine($"JSON written to {Path.GetFullPath(outputPath)}");
        }

        return 0;
    }

    private static int RunDemo()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "samples", "immortal-game.pgn"),
            Path.Combine(Environment.CurrentDirectory, "samples", "immortal-game.pgn")
        };
        var sample = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Could not locate samples/immortal-game.pgn.");

        var analysis = GameAnalyzer.Analyze(PgnReader.ReadFile(sample));
        PrintTimeline(analysis);
        return 0;
    }

    private static void PrintTimeline(GameAnalysis analysis)
    {
        Console.WriteLine($"Piano Man — {analysis.Title}");
        if (analysis.Event is { Length: > 0 }) Console.WriteLine(analysis.Event);
        Console.WriteLine();
        Console.WriteLine(" Ply  Move       Score  Tension  Chord");
        Console.WriteLine(" ───  ─────────  ─────  ───────  ─────────────");

        foreach (var entry in analysis.Timeline)
        {
            var move = entry.Ply == 0
                ? "start"
                : entry.Side == nameof(Color.White)
                    ? $"{entry.MoveNumber}.{entry.San}"
                    : $"{entry.MoveNumber}...{entry.San}";
            Console.WriteLine(
                $" {entry.Ply,3}  {Truncate(move, 9),-9}  " +
                $"{Signed(entry.Harmony.RelativeScore),5}  " +
                $"{entry.Harmony.Tension,7}  {entry.Chord.Symbol}");
        }

        Console.WriteLine();
        Console.WriteLine($"Result:    {analysis.Result ?? "*"}");
        Console.WriteLine($"Final FEN: {analysis.FinalFen}");
    }

    private static void PrintFeatureTable(HarmonySnapshot harmony)
    {
        Console.WriteLine("Feature          White    Black    Delta");
        Console.WriteLine("───────────────  ───────  ───────  ───────");
        PrintFeature("Material", harmony.White.Material, harmony.Black.Material);
        PrintFeature("Activity", harmony.White.Activity, harmony.Black.Activity);
        PrintFeature("Coordination", harmony.White.Coordination, harmony.Black.Coordination);
        PrintFeature("King safety", harmony.White.KingSafety, harmony.Black.KingSafety);
        PrintFeature("Space", harmony.White.Space, harmony.Black.Space);
        PrintFeature("Structure", harmony.White.Structure, harmony.Black.Structure);
        PrintFeature("Pressure", harmony.White.Pressure, harmony.Black.Pressure);
        PrintFeature("Initiative", harmony.White.Initiative, harmony.Black.Initiative);
        Console.WriteLine("───────────────  ───────  ───────  ───────");
        PrintFeature("Total", harmony.White.Total, harmony.Black.Total);
    }

    private static void PrintFeature(string name, int white, int black) =>
        Console.WriteLine($"{name,-15}  {white,7}  {black,7}  {Signed(white - black),7}");

    private static void WriteJson(GameAnalysis analysis, string outputPath)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is { Length: > 0 }) Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(analysis, PianoManJsonContext.Default.GameAnalysis);
        File.WriteAllText(fullPath, json);
    }

    private static string? Option(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option {name} requires a value.");
            }

            return args[index + 1];
        }

        return null;
    }

    private static string Signed(int value) =>
        value >= 0
            ? $"+{value.ToString(CultureInfo.InvariantCulture)}"
            : value.ToString(CultureInfo.InvariantCulture);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static int ShowHelp()
    {
        Console.WriteLine("Piano Man — hear the structure of a chess position");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  pianoman analyze [--fen \"<fen>\"]");
        Console.WriteLine("  pianoman pgn <file.pgn> [--json <output.json>]");
        Console.WriteLine("  pianoman demo");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine("Run 'pianoman help' for usage.");
        return 1;
    }
}
