# Changelog

## Unreleased

- Added Global/White/Black auditory perception and per-move perception deltas.
- Theory recommendations now choose the most harmonious known continuation with corpus weight as deterministic tie-breaker.
- Added guarded Piano Man/Radio Killer decision policies outside theory.
- Added research and educational documentation for the “How Chess Sounds” hypothesis.

## 0.2.0 - 2026-09-28

- Added versioned theory-book format, deterministic Zobrist hashing, packed moves and offline compiler contract.
- Added deterministic one-ply candidate analysis with Piano Man loss classifications outside theory.
- Added multi-game PGN reading and SAN formatting.
- Added ASP.NET Core web player with drag/drop PGN, board/timeline navigation, Web Audio audition and candidate comparison.
- Added listen-first dissonance marking/export for later Stockfish comparison.
- Added theory provenance, validation documentation and CI web smoke tests.

## 0.1.0 - 2026-09-27

- Initial .NET 10 chess/harmony analyzer, CLI, tests and CI.
