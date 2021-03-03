using UnityEngine;
using System.Collections;
using System;

public class Outpost 
{
    private string id="";
    public bool isBuilding;
    public string oldOutpostID;

    public int leftBuildTime;
    public int lastUpdateTime;

    public int blood;
    public int unitNum;
    public int outTroopNum;
    public int factionOrder=-1;
    public Vector2Int position;

    public int power;

    private static int orderForCreateUnrepeatedID;

    public string GetID()
    {
        if(id=="")
        {
            if(factionOrder==-1)
            {
                Debug.Log("Error in Outpost GetID for FactionOrder==-1");
                return null;
            }
            return id = "Outpost_" + factionOrder + "_"+(++orderForCreateUnrepeatedID);
        }
        else
        {
            return id;
        }
    }
}
