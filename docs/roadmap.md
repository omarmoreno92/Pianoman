# Roadmap

## 0.3 — Consonance and free analysis

- Separate move dissonance from tactical energy and side-relative discomfort.
- Keep every theoretical continuation globally consonant.
- Render one deterministic voice per live piece in chord and arpeggio modes.
- Create legal click-to-move branches from any PGN timeline position.

## 0.2 — Theory Book Player

- Pinned CC0 theory corpus and offline compiler.
- Position-keyed theory with transpositions.
- Deterministic one-ply fallback outside theory.
- Global/White/Black auditory perception and per-move deltas.
- Piano Man and guarded Radio Killer decision styles.
- PGN web player with board, timeline, candidate comparison and Web Audio.
- Listen-first dissonance observations exported for later engine comparison.

## Educational direction

- Teach development, coordination, pressure, imbalance and recovery through sound rather than engine numbers alone.
- Add configurable musical vocabularies and eventually genre/rhythm renderers (rock, classical and others) without changing the underlying chess vector.
- Study whether learners can associate recurring chess structures with recognizable auditory signatures without requiring absolute pitch.
- Build guided opening lessons where theoretical continuations can be heard from Global, White and Black perspectives.
- Treat an eventual “useful up to ~2500 Elo” target as a benchmark objective, not a current claim.

## Research direction

- “Afinar” only when there is a measurable harmonic clash instead of on every out-of-book move.
- Explore multi-ply candidate phrases/“sonatas” after the deterministic single-ply baseline is validated.
- Generate complete musical compositions from full games.
- Study whether opening families develop repeatable tonal identities.
- Investigate the inverse problem: song → legal chess game, without assuming every musical sequence has a useful chess inverse.
- Benchmark accuracy, latency, memory and move quality against reference engines at 1-, 3- and 5-minute controls.

## Stockfish hypothesis

The long-term research hypothesis is that a compact harmonic representation may identify useful chess structure with less computation than deep brute-force search in some time-constrained positions. Beating Stockfish is an experimental target, not an established property. Validation must use fixed hardware, fixed engine versions, reproducible time controls, enough games to estimate uncertainty, and published failures as well as successes.

Stockfish remains outside the runtime decision path; it belongs first in benchmark/validation tooling.
