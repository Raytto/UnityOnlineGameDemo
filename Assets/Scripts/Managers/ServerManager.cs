using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class ServerManager : MonoBehaviour
{
    //Singleton
    static ServerManager instance;

    public static ServerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(ServerManager)) as ServerManager;
            }
            return instance;
        }
    }

    //public TCPServer tcpServer;
    public Battlefeild battlefeild;

    //public enum ClientState { UnStarted, Started, Connected };

    //public ClientState clientState;

    // Use this for initialization
    void Start()
    {

    }

    public void SendMapTipMsgToFaction(string mapTipContent,Vector2Int position, int factionOrder)
    {
        MapTipMsg mapTipMsg = new MapTipMsg
        {
            content=mapTipContent,
            positionX=position.x,
            positionY=position.y
        };
        TCPServer.Instance.SendMessageObject(factionOrder, MessageTypes.MapTipMsg, mapTipMsg);
    }

    public void SendToptipMsgToFaction(string toptip,int factionOrder)
    {
        TopTipMsg topTipMsg = new TopTipMsg
        {
            topTipMsg = toptip
        };
        TCPServer.Instance.SendMessageObject(factionOrder,MessageTypes.TopTipMsg, topTipMsg);
    }

    public void SendToptipMsgToAll(string toptip)
    {
        TopTipMsg topTipMsg = new TopTipMsg
        {
            topTipMsg = toptip
        };
        TCPServer.Instance.SendMessageObjectToAll( MessageTypes.TopTipMsg, topTipMsg);
    }

    public void SendMessageOfUnitDestroy(string unitID)
    {
        UnitMsg unitMsg = new UnitMsg
        {
            updateWay=UnitMsg.UpdateWay.Destory,
            id=unitID
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.UnitMsg, unitMsg);
    }

    public void SendMessageOfMovingUnitDestroy(string movingUnitID)
    {
        MovingUnitMsg movingUnitMsg = new MovingUnitMsg
        {
            updateWay = MovingUnitMsg.UpdateWay.Destory,
            id = movingUnitID
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.MovingUnitMsg, movingUnitMsg);
    }

    public void SendMessageOfUnitUpdate(Unit unit)
    {
        UnitMsg unitMsg = new UnitMsg
        {
            updateWay=UnitMsg.UpdateWay.Update,
            id=unit.GetID(),
            positionX=unit.position.x,
            positionY=unit.position.y,
            belongToOutpostID=unit.belongToOutpostID,
            UnitAmount=unit.UnitAmount,
            factionOrder=unit.factionOrder
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.UnitMsg, unitMsg);
    }

    public void SendMessageOfOutpostUpdate(Outpost outpost)
    {
        OutpostMsg outpostMsg = new OutpostMsg()
        {
            msgType = OutpostMsg.MsgType.Update,
            outpostID = outpost.GetID(),
            isBuilding = outpost.isBuilding,
            leftBuildTime = outpost.leftBuildTime,
            blood = outpost.blood,
            unitNum = outpost.unitNum,
            outTroopNum = outpost.outTroopNum,
            factionOrder = outpost.factionOrder,
            positionX = outpost.position.x,
            positionY = outpost.position.y,
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.OutpostMsg, outpostMsg);
    }

    public void SendMessageOfOutpostDestroy(string oldOutpostID)
    {
        OutpostMsg outpostMsg = new OutpostMsg
        {
            msgType=OutpostMsg.MsgType.Destory,
            outpostID=oldOutpostID
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.OutpostMsg, outpostMsg);
    }

    public void SendMessageOfMovingUnitUpdate(MovingUnit movingUnit)
    {
        //Debug.Log("SendMessageOfMovingUnitUpdate:"+movingUnit.belongToOutpostID+"_"+movingUnit.factionOrder);
        MovingUnitMsg movingUnitMsg = new MovingUnitMsg
        {
            updateWay = MovingUnitMsg.UpdateWay.Update,
            id = movingUnit.GetID(),
            belongToOutpostID = movingUnit.belongToOutpostID,
            UnitAmount = movingUnit.UnitAmount,
            startPositionX = movingUnit.startPosition.x,
            startPositionY = movingUnit.startPosition.y,
            endPositionX=movingUnit.endPosition.x,
            endPositionY=movingUnit.endPosition.y,
            totalLength=movingUnit.totalLength,
            currentLength=movingUnit.currentLength,
            speed=movingUnit.speed,
            factionOrder=movingUnit.factionOrder,
            lastUpdateTime=movingUnit.lastUpdateTime
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.MovingUnitMsg, movingUnitMsg);
    }

    public void SendMessageOfWarTowerUpdate(WarTower warTower)
    {
        WarTowerMsg warTowerMsg = new WarTowerMsg
        {
            towerID = warTower.GetID(),
            logicPositionX = warTower.position.x,
            logicPositionY = warTower.position.y,
            isBrithTower = warTower.isBrithTower,
            isLifeTower = warTower.isLifeTower,
            FactionOrder = warTower.factionOrder,
            linkedTowerIDs = warTower.linkedTowerIDs,
            towerValue = warTower.towerValue,
            towerState=warTower.towerState,
            stateChangeLeftTime=warTower.stateChangeLeftTime,
            lastUpdateTime = warTower.lastUpdateTime,
            towerLevel=warTower.towerLevel
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.WarTowerMsg, warTowerMsg);
    }

    public void SendMessageOfLightTowerUpdate(LightTower lightTower)
    {
        LightTowerMsg lightTowerMsg = new LightTowerMsg
        { 
            id = lightTower.GetID(),
            positionX=lightTower.position.x,
            positionY=lightTower.position.y,
            factionState=lightTower.factionState,
            coolDownTime=lightTower.coolDownTime,
            leftLightTime=lightTower.leftLightTime,
            lightRadius = lightTower.lightRadius,
            lastUpdateTime = lightTower.lastUpdateTime
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.LightTowerMsg, lightTowerMsg);
    }

    public void SendMessageOfFactionUpdate(Faction faction)
    {
        //Debug.Log("faction.score:"+faction.score);
        FactionMsg factionMsg = new FactionMsg
        { 
            factionOrder = faction.order,
            factionScores = faction.score,
            factionTowerScores = faction.towerScore
        };
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.FactionMsg, factionMsg);
    }

    // Update is called once per frame
    void Update()
    {
        if (TCPServer.Instance.tcpServerState == TCPServer.TCPServerState.Created)
        {
            while (TCPServer.Instance.fromClientMessages.Count > 0)
            {
                FromClientMessage aMessage = TCPServer.Instance.GetOutAMessage();
                switch (aMessage.messageHead.messageType)
                {
                    case MessageTypes.ReqForTimeAndSpeed:
                        Debug.Log("Server got ReqForTimeAndSpeed");
                        DealReqForTimeAndSpeed(aMessage);
                        break;
                    case MessageTypes.PingMsg:
                        //Debug.Log("got PingMsg from client:"+aMessage.playerOrder);
                        DealPingMsg(aMessage);
                        break;
                    case MessageTypes.ReqForAllBattleInfo:
                        Debug.Log("Server got ReqForAllBattleInfo");
                        DealReqForAllBattleInfo(aMessage);
                        break;
                    //case MessageTypes.CreateMovingUnitFromOutpostReq:
                    //    DealReqForCreateMovingUnitFromOutpostReq(aMessage);
                    //    break;
                    //case MessageTypes.CallBackMovingUnitReq:
                        //DealReqForCallBackMovingUnitReq(aMessage);
                        //break;
                    case MessageTypes.MovingUnitMsg:
                        Debug.Log("Server got MovingUnitMsg");
                        DealReqForMovingUnitMsg(aMessage);
                        break;
                    case MessageTypes.UnitMsg:
                        Debug.Log("Server got UnitMsg");
                        DealReqForUnitMsg(aMessage);
                        break;
                    default:
                        break;
                }
            }
            if (DemoManager.Instance.demoState == DemoManager.DemoRunningAt.Playing)
            {
                battlefeild.UpdateBattleByTime();
            }
        }

    }

    private void DealReqForMovingUnitMsg(FromClientMessage aMessage)
    {
        MovingUnitMsg msgContent = NetworkUtils.Deserialize<MovingUnitMsg>(aMessage.messageContentBytes);

        if(msgContent.updateWay==MovingUnitMsg.UpdateWay.Start)
        {
            battlefeild.TryStartMovingUnit(aMessage.playerOrder,msgContent);
        }
        if (msgContent.updateWay == MovingUnitMsg.UpdateWay.CallBack)
        {
            battlefeild.TryCallBackMovingUnit(aMessage.playerOrder,msgContent);
        }
    }

    private void DealReqForUnitMsg(FromClientMessage aMessage)
    {
        UnitMsg msgContent = NetworkUtils.Deserialize<UnitMsg>(aMessage.messageContentBytes);
        if(msgContent.updateWay==UnitMsg.UpdateWay.CallBack)
        {
            battlefeild.TryCallBackUnit(aMessage.playerOrder, msgContent);
        }
    }

    private void DealReqForTimeAndSpeed(FromClientMessage aMessage)
    {
        TimeAndSpeedMsg msg = new TimeAndSpeedMsg
        {
            timeAt = battlefeild.battleTimeAt,
            speedRate = battlefeild.battleSpeedRate
        };
        TCPServer.Instance.SendMessageObject(aMessage.playerOrder, MessageTypes.TimeAndSpeedMsg, msg);
        SendToptipMsgToAll("战场速率修改为 "+battlefeild.battleSpeedRate);
    }

    private void DealPingMsg(FromClientMessage aMessage)
    {
        PingMsg msgContent = NetworkUtils.Deserialize<PingMsg>(aMessage.messageContentBytes);
        PingMsg backMsg = new PingMsg
        {
            pingOrder = msgContent.pingOrder
        };
        TCPServer.Instance.SendMessageObject(aMessage.playerOrder, MessageTypes.PingMsg, backMsg);
    }


    private void DealReqForAllBattleInfo(FromClientMessage aMessage)
    {
        MapMsg mapMsg = new MapMsg
        {
            mapSizeX = battlefeild.basicSetting.mapSizeX,
            mapSizeY = battlefeild.basicSetting.mapSizeY
        };

        TimeAndSpeedMsg timeAndSpeedMsg = new TimeAndSpeedMsg
        {
            timeAt = battlefeild.battleTimeAt,
            speedRate = battlefeild.battleSpeedRate
        };

        List<WarTowerMsg> warTowerMsgs = new List<WarTowerMsg>();

        foreach (WarTower warTower in battlefeild.warTowers.Values)
        {
            WarTowerMsg warTowerMsg = new WarTowerMsg()
            {
                towerID = warTower.GetID(),
                logicPositionX = warTower.position.x,
                logicPositionY = warTower.position.y,
                isBrithTower = warTower.isBrithTower,
                isLifeTower = warTower.isLifeTower,
                FactionOrder = warTower.factionOrder,
                linkedTowerIDs = warTower.linkedTowerIDs,
                lastUpdateTime = warTower.lastUpdateTime,
                stateChangeLeftTime = warTower.stateChangeLeftTime,
                towerState = warTower.towerState,
                towerValue=warTower.towerValue,
                towerLevel = warTower.towerLevel
            };
            warTowerMsgs.Add(warTowerMsg);
        }

        List<FactionMsg> factionMsgs = new List<FactionMsg>();
        foreach (Faction faction in battlefeild.factions)
        {
            if (faction.order > 0)
            {
                FactionMsg FactionMsg = new FactionMsg
                {
                    factionOrder = faction.order,
                    factionScores = faction.score,
                    factionTowerScores = faction.towerScore
                };
                factionMsgs.Add(FactionMsg);
            }

        }

        List<OutpostMsg> outpostMsgs = new List<OutpostMsg>();
        foreach(Outpost outpost in battlefeild.outposts.Values)
        {
            OutpostMsg outpostMsg = new OutpostMsg()
            {
                msgType = OutpostMsg.MsgType.Update,
                outpostID = outpost.GetID(),
                isBuilding = outpost.isBuilding,
                leftBuildTime = outpost.leftBuildTime,
                blood = outpost.blood,
                unitNum = outpost.unitNum,
                outTroopNum = outpost.outTroopNum,
                factionOrder = outpost.factionOrder,
                positionX = outpost.position.x,
                positionY = outpost.position.y,
            };
            outpostMsgs.Add(outpostMsg);
        }

        List<UnitMsg> unitMsgs = new List<UnitMsg>();
        foreach (Unit unit in battlefeild.units.Values)
        {
            UnitMsg unitMsg = new UnitMsg
            {
                updateWay = UnitMsg.UpdateWay.Update,
                id = unit.GetID(),
                positionX = unit.position.x,
                positionY = unit.position.y,
                belongToOutpostID = unit.belongToOutpostID,
                UnitAmount = unit.UnitAmount,
                factionOrder = unit.factionOrder
            };
            unitMsgs.Add(unitMsg);
        }

        List<MovingUnitMsg> movingUnitMsgs = new List<MovingUnitMsg>();
        foreach (MovingUnit movingUnit in battlefeild.movingUnits.Values)
        {
            MovingUnitMsg movingUnitMsg = new MovingUnitMsg
            {
                updateWay = MovingUnitMsg.UpdateWay.Update,
                id = movingUnit.GetID(),
                belongToOutpostID = movingUnit.belongToOutpostID,
                UnitAmount = movingUnit.UnitAmount,
                startPositionX = movingUnit.startPosition.x,
                startPositionY = movingUnit.startPosition.y,
                endPositionX = movingUnit.endPosition.x,
                endPositionY = movingUnit.endPosition.y,
                totalLength = movingUnit.totalLength,
                currentLength = movingUnit.currentLength,
                speed = movingUnit.speed,
                factionOrder=movingUnit.factionOrder,
                lastUpdateTime = movingUnit.lastUpdateTime
            };
            movingUnitMsgs.Add(movingUnitMsg);
        }

        List<LightTowerMsg> lightTowerMsgs = new List<LightTowerMsg>();
        foreach(LightTower lightTower in battlefeild.lightTowers.Values)
        {
            LightTowerMsg lightTowerMsg = new LightTowerMsg
            {
                id = lightTower.GetID(),
                positionX = lightTower.position.x,
                positionY = lightTower.position.y,
                factionState = lightTower.factionState,
                coolDownTime = lightTower.coolDownTime,
                leftLightTime = lightTower.leftLightTime,
                lightRadius = lightTower.lightRadius,
                lastUpdateTime = lightTower.lastUpdateTime
            };
            lightTowerMsgs.Add(lightTowerMsg);
        }

        AllBattleInfoMsg allBattleInfoMsg = new AllBattleInfoMsg
        {
            mapMsg = mapMsg,
            timeAndSpeedMsg = timeAndSpeedMsg,
            warTowerMsgs = warTowerMsgs,
            factionMsgs=factionMsgs,
            outpostMsgs=outpostMsgs,
            unitMsgs=unitMsgs,
            movingUnitMsgs=movingUnitMsgs,
            lightTowerMsgs=lightTowerMsgs
        };

        TCPServer.Instance.SendMessageObject(aMessage.playerOrder, MessageTypes.AllBattleInfoMsg, allBattleInfoMsg);
    }



    public void BroadcastTimeAndSpeed()
    {
        TimeAndSpeedMsg msg = new TimeAndSpeedMsg
        {
            timeAt = battlefeild.battleTimeAt,
            speedRate = battlefeild.battleSpeedRate
        };
        //Debug.Log("111111");
        TCPServer.Instance.SendMessageObjectToAll(MessageTypes.TimeAndSpeedMsg, msg);
    }

    public void ClearServer()
    {
        //Debug.Log("aaa");
        TCPServer.Instance.ClearAll();

    }

    public void CreateServer(BattleBasicSetting battleBasicSetting)
    {
        TCPServer.Instance.campsNum = battleBasicSetting.PlayerNum;
        TCPServer.Instance.StartServer();
        battlefeild = new Battlefeild();
        battlefeild.CreateBattleFeild(battleBasicSetting);
        Debug.Log("Create Server Success");
    }
}
