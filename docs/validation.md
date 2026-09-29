# Validation

The release gate for Piano Man 0.3 is deterministic and offline after restore.

```bash
dotnet restore PianoMan.sln
dotnet build PianoMan.sln --configuration Release
dotnet test PianoMan.sln --configuration Release --no-build
dotnet run --project src/PianoMan.Cli --configuration Release -- demo
dotnet run --project src/PianoMan.Web --configuration Release
node --check src/PianoMan.Web/wwwroot/app.js
```

Web smoke checks:

```bash
curl -f http://localhost:5000/api/health
curl -f http://localhost:5000/api/demo
curl -f -H 'content-type: application/json' \
  -d '{"pgn":"1. e4 e5 2. Nf3 Nc6 *"}' \
  http://localhost:5000/api/analyze
curl -f -H 'content-type: application/json' \
  -d '{"fen":"rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1","uci":"e7e5","ply":2}' \
  http://localhost:5000/api/move
```

The full Immortal Game in `samples/immortal-game.pgn` must parse through mate. The initial timeline entry must expose 32 piece voices and every first-ply book continuation must have zero global dissonance. CLI JSON must serialize the new analysis decision/candidate types. CI additionally publishes and executes a Linux Native AOT CLI.

## Scientific interpretation

Move-quality labels are Piano Man model labels, not Stockfish labels. Outside the book every legal move is evaluated exactly one static ply from the moving player's perspective; only the top ten plus the played move are retained for presentation. In-theory selection never looks at child evaluations.

## Auditory experiment

The web UI can mark dissonant plies and export observations. Export is designed for a listen-first workflow: Stockfish can be applied later to the same FENs without contaminating the initial auditory judgment.
