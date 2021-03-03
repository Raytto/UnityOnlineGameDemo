using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class WarTower
{
    //public int x;
    //public int y;
    public string id="";
    public Vector2Int position=new Vector2Int();
    public bool isBrithTower=false;
    public bool isLifeTower=false;
    public int factionOrder=0;
    public float towerValue = 1;
    public List<string> linkedTowerIDs=new List<string>();

    public MDs.State towerState = MDs.State.Staying;
    public int stateChangeLeftTime;
    public int lastUpdateTime=0;

    public int towerLevel = 0;

    public string GetID()
    {
        if(id=="")
        {
            return id=GetIDbyLogicPosition(new Vector2(position.x, position.y));
        }
        else
        {
            return id;
        }

    }

    public static string GetIDbyLogicPosition(Vector2 lp)
    {
        return "WarTower" +lp.x + "_" + lp.y;
    }
}
