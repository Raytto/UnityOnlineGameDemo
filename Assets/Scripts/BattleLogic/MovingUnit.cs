using System;
using UnityEngine;

public class MovingUnit
{
    private string id;
    public string belongToOutpostID;
    public int UnitAmount;

    public Vector2Int startPosition;
    public Vector2Int endPosition;
    public float totalLength;
    public float currentLength;
    public float speed;
    public int lastUpdateTime;
    public int factionOrder;

    private static int orderForCreateUnrepeatedID=0;

    public string GetID()
    {
        if (id == null)
        {
            if (factionOrder == -1)
            {
                Debug.Log("Error in Outpost GetID for FactionOrder==-1");
                return null;
            }
            return id = "MovingUnit_" + factionOrder + "_" + (++orderForCreateUnrepeatedID);
        }
        else
        {
            return id;
        }
    }

    //public static MovingUnit CreateMovingUnit(Unit unit,Vector2Int targetPosition)
    //{
    //    MovingUnit movingUnit = new MovingUnit
    //    {
            
    //    };
    //    return movingUnit;
    //}
}

