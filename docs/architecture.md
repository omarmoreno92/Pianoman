# Architecture

Piano Man is split into deterministic chess, harmonic mapping, theory data, perception/decision orchestration, and presentation.

- `PianoMan.Core/Chess`: FEN, legal moves, SAN formatting/parsing, multi-game PGN parsing.
- `PianoMan.Core/Harmony`: `HarmonyAnalyzer` computes the eight-dimensional chess vector; `ChordMapper` independently maps derived state to music.
- `PianoMan.Core/Theory`: deterministic Zobrist hash, packed moves, hash-sorted theory lookup, embedded Brotli loader.
- `PianoMan.Core/Analysis`: builds Global/White/Black perceptions, move deltas, theory decisions, Piano Man decisions and Radio Killer opportunities.
- `PianoMan.Cli`: deterministic text/JSON interface and Native AOT target.
- `PianoMan.Web`: ASP.NET Core API + static HTML/CSS/JS. No npm or client framework.
- `PianoMan.BookCompiler`: offline compiler for pinned opening sources.

## Position identity and transpositions

Theory is keyed by the resulting chess position, not by the historical move string. Equivalent positions reached through different move orders therefore share the same Zobrist key and the same continuation set. Halfmove/fullmove counters do not participate in the identity. This makes opening transpositions first-class and avoids duplicating analysis by move order.

## Three listening perspectives

A single position produces:

1. **Global** — balance and structural/tactical tension of the whole board;
2. **White** — the same position heard from White's advantage, king safety and pressure exposure;
3. **Black** — the mirrored Black perception.

The timeline also stores deltas between consecutive perceptions. Audio can therefore represent not only a position but the *effect* of the last move on each listener.

## Decision flow

### Theory

If the played move exists among the book continuations, the decision remains `TEORÍA · SIN BÚSQUEDA`. The theory book restricts the legal candidate set. Piano Man projects only those known continuations one ply, combines harmonic score with a bounded logarithmic corpus-support prior, and recommends the strongest result. No arbitrary legal child outside theory is considered.

### Outside theory — Piano Man

All legal children are statically evaluated one ply. Static quality defines a correctness window: only moves within 15 Piano Man points of the best candidate are eligible for recommendation. Within that safe set, Piano Man prefers the highest harmonic score.

### Radio Killer

A move with loss >= 81 marks an opportunity for the opponent. If the resulting advantage persists, Radio Killer uses the same correctness window and maximizes the opponent's perceived tension before harmonic/score tie-breakers. It cannot intentionally leave the quality window merely to create dissonance. When the advantage dissipates, behavior returns to Piano Man.

No engine search is multithreaded in 0.3. Web Audio voices are independent audio graphs only; each live piece owns one voice.
