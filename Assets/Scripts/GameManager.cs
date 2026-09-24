using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager instance;


    // Se dispara UNA vez cuando se entra a un estado nuevo
    public static event Action<GameState> OnStateEnter;
    // Opcional: se dispara al salir de un estado
    public static event Action<GameState> OnStateExit;

    public GameState currentGameState = GameState.Setup;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }


    private void Start()
    {
        ChangeState(GameState.Setup);
    }

    public void ChangeState(GameState newState)
    {
        if (newState == currentGameState) return; // evita re-disparar el mismo estado

        OnStateExit?.Invoke(currentGameState);

        currentGameState = newState;
        Debug.Log($"[GameManager] Entrando a estado: {newState}");

        OnStateEnter?.Invoke(currentGameState);
    }

    [ContextMenu("ChangeTobattle")]
    public void ChangeToBattle()
    {
        ChangeState(GameState.InBattle);
    }

    [ContextMenu("ChangeToExploration")]
    public void ChangeToExploration()
    {
        ChangeState(GameState.InExplorationMode);
    }

}

public enum GameState
{
    Setup,
    InExplorationMode,
    InBattle,
}
