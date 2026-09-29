using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PianoMan.Core.Chess;
using PianoMan.Core.Theory;

const string StockfishCommit = "65815ccdbc7727cd4f6aee252ba8f67fb740e92f";
const string LichessCommit = "c67912be581f0793dbaa776be5ccf111e01f88d9";
const string ExpectedZipSha = "14d9bc9fce1fd96b58d2814fe7ab5b0967109c8411570a0d114558e13ad28fce";
const string ExpectedPgnSha = "7b3ede8b13736df09c7c8560a16dd0068c7247cf8cbdec4052073329fff3fb0e";
const string ExpectedCompressedSha = "6b7a83ce712e6de9f6e0b5a01d76203a25b4f208a57e7cc3a8c6c1623ed6e70b";
const int ExpectedPositions = 251_274;
const int ExpectedContinuations = 420_150;
const int ExpectedOpenings = 3_329;
const int ExpectedMaxPlies = 36;
const int ExpectedRawBytes = 6_713_123;
const int ExpectedCompressedBytes = 2_794_787;
const int ExpectedStockfishGames = 198_350;

var options = ParseArgs(args);
var zipPath = Required(options, "stockfish-zip");
var lichessDir = Required(options, "lichess-dir");
var outputDir = options.GetValueOrDefault("output", Path.Combine("src", "PianoMan.Core", "Resources"));
Directory.CreateDirectory(outputDir);

VerifySha(zipPath, ExpectedZipSha, "Stockfish ZIP");
byte[] pgnBytes;
using (var zip = ZipFile.OpenRead(zipPath))
{
    var entry = zip.Entries.SingleOrDefault(e => e.Name == "bjbraams_chessdb_198350_lines.pgn")
        ?? throw new InvalidDataException("Expected PGN entry is missing from Stockfish ZIP.");
    using var source = entry.Open(); using var memory = new MemoryStream(); source.CopyTo(memory); pgnBytes = memory.ToArray();
}
var pgnSha = Convert.ToHexStringLower(SHA256.HashData(pgnBytes));
if (!pgnSha.Equals(ExpectedPgnSha, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"Stockfish PGN SHA-256 mismatch: {pgnSha}.");

var builder = new Builder();
IReadOnlyList<PgnGame> stockfishGames;
try
{
    stockfishGames = PgnReader.ReadMany(Encoding.UTF8.GetString(pgnBytes));
}
catch (Exception ex)
{
    throw new InvalidDataException("The pinned Stockfish PGN could not be parsed as a multi-game PGN corpus.", ex);
}

if (stockfishGames.Count != ExpectedStockfishGames)
    throw new InvalidDataException($"Stockfish PGN expected {ExpectedStockfishGames} games, got {stockfishGames.Count}.");

for (var gameIndex = 0; gameIndex < stockfishGames.Count; gameIndex++)
{
    try { builder.AddLine(stockfishGames[gameIndex].Moves, null, null); }
    catch (Exception ex)
    {
        throw new InvalidDataException($"Illegal/invalid Stockfish source game {gameIndex + 1}.", ex);
    }
}

foreach (var fileName in new[] { "a.tsv", "b.tsv", "c.tsv", "d.tsv", "e.tsv" })
{
    var path = Path.Combine(lichessDir, fileName);
    if (!File.Exists(path)) throw new FileNotFoundException($"Missing pinned Lichess source file {fileName}.", path);
    var lineNo = 0;
    foreach (var line in File.ReadLines(path, Encoding.UTF8))
    {
        lineNo++;
        if (lineNo == 1) continue;
        var columns = line.Split('\t');
        if (columns.Length != 3) throw new InvalidDataException($"Invalid {fileName}:{lineNo}: expected eco, name, pgn.");
        try { builder.AddLine(PgnReader.Read(columns[2]).Moves, columns[0], columns[1]); }
        catch (Exception ex) { throw new InvalidDataException($"Illegal/invalid {fileName}:{lineNo}: {columns[2]}", ex); }
    }
}

var rawPath = Path.Combine(outputDir, "theory-book-v1.bin");
var compressedPath = Path.Combine(outputDir, "theory-book-v1.bin.br");
var manifestPath = Path.Combine(outputDir, "theory-book-v1.manifest.json");
var tempRawPath = rawPath + ".tmp";
var tempCompressedPath = compressedPath + ".tmp";
var tempManifestPath = manifestPath + ".tmp";

DeleteIfExists(tempRawPath);
DeleteIfExists(tempCompressedPath);
DeleteIfExists(tempManifestPath);

try
{
    var result = builder.Write(tempRawPath);
    using (var input = File.OpenRead(tempRawPath))
    using (var output = File.Create(tempCompressedPath))
    using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize))
        input.CopyTo(brotli);

    var compressedSha = Sha(tempCompressedPath);
    var compressedBytes = checked((int)new FileInfo(tempCompressedPath).Length);

    var failures = new List<string>();
    Check(result.Positions, ExpectedPositions, "positions", failures);
    Check(result.Continuations, ExpectedContinuations, "continuations", failures);
    Check(result.Openings, ExpectedOpenings, "opening identities", failures);
    Check(result.MaxPlies, ExpectedMaxPlies, "max plies", failures);
    Check(result.RawBytes, ExpectedRawBytes, "raw bytes", failures);
    Check(compressedBytes, ExpectedCompressedBytes, "compressed bytes", failures);
    if (!compressedSha.Equals(ExpectedCompressedSha, StringComparison.OrdinalIgnoreCase))
        failures.Add($"compressed SHA-256 expected {ExpectedCompressedSha}, got {compressedSha}");

    if (failures.Count != 0)
        throw new InvalidDataException("Pinned corpus did not reproduce the v1 artifact:\n- " + string.Join("\n- ", failures));

    var metadata = new TheoryBookMetadata(
        "1",
        result.Positions,
        result.Continuations,
        result.Openings,
        result.MaxPlies,
        result.RawBytes,
        compressedBytes,
        compressedSha,
        StockfishCommit,
        LichessCommit,
        ExpectedZipSha,
        ExpectedPgnSha);

    File.WriteAllText(
        tempManifestPath,
        JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

    File.Move(tempCompressedPath, compressedPath, overwrite: true);
    File.Move(tempManifestPath, manifestPath, overwrite: true);

    Console.WriteLine($"positions={result.Positions} continuations={result.Continuations} openings={result.Openings} maxPlies={result.MaxPlies}");
    Console.WriteLine($"compressed={compressedBytes} sha256={compressedSha}");
}
finally
{
    DeleteIfExists(tempRawPath);
    DeleteIfExists(tempCompressedPath);
    DeleteIfExists(tempManifestPath);
}

