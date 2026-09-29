# Harmonic model

Piano Man represents each position with an explainable deterministic vector for both sides:

1. material;
2. activity;
3. coordination;
4. king safety;
5. space;
6. pawn structure;
7. pressure;
8. initiative.

`HarmonyAnalyzer` owns this chess-derived vector and tactical energy. `ChordMapper` consumes derived values and maps them to chord/MIDI notes. These components remain independent so changing musical vocabulary cannot silently change chess evaluation.

## Dissonance is not sharpness

The renderer deliberately separates three quantities:

1. **Move dissonance** comes from the played move's loss. In theory, loss is the gap to the best theory candidate after combining harmonic structure with bounded corpus support; outside theory it is the static positional loss.
2. **Tactical energy** comes from checks, captures, attacks and contact. A sound theoretical position may be energetic without sounding wrong.
3. **Perspective discomfort** adds a restrained color for the side whose score, king safety or pressure is worse.

This prevents a sound sacrifice or a sharp book line from being mislabeled as a mistake merely because many pieces attack each other.

## One voice per piece

`PieceSonifier` emits one deterministic voice for every live piece. It uses the selected chord's pitch classes, piece type, color and square to choose register and pitch. The initial position therefore contains 32 voices whose pitch classes all belong to C major. The latest move is explicit context: when its measured dissonance rises, the moved piece receives chromatic offset and detuning before the rest of the position changes. Perspective discomfort is expressed with restrained deterministic detuning.

## Perception

`PerceptionAnalyzer` turns one board snapshot into three listeners:

- **Global** uses move dissonance and intentionally ignores which color benefits from the imbalance.
- **White** uses White-relative score and restrained discomfort from pressure, king fragility and disadvantage.
- **Black** mirrors the score and computes Black's own exposure independently. At the initial position Black hears a very small discomfort because White owns the first-move initiative.

A good move for one side can therefore improve that side's sound while making the opponent's sound more tense even if the global position remains coherent. `PerceptionDelta` stores the before/after change so the musical transition can be taught, not merely the destination chord.

The formulas and weights are research hypotheses. They are deterministic so experiments are reproducible; they are not claims of engine strength or perceptual universality.

## Move decisions

In theory, the corpus determines which continuations count as theory. Piano Man evaluates only those theoretical children and adds a bounded `0..32` logarithmic prior from their corpus support. This gives the pinned Stockfish/Lichess corpus meaningful influence without allowing raw counts to grow without limit. No move name (`e4`, `f4`, and so on) appears in the scoring code.

Outside theory, every legal move is evaluated one static ply. Move quality remains anchored to the best static score. Piano Man style operates only inside a `≤15` correctness window.

When an opponent move loses at least 81 Piano Man points, it can activate **Radio Killer** for the responding side. Radio Killer still stays inside the same correctness window; among those sufficiently correct moves it prefers the line that increases the opponent's perceived tension. This encodes the design rule: *never play badly merely to sound aggressive*.

Quality labels remain Piano Man labels and are not Stockfish centipawn-loss classifications.

## Principal harmonic line

`HarmonicLineAnalyzer` advances only the currently selected best continuation at every ply. It does not build a conventional broad tree. A one-reply minimax extension is activated only for a rook-or-greater material swing, an exposed queen, check, promotion or extreme tactical energy. This is an experimental selective-search policy, not a claim of solved chess.
