# Contributing

Piano Man is an experiment, so changes to the model need evidence that can be
reproduced.

## Local workflow

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project src/PianoMan.Cli -- demo
```

## Pull requests

- Keep the chess evaluation independent from the chord mapper.
- Explain a new feature or weight in `docs/harmonic-model.md`.
- Add a focused test for move-generation and PGN correctness changes.
- Include the PGN or FEN for any chess-specific bug.
- Include before/after benchmark data for performance claims.

Avoid tuning weights against a single famous game. Put candidate changes through a
fixed corpus so that improvements remain measurable.
