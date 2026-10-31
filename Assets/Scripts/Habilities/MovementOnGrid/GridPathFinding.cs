using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GridPathFinding : MonoBehaviour,IAbility
{

    [SerializeField] bool debug;
    [SerializeField] float maxDepth;
    [SerializeField] float cellSize;
    [SerializeField] GameObject entityGameObject;
    Entity entity;
    private bool hasArrived;


    [HideInInspector]
    public SerializedDictionary<Vector3, float> allGridPoints;

    public bool HasArrived { get => hasArrived; set => hasArrived = value; }

    private void Awake()
    {
        entity = entityGameObject.GetComponent<Entity>();

        allGridPoints = new SerializedDictionary<Vector3, float>();
    }

    private void OnEnable()
    {
        if (entity == null)
        {
            entity = entityGameObject.GetComponent<Entity>();
        }

        if (allGridPoints == null)
        {
            allGridPoints = new SerializedDictionary<Vector3, float>();
        }
        //CalculateMovementRegion();
    }



    public void CalculateMovementRegion()
    {

        TurnManager.instance.ScheduleRepeatingOnTurnStart(1, () => CreateMovementRegion(entity.gameObject, entity.Stats[StatName.MovementPoints].current, entity.CellSize, ref allGridPoints));
    }

    public void SelectedPlayable()
    {
        CalculateMovementRegion();
        GridPointer.instance.currentEntityCore = entity;
        GridPointer.instance.allGridPoints = allGridPoints;
        GridPointer.instance.OnGridPointConfirmed += HandleGridConfirmed;
    }

    public void SelectableAI()
    {
        throw new System.NotImplementedException();
    }
    public void UnSelectedPlayable()
    {
        allGridPoints.Clear();
        GridPointer.instance.currentEntityCore = null;
        GridPointer.instance.allGridPoints = null;
        GridPointer.instance.OnGridPointConfirmed -= HandleGridConfirmed;
    }

    public void UnSelectableAI()
    {
        throw new System.NotImplementedException();
    }

    public void HandleGridConfirmed(Vector3 pos)
    {
        NavMeshAgent navMeshAgentEntity = entity.GetComponent<NavMeshAgent>();
        navMeshAgentEntity.SetDestination(pos);
        entity.Stats[StatName.MovementPoints].current -= allGridPoints[pos];
        Debug.Log(entity.Stats[StatName.MovementPoints].current + "  " + allGridPoints[pos]);
        allGridPoints.Clear();
        TurnManager.instance.ScheduleRepeatingOnTurnEnd(1, () => entity.Stats[StatName.MovementPoints].current = entity.Stats[StatName.MovementPoints].max);
        CheckIfArrived(navMeshAgentEntity);
    }

    private void CheckIfArrived(NavMeshAgent agent)
    {
        // Si todavía está calculando el camino, no hacemos nada
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Evita que se dispare en cada frame mientras está detenido
            if (!HasArrived && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f))
            {
                HasArrived = true;
                OnReachedDestination();
            }
        }
        else
        {
            HasArrived = false;
        }
    }

    private void OnReachedDestination()
    {
        Debug.Log("Llegó al destino");
        CalculateMovementRegion();
    }

    #region Create Movement Region

    public void CreateMovementRegion(GameObject start, float maxDepth, float cellSize, ref SerializedDictionary<Vector3, float> gridPoints)
    {
        gridPoints.Clear();
        Queue<(Vector3 pos, float depth)> queue = new Queue<(Vector3 pos, float depth)>();
        HashSet<Vector3> visited = new HashSet<Vector3>();

        Vector3 roundedStart = RoundToGrid(start.transform.position, cellSize);
        queue.Enqueue((roundedStart, 0));
        visited.Add(roundedStart); // marcamos como visitado, pero NO lo agregamos a gridPoints

        Vector3[] directions = new Vector3[]
        {
        new Vector3(cellSize,0,0),
        new Vector3(-cellSize,0,0),
        new Vector3(0,0,cellSize),
        new Vector3(0,0,-cellSize),

        // Diagonales
        new Vector3(cellSize, 0, cellSize),
        new Vector3(cellSize, 0, -cellSize),
        new Vector3(-cellSize, 0, cellSize),
        new Vector3(-cellSize, 0, -cellSize)
        };

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();

            if (depth >= maxDepth)
                continue;

            int currentDirIndex = 0;
            foreach (var dir in directions)
            {
                Vector3 target = current + dir;
                NavMeshHit hit;

                if (!NavMesh.Raycast(current, target, out hit, NavMesh.AllAreas))
                {
                    Vector3 roundedHit = RoundToGrid(hit.position, cellSize);

                    if (!visited.Contains(roundedHit))
                    {
                        visited.Add(roundedHit);

                        if (currentDirIndex < 4)
                        {
                            gridPoints[roundedHit] = depth + 1;
                            queue.Enqueue((roundedHit, depth + 1));
                        }
                        else
                        {
                            gridPoints[roundedHit] = depth + 1.5f;
                            queue.Enqueue((roundedHit, depth + 1.5f));
                        }
                    }
                }
                currentDirIndex++;
            }
        }
    }


    public static Vector3 RoundToGrid(Vector3 pos, float cellSize)
    {
        return new Vector3(
            Mathf.Round(pos.x / cellSize) * cellSize,
            Mathf.Round(pos.y / cellSize) * cellSize,
            Mathf.Round(pos.z / cellSize) * cellSize
        );
    }

    #endregion

    #region Create Visibility Region
    /*public void CreateVisibilityRegion(GameObject start, float currentSightVision, float maxSightVision, float cellSize, ref Dictionary<Vector3, bool> gridPointsVisibiliy, LayerMask visibilityFilter)
    {

        Dictionary<Vector3, float> AllGridPoints = new Dictionary<Vector3, float>();


        AllGridPoints.Clear();
        Queue<(Vector3 pos, float depth)> queue = new Queue<(Vector3 pos, float depth)>();

        Vector3 roundedStart = RoundToGrid(start.transform.position);
        queue.Enqueue((roundedStart, 0));
        AllGridPoints[roundedStart] = 0;

        Vector3[] directions = new Vector3[]
        {
            new Vector3(cellSize,0,0),
            new Vector3(-cellSize,0,0),
            new Vector3(0,0,cellSize),
            new Vector3(0,0,-cellSize),

            // Diagonales
            new Vector3(cellSize, 0, cellSize),
            new Vector3(cellSize, 0, -cellSize),
            new Vector3(-cellSize, 0, cellSize),
            new Vector3(-cellSize, 0, -cellSize)
        };

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();

            if (depth >= maxSightVision)
                continue; // No expandimos más allá de 15

            //julian no me pegues por este mutante for-foreach sjsjsjsjsj
            int currentDirIndex = 0;
            foreach (var dir in directions)
            {
                Vector3 target = current + dir;
                NavMeshHit hit;

                if (!NavMesh.Raycast(current, target, out hit, NavMesh.AllAreas))
                {
                    Vector3 roundedHit = RoundToGrid(hit.position);



                    if (!AllGridPoints.ContainsKey(roundedHit))
                    {
                        if (currentDirIndex < 4)
                        {
                            AllGridPoints[roundedHit] = depth + 1;
                            queue.Enqueue((roundedHit, depth + 1));
                        }
                        else
                        {
                            AllGridPoints[roundedHit] = depth + 1.5f;
                            queue.Enqueue((roundedHit, depth + 1.5f));
                        }

                        Vector3 dummyDir = new Vector3(roundedHit.x, roundedHit.y + 2f, roundedHit.z);
                        Vector3 direction = (dummyDir - TurnManager.instance.currentEntity.transform.position).normalized;
                        float distance = Vector3.Distance(TurnManager.instance.currentEntity.transform.position, dummyDir);
                        RaycastHit hit1;
                        if (!Physics.Raycast(TurnManager.instance.currentEntity.transform.position, direction, out hit1, distance, visibilityFilter)
                            && distance <= currentSightVision
                            )
                        {
                            //Debug.DrawLine(TurnManager.instance.currentEntity.transform.position, dummyDir, Color.magenta, 1000f);
                            if (!gridPointsVisibiliy.ContainsKey(roundedHit))
                            {
                                gridPointsVisibiliy.Add(roundedHit, true);
                            }
                        }
                        else
                        {
                            //Debug.DrawLine(TurnManager.instance.currentEntity.transform.position, hit1.point, Color.green, 1000f);
                        }
                        //gridPoints[roundedHit] = depth + 1;
                        //queue.Enqueue((roundedHit, depth + 1));
                    }
                }
                currentDirIndex++;
            }
        }

        Dictionary<Vector3, float> gridPoints = new Dictionary<Vector3, float>();

        Debug.Log("Calculating Region");
        gridPoints.Clear();
        gridPointsVisibiliy.Clear();
        
        foreach (var dir in AllGridPoints)
        {
            Vector3 dummyDir = new Vector3(dir.Key.x, dir.Key.y + 2f, dir.Key.z);
            Vector3 direction = (dummyDir - TurnManager.instance.currentEntity.transform.position).normalized;
            float distance = Vector3.Distance(TurnManager.instance.currentEntity.transform.position, dummyDir);
            RaycastHit hit1;
            if (!Physics.Raycast(TurnManager.instance.currentEntity.transform.position, direction, out hit1, distance, visibilityFilter) 
                && distance <= currentSightVision
                )
            {
                //Debug.DrawLine(TurnManager.instance.currentEntity.transform.position, dummyDir, Color.magenta, 1000f);
                if (!gridPointsVisibiliy.ContainsKey(dir.Key))
                {
                    gridPointsVisibiliy.Add(dir.Key, true);
                }
            }
            else
            {
                //Debug.DrawLine(TurnManager.instance.currentEntity.transform.position, hit1.point, Color.green, 1000f);
            }
        }
    }*/


    #endregion

    #region Draw Gizmos
    private void OnDrawGizmos()
    {
        if (debug)
        {
            Gizmos.color = Color.cyan;
            foreach (var item in allGridPoints)
            {
                Gizmos.DrawCube(item.Key, new Vector3(1.8f, 0.1f, 1.8f));
            }
        }
    }


    #endregion

}

