# Architecture

Piano Man is split into deterministic chess, harmonic mapping, theory data, analysis orchestration, and presentation.

- `PianoMan.Core/Chess`: FEN, legal moves, SAN formatting/parsing, multi-game PGN parsing.
- `PianoMan.Core/Harmony`: `HarmonyAnalyzer` computes the eight-dimensional chess vector; `ChordMapper` independently maps the resulting snapshot to music. Their independence is intentional and preserved.
- `PianoMan.Core/Theory`: deterministic Zobrist hash, packed moves, hash-sorted theory lookup, weighted selection, embedded Brotli loader.
- `PianoMan.Core/Analysis`: chooses between `Theory` and deterministic one-ply `Tuning` (`Afinando…`) modes and builds candidate/timeline DTOs.
- `PianoMan.Cli`: deterministic text/JSON interface and Native AOT target.
- `PianoMan.Web`: ASP.NET Core API + static HTML/CSS/JS. No npm or client framework.
- `PianoMan.BookCompiler`: offline compiler for pinned opening sources.

## Analysis flow

Before every played move, the theory hash is queried. If the exact move is a continuation, its decision is theory-only and harmonic loss is omitted. Otherwise all legal child positions are statically evaluated from the side-to-move perspective, sorted deterministically, and classified by loss from the best candidate.

No engine search is multithreaded in 0.2. Web Audio “voices” are independent audio graphs only.
