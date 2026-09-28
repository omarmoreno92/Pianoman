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

`HarmonyAnalyzer` owns this chess-derived vector and tension calculation. `ChordMapper` consumes the finished snapshot and maps it to a chord/MIDI notes. These components intentionally remain independent: changing the musical vocabulary must not silently change chess evaluation, and changing evaluation must not make the chord mapper a chess oracle.

The current weights are research hypotheses, not a strength claim. Tension includes checks, attacked/loose pieces and captures. Chord quality uses tension bands plus the white-minus-black relative score. The mapping is deterministic so repeated experiments are comparable.

Version 0.2 adds a separate move-decision layer. In theory, corpus weights choose candidates without using harmony. Outside theory, every legal move is evaluated one static ply, and loss is measured against the best score from the current player's perspective. The labels are explicitly Piano Man labels and are not equivalent to Stockfish centipawn-loss classifications.
