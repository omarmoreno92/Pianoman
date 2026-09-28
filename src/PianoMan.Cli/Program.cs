using System.Text.Json;
using PianoMan.Cli;
using PianoMan.Core.Analysis;
using PianoMan.Core.Chess;
using PianoMan.Core.Theory;

try
{
    return args.Length==0?RunAnalyze([]):args[0].ToLowerInvariant() switch
    {
        "demo"=>RunDemo(),
        "analyze"=>RunAnalyze(args[1..]),
        "pgn"=>RunPgn(args[1..]),
        "book"=>RunBook(),
        _=>Usage($"Unknown command '{args[0]}'.")
    };
}
catch(Exception ex)
{
    Console.Error.WriteLine(ex.Message);return 1;
}

static int RunDemo()
{
    const string pgn="""
[Event "Piano Man demo"]
[White "White"]
[Black "Black"]
[Result "*"]

1. e4 e5 2. Nf3 Nc6 *
""";
    var analysis=GameAnalyzer.Analyze(PgnReader.Read(pgn),TheoryBook.LoadEmbedded());Console.WriteLine(JsonSerializer.Serialize(analysis,PianoManJsonContext.Default.GameAnalysis));return 0;
}
static int RunAnalyze(string[] args)
{
    var fen=GetOption(args,"--fen")??Fen.InitialPosition;var position=Fen.Parse(fen);var(h,c)=GameAnalyzer.Analyze(position);Console.WriteLine($"FEN: {position.ToFen()}");Console.WriteLine($"Chord: {c.Symbol} [{string.Join(", ",c.MidiNotes)}]");Console.WriteLine($"Balance: {h.RelativeScore}; tension: {h.Tension}; phase: {h.Phase}");return 0;
}
static int RunPgn(string[] args)
{
    if(args.Length==0)return Usage("pgn requires a file path.");var path=args[0];var jsonPath=GetOption(args,"--json");var games=PgnReader.ReadMany(File.ReadAllText(path));var book=TheoryBook.LoadEmbedded();var analyses=games.Select(g=>GameAnalyzer.Analyze(g,book)).ToArray();var json=JsonSerializer.Serialize(analyses,PianoManJsonContext.Default.GameAnalysisArray);if(jsonPath is null)Console.WriteLine(json);else{Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(jsonPath))!);File.WriteAllText(jsonPath,json);Console.WriteLine(jsonPath);}return 0;
}
static int RunBook(){var b=TheoryBook.LoadEmbedded();Console.WriteLine(JsonSerializer.Serialize(b.Metadata,PianoManJsonContext.Default.TheoryBookMetadata));return 0;}
static string? GetOption(string[] args,string option){var i=Array.IndexOf(args,option);if(i<0)return null;if(i+1>=args.Length)throw new ArgumentException($"Missing value for {option}.");return args[i+1];}
static int Usage(string? error=null){if(error is not null)Console.Error.WriteLine(error);Console.Error.WriteLine("Usage: pianoman demo | analyze [--fen FEN] | pgn FILE [--json FILE] | book");return error is null?0:2;}
