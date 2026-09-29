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

1. **Move dissonance** comes from the played move's loss outside theory. Theory has zero global move dissonance.
2. **Tactical energy** comes from checks, captures, attacks and contact. A sound theoretical position may be energetic without sounding wrong.
3. **Perspective discomfort** adds a restrained color for the side whose score, king safety or pressure is worse.

This prevents a sound sacrifice or a sharp book line from being mislabeled as a mistake merely because many pieces attack each other.

## One voice per piece

`PieceSonifier` emits one deterministic voice for every live piece. It uses the selected chord's pitch classes, piece type, color and square to choose register and pitch. The initial position therefore contains 32 voices whose pitch classes all belong to C major. Perspective discomfort is expressed with small deterministic detuning instead of changing the objective board chord.

## Perception

`PerceptionAnalyzer` turns one board snapshot into three listeners:

- **Global** uses move dissonance and intentionally ignores which color benefits from the imbalance.
- **White** uses White-relative score and restrained discomfort from pressure, king fragility and disadvantage.
- **Black** mirrors the score and computes Black's own exposure independently. At the initial position Black hears a very small discomfort because White owns the first-move initiative.

A good move for one side can therefore improve that side's sound while making the opponent's sound more tense even if the global position remains coherent. `PerceptionDelta` stores the before/after change so the musical transition can be taught, not merely the destination chord.

The formulas and weights are research hypotheses. They are deterministic so experiments are reproducible; they are not claims of engine strength or perceptual universality.

## Move decisions

In theory, the corpus determines which continuations count as theory. Piano Man evaluates only those theoretical children harmonically and recommends the most harmonious continuation, using corpus weight only as a deterministic tie-breaker.

Outside theory, every legal move is evaluated one static ply. Move quality remains anchored to the best static score. Piano Man style operates only inside a `≤15` correctness window.

When an opponent move loses at least 81 Piano Man points, it can activate **Radio Killer** for the responding side. Radio Killer still stays inside the same correctness window; among those sufficiently correct moves it prefers the line that increases the opponent's perceived tension. This encodes the design rule: *never play badly merely to sound aggressive*.

Quality labels remain Piano Man labels and are not Stockfish centipawn-loss classifications.
