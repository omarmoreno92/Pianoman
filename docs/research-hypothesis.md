# Research hypothesis — How Chess Sounds

## Goal

Piano Man is an experiment in whether the mathematics of musical harmony can provide a compact, explainable representation of chess structure. The product goal is educational: let a learner hear development, coordination, balance, pressure, mistakes and recovery quickly enough that musical intuition becomes another way to understand a position.

The engine goal is deliberately different from “search deeper than Stockfish.” Piano Man should spend less computation on broad tree search and more on a compact structural representation. The long-term hypothesis is that this can be competitive in some time-constrained positions. That hypothesis is unproven.

## What would count as evidence

A claim that Piano Man is more efficient or stronger than a reference engine requires controlled experiments:

- pinned Piano Man and Stockfish versions;
- identical hardware and resource limits;
- explicit time controls and thread/hash settings;
- a large, published position/game corpus;
- legal-move and adjudication rules fixed in advance;
- latency, memory, nodes/work performed and move quality recorded;
- confidence intervals and losses reported, not only wins.

A single attractive game, a tactical upset, or a position where Stockfish misses something is not enough.

## Teaching hypothesis

Humans often learn recurring patterns more easily when several sensory representations agree. Piano Man will test whether chess structures can acquire stable auditory signatures. Future renderers may express the same underlying harmonic vector through different musical languages—rock, classical, rhythmic or other vocabularies—without changing chess evaluation.

The target is not absolute pitch. A learner should be able to recognize relative changes: resolution, increased tension, deteriorating king safety, loss of coordination, regained balance, or an opponent beginning to suffer.

## Piano Man and Radio Killer

Piano Man seeks correct, harmonious play and defensive stability. It is not required to attack merely because an attack exists.

Radio Killer is not a separate chess engine. It is a constrained exploitation policy activated by an opponent mistake. It preserves a correctness window first, then increases the opponent's perceived tension. If the advantage disappears, behavior returns to Piano Man.

This separation is intended to make the music pedagogical: a learner should hear *why* the position became uncomfortable rather than merely receive a numerical verdict.
