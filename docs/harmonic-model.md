# Harmonic model v0.1

This document defines the model implemented in `HarmonyAnalyzer`. Keeping a written
definition lets us change weights deliberately and compare versions.

## Evaluation vector

For color $c$ in position $P$:

$$
H_c(P)=M_c+A_c+C_c+K_c+S_c+P_c+R_c+I_c
$$

where:

- $M$ is material.
- $A$ is activity.
- $C$ is coordination.
- $K$ is king safety.
- $S$ is space.
- $P$ is pawn structure.
- $R$ is pressure on the opponent.
- $I$ is initiative.

The relative score is:

$$H(P)=H_{white}(P)-H_{black}(P)$$

All arithmetic is integer arithmetic. Positive values favor White and negative
values favor Black.

Checkmate and stalemate are terminal states. Checkmate resolves to `+100000` for a
White win or `-100000` for a Black win; stalemate resolves to `0`. Their unresolved
tension is zero because no reply remains. This lets a sound sacrifice finish in a
decisive resolution even when the winner has less material on the final board.

## Current signals

### Material

| Piece | Value |
|---|---:|
| Pawn | 100 |
| Knight | 320 |
| Bishop | 330 |
| Rook | 500 |
| Queen | 900 |

### Activity and space

Activity awards two units per controlled square. Space awards two units for each
controlled square in the opponent's half of the board.

### Coordination

Every non-king piece defended by a friendly piece contributes four units. This is
deliberately simple; batteries, pins, overloaded defenders, and x-rays belong to a
later relationship layer.

### Pawn structure

- Doubled pawn: `-12` for every pawn beyond the first on a file.
- Isolated pawn: `-8`.
- Connected pawn: `+4`.
- Passed pawn: `+5`, plus `+3` per rank advanced from its starting rank.

### King safety

- Pawn in the immediate shield: `+10`.
- Attacked square in the 3×3 king zone: `-12`.
- King currently in check: `-50`.
- Castled king on its home rank: `+12`.
- No friendly pawn on the king's file: `-8`.

### Pressure

Pressure counts attacked enemy pieces using a small piece-dependent value and adds
a bonus when the target is not defended. Controlled squares in the enemy king zone
add five units each.

### Initiative

The side to move receives eight units. This prevents the representation from
treating tempo as invisible while keeping it small relative to a pawn.

## Tension

Tension is independent from relative advantage:

$$
T(P)=T_{check}+T_{attacked}+T_{loose}+T_{captures}
$$

- A checked king adds `70`.
- An attacked non-king piece adds at least `2`, scaled by material value.
- An undefended attacked piece doubles its local contribution.
- Each currently legal capture adds `2`.
- The final value is clamped to `0..255`.

The first stability threshold is `T < 40`. Future combination search will continue
while tension is unresolved rather than to a fixed nominal depth.

## Chord translation

The chord layer describes the data; it does not determine the chess score.

### Root

The root begins at C and moves along the circle of fifths in 120-unit score bands:

```text
Black advantage                 White advantage
Ab  Eb  Bb  F   C   G   D   A   E
```

### Quality

| Condition | Quality |
|---|---|
| Tension ≥ 140 | diminished seventh |
| Tension ≥ 100 | dominant seventh flat ninth |
| Tension ≥ 65 | suspended fourth add ninth |
| Stable Black advantage | minor |
| Stable White advantage | major seventh |
| Near equality | major |

The mapper also emits MIDI note numbers so another component can render sound
without knowing chess.

## Known omissions

Version 0.1 does not yet model pins, overloaded pieces, discovered attacks,
fortresses, repetition, the fifty-move rule, insufficient material, tablebases, or
long forcing sequences. These omissions are explicit research targets rather than
hidden inside a learned score.
