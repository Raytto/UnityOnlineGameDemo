using System;
using UnityEngine;

public class Unit
{
    private string id="";
    public Vector2Int position;
    public string belongToOutpostID="";
    public int UnitAmount;
    public int factionOrder;

    private static int orderForCreateUnrepeatedID=0;

    public string GetID()
    {
        if (id == "")
        {
            if (factionOrder == -1)
            {
                Debug.Log("Error in Outpost GetID for FactionOrder==-1");
                return null;
            }
            //Debug.Log("Outpost_" + factionOrder + "_" + (++orderForCreateUnrepeatedID));
            return id = "Outpost_" + factionOrder + "_" + (++orderForCreateUnrepeatedID);
        }
        else
        {
            return id;
        }
    }
}

