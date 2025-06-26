# Zonengenerator Prototype

This repository contains a small Godot + .NET prototype used to experiment with
procedural zone generation on a hex grid. It demonstrates how a map can be
generated at runtime and how gameplay systems can interact with it.

## Building and Running

1. Install **Godot 4** with C# support.
2. Clone this repository and open `project.godot` in the Godot editor.
3. Build the C# solution from within Godot or using `dotnet build` if the .NET
   SDK is installed.
4. Run the project from the editor to explore the prototype.

## Key Components

- **MapRoot** – Root node that holds the generated tilemaps, manages fog of war
  and exposes events such as `OnTileEntered` and `OnLocationDiscovered`.
- **MapGenerator** – Procedurally creates the terrain layout and transition
  tiles. It can also place `LocationInfo` objects according to quadrant hints.
- **CharacterNode** – The player's character. It updates fog of war while
  moving and emits a signal when clicked.
- **MapCameraController** – Handles edge scrolling and automatically follows the
  character.
- **Game** – Example control that creates different sized maps and wires up
  basic event handlers.

## Working With Events

`MapRoot` provides several events which you can subscribe to in your own nodes:

```csharp
map.OnTileEntered += tile => GD.Print($"Entered {tile}");
map.OnLocationDiscovered += info => GD.Print($"Found {info.Name}");
```

You can extend the system by adding your own logic in these callbacks or by
creating additional nodes that react to movement and discovery.

## Integrating Into Your Game

The prototype is intentionally self‑contained. To use it in a larger game you
can instantiate `MapRoot` scenes, provide your own `MapGenerator`
implementation and subscribe to the exposed events to drive gameplay. The
`CharacterNode` is a simple example that can be replaced with your own player or
AI agents.

## License

This project is released under the MIT License. See `LICENSE.md` for the full
text.

