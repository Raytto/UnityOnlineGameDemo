using UnityEngine;
using System.Collections;
using System;

public class Faction
{
    public int order = 0;//0 represent npc
    public float score = 0;
    public float towerScore = 1;
    public int lastUpdateBattleTime=0;


    //for server only
    public WarTower BrithTower = null;
    public int haveWarTowerNum = 0;

    public string GetID()
    {
        return MakeIDByOrder(order);
    }

    public static string MakeIDByOrder(int order)
    {
        return "Faction" + order;
    }


}
