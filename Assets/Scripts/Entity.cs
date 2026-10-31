using AYellowpaper.SerializedCollections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] public TeamsEnum team;

    [SerializeField] int cellSize = 2;

    [SerializeField] private SerializedDictionary<StatName, StatData> stats;

    [SerializeField] private SerializedDictionary<ActionName, ActionsData> actions;
    
    [SerializeField] private List<GameObject> actionsBeingUsed;

    [SerializeField] private bool playeable;

    [SerializeField] private Outline outlineEntity;

    public SerializedDictionary<StatName, StatData> Stats { get => stats; set => stats = value; }

    public SerializedDictionary<ActionName, ActionsData> Actions { get => actions; set => actions = value; }


    public int CellSize { get => cellSize; set => cellSize = value; }
    public List<GameObject> ActionsBeingUsed { get => actionsBeingUsed; set => actionsBeingUsed = value; }
    public bool Playeable { get => playeable; set => playeable = value; }
    public Outline OutlineEntity { get => outlineEntity; set => outlineEntity = value; }

    private void Awake()
    {
        outlineEntity.enabled = false;
    }

    private void OnEnable()
    {
        outlineEntity.enabled = false;
    }

    public void OnBattle(bool inTurn)
    {
        if (inTurn)
        {
            foreach (var item in Actions)
            {
                if (item.Value.activeOnStart)
                {
                    GameObject action = Instantiate(item.Value.prefab);
                    ActionsBeingUsed.Add(action);
                }
            }
            if (TurnManager.instance.currentTeam == TeamsEnum.player)
            {
                playeable = true;
            }
        }
        else
        {
            foreach (var item in ActionsBeingUsed)
            {
                ActionsBeingUsed.Remove(item);
            }
            if (TurnManager.instance.currentTeam == TeamsEnum.player)
            {
                playeable = false;
            }
        }
    }

}


public enum StatName
{
    Strength,
    dextery,
    Intelligence,
    MovementPoints,
}

public enum ActionName
{
    MoveOnGrid,
}

[Serializable]
public class StatData
{
    public float min, current, max;
    public List<string> minEquation, currentEquation, maxEquation;
}

[Serializable]
public class ActionsData
{
    public ActionName actionName;
    public GameObject prefab;

    public bool activeOnStart;
}

