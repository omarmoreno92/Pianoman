# Theory book v1

Piano Man uses a versioned opening corpus so known continuations can be selected without search. This is **not** a claim to contain all known chess theory. The v1 corpus combines pinned CC0 sources from `official-stockfish/books` and `lichess-org/chess-openings`.

## Pinned corpus contract

| Item | Value |
|---|---:|
| Unique positions | 251,274 |
| Continuations | 420,150 |
| ECO/name identities | 3,329 |
| Maximum depth | 36 plies |
| Raw binary | 6,713,123 bytes |
| Brotli resource | 2,794,787 bytes |
| Expected compressed SHA-256 | `6b7a83ce712e6de9f6e0b5a01d76203a25b4f208a57e7cc3a8c6c1623ed6e70b` |

The compiler in `tools/PianoMan.BookCompiler` verifies source hashes and aborts on any illegal source move. The semantic corpus contract is pinned by positions, continuations, identities and depth; the byte-size/SHA values pin Piano Man's current v1 serializer. Generation is transactional: temporary files are validated before the embedded resource and manifest are replaced.

## Position identity

`ZobristHasher` is deterministic. It hashes pieces, side to move, castling rights, and en-passant **only if a pawn of the side to move can capture onto the en-passant square**. Halfmove/fullmove counters are intentionally ignored.

## Binary layout

The resource starts with a versioned header, followed by a hash-sorted position table, packed continuations, and ECO/name strings. Position lookup is binary search. Moves are packed into `ushort`; continuation weights are `uint`. The book itself contains no engine evaluation.

## Selection versus sonification

If a played move is in the current book position, analysis mode is `Theory` and the UI shows `TEORÍA · SIN BÚSQUEDA`. Book membership restricts the candidates to known theoretical continuations. Piano Man projects only those continuations through its one-ply harmonic model and adds a bounded logarithmic prior (`0..32`) from stored continuation support. Dissonance is the gap to the best resulting theory candidate, not a table written by opening name. Corpus frequency is evidence of theoretical support, not an independent proof of chess correctness.

## Reproduction

The compiler itself performs no network access. To reproduce the release resource from already-downloaded inputs, run:

```bash
dotnet run --project tools/PianoMan.BookCompiler -- \
  --stockfish-zip path/to/bjbraams_chessdb_198350_lines.pgn.zip \
  --lichess-dir path/to/chess-openings \
  --output src/PianoMan.Core/Resources
```

On PowerShell, the development helper downloads the exact pinned inputs, verifies the Stockfish ZIP hash, invokes the offline compiler, and verifies the final resource hash:

```powershell
./tools/PianoMan.BookCompiler/build-release-book.ps1
```

> Development note: a tiny bootstrap resource can be used while working on the loader/UI, but it is not the release v1 corpus and must never be reported with the pinned v1 hash/counts.
