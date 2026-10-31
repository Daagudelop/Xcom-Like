using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Inventory))]
public class Soldier : MonoBehaviour
{
    public static readonly List<Soldier> All = new List<Soldier>();

    [Header("Datos")]
    public string soldierName = "Ranger";

    [Header("Movimiento")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("Selección")]
    [Tooltip("Objeto hijo (anillo/decal) que se activa al seleccionar")]
    public GameObject selectionRing;

    public Inventory Inventory { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public bool IsSelected { get; private set; }

    public event Action<Soldier, bool> OnSelectionChanged;

    public bool IsMoving =>
        !Agent.pathPending && Agent.remainingDistance > Agent.stoppingDistance + 0.05f;

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Inventory = GetComponent<Inventory>();
        ApplySelectionVisual(false);
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;
        IsSelected = value;
        ApplySelectionVisual(value);
        OnSelectionChanged?.Invoke(this, value);
    }

    private void ApplySelectionVisual(bool value)
    {
        if (selectionRing != null) selectionRing.SetActive(value);
    }

    public void MoveTo(Vector3 destination, bool run)
    {
        Agent.speed = run ? runSpeed : walkSpeed;
        Agent.isStopped = false;
        Agent.SetDestination(destination);
    }

    public void Stop()
    {
        Agent.ResetPath();
        Agent.isStopped = true;
    }
}
