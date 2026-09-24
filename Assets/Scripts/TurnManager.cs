using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    [SerializeField] public SerializedDictionary<TeamsEnum, SerializedDictionary<Entity, bool>> teams;
    [SerializeField] public Queue<TeamsEnum> teamsStack;

    [SerializeField] public TeamsEnum currentTeam;

    

    // Se dispara UNA vez cuando se entra a un estado nuevo
    public static event Action<TeamsEnum> OnStateEnter;
    // Opcional: se dispara al salir de un estado
    public static event Action<TeamsEnum> OnStateExit;

    private Entity currentSelectedEntity;

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

    public void ChangeState(TeamsEnum newTurn)
    {
        if (newTurn == currentTeam) return; // evita re-disparar el mismo estado

        OnStateExit?.Invoke(currentTeam);

        currentTeam = newTurn;
        Debug.Log($"[GameManager] Entrando al turno de: {newTurn}");

        OnStateEnter?.Invoke(currentTeam);
    }

    private void OnEnable()
    {
        GameManager.OnStateEnter += HandleStateEnter; // no necesita instancia de GameManager
    }

    private void OnDisable()
    {
        GameManager.OnStateEnter -= HandleStateEnter;
    }

    private void HandleStateEnter(GameState state)
    {
        if (state == GameState.InBattle)
            FirstTurn(TeamsEnum.player);
    }



    #region Turns

    public void FirstTurn(TeamsEnum InitialTeam)
    {
        GameObject[] entityInBattle = GameObject.FindGameObjectsWithTag("Entity");

        foreach (var entity in entityInBattle)
        {
            TeamsEnum entityTeam = entity.GetComponent<Entity>().team;
            if (!teams.ContainsKey(entityTeam))
            {
                teams.Add(entityTeam, new SerializedDictionary<Entity, bool>());
                teams[entityTeam].Add(entity.GetComponent<Entity>(), false);
            }
            else
            {
                teams[entityTeam].Add(entity.GetComponent<Entity>(), false);
            }
        }

        teamsStack = new Queue<TeamsEnum>();
        teamsStack.Enqueue(InitialTeam);

        currentTeam = InitialTeam;

        foreach (var item in teams)
        {
            if (!teamsStack.Contains(item.Key))
            {
                teamsStack.Enqueue(item.Key);
            }
        }



        foreach (var entity in teams[InitialTeam].Keys.ToList())
        {
            teams[InitialTeam][entity] = true;

            entity.OnBattle(teams[InitialTeam][entity]);
        }

        // Ejecuta esto cuando comience el 3er turno a partir de ahora
        //TurnManager.instance.ScheduleOnTurnStart(3, () => Debug.Log("Inicio: pasaron 3 turnos"));

        // Ejecuta esto cuando termine el 3er turno a partir de ahora
        //TurnManager.instance.ScheduleOnTurnEnd(3, () => Debug.Log("Fin: pasaron 3 turnos"));

        //TurnManager.instance.ScheduleRepeatingOnTurnStart(3, () => Debug.Log("cada turno al inicio"));
        //TurnManager.instance.ScheduleRepeatingOnTurnEnd(3, () => Debug.Log("Cada turno al final"));
    }

    [ContextMenu("Next Turn")]

    // ================== NEXT TURN ==================

    public void NextTurn()
    {
        TeamsEnum lastTeam = currentTeam;

        OnStateExit?.Invoke(lastTeam);
        foreach (var entity in teams[lastTeam].Keys.ToList())
        {
            teams[lastTeam][entity] = false;
            entity.OnBattle(teams[lastTeam][entity]);
        }

        teamsStack.Enqueue(teamsStack.Dequeue());
        currentTeam = teamsStack.Peek();
        turnNumber++;

        OnStateEnter?.Invoke(currentTeam);
        foreach (var entity in teams[currentTeam].Keys.ToList())
        {
            teams[currentTeam][entity] = true;
            entity.OnBattle(teams[currentTeam][entity]);
        }

        TickActions(startActions);
        TickActions(endActions);
    }

    #endregion

    #region ActionsOnTurn
    private class ScheduledAction
    {
        public int targetTurn;
        public int remainingExecutions;
        public Action action;
    }

    private int turnNumber = 1;
    private readonly List<ScheduledAction> startActions = new List<ScheduledAction>();
    private readonly List<ScheduledAction> endActions = new List<ScheduledAction>();

    // ================== UNA SOLA VEZ ==================

    // Se ejecuta UNA vez, al INICIO del turno especificado (contando el actual como 1)
    // Ej: en turno 1, ScheduleOnTurnStart(3, ...) -> se ejecuta al inicio del turno 3
    public void ScheduleOnTurnStart(int turnsFromNow, Action action)
    {
        int target = turnNumber + turnsFromNow - 1;
        startActions.Add(new ScheduledAction { targetTurn = target, remainingExecutions = 1, action = action });
    }

    // Se ejecuta UNA vez, representando el FINAL del turno especificado,
    // lo cual se resuelve al INICIO del turno siguiente.
    // Ej: en turno 1, ScheduleOnTurnEnd(3, ...) -> "fin del turno 3" -> se dispara al iniciar el turno 4
    public void ScheduleOnTurnEnd(int turnsFromNow, Action action)
    {
        int target = turnNumber + turnsFromNow;
        endActions.Add(new ScheduledAction { targetTurn = target, remainingExecutions = 1, action = action });
    }

    // ================== REPETIDAS (con conteo) ==================

    // Ejecuta AHORA MISMO (inicio del turno actual) y luego al inicio de cada
    // turno siguiente, hasta completar "count" ejecuciones en total.
    public void ScheduleRepeatingOnTurnStart(int count, Action action)
    {
        action?.Invoke(); // 1ra ejecución, inicio del turno actual

        int remaining = count - 1;
        if (remaining > 0)
        {
            startActions.Add(new ScheduledAction
            {
                targetTurn = turnNumber + 1,
                remainingExecutions = remaining,
                action = action
            });
        }
    }

    // NO ejecuta inmediatamente (el turno actual aún no terminó).
    // La 1ra ejecución representa el fin del turno actual y se resuelve
    // al iniciar el turno siguiente. Repite hasta completar "count".
    public void ScheduleRepeatingOnTurnEnd(int count, Action action)
    {
        endActions.Add(new ScheduledAction
        {
            targetTurn = turnNumber + 1,
            remainingExecutions = count,
            action = action
        });
    }

    // ================== TICK (compartido por ambos tipos) ==================

    private void TickActions(List<ScheduledAction> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (turnNumber >= list[i].targetTurn)
            {
                list[i].action?.Invoke();
                list[i].remainingExecutions--;

                if (list[i].remainingExecutions <= 0)
                    list.RemoveAt(i);
                else
                    list[i].targetTurn += 1;
            }
        }
    }

    #endregion

    #region SelectionOnTurn

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelect();
        }
    }

    private void SetOutline(bool state, Outline outline)
    {
        if (outline != null)
            outline.enabled = state;
    }

    private void TrySelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Entity clickedEntity = hit.collider.GetComponent<Entity>();

            if (clickedEntity != null && clickedEntity.Playeable)
            {
                // Deseleccionar la entidad anterior (si existe y es distinta a la nueva)
                if (currentSelectedEntity != null && currentSelectedEntity != clickedEntity)
                {
                    Deselect(currentSelectedEntity);
                }

                // Seleccionar la nueva entidad
                Select(clickedEntity);

                currentSelectedEntity = clickedEntity;
            }
            else
            {
                Deselect(currentSelectedEntity);
            }
        }
        else
        {
            Deselect(currentSelectedEntity);
        }
    }

    private void Select(Entity entity)
    {
        if (currentTeam == TeamsEnum.player)
        {
            foreach (var item in entity.Actions)
            {
                item.Value.prefab.GetComponent<IAbility>().SelectedPlayable();
            }
        }
        else
        {
            foreach (var item in entity.Actions)
            {
                item.Value.prefab.GetComponent<IAbility>().SelectableAI();
            }
        }
        SetOutline(true, entity.OutlineEntity);
    }

    private void Deselect(Entity entity)
    {
        if (entity == null) return;
        if (currentTeam == TeamsEnum.player)
        {
            foreach (var item in entity.Actions)
            {
                item.Value.prefab.GetComponent<IAbility>().UnSelectedPlayable();
            }
        }
        else
        {
            foreach (var item in entity.Actions)
            {
                item.Value.prefab.GetComponent<IAbility>().UnSelectableAI();
            }
        }
        SetOutline(false, entity.OutlineEntity);
    }


    #endregion
}

public interface IAbility
{
    void SelectedPlayable();
    void SelectableAI();
    void UnSelectedPlayable();
    void UnSelectableAI();
}


public enum TeamsEnum
{
    Ally,
    player,
    Enemy,
}
