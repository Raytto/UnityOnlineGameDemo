using UnityEngine;
using System.Collections;

public class WarTowerUI : MonoBehaviour
{
    public string id;
    public Vector2Int logicPosition = new Vector2Int();
    public bool isBrithTower = false;
    public bool isLifeTower = false;
    public int FactionOrder = 0;
    public float towerValue = 0;

    public MDs.State towerState = MDs.State.Staying;
    public int stateChangeLeftTime;
    public int lastUpdateTime = 0;

    public int towerLevel = 0;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public Vector3 GetUIPosition()
    {
        return LogicToUIPosition(logicPosition);
    }

    public static Vector3 LogicToUIPosition(Vector2Int logicVector)
    {
        return new Vector3(logicVector.x + 0.5f, logicVector.y + 0.5f, 0);
    }

    public static Vector2Int UIToLogicPosition(Vector3 v3)
    {
        return new Vector2Int((int)v3.x, (int)v3.y);
    }
}
