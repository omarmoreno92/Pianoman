# Roadmap

## 0.1 — Audible position model

- [x] FEN, legal move, SAN, and main-line PGN support.
- [x] Explainable harmony vector and independent tension score.
- [x] Deterministic chord and MIDI-note output.
- [x] CLI, JSON export, automated tests, and Native AOT build.

Acceptance: a checked-in master game replays from start to finish and produces the
same timeline on every supported machine.

## 0.2 — Correctness corpus

- [ ] Add perft positions for legal-move validation.
- [ ] Support multiple games per PGN file.
- [ ] Add repetition, fifty-move, and insufficient-material state.
- [ ] Build a curated corpus of roughly 100 annotated master games.
- [ ] Version every change to weights and thresholds.

Acceptance: move-generation matches published perft counts and every corpus game
replays without manual exceptions.

## 0.3 — Sonification

- [ ] Emit Standard MIDI Files.
- [ ] Map the eight dimensions to voicing, register, velocity, and tempo.
- [ ] Render an interactive harmony/tension timeline.
- [ ] Compare player-level harmonic signatures.

Acceptance: listeners can replay a game and every audible event links back to a
documented chess signal.

## 0.4 — Measurement

- [ ] Freeze a position benchmark before tuning.
- [ ] Rank legal moves using one-ply harmony deltas.
- [ ] Measure top-1, top-3, and top-5 agreement with a fixed reference engine.
- [ ] Measure positions resolved per node and per millisecond.

Acceptance: benchmark scripts publish inputs, settings, raw results, and hardware.

## 0.5 — Piano Man engine

- [ ] Add harmonic-shock detection.
- [ ] Search forcing and tension-resolving moves until stability returns.
- [ ] Add a compact weighted opening repertoire keyed by Zobrist hash.
- [ ] Expose the engine through UCI.
- [ ] Add time management and transposition storage.

Acceptance: the UCI engine completes games legally and reports both calculation
nodes and harmonic-resolution events.
