using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public class LightTowerMsg
{
    public string id = "";
    public int positionX;
    public int positionY;

    public MDs.LightState[] factionState;
    public int[] coolDownTime;
    public int[] leftLightTime;

    public int lightRadius = 20;

    public int lastUpdateTime = 0;
}

