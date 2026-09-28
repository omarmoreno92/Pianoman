# Contributing

Piano Man is a reproducible experiment. Keep chess evaluation independent from `ChordMapper`, document changes to the harmonic model, and include focused tests for chess-rule or PGN changes.

Local gate:

```bash
dotnet restore PianoMan.sln
dotnet build PianoMan.sln -c Release
dotnet test PianoMan.sln -c Release --no-build
node --check src/PianoMan.Web/wwwroot/app.js
```

Theory-book changes must use pinned inputs, verify source SHA-256 values, reject illegal moves instead of skipping them, and document a new resource version/hash if output changes. Do not add runtime network calls, Stockfish, or unnecessary production NuGet packages.
