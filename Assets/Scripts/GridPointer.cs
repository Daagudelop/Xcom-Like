
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class GridPointer : MonoBehaviour
{
    #region Variables

    public static GridPointer instance;

    public LayerMask walkableLayer;

    [HideInInspector]
    public int entityCellSize;
    [HideInInspector]
    public Vector3 LastPos = Vector3.zero;
    [HideInInspector]
    public Camera mainC;
    [HideInInspector]
    public Vector3 pos;
    //[HideInInspector]
    public Entity currentEntityCore;
    public SerializedDictionary<Vector3, float> allGridPoints;

    #endregion

    private void Awake()
    {
        mainC = Camera.main;
        if (instance = null)
        {
            instance = this;
        }
    }

    private void Update()
    {
        MovePointerToMouse();
    }

    public void MovePointerToMouse()
    {
        //currentEntityCore = TurnManager.instance.currentEntity.GetComponent<EntityCore>();
        if (currentEntityCore != null && allGridPoints != null)
        {
            entityCellSize = currentEntityCore.CellSize;
            Ray ray = mainC.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, walkableLayer))
            {
                if (LastPos == Vector3.zero
                    || raycastHit.point.x > LastPos.x + entityCellSize
                    || raycastHit.point.x < LastPos.x + entityCellSize
                    || raycastHit.point.z > LastPos.z + entityCellSize
                    || raycastHit.point.z < LastPos.z + entityCellSize)
                {
                    //debug.Log("in");
                    pos = (Vector3)Utilities.CenterPosOnCellYNull(raycastHit.point, entityCellSize);
                    pos = new Vector3(pos.x, Mathf.Round(pos.y / 2f) * 2f, pos.z);
                    if (allGridPoints.ContainsKey(pos))
                    {
                        gameObject.transform.position = new Vector3(pos.x, pos.y, pos.z);
                        //Debug.Log($"grid Data: {currentEntityCore.GridPoints[pos]} {currentEntityCore.GridPointsVisibility[pos]}");
                        LastPos = gameObject.transform.position;
                    
                    }
                }
            }
        }
            
        }
}