using UnityEngine;
using System.Collections;

public class LightTowerUI : MonoBehaviour
{
    public string id = "";
    public Vector2Int position = new Vector2Int();

    public MDs.LightState[] factionState;
    public int[] coolDownTime;
    public int[] leftLightTime;

    public int lightRadius = 20;

    public int lastUpdateTime = 0;


    public string GetID()
    {
        if (id == "")
        {
            return id = GetIDbyLogicPosition(new Vector2Int(position.x, position.y));
        }
        else
        {
            return id;
        }
    }

    public static string GetIDbyLogicPosition(Vector2Int lp)
    {
        return "LightTower" + lp.x + "_" + lp.y;
    }

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
        return LogicToUIPosition(position);
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
