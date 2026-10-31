/// <summary>
/// Define en qué estados del juego funciona cada parte del sistema de escuadrón.
/// Si no hay GameManager en la escena (escena de pruebas), se asume exploración.
/// </summary>
public static class SquadGameRules
{
    private static GameState Current =>
        GameManager.instance != null
            ? GameManager.instance.currentGameState
            : GameState.InExplorationMode;

    /// <summary>Selección y movimiento point & click: solo exploración.</summary>
    public static bool CanSelectAndMove => Current == GameState.InExplorationMode;

    /// <summary>Inventario: exploración y combate.</summary>
    public static bool CanUseInventory =>
        Current == GameState.InExplorationMode || Current == GameState.InBattle;
}
