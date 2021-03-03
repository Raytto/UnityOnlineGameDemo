using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System;

public class Battlefeild
{
    public BattleBasicSetting basicSetting;
    public List<Faction> factions;
    public Dictionary<string, WarTower> warTowers;
    public Dictionary<string, LightTower> lightTowers;
    public Dictionary<string, Outpost> outposts;
    public Dictionary<string, Unit> units;
    public Dictionary<string, MovingUnit> movingUnits;
    public int[,] safeZone;

    //public List<Outpost> outpostsInbuilding;

    public DateTime battleStartTime;
    public int battleTimeAt;//present battle at(millisecond)
    public DateTime lastUpdateAtTime;
    public int battleSpeedRate;

    private int lastRecoverAt;
    private int lastNewOutpostAt;

    public void UpdateBattleByTime()
    {
        List<System.Object> updateObjs = new List<object>();

        DateTime timeNow = DateTime.Now;
        battleTimeAt += (TimeUtils.GetTotalMilliseconds(timeNow, battleStartTime) - TimeUtils.GetTotalMilliseconds(lastUpdateAtTime, battleStartTime)) * battleSpeedRate;
        lastUpdateAtTime = timeNow;

        //Update Factions score
        foreach (Faction faction in factions)
        {
            faction.score += faction.towerScore * (battleTimeAt - faction.lastUpdateBattleTime)/MySettings.towerScoreToRealScorePerTime;
            faction.lastUpdateBattleTime = battleTimeAt;
        }

        //Update WarTowerState
        foreach (WarTower warTower in warTowers.Values)
        {
            if (warTower.towerState == MDs.State.Getting)
            {
                warTower.stateChangeLeftTime -= battleTimeAt - warTower.lastUpdateTime;
                //Debug.Log("in MDs.State.Getting");
                warTower.lastUpdateTime = battleTimeAt;
                if (warTower.stateChangeLeftTime <= 0)
                {
                    WarTowerFinishGetting(warTower);
                    break;//update others next updating
                }
            }
            if (warTower.towerState == MDs.State.Losing)
            {
                warTower.stateChangeLeftTime -= battleTimeAt - warTower.lastUpdateTime;
                //Debug.Log("in MDs.State.Losing");
                warTower.lastUpdateTime = battleTimeAt;
                if (warTower.stateChangeLeftTime <= 0)
                {
                    ServerManager.Instance.SendToptipMsgToFaction("失去战塔", warTower.factionOrder);
                    ServerManager.Instance.SendMapTipMsgToFaction("失去战塔", warTower.position, warTower.factionOrder);
                    WarTowerFinishLosing(warTower);
                    break;//update others next updating
                }
            }
        }

        //Update OutPostState
        foreach (Outpost outpost in outposts.Values)
        {
            if (outpost.isBuilding)
            {
                //Debug.Log("Update OutPostState");
                outpost.leftBuildTime -= battleTimeAt - outpost.lastUpdateTime;
                outpost.lastUpdateTime = battleTimeAt;
                if (outpost.leftBuildTime <= 0)
                {
                    Debug.Log("OutpostFinishBuilding");
                    OutpostFinishBuilding(outpost);
                    ServerManager.Instance.SendToptipMsgToFaction("据点建造成功", outpost.factionOrder);
                    ServerManager.Instance.SendMapTipMsgToFaction("据点建造成功", outpost.position, outpost.factionOrder);
                    break;//update others next updating
                }
            }
        }

        //Update MovingUnitState
        foreach (MovingUnit movingUnit in movingUnits.Values)
        {
            movingUnit.currentLength += (battleTimeAt - movingUnit.lastUpdateTime) * movingUnit.speed;
            movingUnit.lastUpdateTime = battleTimeAt;
            if (movingUnit.currentLength >= movingUnit.totalLength)
            {
                MovingUnitReach(movingUnit);
                break;//update others next updating
            }
        }

        //Update LightTower
        foreach (LightTower lightTower in lightTowers.Values)
        {
            for (int i = 1; i <= 4;i++)
            {
                if(lightTower.factionState[i]==MDs.LightState.Light)
                {
                    lightTower.leftLightTime[i] -= battleTimeAt - lightTower.lastUpdateTime;
                    if(lightTower.leftLightTime[i]<=0)
                    {
                        LightTowerFinishLight(lightTower, i);
                    }
                }
                if(lightTower.coolDownTime[i]>0)
                {
                    lightTower.coolDownTime[i]-=battleTimeAt - lightTower.lastUpdateTime;
                }
            }
            lightTower.lastUpdateTime = battleTimeAt;
        }

        //Recover a random outpost
        while(battleTimeAt-lastRecoverAt>=MySettings.recoverAnOutpostPer)
        {
            Outpost outpost=(new List<Outpost>(outposts.Values))[(int)UnityEngine.Random.Range(0, outposts.Count - 1)];
            int recoverNum = (int)(outpost.power * MySettings.recoverRateOfPower);
            if(recoverNum>0)
            {
                outpost.unitNum += recoverNum;
                ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
                ServerManager.Instance.SendMapTipMsgToFaction("士兵增加:"+recoverNum,outpost.position,outpost.factionOrder);
            }

            lastRecoverAt += MySettings.recoverAnOutpostPer;
        }

        //New a outpost
        while(battleTimeAt-lastNewOutpostAt>=MySettings.newOutpostPer)
        {
            lastNewOutpostAt += MySettings.newOutpostPer;
            int factionOrder = (int)UnityEngine.Random.Range(1, 5f);
            List<object> positionObjs;
            if(factionOrder<1||factionOrder>4)
            {
                break;
            }
            Faction faction = factions[factionOrder];
            Vector2Int randPosition=new Vector2Int(0,0);
            int tryTimes = 0;
            do
            {
                if(tryTimes>100)
                {
                    break;
                }
                tryTimes++;
                randPosition = new Vector2Int
                {
                    x = faction.BrithTower.position.x + (int)UnityEngine.Random.Range(-7.5f, 8.5f),
                    y = faction.BrithTower.position.y + (int)UnityEngine.Random.Range(-7.5f, 8.5f)
                };
                positionObjs = GetUnmoveObjAtLocation(randPosition);
            } while (positionObjs.Count > 0 || randPosition.x < 0 || randPosition.y < 0 || randPosition.x >= basicSetting.mapSizeX || randPosition.y >= basicSetting.mapSizeY);
            if(tryTimes>100)
            {
                break;
            }
            Outpost outpost = new Outpost()
            {
                isBuilding = false,
                leftBuildTime = 0,
                blood = MySettings.outpostInitialBlood,
                unitNum = (int)UnityEngine.Random.Range(10, 500),
                outTroopNum = 0,
                factionOrder = faction.order,
                position = randPosition
            };
            outpost.power = outpost.unitNum;
            outposts.Add(outpost.GetID(), outpost);
            ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
            ServerManager.Instance.SendMapTipMsgToFaction("新据点加入", outpost.position, outpost.factionOrder);
            ServerManager.Instance.SendToptipMsgToFaction("新据点加入", outpost.factionOrder);
        }
    }

    private void LightTowerFinishLight(LightTower lightTower,int factionOrder)
    {
        lightTower.factionState[factionOrder] = MDs.LightState.Dark;
        ServerManager.Instance.SendMessageOfLightTowerUpdate(lightTower);
        ServerManager.Instance.SendToptipMsgToFaction("灯塔熄灭", factionOrder);
        ServerManager.Instance.SendMapTipMsgToFaction("灯塔熄灭", lightTower.position, factionOrder);
    }

