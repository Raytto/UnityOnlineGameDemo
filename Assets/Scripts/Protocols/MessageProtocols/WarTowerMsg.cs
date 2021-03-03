using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public class WarTowerMsg
{
    public string towerID;
    public int logicPositionX;
    public int logicPositionY;
    public bool isBrithTower;
    public bool isLifeTower;
    public int FactionOrder;
    public float towerValue;
    public List<string> linkedTowerIDs;

    public int towerLevel = 0;

    public MDs.State towerState = MDs.State.Staying;
    public int stateChangeLeftTime;
    public int lastUpdateTime = 0;
}

