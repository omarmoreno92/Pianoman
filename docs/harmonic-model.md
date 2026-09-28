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

`HarmonyAnalyzer` owns this chess-derived vector and tactical tension. `ChordMapper` consumes derived values and maps them to chord/MIDI notes. These components remain independent so changing musical vocabulary cannot silently change chess evaluation.

## Perception

`PerceptionAnalyzer` turns one board snapshot into three listeners:

- **Global** uses board tension plus imbalance and intentionally ignores which color benefits from the imbalance.
- **White** uses White-relative score and additional tension from pressure, king fragility and disadvantage.
- **Black** mirrors the score and computes Black's own exposure independently.

A good move for one side can therefore improve that side's sound while making the opponent's sound more tense even if the global position remains coherent. `PerceptionDelta` stores the before/after change so the musical transition can be taught, not merely the destination chord.

The formulas and weights are research hypotheses. They are deterministic so experiments are reproducible; they are not claims of engine strength or perceptual universality.

## Move decisions

In theory, the corpus determines which continuations count as theory. Piano Man evaluates only those theoretical children harmonically and recommends the most harmonious continuation, using corpus weight only as a deterministic tie-breaker.

Outside theory, every legal move is evaluated one static ply. Move quality remains anchored to the best static score. Piano Man style operates only inside a `≤15` correctness window.

When an opponent move loses at least 81 Piano Man points, it can activate **Radio Killer** for the responding side. Radio Killer still stays inside the same correctness window; among those sufficiently correct moves it prefers the line that increases the opponent's perceived tension. This encodes the design rule: *never play badly merely to sound aggressive*.

Quality labels remain Piano Man labels and are not Stockfish centipawn-loss classifications.
