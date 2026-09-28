# Piano Man 0.2

**What if a chess position could be heard?**

Piano Man is a .NET 10 research project that represents chess positions as harmonic structures and lets a user hear a game while inspecting deterministic chess signals. Version 0.2 adds a versioned opening-book path, a deterministic one-ply fallback outside theory, and a web PGN player with Web Audio.

> The harmonic model, candidate-loss thresholds and quality labels are Piano Man's experimental model. They are not Stockfish judgments and are not playing-strength claims.

## 0.2 behavior

Before each played move:

- if the position and move exist in the theory corpus, Piano Man shows **`TEORÍA · SIN BÚSQUEDA`**, ranks continuations by corpus weight, and does not assign harmonic loss;
- otherwise it shows **`Afinando…`**, evaluates every legal move at one static ply from the moving player's perspective, retains the top ten plus the played move, and reports its rank/loss.

The provisional Piano Man loss bands are: `0 Best`, `1–15 Excellent`, `16–40 Good`, `41–80 Inaccuracy`, `81–160 Mistake`, and `>160 Blunder`. The web UI translates them to Spanish and keeps the model attribution visible.

Theory sound is not a correctness test. A theoretical move may sound dissonant: the chord is simply the current mathematical representation.

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
- complete move timeline;
- ECO/name and theory state;
- chord, MIDI notes, tension, balance and all eight harmonic components;
- candidate rank, SAN/UCI, weight/score/loss, chord and tension;
- Web Audio chord/arpeggio playback, tempo, autoplay and independent candidate voices;
- listen-first **Marcar disonancia** observations and JSON export for later external-engine comparison.

API endpoints:

- `POST /api/analyze`
- `GET /api/demo`
- `GET /api/health`

No runtime network access, AI model or Stockfish process is required for analysis.

## CLI

```bash
dotnet run --project src/PianoMan.Cli --configuration Release -- demo
dotnet run --project src/PianoMan.Cli --configuration Release -- analyze --fen "<fen>"
dotnet run --project src/PianoMan.Cli --configuration Release -- pgn samples/immortal-game.pgn --json artifacts/immortal.json
```

The CLI remains a Native AOT target.

## Theory corpus

The intended release book is a broad, reproducible and versioned CC0 corpus compiled from pinned `official-stockfish/books` and `lichess-org/chess-openings` inputs. It must **not** be described as “all known theory”. See [`docs/theory-book.md`](docs/theory-book.md) and [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

> **Repository resource note:** the checked-in `theory-book-v1.bin.br` is a 69-byte bootstrap fixture used to exercise the loader/UI in constrained development environments. It is **not** the pinned release corpus. A release build must run `PianoMan.BookCompiler` against the pinned inputs and reproduce the v1 counts/hash below before replacing that fixture.

Pinned v1 target:

- 251,274 unique positions;
- 420,150 continuations;
- 3,329 ECO/name identities;
- 36 plies maximum;
- 7,586,743 raw bytes;
- approximately 2,910,309 Brotli bytes;
- SHA-256 `36cd056f0fdea527c25fa40b184ae26ef0f5eafb9722a796be9cae0d36927d47`.

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
