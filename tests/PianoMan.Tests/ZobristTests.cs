using PianoMan.Core.Chess;using PianoMan.Core.Theory;
namespace PianoMan.Tests;
public sealed class ZobristTests
{
    [Fact] public void HashIgnoresFenCounters(){var a=Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");var b=Fen.Parse("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 87 42");Assert.Equal(ZobristHasher.Hash(a),ZobristHasher.Hash(b));}
    [Fact] public void NonCapturableEnPassantDoesNotChangeHash(){var a=Fen.Parse("8/8/8/3p4/8/8/8/4K2k w - d6 0 1");var b=Fen.Parse("8/8/8/3p4/8/8/8/4K2k w - - 0 1");Assert.Equal(ZobristHasher.Hash(a),ZobristHasher.Hash(b));}
    [Fact] public void CapturableEnPassantChangesHash(){var a=Fen.Parse("8/8/8/3pP3/8/8/8/4K2k w - d6 0 1");var b=Fen.Parse("8/8/8/3pP3/8/8/8/4K2k w - - 0 1");Assert.NotEqual(ZobristHasher.Hash(a),ZobristHasher.Hash(b));}
}
