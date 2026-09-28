using System.IO.Compression;using System.Reflection;using System.Security.Cryptography;using System.Text;using System.Text.Json;using PianoMan.Core.Chess;
namespace PianoMan.Core.Theory;
public sealed class TheoryBook
{
    public const uint Magic=0x31424D50; // PMB1
    private readonly TheoryPosition[] _positions;private readonly TheoryContinuation[] _moves;private readonly OpeningIdentity[] _openings;
    public TheoryBook(TheoryPosition[] positions,TheoryContinuation[] moves,OpeningIdentity[] openings,TheoryBookMetadata metadata){_positions=positions;_moves=moves;_openings=openings;Metadata=metadata;}
    public TheoryBookMetadata Metadata{get;}
    public TheoryLookup? Lookup(Position position)
    {
        var hash=ZobristHasher.Hash(position);var lo=0;var hi=_positions.Length-1;while(lo<=hi){var mid=lo+((hi-lo)>>1);var h=_positions[mid].Hash;if(h==hash){var p=_positions[mid];var arr=new TheoryContinuation[p.ContinuationCount];Array.Copy(_moves,p.ContinuationStart,arr,0,p.ContinuationCount);var opening=p.OpeningId< _openings.Length?_openings[p.OpeningId]:null;return new TheoryLookup(opening?.Eco,opening?.Name,arr);}if(h<hash)lo=mid+1;else hi=mid-1;}return null;
    }
    public TheoryContinuation? Find(Position position,Move move)=>Lookup(position)?.Continuations.FirstOrDefault(c=>c.PackedMove==PackedMove.Pack(move));
    public Move? SelectWeighted(Position position,ulong seed)
    {
        var lookup=Lookup(position);if(lookup is null||lookup.Continuations.Count==0)return null;ulong total=0;foreach(var c in lookup.Continuations)total+=c.Weight;var target=seed%total;ulong cursor=0;foreach(var c in lookup.Continuations){cursor+=c.Weight;if(target<cursor)return PackedMove.Resolve(position,c.PackedMove);}return PackedMove.Resolve(position,lookup.Continuations[^1].PackedMove);
    }
    public static TheoryBook LoadEmbedded()
    {
        var asm=typeof(TheoryBook).Assembly;using var manifestStream=asm.GetManifestResourceStream("PianoMan.Core.Resources.theory-book-v1.manifest.json")??throw new InvalidOperationException("Theory manifest resource missing.");using var manifest=JsonDocument.Parse(manifestStream);var root=manifest.RootElement;var metadata=new TheoryBookMetadata(root.GetProperty("Version").GetString()??throw new InvalidDataException("Missing version."),root.GetProperty("UniquePositions").GetInt32(),root.GetProperty("Continuations").GetInt32(),root.GetProperty("OpeningIdentities").GetInt32(),root.GetProperty("MaxPlies").GetInt32(),root.GetProperty("RawBytes").GetInt32(),root.GetProperty("CompressedBytes").GetInt32(),root.GetProperty("CompressedSha256").GetString()??throw new InvalidDataException("Missing SHA."),root.GetProperty("StockfishCommit").GetString()??string.Empty,root.GetProperty("LichessCommit").GetString()??string.Empty,root.GetProperty("StockfishZipSha256").GetString()??string.Empty,root.GetProperty("StockfishPgnSha256").GetString()??string.Empty);
        using var compressed=asm.GetManifestResourceStream("PianoMan.Core.Resources.theory-book-v1.bin.br")??throw new InvalidOperationException("Theory book resource missing.");using var memory=new MemoryStream();compressed.CopyTo(memory);var compressedBytes=memory.ToArray();var sha=Convert.ToHexStringLower(SHA256.HashData(compressedBytes));if(!sha.Equals(metadata.CompressedSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException($"Theory resource SHA-256 mismatch: {sha}.");memory.Position=0;using var brotli=new BrotliStream(memory,CompressionMode.Decompress);using var raw=new MemoryStream();brotli.CopyTo(raw);if(raw.Length!=metadata.RawBytes)throw new InvalidDataException($"Theory resource size mismatch: {raw.Length}.");raw.Position=0;return Read(raw,metadata);
    }
    public static TheoryBook Read(Stream stream,TheoryBookMetadata metadata)
    {
        using var reader=new BinaryReader(stream,Encoding.UTF8,leaveOpen:true);if(reader.ReadUInt32()!=Magic)throw new InvalidDataException("Invalid theory book magic.");var version=reader.ReadUInt16();if(version!=1)throw new InvalidDataException($"Unsupported theory book version {version}.");var posCount=reader.ReadInt32();var moveCount=reader.ReadInt32();var openingCount=reader.ReadInt32();var positions=new TheoryPosition[posCount];for(var i=0;i<posCount;i++)positions[i]=new TheoryPosition(reader.ReadUInt64(),reader.ReadInt32(),reader.ReadUInt16(),reader.ReadUInt16());var moves=new TheoryContinuation[moveCount];for(var i=0;i<moveCount;i++)moves[i]=new TheoryContinuation(reader.ReadUInt16(),reader.ReadUInt32());var openings=new OpeningIdentity[openingCount];for(ushort i=0;i<openingCount;i++)openings[i]=new OpeningIdentity(i,reader.ReadString(),reader.ReadString());return new TheoryBook(positions,moves,openings,metadata);
    }
}
