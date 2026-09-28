[CmdletBinding()]
param(
    [string]$InputDirectory = (Join-Path $PSScriptRoot 'input'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\src\PianoMan.Core\Resources')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$stockfishCommit = '65815ccdbc7727cd4f6aee252ba8f67fb740e92f'
$lichessCommit = 'c67912be581f0793dbaa776be5ccf111e01f88d9'
$zipSha256 = '14d9bc9fce1fd96b58d2814fe7ab5b0967109c8411570a0d114558e13ad28fce'
$expectedBookSha256 = '36cd056f0fdea527c25fa40b184ae26ef0f5eafb9722a796be9cae0d36927d47'

$stockfishDirectory = Join-Path $InputDirectory 'stockfish'
$lichessDirectory = Join-Path $InputDirectory 'lichess'
$zipPath = Join-Path $stockfishDirectory 'bjbraams_chessdb_198350_lines.pgn.zip'

New-Item -ItemType Directory -Force -Path $stockfishDirectory, $lichessDirectory, $OutputDirectory | Out-Null

function Get-PinnedFile {
    param([Parameter(Mandatory)][string]$Uri, [Parameter(Mandatory)][string]$Destination)
    if (-not (Test-Path $Destination)) {
        Write-Host "Downloading $Uri"
        Invoke-WebRequest -Uri $Uri -OutFile $Destination
    }
}

Get-PinnedFile `
    -Uri "https://raw.githubusercontent.com/official-stockfish/books/$stockfishCommit/bjbraams_chessdb_198350_lines.pgn.zip" `
    -Destination $zipPath

$actualZipSha = (Get-FileHash -Algorithm SHA256 $zipPath).Hash.ToLowerInvariant()
if ($actualZipSha -ne $zipSha256) {
    throw "Stockfish ZIP SHA-256 mismatch. Expected $zipSha256, got $actualZipSha."
}

foreach ($file in 'a.tsv','b.tsv','c.tsv','d.tsv','e.tsv') {
    Get-PinnedFile `
        -Uri "https://raw.githubusercontent.com/lichess-org/chess-openings/$lichessCommit/$file" `
        -Destination (Join-Path $lichessDirectory $file)
}

Push-Location (Join-Path $PSScriptRoot '..\..')
try {
    dotnet run --project tools/PianoMan.BookCompiler --configuration Release -- `
        --stockfish-zip $zipPath `
        --lichess-dir $lichessDirectory `
        --output $OutputDirectory

    $bookPath = Join-Path $OutputDirectory 'theory-book-v1.bin.br'
    $actualBookSha = (Get-FileHash -Algorithm SHA256 $bookPath).Hash.ToLowerInvariant()
    if ($actualBookSha -ne $expectedBookSha256) {
        throw "Release book SHA-256 mismatch. Expected $expectedBookSha256, got $actualBookSha."
    }

    Write-Host "Piano Man theory book v1 reproduced successfully: $actualBookSha"
}
finally {
    Pop-Location
}
