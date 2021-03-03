using UnityEngine;
using System.Collections;

public class MovingUnitUI : MonoBehaviour
{
    public GameObject unitObj;
    public GameObject routeObj;

    public string id;
    public string belongToOutpostID;
    public int UnitAmount;

    public Vector2Int startPosition;
    public Vector2Int endPosition;
    public float totalLength;
    public float currentLength;
    public float speed;
    public int lastUpdateTime;
    public int factionOrder;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
