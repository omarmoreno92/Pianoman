namespace PianoMan.Core.Theory;
public sealed record TheoryContinuation(ushort PackedMove,uint Weight);
public sealed record OpeningIdentity(ushort Id,string Eco,string Name);
public sealed record TheoryPosition(ulong Hash,int ContinuationStart,ushort ContinuationCount,ushort OpeningId);
public sealed record TheoryBookMetadata(string Version,int UniquePositions,int Continuations,int OpeningIdentities,int MaxPlies,int RawBytes,int CompressedBytes,string CompressedSha256,string StockfishCommit,string LichessCommit,string StockfishZipSha256,string StockfishPgnSha256);
public sealed record TheoryLookup(string? Eco,string? Name,IReadOnlyList<TheoryContinuation> Continuations);
