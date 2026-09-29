# Piano Man 0.3

**How chess sounds.**

Piano Man asks whether chess structure can be represented musically well enough that a human can *hear* development, balance, pressure, mistakes and recovery instead of learning those ideas only as notation or engine numbers.

Piano Man is a .NET 10 research project that represents chess positions as harmonic structures and lets a user hear a game while inspecting deterministic chess signals. Version 0.3 separates error dissonance from tactical energy, gives every live piece an audible voice, and lets the user branch freely from any PGN position.

> The harmonic model, candidate-loss thresholds and quality labels are Piano Man's experimental model. They are not Stockfish judgments and are not playing-strength claims.

## 0.3 behavior

Before each played move:

- if the position and move exist in the theory corpus, Piano Man shows **`TEORÍA · SIN BÚSQUEDA`**, restricts candidates to known theory and combines harmonic structure with a bounded logarithmic support prior derived from the corpus;
- otherwise it shows **`Afinando…`**, evaluates every legal move at one static ply from the moving player's perspective, retains the top ten plus the played move, and reports its rank/loss.

The provisional Piano Man loss bands are: `0 Best`, `1–15 Excellent`, `16–40 Good`, `41–80 Inaccuracy`, `81–160 Mistake`, and `>160 Blunder`. The web UI translates them to Spanish and keeps the model attribution visible.

Theory membership defines the admissible opening continuations. Piano Man then chooses among those theoretical continuations by harmonic score plus corpus support. The audible theoretical gap is measured against the best continuation at that exact position; no opening name or first move has a manually assigned tension. Sharpness remains audible as **tactical energy**, while a side that is worse can hear mild perspective-specific discomfort.

The auditory model keeps three independent signals:

- **dissonance**: the measured gap to the best admissible continuation—book-derived in theory and position-derived outside it;
- **energy**: attacks, captures, checks and board contact, without implying an error;
- **perspective discomfort**: the disadvantage experienced by White or Black.

Every live piece contributes one deterministic piano voice. The initial board therefore plays 32 voices, all quantized to the initial C harmony. Piece type and square establish its register and pitch; when a move has measured dissonance, the moved piece is altered first and contaminates the aggregate chord in proportion to the loss. Chord mode plays those voices together; arpeggio mode plays every piece one at a time and can be stopped immediately.

Every position now exposes three simultaneous listening perspectives: **Global**, **White**, and **Black**. The same board can therefore remain globally coherent while becoming increasingly uncomfortable for one side.

## Web player

```bash
dotnet restore PianoMan.sln
dotnet run --project src/PianoMan.Web --configuration Release
```

Open the URL printed by ASP.NET Core. The UI supports:

- PGN file picker, drag/drop or pasted PGN (2 MB limit);
- multiple games per PGN;
- board from FEN with last-move highlighting;
- first/previous/next/last controls and keyboard arrows;
- legal click-to-move branching from any loaded PGN position, with a one-click return to the original PGN;
- on-demand single principal harmonic line, with reply extensions only for abrupt material/tactical anomalies;
- complete move timeline;
- ECO/name and theory state;
- chord, MIDI notes, tension, balance and all eight harmonic components;
- candidate rank, SAN/UCI, weight/score/loss, chord and tension;
- Web Audio chord/arpeggio playback with one voice per live piece, tempo, autoplay and independent candidate voices;
- listen-first **Marcar disonancia** observations and JSON export for later external-engine comparison.

API endpoints:

- `POST /api/analyze`
- `POST /api/move`
- `POST /api/principal-line`
- `GET /api/demo`
- `GET /api/health`

No runtime network access, AI model or Stockfish process is required for analysis.

Outside theory, normal **Piano Man** mode preserves chess quality first and prefers the most harmonious continuation inside a narrow correctness window. A **Radio Killer** opportunity is triggered after a Piano Man loss of at least 81: while the advantage persists, the selector still stays inside the `Excellent` (`≤15`) window and only then prefers continuations that maximize the opponent's perceived tension. Dissonance is therefore a consequence of exploiting a mistake, never permission to play a bad move.

## CLI

```bash
dotnet run --project src/PianoMan.Cli --configuration Release -- demo
dotnet run --project src/PianoMan.Cli --configuration Release -- analyze --fen "<fen>"
dotnet run --project src/PianoMan.Cli --configuration Release -- pgn samples/immortal-game.pgn --json artifacts/immortal.json
```

The CLI remains a Native AOT target.

## Theory corpus

The intended release book is a broad, reproducible and versioned CC0 corpus compiled from pinned `official-stockfish/books` and `lichess-org/chess-openings` inputs. It must **not** be described as “all known theory”. See [`docs/theory-book.md`](docs/theory-book.md) and [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

The checked-in `theory-book-v1.bin.br` is the pinned release artifact. A rebuild must run `PianoMan.BookCompiler` against the pinned inputs and reproduce the v1 counts/hash below before replacing it.

Pinned v1 target:

- 251,274 unique positions;
- 420,150 continuations;
- 3,329 ECO/name identities;
- 36 plies maximum;
- 6,713,123 raw bytes;
- 2,794,787 Brotli bytes;
- SHA-256 `6b7a83ce712e6de9f6e0b5a01d76203a25b4f208a57e7cc3a8c6c1623ed6e70b`.

`tools/PianoMan.BookCompiler` is offline and fails on source-hash mismatches, illegal moves, count/size mismatches or a final compressed hash mismatch.

## Project layout

```text
src/PianoMan.Core           chess, harmony, theory and analysis
src/PianoMan.Cli            CLI / Native AOT target
src/PianoMan.Web            ASP.NET Core + static frontend
tools/PianoMan.BookCompiler reproducible book compiler
tests/PianoMan.Tests        deterministic unit tests
docs                         architecture, model, book and validation notes
```

## Validation

```bash
dotnet restore PianoMan.sln
dotnet build PianoMan.sln --configuration Release
dotnet test PianoMan.sln --configuration Release --no-build
dotnet run --project src/PianoMan.Cli --configuration Release -- demo
dotnet run --project src/PianoMan.Web --configuration Release
node --check src/PianoMan.Web/wwwroot/app.js
```

See [`docs/validation.md`](docs/validation.md) for web smoke tests and scientific interpretation.
