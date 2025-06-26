# zonengenerator_prototyp

## MapRoot Events

`MapRoot` exposes several events that allow game logic to react to player
interactions:

* `OnTileEntered(Vector2I tile)` - fired when the character moves onto a tile.
* `OnTileRightClicked(Vector2I tile)` - fired when a tile is right-clicked.
* `OnLocationDiscovered(LocationInfo info)` - fired the first time a location
  becomes visible.

See `Game.cs` for a simple subscription example.
