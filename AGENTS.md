# AGENTS.md

## Übersicht
- .NET‑8-Projekt im Repo‑Root mit Godot‑4.4‑Setup
- Laufumgebung: Docker‑Container mit .NET 8, Godot CLI 4.4

## Build & Test
- Dotnet‑Build: `dotnet build`
- Godot-build: `./Godot_v4.4.1-stable_mono_linux.x86_64 --headless --path . --main-scene scenes/game.tscn --quit -v`

## Code‑Stil
- C#: StyleCop-Anweisungen, Benennungskonventionen, Vollständig kommentiert
- GDScript, wenn möglich vermeiden
- GDShader mit vollständiger erklärung was?,wie?,warum?

## PR‑Format
- "[ChatGPT] <Kurzbeschreibung>"
- Abschnitt "Tests bestanden: ✅"

## Umgebungsvoraussetzungen
- `.NET SDK 8.x`
- `Godot 4.4 CLI`
- Tests müssen ohne GUI ausführbar sein