    public void TryCallBackUnit(int factionOrder, UnitMsg msgContent)
    {
        if (units.ContainsKey(msgContent.id))
        {
            Unit unit = units[msgContent.id];
            if (unit.factionOrder != factionOrder)
            {
                Debug.Log("TryCallBackUnit:wrong faction order:unit.factionOrder=" + unit.factionOrder + "  ReqOrder=" + factionOrder);
                return;
            }
            CallBackUnit(unit);

        }
        else
        {
            Debug.Log("TryCallBackUnit:No Unit id");
        }
    }

    private void CallBackUnit(Unit unit)
    {
        if(!outposts.ContainsKey(unit.belongToOutpostID))
        {
            Debug.Log("Call back a unit with no outpost");
            units.Remove(unit.GetID());
            ServerManager.Instance.SendMessageOfUnitDestroy(unit.GetID());
            ServerManager.Instance.SendToptipMsgToFaction("ERROR:Call back a unit with no outpost", unit.factionOrder);
            return;
        }
        MovingUnit movingUnit = new MovingUnit
        {
            UnitAmount = unit.UnitAmount,
            startPosition = unit.position,
            endPosition = outposts[unit.belongToOutpostID].position,
            currentLength = 0f,
            speed = MySettings.unitMovingSpeed,
            lastUpdateTime = battleTimeAt,
            factionOrder = unit.factionOrder,
            belongToOutpostID=unit.belongToOutpostID
        };
        movingUnit.totalLength = Mathf.Sqrt((movingUnit.startPosition.x - movingUnit.endPosition.x) * (movingUnit.startPosition.x - movingUnit.endPosition.x) + (movingUnit.startPosition.y - movingUnit.endPosition.y) * (movingUnit.startPosition.y - movingUnit.endPosition.y));
        movingUnits.Add(movingUnit.GetID(), movingUnit);
        units.Remove(unit.GetID());
        ServerManager.Instance.SendMessageOfMovingUnitUpdate(movingUnit);
        ServerManager.Instance.SendMessageOfUnitDestroy(unit.GetID());
        ServerManager.Instance.SendToptipMsgToFaction("部队开始返回", unit.factionOrder);
        ServerManager.Instance.SendMapTipMsgToFaction("部队开始返回", unit.position, unit.factionOrder);

        //Deal with left from his tower
        DealWithLeftFromHisTower(unit);
        DealWithLeftFromHisBuildingOutpost(unit);
    }

