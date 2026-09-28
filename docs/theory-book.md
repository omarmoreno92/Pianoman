# Theory book v1

Piano Man 0.2 uses a versioned opening corpus so known continuations can be selected without search. This is **not** a claim to contain all known chess theory. The intended v1 corpus combines pinned CC0 sources from `official-stockfish/books` and `lichess-org/chess-openings`.

## Pinned corpus contract

| Item | Value |
|---|---:|
| Unique positions | 251,274 |
| Continuations | 420,150 |
| ECO/name identities | 3,329 |
| Maximum depth | 36 plies |
| Raw binary | 7,586,743 bytes |
| Brotli resource | approximately 2,910,309 bytes |
| Expected compressed SHA-256 | `36cd056f0fdea527c25fa40b184ae26ef0f5eafb9722a796be9cae0d36927d47` |

The compiler in `tools/PianoMan.BookCompiler` verifies source hashes and aborts on any illegal source move. It also refuses to publish a v1 artifact if counts, sizes, or the final compressed hash differ from the pinned contract.

## Position identity

`ZobristHasher` is deterministic. It hashes pieces, side to move, castling rights, and en-passant **only if a pawn of the side to move can capture onto the en-passant square**. Halfmove/fullmove counters are intentionally ignored.

## Binary layout

The resource starts with a versioned header, followed by a hash-sorted position table, packed continuations, and ECO/name strings. Position lookup is binary search. Moves are packed into `ushort`; continuation weights are `uint`. Theory selection uses weights only and never evaluates child positions.

## Selection versus sonification

If a played move is in the current book position, analysis mode is `Theory` and the UI shows `TEORÍA · SIN BÚSQUEDA`. Candidate ordering is purely corpus-weight ordering. Piano Man may still calculate the current/resulting chord so the line can be heard, but that sound does not influence theory selection. A theoretical move may sound dissonant; the chord is the current mathematical representation, not an independent correctness oracle.

## Reproduction

Place the pinned Stockfish ZIP and the five pinned Lichess TSV files locally, then run:

```bash
dotnet run --project tools/PianoMan.BookCompiler -- \
  --stockfish-zip path/to/bjbraams_chessdb_198350_lines.pgn.zip \
  --lichess-dir path/to/chess-openings \
  --output src/PianoMan.Core/Resources
```

The compiler performs no network access.

> Development note: a tiny bootstrap resource can be used while working on the loader/UI, but it is not the release v1 corpus and must never be reported with the pinned v1 hash/counts.
