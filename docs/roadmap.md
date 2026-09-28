# Roadmap

## 0.2 — Theory Book Player

- Pinned CC0 theory corpus and offline compiler.
- Deterministic one-ply fallback outside theory.
- PGN web player with board, timeline, candidate comparison and Web Audio.
- Listen-first dissonance observations exported for later engine comparison.

## Next research

- “Afinar” only when there is a measurable harmonic clash instead of on every out-of-book move.
- Explore multiply candidate phrases/“sonatas” after the deterministic single-ply baseline is validated.
- Generate complete musical compositions from full games.
- Study whether opening families develop repeatable tonal identities.
- Investigate the inverse problem: song → legal chess game, without assuming every musical sequence has a useful chess inverse.
- Benchmark accuracy, latency and memory against reference engines at 1-, 3- and 5-minute controls before making strength claims.

Stockfish integration is deliberately deferred. It belongs in validation/benchmark tooling first, not in the 0.2 runtime decision path.