static Dictionary<string,string> ParseArgs(string[] args)
{
    var d = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    for (var i=0;i<args.Length;i++) { if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unexpected argument {args[i]}."); var key=args[i][2..]; if (++i>=args.Length) throw new ArgumentException($"Missing value for --{key}."); d[key]=args[i]; }
    return d;
}
static string Required(Dictionary<string,string> d,string key)=>d.TryGetValue(key,out var v)?v:throw new ArgumentException($"Missing --{key}.");
static string Sha(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
static void VerifySha(string path,string expected,string label){var actual=Sha(path);if(!actual.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException($"{label} SHA-256 mismatch: {actual}.");}
static void Check(int actual,int expected,string label,List<string> errors){if(actual!=expected)errors.Add($"{label} expected {expected}, got {actual}");}
static void DeleteIfExists(string path){if(File.Exists(path))File.Delete(path);}

sealed class Builder
{
    private readonly Dictionary<ulong, PositionNode> _positions = [];
    private readonly Dictionary<(string Eco,string Name), ushort> _openingIds = [];
    private readonly List<(string Eco,string Name)> _openings = [];
    public int MaxPlies { get; private set; }

    public void AddLine(IReadOnlyList<string> sans,string? eco,string? name)
    {
        var p=Position.Initial; MaxPlies=Math.Max(MaxPlies,sans.Count);
        for(var ply=0;ply<sans.Count;ply++)
        {
            Move move;
            try { move=SanParser.Parse(p,sans[ply]); } catch(Exception ex) { throw new InvalidDataException($"Illegal move at ply {ply+1} from {p.ToFen()}: {sans[ply]}", ex); }
            var hash=ZobristHasher.Hash(p);if(!_positions.TryGetValue(hash,out var node)){node=new PositionNode();_positions.Add(hash,node);}var packed=PackedMove.Pack(move);node.Moves[packed]=checked(node.Moves.GetValueOrDefault(packed)+1u);p.Apply(move);
        }
        if(eco is not null&&name is not null)
        {
            var id=OpeningId(eco,name);var finalHash=ZobristHasher.Hash(p);if(!_positions.TryGetValue(finalHash,out var finalNode)){finalNode=new PositionNode();_positions.Add(finalHash,finalNode);}finalNode.OpeningId=id;
        }
    }
    private ushort OpeningId(string eco,string name){var key=(eco,name);if(_openingIds.TryGetValue(key,out var id))return id;if(_openings.Count>=ushort.MaxValue)throw new InvalidDataException("Too many opening identities.");id=(ushort)_openings.Count;_openingIds.Add(key,id);_openings.Add(key);return id;}
    public (int Positions,int Continuations,int Openings,int MaxPlies,int RawBytes) Write(string path)
    {
        var sorted=_positions.OrderBy(x=>x.Key).ToArray();var continuationCount=sorted.Sum(x=>x.Value.Moves.Count);using var stream=File.Create(path);using var writer=new BinaryWriter(stream,Encoding.UTF8,leaveOpen:true);writer.Write(TheoryBook.Magic);writer.Write((ushort)1);writer.Write(sorted.Length);writer.Write(continuationCount);writer.Write(_openings.Count);var offset=0;foreach(var(hash,node)in sorted){writer.Write(hash);writer.Write(offset);writer.Write(checked((ushort)node.Moves.Count));writer.Write(node.OpeningId);offset+=node.Moves.Count;}foreach(var(_,node)in sorted)foreach(var move in node.Moves.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key)){writer.Write(move.Key);writer.Write(move.Value);}foreach(var (eco,name) in _openings){writer.Write(eco);writer.Write(name);}writer.Flush();return(sorted.Length,continuationCount,_openings.Count,MaxPlies,checked((int)stream.Length));
    }
    sealed class PositionNode { public Dictionary<ushort,uint> Moves { get; }=[]; public ushort OpeningId { get; set; }=ushort.MaxValue; }
}