    private void DealWithLeftFromHisBuildingOutpost(Unit unit)
    {
        List<object> localObjects = GetUnmoveObjAtLocation(unit.position);
        Outpost hisOutpost = null;
        foreach (object o in localObjects)
        {
            if (o.GetType().ToString() == "Outpost")
            {
                Outpost outpost = (Outpost)o;
                if (outpost.factionOrder == unit.factionOrder&&outpost.isBuilding&&outpost.oldOutpostID==unit.belongToOutpostID)
                {
                    hisOutpost = outpost;
                }
            }
        }
        if(hisOutpost!=null)
        {
            outposts.Remove(hisOutpost.GetID());
            ServerManager.Instance.SendMessageOfOutpostDestroy(hisOutpost.GetID());
            ServerManager.Instance.SendToptipMsgToFaction("因部队离开建造据点失败",hisOutpost.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("因部队离开建造据点失败", hisOutpost.position, hisOutpost.factionOrder);

        }
    }

    private void DealWithLeftFromHisTower(Unit unit)
    {
        List<object> localObjects = GetUnmoveObjAtLocation(unit.position);
        bool stillPartnerThere = false;
        WarTower warTowerThere = null;
        foreach (object o in localObjects)
        {
            if (o.GetType().ToString() == "WarTower")
            {
                WarTower warTower = (WarTower)o;
                if (warTower.factionOrder == unit.factionOrder||warTower.towerState==MDs.State.Getting)
                {
                    warTowerThere = warTower;
                }
            }
            if (o.GetType().ToString() == "Unit")
            {
                Unit pu = (Unit)o;
                if (pu.factionOrder == unit.factionOrder)
                {
                    stillPartnerThere = true;
                }
            }
        }
        if (warTowerThere != null && !stillPartnerThere)
        {
            switch (warTowerThere.towerState)
            {
                case MDs.State.Getting:
                    warTowerThere.towerState = MDs.State.Staying;
                    ServerManager.Instance.SendMessageOfWarTowerUpdate(warTowerThere);
                    break;
                case MDs.State.Losing:
                    break;
                case MDs.State.Staying:
                    Debug.Log("Start to Losing Tower");
                    warTowerThere.towerState = MDs.State.Losing;
                    warTowerThere.stateChangeLeftTime = MySettings.warTowerLosingTIme;
                    warTowerThere.lastUpdateTime = battleTimeAt;
                    ServerManager.Instance.SendMessageOfWarTowerUpdate(warTowerThere);
                    ServerManager.Instance.SendToptipMsgToFaction("正在失去战塔控制权",warTowerThere.factionOrder);
                    ServerManager.Instance.SendMapTipMsgToFaction("正在失去战塔控制权", warTowerThere.position, warTowerThere.factionOrder);
                    break;
                default:
                    break;
            }
        }
    }

    public void TryCallBackMovingUnit(int factionOrder, MovingUnitMsg msgContent)
    {
        if (movingUnits.ContainsKey(msgContent.id))
        {
            MovingUnit movingUnit = movingUnits[msgContent.id];
            if (movingUnit.factionOrder != factionOrder)
            {
                Debug.Log("TryCallBackMovingUnit:wrong faction order:unit.factionOrder=" + movingUnit.factionOrder + "  ReqOrder=" + factionOrder);
                return;
            }
            CallBackMovingUnit(movingUnit);
        }
        else
        {
            Debug.Log("TryCallBackUnit:No Unit id");
        }
    }

    private void CallBackMovingUnit(MovingUnit movingUnit)
    {
        MovingUnit backMovingUnit = new MovingUnit
        {
            UnitAmount = movingUnit.UnitAmount,
            totalLength = movingUnit.currentLength,
            endPosition = movingUnit.startPosition,
            currentLength = 0f,
            speed = MySettings.unitMovingSpeed,
            lastUpdateTime = battleTimeAt,
            factionOrder = movingUnit.factionOrder,
            belongToOutpostID=movingUnit.belongToOutpostID
        };
        Vector3 cl = Vector3.Lerp(new Vector3(movingUnit.startPosition.x, movingUnit.startPosition.y, 0f), new Vector3(movingUnit.endPosition.x, movingUnit.endPosition.y, 0f), movingUnit.currentLength / movingUnit.totalLength);
        backMovingUnit.startPosition = new Vector2Int((int)cl.x, (int)cl.y);
        movingUnits.Remove(movingUnit.GetID());
        movingUnits.Add(backMovingUnit.GetID(), backMovingUnit);
        ServerManager.Instance.SendMessageOfMovingUnitUpdate(backMovingUnit);
        ServerManager.Instance.SendMessageOfMovingUnitDestroy(movingUnit.GetID());
        ServerManager.Instance.SendToptipMsgToFaction("部队开始返回", movingUnit.factionOrder);
        ServerManager.Instance.SendMapTipMsgToFaction("部队开始返回", movingUnit.startPosition, movingUnit.factionOrder);
    }




    public void TryStartMovingUnit(int factionOrder, MovingUnitMsg msgContent)
    {
        if (outposts.ContainsKey(msgContent.belongToOutpostID))
        {
            Outpost outpost = outposts[msgContent.belongToOutpostID];
            if (outpost.factionOrder != factionOrder)
            {
                Debug.Log("TryStartMovingUnit:wrong faction order:unit.factionOrder=" + outpost.factionOrder + "  ReqOrder=" + factionOrder);
                return;
            }
            if (outpost.unitNum < msgContent.UnitAmount)
            {
                Debug.Log("TryStartMovingUnit:outpost.unitNum<msgContent.UnitAmount");
                return;
            }
            if (outpost.outTroopNum > MySettings.outpostTroopLimit)
            {
                Debug.Log("TryStartMovingUnit:outpost.outTroopNum >="+MySettings.outpostTroopLimit);
                ServerManager.Instance.SendToptipMsgToFaction("据点行军数量达到上限",factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("据点行军数量达到上限", new Vector2Int(msgContent.endPositionX, msgContent.endPositionY), factionOrder);
                return;
            }
            if (msgContent.UnitAmount <=0)
            {
                Debug.Log("msgContent.UnitAmount <=0");
                ServerManager.Instance.SendToptipMsgToFaction("行军数量不可为0", factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("行军数量不可为0", new Vector2Int(msgContent.endPositionX, msgContent.endPositionY), factionOrder);
                return;
            }
            if(safeZone[msgContent.endPositionX,msgContent.endPositionY]!=0&&safeZone[msgContent.endPositionX, msgContent.endPositionY] != factionOrder)
            {
                Debug.Log("TryStartMovingUnit:safeZone");
                ServerManager.Instance.SendToptipMsgToFaction("无法向敌方安全区行军", factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("无法向敌方安全区行军",new Vector2Int(msgContent.endPositionX,msgContent.endPositionY), factionOrder);
                return;
            }

            foreach(object o in GetUnmoveObjAtLocation(new Vector2Int(msgContent.endPositionX,msgContent.endPositionY)))
            {
                if(o.GetType().ToString()=="WarTower")
                {
                    WarTower warTower = (WarTower)o;
                    if(warTower.factionOrder==factionOrder)
                    {
                        break;//ok to start unit
                    }

                    bool isLinkedTower = false;

                    foreach(string linkedTowerID in warTower.linkedTowerIDs)
                    {
                        if(warTowers[linkedTowerID].factionOrder==factionOrder)
                        {
                            isLinkedTower = true;
                            break;
                        }
                    }
                    if(!isLinkedTower)
                    {
                        Debug.Log("TryStartMovingUnit:is not LinkedTower");
                        ServerManager.Instance.SendToptipMsgToFaction("仅可向能连接的战塔行军",factionOrder);
                        ServerManager.Instance.SendMapTipMsgToFaction("仅可向能连接的战塔行军", warTower.position, factionOrder);
                        return;
                    }
                    break;
                }
            }
            StartMovingUnit(outpost, msgContent);
        }
        else
        {
            Debug.Log("TryCallBackUnit:No outposts.ContainsKey(msgContent.belongToOutpostID)");
        }
    }

    private void StartMovingUnit(Outpost outpost, MovingUnitMsg msgContent)
    {
        //Debug.Log("StartMovingUnit");
        MovingUnit movingUnit = new MovingUnit
        {
            belongToOutpostID = outpost.GetID(),
            UnitAmount = msgContent.UnitAmount,
            totalLength = Mathf.Sqrt((outpost.position.x - msgContent.endPositionX) * (outpost.position.x - msgContent.endPositionX) + (outpost.position.y - msgContent.endPositionY) * (outpost.position.y - msgContent.endPositionY)),
            startPosition = outpost.position,
            endPosition = new Vector2Int(msgContent.endPositionX, msgContent.endPositionY),
            currentLength = 0f,
            speed = MySettings.unitMovingSpeed,
            lastUpdateTime = battleTimeAt,
            factionOrder = outpost.factionOrder
        };
        outpost.outTroopNum++;
        outpost.unitNum -= msgContent.UnitAmount;
        movingUnits.Add(movingUnit.GetID(), movingUnit);
        ServerManager.Instance.SendMessageOfMovingUnitUpdate(movingUnit);
        ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
        ServerManager.Instance.SendToptipMsgToFaction("开始行军", movingUnit.factionOrder);
        ServerManager.Instance.SendMapTipMsgToFaction("开始行军", movingUnit.startPosition, movingUnit.factionOrder);
    }

    //public string GetNewestOutpostID(string outpostID)
    //{
    //    //if (outpostIDChangeTable.ContainsKey(outpostID))
    //    //{
    //    //    string newID = GetNewestOutpostID(outpostIDChangeTable[outpostID]);
    //    //    outpostIDChangeTable.Remove(outpostID);
    //    //    outpostIDChangeTable.Add(outpostID, newID);
    //    //    return GetNewestOutpostID(outpostIDChangeTable[outpostID]);
    //    //}
    //    //else
    //    //{
    //    //    return outpostID;
    //    //}
    //}

    //private Dictionary<string, string> outpostIDChangeTable = new Dictionary<string, string>();
    public void OutpostFinishBuilding(Outpost outpost)
    {
        Debug.Log("OutpostFinishBuilding");
        outpost.isBuilding = false;
        outpost.blood = MySettings.outpostInitialBlood;
        int unitCount = 0;

        //outpostIDChangeTable.Add(outpost.oldOutpostID, outpost.GetID());

        if (outposts.ContainsKey(outpost.oldOutpostID))
        {
            Outpost oldOutpost = outposts[outpost.oldOutpostID];

            if (oldOutpost.unitNum > 0)
            {
                outpost.unitNum += oldOutpost.unitNum;

                //MovingUnit movingUnit = new MovingUnit()
                //{
                //    belongToOutpostID = outpost.GetID(),
                //    UnitAmount = oldOutpost.unitNum,
                //    startPosition = oldOutpost.position,
                //    endPosition = outpost.position,
                //    totalLength = Mathf.Sqrt((outpost.position.x - oldOutpost.position.x) * (outpost.position.x - oldOutpost.position.x) + (outpost.position.y - oldOutpost.position.y) * (outpost.position.y - oldOutpost.position.y)),
                //    currentLength = 0f,
                //    speed = MySettings.unitMovingSpeed,
                //    lastUpdateTime = battleTimeAt,
                //    factionOrder = outpost.factionOrder
                //};
                //movingUnits.Add(movingUnit.GetID(), movingUnit);
                //ServerManager.Instance.SendMessageOfMovingUnitUpdate(movingUnit);
                //unitCount++;
            }

            foreach(MovingUnit movingUnit in movingUnits.Values)
            {
                if(movingUnit.belongToOutpostID==oldOutpost.GetID())
                {
                    movingUnit.belongToOutpostID = outpost.GetID();
                    unitCount++;
                }
            }
            bool again = false;
            do
            {
                again = false;
                foreach (Unit unit in units.Values)
                {
                    if (unit.belongToOutpostID == oldOutpost.GetID())
                    {
                        //Debug.Log("Find and change unit id");
                        //unit.belongToOutpostID=outpost.GetID();
                        if (unit.position == outpost.position)
                        {
                            outpost.unitNum += unit.UnitAmount;
                            outpost.outTroopNum--;
                            //unit.belongToOutpostID = outpost.GetID();
                            units.Remove(unit.GetID());
                            ServerManager.Instance.SendMessageOfUnitDestroy(unit.GetID());
                            ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
                            //break;//get only one
                            again = true;
                            break;
                        }
                        else
                        {
                            unit.belongToOutpostID = outpost.GetID();
                            unitCount++;
                        }
                    }
                }
            } while (again);
            outpost.outTroopNum = unitCount;
            outposts.Remove(outpost.oldOutpostID);
        }
        ServerManager.Instance.SendMessageOfOutpostDestroy(outpost.oldOutpostID);
        ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
    }

    public void MovingUnitReach(MovingUnit movingUnit)
    {
        //Debug.Log("in MovingUnitReach1");
        List<object> objs = GetUnmoveObjAtLocation(movingUnit.endPosition);
        WarTower warTower = null;
        LightTower lightTower = null;
        Outpost outpost = null;
        bool fought = false;
        bool friendUnitThere = false;
        //Debug.Log("objs.Count" + objs.Count);
        for (int i = 0; i < objs.Count; i++)
        {
            if (objs[i].GetType().ToString() == "Unit")
            {
                //Debug.Log("in MovingUnitReach2");
                Unit unit = (Unit)objs[i];
                if (unit.factionOrder != movingUnit.factionOrder)
                {
                    fought = true;

                    if (Unitfight(unit, movingUnit) == 0)
                    {
                        break;
                    }
                }
                else
                {
                    friendUnitThere = true;
                }
            }
            else if (objs[i].GetType().ToString() == "WarTower")
            {
                warTower = (WarTower)objs[i];
            }
            else if (objs[i].GetType().ToString() == "LightTower")
            {
                lightTower = (LightTower)objs[i];
            }
            else if (objs[i].GetType().ToString() == "Outpost")
            {
                //Debug.Log("in MovingUnitReach3");
                outpost = (Outpost)objs[i];
                if(outpost.factionOrder!=movingUnit.factionOrder)
                {
                    //Debug.Log("in MovingUnitReach4");
                    fought = true;
                    if (Unitfight(outpost, movingUnit) == 0)
                    {
                        break;
                    }
                }
            }
        }
        //Debug.Log("in MovingUnitReach5");
        if (movingUnit.UnitAmount > 0)
        {
            //Debug.Log("in MovingUnitReach6");
            if (warTower != null)
            {
                //Debug.Log("in MovingUnitReach7");
                UnitSettle(movingUnit);
                bool linked = false;
                foreach (string id in warTower.linkedTowerIDs)
                {
                    if (warTowers[id].factionOrder == movingUnit.factionOrder)
                    {
                        linked = true;
                    }
                }
                switch (warTower.towerState)
                {
                    case MDs.State.Staying:
                        if(!linked)
                        {
                            break;
                        }
                        warTower.towerState = MDs.State.Getting;
                        warTower.stateChangeLeftTime=MySettings.TowerGettingTime(warTower.towerLevel,factions[movingUnit.factionOrder].haveWarTowerNum);
                        warTower.lastUpdateTime = battleTimeAt;
                        //Debug.Log("warTower.towerState = MDs.State.Getting;");
                        ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                        ServerManager.Instance.SendToptipMsgToFaction("开始占领战塔", movingUnit.factionOrder);
                        ServerManager.Instance.SendMapTipMsgToFaction("开始占领战塔", warTower.position, movingUnit.factionOrder);
                        break;
                    case MDs.State.Losing:
                        if (warTower.factionOrder == movingUnit.factionOrder)
                        {
                            warTower.towerState = MDs.State.Staying;
                            //Debug.Log("warTower.towerState = MDs.State.Getting;");
                            ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                        }
                        else
                        {
                            warTower.towerState = MDs.State.Losing;
                            warTower.stateChangeLeftTime = MySettings.warTowerLosingTIme;
                            warTower.lastUpdateTime = battleTimeAt;
                            //Debug.Log("warTower.towerState = MDs.State.Losing;");
                            ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                        }
                        break;
                    case MDs.State.Getting:
                        if (warTower.factionOrder != movingUnit.factionOrder)
                        {
                            if (!linked)
                            {
                                warTower.towerState = MDs.State.Staying;
                                break;
                            }
                            if (friendUnitThere)
                            {
                                break;
                            }
                            warTower.towerState = MDs.State.Getting;
                            warTower.stateChangeLeftTime = MySettings.TowerGettingTime(warTower.towerLevel,factions[movingUnit.factionOrder].haveWarTowerNum);
                            warTower.lastUpdateTime = battleTimeAt;
                            //Debug.Log("warTower.towerState = MDs.State.Losing;");
                            ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if(lightTower!=null)
            {
                if(lightTower.coolDownTime[movingUnit.factionOrder]<=0)
                {
                    lightTower.factionState[movingUnit.factionOrder] = MDs.LightState.Light;
                    lightTower.coolDownTime[movingUnit.factionOrder] = MySettings.lightTowerCoolDownTime;
                    lightTower.leftLightTime[movingUnit.factionOrder] = MySettings.lightTowerLightTime;
                    lightTower.lastUpdateTime = battleTimeAt;
                    ServerManager.Instance.SendMessageOfLightTowerUpdate(lightTower);
                    ServerManager.Instance.SendToptipMsgToFaction("成功点亮灯塔",movingUnit.factionOrder);
                    ServerManager.Instance.SendMapTipMsgToFaction("成功点亮灯塔", lightTower.position, movingUnit.factionOrder);
                    CallBackMovingUnit(movingUnit);
                }
                else//not cool down
                {
                    ServerManager.Instance.SendMapTipMsgToFaction("灯塔未可点亮，需冷却", lightTower.position, movingUnit.factionOrder);
                    ServerManager.Instance.SendToptipMsgToFaction("灯塔未可点亮，需冷却",movingUnit.factionOrder);
                    CallBackMovingUnit(movingUnit);
                }

            }
            else if (outpost != null)
            {
          
                if (outpost.factionOrder != movingUnit.factionOrder)
                {
                    CallBackMovingUnit(movingUnit);
                    return;
                }
                else
                {
                    if(outpost.GetID()==movingUnit.belongToOutpostID)
                    {
                        outpost.unitNum += movingUnit.UnitAmount;
                        outpost.outTroopNum--;
                        movingUnits.Remove(movingUnit.GetID());
                        ServerManager.Instance.SendMessageOfMovingUnitDestroy(movingUnit.GetID());
                        ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
                    }
                    else{
                        //Debug.Log("in MovingUnitReach84");
                        if(!fought)
                        {
                            //Debug.Log("in MovingUnitReach85");
                            this.UnitSettle(movingUnit);
                            return;
                        }

                    }
                }
            }
            else if (fought)
            {
                //Debug.Log("in MovingUnitReach9");
                //Debug.Log("Back to Home");
                CallBackMovingUnit(movingUnit);
                return;
            }
            else
            {
                //Debug.Log("in MovingUnitReach10");
                //Debug.Log("build Home");
                UnitSettle(movingUnit);
                StartToBuildOutpostByMovingUnit(movingUnit);
                return;
            }
        }
        //Debug.Log("in MovingUnitReac11");
    }

    public void DestroyAllUnitOfAOutPost(Outpost outpost)
    {
        List<Unit> destroyUnits = new List<Unit>();
        List<MovingUnit> destroyMovingUnits = new List<MovingUnit>();
        foreach(Unit unit in units.Values)
        {
            if(unit.belongToOutpostID==outpost.GetID())
            {
                destroyUnits.Add(unit);
            }
        }
        foreach (MovingUnit movingUnit in movingUnits.Values)
        {
            if (movingUnit.belongToOutpostID == outpost.GetID())
            {
                destroyMovingUnits.Add(movingUnit);
            }
        }

        foreach (Unit unit in destroyUnits)
        {
            units.Remove(unit.GetID());
            ServerManager.Instance.SendMessageOfUnitDestroy(unit.GetID());
        }
        foreach (MovingUnit movingUnit in destroyMovingUnits)
        {
            movingUnits.Remove(movingUnit.GetID());
            ServerManager.Instance.SendMessageOfUnitDestroy(movingUnit.GetID());
        }
    }

    public void StartToBuildOutpostByMovingUnit(MovingUnit movingUnit)
    {
        //Debug.Log("StartToBuildOutpostByMovingUnit");
        Outpost oldOutpost = null;
        if (outposts.ContainsKey(movingUnit.belongToOutpostID))
        {
            oldOutpost = outposts[movingUnit.belongToOutpostID];
        }
        Outpost outpost = new Outpost
        {
            isBuilding = true,
            oldOutpostID = movingUnit.belongToOutpostID,
            leftBuildTime = MySettings.outpostBuildTime,
            lastUpdateTime = battleTimeAt,
            blood = 1,
            unitNum = 0,
            outTroopNum = oldOutpost == null?1 : oldOutpost.outTroopNum,
            factionOrder = movingUnit.factionOrder,
            position = movingUnit.endPosition
        };
        outposts.Add(outpost.GetID(),outpost);
        ServerManager.Instance.SendMessageOfOutpostUpdate(outpost);
        ServerManager.Instance.SendToptipMsgToFaction("开始建造据点,建造完成将摧毁原据点",movingUnit.factionOrder);
        ServerManager.Instance.SendMapTipMsgToFaction("开始建造据点,建造完成将摧毁原据点", outpost.position, movingUnit.factionOrder);
    }

    public void UnitSettle(MovingUnit movingUnit)
    {
        Unit unit = new Unit
        {
            position = movingUnit.endPosition,
            belongToOutpostID = movingUnit.belongToOutpostID,
            UnitAmount = movingUnit.UnitAmount,
            factionOrder = movingUnit.factionOrder
        };
        ServerManager.Instance.SendMessageOfUnitUpdate(unit);
        ServerManager.Instance.SendMessageOfMovingUnitDestroy(movingUnit.GetID());
        units.Add(unit.GetID(), unit);
        movingUnits.Remove(movingUnit.GetID());
        //ServerManager.Instance.SendToptipMsgToFaction("士兵到达目的地", movingUnit.factionOrder);
    }

    public int Unitfight(Unit defendUnit, MovingUnit attackUnit)
    {
        int killedAmount = Mathf.Min(attackUnit.UnitAmount, defendUnit.UnitAmount);
        bool attckWin = false;
        if(attackUnit.factionOrder==0)
        {
            factions[defendUnit.factionOrder].score += killedAmount * MySettings.npcScorePerUnit;//score per kill
        }
        else{
            factions[defendUnit.factionOrder].score += killedAmount * MySettings.pvpScorePerUnit;//score per kill
        }
        if(defendUnit.factionOrder==0)
        {
            float p = killedAmount * MySettings.npcScorePerUnit;
            factions[attackUnit.factionOrder].score += p;
            //ServerManager.Instance.SendToptipMsgToFaction("狩猎获得" + p + "积分",attackUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("狩猎获得" + p + "积分", defendUnit.position, attackUnit.factionOrder);
        }
        else{
            factions[attackUnit.factionOrder].score += killedAmount * MySettings.pvpScorePerUnit;
        }

        ServerManager.Instance.SendMessageOfFactionUpdate(factions[defendUnit.factionOrder]);
        ServerManager.Instance.SendMessageOfFactionUpdate(factions[attackUnit.factionOrder]);
        ;//score per kill
        defendUnit.UnitAmount -= killedAmount;
        attackUnit.UnitAmount -= killedAmount;
        if (attackUnit.UnitAmount <= 0)
        {
            if (outposts.ContainsKey(attackUnit.belongToOutpostID))
            {
                outposts[attackUnit.belongToOutpostID].outTroopNum--;
                ServerManager.Instance.SendMessageOfOutpostUpdate(outposts[attackUnit.belongToOutpostID]);
            }
            movingUnits.Remove(attackUnit.GetID());
            ServerManager.Instance.SendMessageOfMovingUnitDestroy(attackUnit.GetID());
            ServerManager.Instance.SendToptipMsgToFaction("军队进攻覆没", defendUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("军队进攻覆没", defendUnit.position, attackUnit.factionOrder);
            attckWin = false;
            //return 0;//attack lose
        }
        else{
            //ServerManager.Instance.SendMapTipMsgToFaction("进攻胜利", defendUnit.position, attackUnit.factionOrder);
        }
        if (defendUnit.UnitAmount <= 0)
        {
            attckWin = true;
            if(defendUnit.factionOrder != 0)
            {
                ServerManager.Instance.SendToptipMsgToFaction("成功击杀敌方军队", attackUnit.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("成功击杀敌方军队", defendUnit.position, attackUnit.factionOrder);
            }
            ServerManager.Instance.SendToptipMsgToFaction("军队被击杀", defendUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("军队被击杀", defendUnit.position, defendUnit.factionOrder);
            if (outposts.ContainsKey(defendUnit.belongToOutpostID))
            {
                outposts[defendUnit.belongToOutpostID].outTroopNum--;
                ServerManager.Instance.SendMessageOfOutpostUpdate(outposts[defendUnit.belongToOutpostID]);
            }
            units.Remove(defendUnit.GetID());
            //Debug.Log("DealWithLeftFromHisTower");
            DealWithLeftFromHisTower(defendUnit);
            ServerManager.Instance.SendMessageOfUnitDestroy(defendUnit.GetID());
            //return 1;//attack win
        }
        else{
            ServerManager.Instance.SendToptipMsgToFaction("成功防守敌方进攻", defendUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("成功防守敌方进攻", defendUnit.position, defendUnit.factionOrder);
            ServerManager.Instance.SendMessageOfUnitUpdate(defendUnit);
        }
        if(attckWin)
        {
            return 1;
        }
        else{
            return 0;
        }

    }

    public int Unitfight(Outpost defendOutpost, MovingUnit attackUnit)
    {
        bool attckWin = false;
        int killedAmount = Mathf.Min(attackUnit.UnitAmount, defendOutpost.unitNum);
        Debug.Log("in defendOutpost");
        factions[defendOutpost.factionOrder].score += killedAmount * 1f;//score per kill
        factions[attackUnit.factionOrder].score += killedAmount * 1f;
    
        ServerManager.Instance.SendMessageOfFactionUpdate(factions[defendOutpost.factionOrder]);
        ServerManager.Instance.SendMessageOfFactionUpdate(factions[attackUnit.factionOrder]);

        defendOutpost.unitNum -= killedAmount;
        attackUnit.UnitAmount -= killedAmount;
        Debug.Log("in defendOutpost2");
        if (attackUnit.UnitAmount <= 0)
        {
            if (outposts.ContainsKey(attackUnit.belongToOutpostID))
            {
                outposts[attackUnit.belongToOutpostID].outTroopNum--;
                ServerManager.Instance.SendMessageOfOutpostUpdate(outposts[attackUnit.belongToOutpostID]);
            }
            movingUnits.Remove(attackUnit.GetID());
            ServerManager.Instance.SendMessageOfMovingUnitDestroy(attackUnit.GetID());
            ServerManager.Instance.SendToptipMsgToFaction("军队进攻覆没", attackUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("军队进攻覆没", defendOutpost.position, attackUnit.factionOrder);
            ServerManager.Instance.SendToptipMsgToFaction("据点成功防御进攻", defendOutpost.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("据点成功防御进攻", defendOutpost.position, defendOutpost.factionOrder);
            Debug.Log("in defendOutpost3");
            attckWin = false;
            //return 0;//attack lose
        }
        if (defendOutpost.unitNum <= 0)
        {
            Debug.Log("in defendOutpost4");
            defendOutpost.blood -= MySettings.outpostLoseBloodPerAttack;
            if(defendOutpost.blood>0)
            {
                ServerManager.Instance.SendToptipMsgToFaction("成功伤害敌方据点", attackUnit.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("成功伤害敌方据点", defendOutpost.position, attackUnit.factionOrder);

                ServerManager.Instance.SendToptipMsgToFaction("据点受到伤害", defendOutpost.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("据点受到伤害", defendOutpost.position, defendOutpost.factionOrder);
                ServerManager.Instance.SendMessageOfOutpostUpdate(defendOutpost);
                Debug.Log("in defendOutpost5");
            }else{
                ServerManager.Instance.SendToptipMsgToFaction("成功摧毁敌方据点", attackUnit.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("成功摧毁敌方据点", defendOutpost.position, attackUnit.factionOrder);

                ServerManager.Instance.SendToptipMsgToFaction("据点被破坏", defendOutpost.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("据点被破坏", defendOutpost.position, defendOutpost.factionOrder);
                outposts.Remove(defendOutpost.GetID());
                ServerManager.Instance.SendMessageOfOutpostDestroy(defendOutpost.GetID());

                //clear the unit and movingunit of the outpost
                List<Unit> clearUnits = new List<Unit>();
                foreach(Unit unit in units.Values)
                {
                    if(unit.belongToOutpostID==defendOutpost.GetID())
                    {
                        clearUnits.Add(unit);
                    }
                }
                while(clearUnits.Count>0)
                {
                    Unit unit = clearUnits[0];
                    clearUnits.RemoveAt(0);
                    units.Remove(unit.GetID());
                    DealWithLeftFromHisTower(unit);
                    ServerManager.Instance.SendMessageOfUnitDestroy(unit.GetID());
                    ServerManager.Instance.SendToptipMsgToFaction("军队因据点被破坏而死亡", unit.factionOrder);
                    ServerManager.Instance.SendMapTipMsgToFaction("军队因据点被破坏而死亡", unit.position, unit.factionOrder);
                }

                List<MovingUnit> clearMovingUnits = new List<MovingUnit>();
                foreach (MovingUnit movingUnit in movingUnits.Values)
                {
                    if (movingUnit.belongToOutpostID == defendOutpost.GetID())
                    {
                        clearMovingUnits.Add(movingUnit);
                    }
                }
                while (clearMovingUnits.Count > 0)
                {
                    MovingUnit movingUnit = clearMovingUnits[0];
                    clearMovingUnits.RemoveAt(0);
                    movingUnits.Remove(movingUnit.GetID());
                    ServerManager.Instance.SendMessageOfMovingUnitDestroy(movingUnit.GetID());
                    ServerManager.Instance.SendToptipMsgToFaction("军队因据点被破坏而死亡", movingUnit.factionOrder);
                    //ServerManager.Instance.SendMapTipMsgToFaction("军队因据点被破坏而死亡", movingUnit., movingUnit.factionOrder);
                }

            }
            attckWin = true;
            return 1;//attack outpost win
        }else{
            ServerManager.Instance.SendMessageOfOutpostUpdate(defendOutpost);
        }
        if(attckWin)
        {
            return 1;
        }
        else{
            return 0;
        }
        //return 0;//attack lose
    }


    private List<Vector2Int> AroundPosition(Vector2Int center)
    {
        List<Vector2Int> re = new List<Vector2Int>
        {
            center+new Vector2Int(-1, 1),
            center+new Vector2Int(0, 1),
            center+new Vector2Int(1, 1),
            center+new Vector2Int(-1, 0),
            center+new Vector2Int(1, 0),
            center+new Vector2Int(-1, -1),
            center+new Vector2Int(0, -1),
            center+new Vector2Int(1, -1)
        };
        return re;
    }

    private bool InZone(Vector2Int position,int x0,int y0,int x1,int y1)
    {
        if(position.x>=x0&&position.x<=x1&&position.y>=y0&&position.y<=y1)
        {
            return true;
        }
        return false;
    }

    public void WarTowerFinishGetting(WarTower warTower)
    {
        List<object> objs = GetUnmoveObjAtLocation(warTower.position);
        warTower.factionOrder = 0;
        for (int i = 0; i < objs.Count; i++)
        {
            if (objs[i].GetType().ToString() == "Unit")
            {
                Unit unit = (Unit)objs[i];
                warTower.factionOrder = unit.factionOrder;
                warTower.towerState = MDs.State.Staying;
                factions[unit.factionOrder].towerScore += warTower.towerValue;
                factions[unit.factionOrder].haveWarTowerNum += 1;
                ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                ServerManager.Instance.SendMessageOfFactionUpdate(factions[unit.factionOrder]);
                ServerManager.Instance.SendToptipMsgToFaction("成功获取战塔", warTower.factionOrder);
                ServerManager.Instance.SendMapTipMsgToFaction("成功获取战塔", warTower.position, warTower.factionOrder);

                if(warTower.isLifeTower)
                {
                    foreach(Vector2Int p in AroundPosition(warTower.position))
                    {
                        if(!InZone(p,0,0,basicSetting.mapSizeX-1,basicSetting.mapSizeY-1))
                        {
                            continue;
                        }
                        safeZone[p.x, p.y] = warTower.factionOrder;
                    }
                }

                break;
            }
        }

    }



    private void WarTowerFinishLosing(WarTower warTower)
    {
        int oldFactionOrder = warTower.factionOrder;

        factions[warTower.factionOrder].towerScore -= warTower.towerValue;
        factions[warTower.factionOrder].haveWarTowerNum -= 1;

        ServerManager.Instance.SendMessageOfFactionUpdate(factions[warTower.factionOrder]);
        warTower.factionOrder = 0;
        warTower.towerState = MDs.State.Staying;
        ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
        TryLeaveStayingTowerTolocalUnit(warTower);

        if(warTower.isLifeTower)
        {
            foreach (Vector2Int p in AroundPosition(warTower.position))
            {
                if (!InZone(p, 0, 0, basicSetting.mapSizeX - 1, basicSetting.mapSizeY - 1))
                {
                    continue;
                }
                safeZone[p.x, p.y] = 0;
            }
        }

        Unit presentLocalUnit=null;
        float localUnitScore = warTower.towerValue*MySettings.warTowerSnatchRate;
        foreach(Unit unit in units.Values)
        {
            if(unit.position==warTower.position)
            {
                presentLocalUnit = unit;
                break;
            }
        }

        //update connected warTower
        //oldFactionOrder;
        List<string> toCheckRootTowerID = new List<string>();
        List<string> alreadyAddedTowerID = new List<string>();
        foreach(string linkedID in warTower.linkedTowerIDs)
        {
            if(warTowers[linkedID].factionOrder==oldFactionOrder)
            {
                toCheckRootTowerID.Add(linkedID);
                //alreadyAddedTowerID.Add(linkedID);
            }
        }
        int checkAtIndex = 0;
        while(checkAtIndex<toCheckRootTowerID.Count)
        {
            if(alreadyAddedTowerID.Contains(toCheckRootTowerID[checkAtIndex]))
            {
                checkAtIndex++;
                continue;
            }
            List<string> sectionLinked= new List<string>();
            bool sectionLifed = false;
            int sectionCheckAtIndex = 0;
            sectionLinked.Add(toCheckRootTowerID[checkAtIndex]);
            alreadyAddedTowerID.Add(toCheckRootTowerID[checkAtIndex]);
            while (sectionCheckAtIndex<sectionLinked.Count)
            {
                if (warTowers[sectionLinked[sectionCheckAtIndex]].factionOrder==oldFactionOrder&&warTowers[sectionLinked[sectionCheckAtIndex]].isLifeTower)
                {
                    sectionLifed = true;
                }
                foreach(string linkedID in warTowers[sectionLinked[sectionCheckAtIndex]].linkedTowerIDs)
                {
                    if(alreadyAddedTowerID.Contains(linkedID))
                    {
                        continue;
                    }
                    if (warTowers[linkedID].factionOrder == oldFactionOrder)
                    {
                        sectionLinked.Add(linkedID);
                        alreadyAddedTowerID.Add(linkedID);
                        continue;
                    }
                }
                sectionCheckAtIndex++;
            }

            if(sectionLifed==false)
            {
                //lose section towers
                foreach(string towerID in sectionLinked)
                {
                    while(true)
                    {
                        bool finish = true;
                        foreach (Unit unit in units.Values)
                        {
                            if (unit.position == warTowers[towerID].position && unit.factionOrder == warTowers[towerID].factionOrder)
                            {
                                CallBackUnit(unit);
                                finish = false;
                                break;
                            }
                        }
                        if(finish)
                        {
                            break;
                        }
                    }


                    factions[warTowers[towerID].factionOrder].towerScore -= warTowers[towerID].towerValue;
                    factions[warTowers[towerID].factionOrder].haveWarTowerNum -= 1;

                    ServerManager.Instance.SendMessageOfFactionUpdate(factions[warTowers[towerID].factionOrder]);
                    warTowers[towerID].factionOrder = 0;
                    warTowers[towerID].towerState = MDs.State.Staying;
                    ServerManager.Instance.SendMessageOfWarTowerUpdate(warTowers[towerID]);

                    if(presentLocalUnit!=null)
                    {
                        localUnitScore += warTowers[towerID].towerValue * MySettings.warTowerSnatchRate;
                    }

                    //TryLeaveStayingTowerTolocalUnit(warTowers[towerID]);
                }
            }
            checkAtIndex++;
        }

        //score the local unit
        if (presentLocalUnit != null)
        {
            factions[presentLocalUnit.factionOrder].score += Mathf.Ceil(localUnitScore);
            ServerManager.Instance.SendMessageOfFactionUpdate(factions[presentLocalUnit.factionOrder]);
            ServerManager.Instance.SendToptipMsgToFaction("通过抢占敌方战塔获得 "+(int)Mathf.Ceil(localUnitScore)+" 积分",presentLocalUnit.factionOrder);
            ServerManager.Instance.SendMapTipMsgToFaction("通过抢占敌方战塔获得 " + (int)Mathf.Ceil(localUnitScore) + " 积分", presentLocalUnit.position, presentLocalUnit.factionOrder);
        }

        //Set unlinked getting to staying
        foreach(WarTower aWarTower in warTowers.Values)
        {
            if(aWarTower.towerState==MDs.State.Getting)
            {
                bool isGettingByThisFaction = false;
                List<Unit> itsUnits=new List<Unit>();
                foreach(Unit unit in units.Values)
                {
                    if(unit.position==aWarTower.position&&unit.factionOrder==oldFactionOrder)
                    {
                        isGettingByThisFaction = true;
                        itsUnits.Add(unit);
                        break;
                    }
                }
                bool isStillLinked = false;
                foreach(string linkedID in aWarTower.linkedTowerIDs)
                {
                    if(warTowers[linkedID].factionOrder==oldFactionOrder)
                    {
                        isStillLinked = true;
                    }
                }
                if(!isStillLinked&&isGettingByThisFaction)
                {
                    aWarTower.towerState = MDs.State.Staying;
                    ServerManager.Instance.SendMessageOfWarTowerUpdate(aWarTower);
                    foreach(Unit unit in itsUnits)
                    {
                        CallBackUnit(unit);
                    }
                }
            }
        }
    }

    private void TryLeaveStayingTowerTolocalUnit(WarTower warTower)
    {
        List<object> objs = GetUnmoveObjAtLocation(warTower.position);
        for (int i = 0; i < objs.Count; i++)
        {
            if (objs[i].GetType().ToString() == "Unit")
            {
                Unit unit = (Unit)objs[i];
                bool canget = false;
                foreach(string linkedTower in warTower.linkedTowerIDs)
                {
                    if(warTowers[linkedTower].factionOrder==unit.factionOrder)
                    {
                        canget = true;
                    }
                }
                if(canget)
                {
                    warTower.towerState = MDs.State.Getting;
                    warTower.stateChangeLeftTime = MySettings.TowerGettingTime(warTower.towerLevel,factions[unit.factionOrder].haveWarTowerNum);
                    //warTower.stateChangeLeftTime = (int)(MySettings.warTowerGettingTIme * Mathf.Pow(MySettings.warTowerGettingTImeRisingRate, factions[unit.factionOrder].haveWarTowerNum));//second
                    ServerManager.Instance.SendMessageOfWarTowerUpdate(warTower);
                    break;
                }
            }
        }
    }

    //public Dictionary<string, List<object>> map;
    private List<object> GetUnmoveObjAtLocation(Vector2Int position)
    {
        List<object> re = new List<object>();

        if (units != null)
        {
            foreach (Unit unit in units.Values)
            {
                if (unit.position == position)
                {
                    re.Add(unit);
                }
            }
        }

        if (warTowers != null)
        {
            if (warTowers.ContainsKey(WarTower.GetIDbyLogicPosition(position)))
            {
                re.Add(warTowers[WarTower.GetIDbyLogicPosition(position)]);
            }
        }
        if(lightTowers!=null)
        {
            if(lightTowers.ContainsKey(LightTower.GetIDbyLogicPosition(position)))
            {
                re.Add(lightTowers[LightTower.GetIDbyLogicPosition(position)]);
            }
        }
        if (outposts != null)
        {
            foreach (Outpost outpost in outposts.Values)
            {
                if (outpost.position == position)
                {
                    re.Add(outpost);
                }
            }
        }
        return re;
    }

    public void SetBasicSetting(BattleBasicSetting basicSetting)
    {
        this.basicSetting = basicSetting;
    }

    public void CreateBattleFeild(BattleBasicSetting basicSetting)
    {
        this.basicSetting = basicSetting;
        Debug.Log("Start to create battle feild");
        safeZone = new int[basicSetting.mapSizeX, basicSetting.mapSizeY];
        CreateFactionsByBasicSetting();
        CreateWarTowersByBasicSetting();//need initial factions
        CreateLightTowersByBasicSetting();
        CreateOutpostBysideBirthTower();
        CreateSomeNPCUnit();
        //units = new Dictionary<string, Unit>();
        //lightTowers = new Dictionary<string, LightTower>();
        movingUnits = new Dictionary<string, MovingUnit>();
        battleStartTime = DateTime.Now;
        battleSpeedRate = 1;
        battleTimeAt = 0;//////for test
        lastUpdateAtTime = DateTime.Now;
        Debug.Log("Battle feild initialed");
    }

    void CreateFactionsByBasicSetting()
    {
        factions = new List<Faction>();
        //Create players
        for (int i = 0; i <= basicSetting.PlayerNum; i++)
        {
            Faction faction = new Faction
            {
                order = i,
                score = 0,
                towerScore = 1
            };
            factions.Add(faction);
        }
    }

    void CreateLightTowersByBasicSetting()
    {
        LightTower[,] lightTowersForCreateUse = new LightTower[basicSetting.lightHouseXnum, basicSetting.lightHouseYnum];
        lightTowers = new Dictionary<string, LightTower>();
        for (int ty = 0; ty < basicSetting.lightHouseYnum; ty++)
        {
            for (int tx = 0; tx < basicSetting.lightHouseXnum; tx++)
            {
                Vector2Int p;
                do
                {
                    p = new Vector2Int
                    {
                        x = (int)((tx + 0.5f) * basicSetting.mapSizeX / basicSetting.lightHouseXnum) + (int)UnityEngine.Random.Range(-1.5f, 2.5f),
                        y = (int)((ty + 0.5f) * basicSetting.mapSizeY / basicSetting.lightHouseYnum) + (int)UnityEngine.Random.Range(-1.5f, 2.5f)
                        //x = (int)((tx + 0.5f) * basicSetting.mapSizeX / basicSetting.warTowerXnum),
                        //y = (int)((ty + 0.5f) * basicSetting.mapSizeY / basicSetting.warTowerYnum)
                    };
                } while (warTowers.ContainsKey(WarTower.GetIDbyLogicPosition(p)));
                LightTower lightTower = new LightTower();
                lightTower.position = p;
                lightTower.factionState =new MDs.LightState[5]{ MDs.LightState.Dark,MDs.LightState.Dark,MDs.LightState.Dark,MDs.LightState.Dark,MDs.LightState.Dark};
                lightTower.coolDownTime = new int[5];
                lightTower.leftLightTime = new int[5];
                lightTower.lightRadius = MySettings.lightTowerSeeDist;
                lightTower.lastUpdateTime = 0;
                lightTowers.Add(lightTower.GetID(),lightTower);
            }
            
        }
    }

    void CreateWarTowersByBasicSetting()
    {
        WarTower[,] warTowersForCreateUse = new WarTower[basicSetting.warTowerXnum, basicSetting.warTowerYnum];
        warTowers = new Dictionary<string, WarTower>();
        for (int ty = 0; ty < basicSetting.warTowerYnum; ty++)
        {
            for (int tx = 0; tx < basicSetting.warTowerXnum; tx++)
            {
                Vector2Int p = new Vector2Int
                {
                    x = (int)((tx + 0.5f) * basicSetting.mapSizeX / basicSetting.warTowerXnum) + (int)UnityEngine.Random.Range(-3f, 3f),
                    y = (int)((ty + 0.5f) * basicSetting.mapSizeY / basicSetting.warTowerYnum) + (int)UnityEngine.Random.Range(-3f, 3f)
                };
                WarTower warTower = new WarTower
                {
                    position = p,
                    factionOrder = 0,
                    towerState = MDs.State.Staying,
                    towerLevel=MySettings.TowerLevel(p,basicSetting),
                    towerValue=MySettings.TowerValue(MySettings.TowerLevel(p, basicSetting))
                };
                //set birth tower
                if (basicSetting.PlayerNum >= 1 && tx == (int)(basicSetting.warTowerXnum *0.4f) && ty == basicSetting.warTowerYnum - 1)
                {
                    warTower.isBrithTower = true;
                    warTower.factionOrder = 1;
                    factions[1].BrithTower = warTower;
                    factions[1].haveWarTowerNum = 1;
                }
                if (basicSetting.PlayerNum >= 2 && tx == (int)(basicSetting.warTowerXnum *0.6f) && ty == 0)
                {
                    warTower.isBrithTower = true;
                    warTower.factionOrder = 2;
                    factions[2].BrithTower = warTower;
                    factions[2].haveWarTowerNum = 1;
                }
                if (basicSetting.PlayerNum >= 3 && tx == basicSetting.warTowerXnum - 1 && ty == (int)(basicSetting.warTowerYnum * 0.4f))
                {
                    warTower.isBrithTower = true;
                    warTower.factionOrder = 3;
                    factions[3].BrithTower = warTower;
                    factions[3].haveWarTowerNum = 1;
                }
                if (basicSetting.PlayerNum >= 4 && tx == 0 && ty == (int)(basicSetting.warTowerYnum * 0.6f))
                {
                    warTower.isBrithTower = true;
                    warTower.factionOrder = 4;
                    factions[4].BrithTower = warTower;
                    factions[4].haveWarTowerNum = 1;
                }

                //set life tower
                if(tx == 0 || tx == basicSetting.warTowerXnum - 1 || ty == 0 || ty == basicSetting.warTowerYnum - 1)
                {
                    warTower.isLifeTower = true;
                    warTower.towerValue = 0f;

                }

                warTowers.Add(warTower.GetID(), warTower);
                warTowersForCreateUse[tx, ty] = warTower;
            }
        }

        //Set birthtowerSafeZone
        foreach(WarTower warTower in warTowers.Values)
        {
            if(warTower.isBrithTower)
            {
                foreach (Vector2Int p in AroundPosition(warTower.position))
                {
                    if (!InZone(p, 0, 0, basicSetting.mapSizeX - 1, basicSetting.mapSizeY - 1))
                    {
                        continue;
                    }
                    safeZone[p.x, p.y] = warTower.factionOrder;
                }
                safeZone[warTower.position.x, warTower.position.y] = warTower.factionOrder;
            }
        }

        //Create Links
        for (int ty = 0; ty < basicSetting.warTowerYnum; ty++)
        {
            for (int tx = 0; tx < basicSetting.warTowerXnum; tx++)
            {
                WarTower warTower = warTowersForCreateUse[tx, ty];
                //Set warTowerlinks
                warTower.linkedTowerIDs = new List<string>();
                //upside link
                if (ty < basicSetting.warTowerYnum - 1)
                {
                    warTower.linkedTowerIDs.Add(warTowersForCreateUse[tx, ty + 1].GetID());
                }
                //downside link
                if (ty > 0)
                {
                    warTower.linkedTowerIDs.Add(warTowersForCreateUse[tx, ty - 1].GetID());
                }
                //leftside link
                if (tx > 0)
                {
                    warTower.linkedTowerIDs.Add(warTowersForCreateUse[tx - 1, ty].GetID());
                }
                //rightside link
                if (tx < basicSetting.warTowerXnum - 1)
                {
                    warTower.linkedTowerIDs.Add(warTowersForCreateUse[tx + 1, ty].GetID());
                }
            }
        }
    }


    void CreateOutpostBysideBirthTower()
    {
        outposts = new Dictionary<string, Outpost>();
        for (int i = 1; i < factions.Count; i++)
        {
            Faction faction = factions[i];
            WarTower factionLifeTower = faction.BrithTower;
            Vector2Int randPosition=new Vector2Int(0,0);
            List<object> positionObjs;
            for (int i2 = 0; i2 < MySettings.initialOutpostNum; i2++)
            {
                int tryTimes = 0;
                do
                {
                    if (tryTimes > 100)
                    {
                        break;
                    }
                    tryTimes++;
                    randPosition = new Vector2Int
                    {
                        x = factionLifeTower.position.x + (int)UnityEngine.Random.Range(-7.5f, 8.5f),
                        y = factionLifeTower.position.y + (int)UnityEngine.Random.Range(-7.5f, 8.5f)
                    };
                    positionObjs = GetUnmoveObjAtLocation(randPosition);
                } while (positionObjs.Count > 0 || randPosition.x < 0 || randPosition.y < 0 || randPosition.x >= basicSetting.mapSizeX || randPosition.y >= basicSetting.mapSizeY);
                if(tryTimes>100)
                {
                    continue;
                }
                Outpost outpost = new Outpost()
                {
                    isBuilding = false,
                    leftBuildTime = 0,
                    blood = MySettings.outpostInitialBlood,
                    unitNum = (int)UnityEngine.Random.Range(10, 500),
                    outTroopNum = 0,
                    factionOrder = faction.order,
                    position = randPosition
                };
                outpost.power = outpost.unitNum;
                outposts.Add(outpost.GetID(), outpost);
            }
        }
    }

    void CreateSomeNPCUnit()
    {
        units = new Dictionary<string, Unit>();
        for (int i = 0; i < (basicSetting.mapSizeX * basicSetting.mapSizeY) / 100*MySettings.npcNumInArea100; i++)
        {
            Unit unit = new Unit
            {
                position = new Vector2Int((int)UnityEngine.Random.Range(0, basicSetting.mapSizeX), (int)UnityEngine.Random.Range(0, basicSetting.mapSizeY)),
                belongToOutpostID = "none",
                UnitAmount = (int)UnityEngine.Random.Range(1, 20),
                factionOrder = 0
            };
            List<object> positionObjs = GetUnmoveObjAtLocation(unit.position);
            if (positionObjs.Count == 0)
            {
                units.Add(unit.GetID(), unit);
            }
        }
    }
}
