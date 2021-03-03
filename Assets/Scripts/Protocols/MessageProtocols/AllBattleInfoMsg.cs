using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[Serializable]
public class AllBattleInfoMsg
{
    public MapMsg mapMsg;
    public TimeAndSpeedMsg timeAndSpeedMsg;
    public List<WarTowerMsg> warTowerMsgs;
    public List<FactionMsg> factionMsgs;
    public List<OutpostMsg> outpostMsgs;
    public List<UnitMsg> unitMsgs;
    public List<MovingUnitMsg> movingUnitMsgs;
    public List<LightTowerMsg> lightTowerMsgs;
}

