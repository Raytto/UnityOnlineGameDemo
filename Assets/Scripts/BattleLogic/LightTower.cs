using UnityEngine;
using System.Collections;
using System;

public class LightTower
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
            return id = GetIDbyLogicPosition(new Vector2(position.x, position.y));
        }
        else
        {
            return id;
        }
    }

    public static string GetIDbyLogicPosition(Vector2 lp)
    {
        return "LightTower" + lp.x + "_" + lp.y;
    }
}
