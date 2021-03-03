using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class ClientManager : MonoBehaviour
{
    //Singleton
    static ClientManager instance;

    public static ClientManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(ClientManager)) as ClientManager;
            }
            return instance;
        }
    }


    public enum ClientState { UnStarted, TryToConnect, Connected };

    public ClientState clientState;

    public TCPClient tcpClient;
    public UIManager uiManager;

    //public int battleTimeAt;//present battle at(millisecond)
    //private int battleSpeedRate;
    //private DateTime lastUpdateAtTime;
    //private DateTime clientStartTime;

    //For Ping
    public int pingOrder;
    public DateTime pingAtTime;//present battle at(millisecond)



    // Use this for initialization
    void Start()
    {
        clientState = ClientState.UnStarted;
        pingOrder = 0;

    }

    // Update is called once per frame
    void Update()
    {
        //If is trying to connect to server
        if (clientState==ClientState.TryToConnect)
        {
            //Check if successfully connected 
            if(TCPClient.Instance.tcpClientState==TCPClient.TCPClientState.Connected){
                clientState = ClientState.Connected;
                FirstConnectedToServer();
            }
        }

        //
        //check msg
        if(clientState==ClientState.Connected)
        {
            while (TCPClient.Instance.fromServerMessages.Count>0)
            {
                FromServerMessage aMessage=TCPClient.Instance.GetOutAMessage();
                switch(aMessage.messageHead.messageType)
                {
                    case MessageTypes.TimeAndSpeedMsg:
                        Debug.Log("Client Got Msg TimeAndSpeedMsg");
                        DealTimeAndSpeedMsg(aMessage);
                        break;
                    case MessageTypes.PingMsg:
                        //Debug.Log("Got Msg PingMsg");
                        DealPingMsg(aMessage);
                        break;
                    case MessageTypes.AllBattleInfoMsg:
                        Debug.Log("Client Got Msg AllBattleInfoMsg");
                        DealAllBattleInfoMsg(aMessage);
                        break;
                    case MessageTypes.OutpostMsg:
                        Debug.Log("Client Got Msg OutpostUpdateMsg");
                        DealOutpostMsg(aMessage);
                        break;
                    case MessageTypes.UnitMsg:
                        Debug.Log("Client Got Msg UnitUpdateMsg");
                        DealUnitUpdateMsg(aMessage);
                        break;
                    case MessageTypes.MovingUnitMsg:
                        Debug.Log("Client Got Msg MovingUnitMsg");
                        DealMovingUnitMsg(aMessage);
                        break;
                    case MessageTypes.FactionMsg:
                        Debug.Log("Client Got Msg MovingUnitMsg");
                        DealFactionMsg(aMessage);
                        break;
                    case MessageTypes.WarTowerMsg:
                        Debug.Log("Client Got Msg WarTowerMsg");
                        DealWarTowerMsg(aMessage);
                        break;
                    case MessageTypes.TopTipMsg:
                        //Debug.Log("Client Got Msg TopTipMsg");
                        DealTopTipMsg(aMessage);
                        break;
                    case MessageTypes.LightTowerMsg:
                        Debug.Log("Client Got Msg LightTowerMsg");
                        DealLightTowerMsg(aMessage);
                        break;
                    case MessageTypes.MapTipMsg:
                        //Debug.Log("Client Got Msg MapTipMsg");
                        DealMapTipMsg(aMessage);
                        break;
                    default:
                        Debug.Log("Unknow MessageTypes:" + aMessage.messageHead.messageType);
                        break;
                }
            }
        }

        //Playing Update
        if (DemoManager.Instance.demoState == DemoManager.DemoRunningAt.Playing)
        {
            CheckPingState();
        }
    }

    private void DealMapTipMsg(MapTipMsg msg)
    {
        UIManager.Instance.CreateAMapTip(msg.content, new Vector2Int(msg.positionX, msg.positionY));
    }

    private void DealMapTipMsg(FromServerMessage aMessage)
    {
        MapTipMsg msg=NetworkUtils.Deserialize<MapTipMsg>(aMessage.messageContentBytes);
        DealMapTipMsg(msg);
    }

    private void DealLightTowerMsg(LightTowerMsg msg)
    {
        UIManager.Instance.LightTowerUpdate(msg);
    }

    private void DealLightTowerMsg(FromServerMessage aMessage)
    {
        LightTowerMsg msg=NetworkUtils.Deserialize<LightTowerMsg>(aMessage.messageContentBytes);
        DealLightTowerMsg(msg);
    }

    private void DealTopTipMsg(FromServerMessage aMessage)
    {
        TopTipMsg topTipMsg=NetworkUtils.Deserialize<TopTipMsg>(aMessage.messageContentBytes);
        UIManager.Instance.AddATopTip(topTipMsg.topTipMsg);
    }

    private void DealWarTowerMsg(FromServerMessage aMessage)
    {
        WarTowerMsg msg = NetworkUtils.Deserialize<WarTowerMsg>(aMessage.messageContentBytes);
        DealWarTowerMsg(msg);
    }

    private void DealUnitUpdateMsg(FromServerMessage aMessage)
    {
        UnitMsg msg = NetworkUtils.Deserialize<UnitMsg>(aMessage.messageContentBytes);
        UIManager.Instance.UnitUpdate(msg);
    }

    private void DealMovingUnitMsg(MovingUnitMsg msg)
    {
        //Debug.Log("DealMovingUnitMsg msg.updateWay:"+msg.updateWay);
        //Debug.Log("DealMovingUnitMsg msg.startPositionX:" + msg.startPositionX);
        UIManager.Instance.UpdateMovingUnit(msg);
    }

    private void DealMovingUnitMsg(FromServerMessage aMessage)
    { 
        MovingUnitMsg msg = NetworkUtils.Deserialize<MovingUnitMsg>(aMessage.messageContentBytes);
        //Debug.Log("aMessage.messageContentBytes.Length"+aMessage.messageContentBytes.Length);
        DealMovingUnitMsg(msg);
    }

    private void DealTimeAndSpeedMsg(FromServerMessage aMessage)
    {
        TimeAndSpeedMsg msg=NetworkUtils.Deserialize<TimeAndSpeedMsg>(aMessage.messageContentBytes);
        DealTimeAndSpeedMsg(msg);
    }

    private void DealTimeAndSpeedMsg(TimeAndSpeedMsg msg)
    {
        UIManager.Instance.battleTimeAt = msg.timeAt;
        UIManager.Instance.battleSpeedRate = msg.speedRate;
        UIManager.Instance.lastUpdateAtTime = DateTime.Now;
        UIManager.Instance.SetTimeText(msg.timeAt);
        UIManager.Instance.SetSpeedText(msg.speedRate);
    }

    //private void DealTimeAndSpeedMsg(TimeAndSpeedMsg msg)
    //{
    //    this.battleTimeAt = msg.timeAt;
    //    this.battleSpeedRate = msg.speedRate;
    //    this.lastUpdateAtTime = DateTime.Now;
    //    UIManager.Instance.SetTimeText(msg.timeAt);
    //    UIManager.Instance.SetSpeedText(msg.speedRate);
    //}

    private bool waitingPing = false;

    private void DealPingMsg(FromServerMessage aMessage)
    {
        PingMsg msgContent = NetworkUtils.Deserialize<PingMsg>(aMessage.messageContentBytes);
        if(msgContent.pingOrder==this.pingOrder)
        {
            DateTime backTime = DateTime.Now;
            int usedTime=backTime.Second*1000+backTime.Millisecond - pingAtTime.Second*1000-pingAtTime.Millisecond;
            UIManager.Instance.SetPingText(usedTime);
            waitingPing = false;
            //Debug.Log("State:"+DemoManager.Instance.demoState);
            if (DemoManager.Instance.demoState == DemoManager.DemoRunningAt.Playing)
            {
                StartCoroutine(StartAPing());
            }
        }
        else
        {
            Debug.Log("error ping back");
        }
    }

    private void DealMapMsg(MapMsg msg)
    {
        UIManager.Instance.UpdateMap(msg);
    }

    private void DealWarTowerMsg(WarTowerMsg msg)
    {
        UIManager.Instance.UpdateAWarTower(msg);
    }

    private void DealFactionMsg(FromServerMessage aMessage)
    {
        FactionMsg msgContent=NetworkUtils.Deserialize<FactionMsg>(aMessage.messageContentBytes);
        DealFactionMsg(msgContent);
    }

    private void DealFactionMsg(FactionMsg msg)
    {
        FactionInfo factionInfo = UIManager.Instance.factionInfos[msg.factionOrder];
        factionInfo.factionScore = msg.factionScores;
        factionInfo.factionTowerScore = msg.factionTowerScores;
        factionInfo.lastUpdateBattleTime = UIManager.Instance.battleTimeAt;
        uiManager.SetFactionText(factionInfo);
    }

    private void DealOutpostMsg(FromServerMessage aMessage)
    {
        OutpostMsg msg = NetworkUtils.Deserialize<OutpostMsg>(aMessage.messageContentBytes);
        DealOutpostMsg(msg);
    }

    private void DealOutpostMsg(OutpostMsg msg)
    {
        UIManager.Instance.UpdateOutpost(msg);
    }

    private void DealUnitMsg(FromServerMessage aMessage)
    {
        UnitMsg msg = NetworkUtils.Deserialize<UnitMsg>(aMessage.messageContentBytes);
        DealUnitMsg(msg);
    }

    private void DealUnitMsg(UnitMsg msg)
    {
        UIManager.Instance.UnitUpdate(msg);
    }

    private void DealAllBattleInfoMsg(FromServerMessage aMessage)
    { 
        AllBattleInfoMsg msgContent = NetworkUtils.Deserialize<AllBattleInfoMsg>(aMessage.messageContentBytes);

        DealTimeAndSpeedMsg(msgContent.timeAndSpeedMsg);

        DealMapMsg(msgContent.mapMsg);

        foreach(WarTowerMsg msg in msgContent.warTowerMsgs)
        {
            DealWarTowerMsg(msg);
        }

        foreach (FactionMsg msg in msgContent.factionMsgs)
        {
            DealFactionMsg(msg);
        }

        foreach (OutpostMsg msg in msgContent.outpostMsgs)
        {
            DealOutpostMsg(msg);
        }
        foreach (UnitMsg msg in msgContent.unitMsgs)
        {
            DealUnitMsg(msg);
        }
        foreach (MovingUnitMsg msg in msgContent.movingUnitMsgs)
        {
            DealMovingUnitMsg(msg);
        }
        foreach (LightTowerMsg msg in msgContent.lightTowerMsgs)
        {
            DealLightTowerMsg(msg);
        }
    }





    public void OpenChooseHostOrClientUI()
    {
        uiManager.AskToChooseServer();
    }

    public void ClearScene()
    {
        UIManager.Instance.CleanAll();
        TCPClient.Instance.ClearAll();
        clientState = ClientState.UnStarted;
        UIManager.Instance.battleTimeAt = MDs.UnsetForPositive;
    }

    public void CreateClient(string serverIP,int clientOrder)
    {
        UIManager.Instance.myFactionOrder = clientOrder;
        clientState = ClientState.TryToConnect;
        TCPClient.Instance.order = clientOrder;
        TCPClient.Instance.StartClient(serverIP);
        for (int i = 0; i <= 4;i++)
        {
            FactionInfo factionInfo = new FactionInfo
            {
                factionOrder = i,
                factionScore=0,
                factionTowerScore=0,
                lastUpdateBattleTime=0
            };
        }
        UIManager.Instance.CreateTipSquare(clientOrder);
    }

    public void FirstConnectedToServer()
    {
        Debug.Log("FirstConnectedToServer");

        UIManager.Instance.SetServerIPText(DemoManager.Instance.serverIP);
        //UIManager.Instance.SetPingText(ping);
        //UIManager.Instance.SetTimeText(ping);
        TCPClient.Instance.SendMessageObject(MessageTypes.ReqForAllBattleInfo,null);
        StartCoroutine(StartAPing());
    }

    private IEnumerator StartAPing()
    {
        yield return new WaitForSeconds(3.0f);
        this.pingOrder++;
        PingMsg pingMsg = new PingMsg
        {
            pingOrder = this.pingOrder
        };
        TCPClient.Instance.SendMessageObject(MessageTypes.PingMsg, pingMsg);
        //Debug.Log("send ping at order:"+pingOrder);
        pingAtTime=DateTime.Now;
        waitingPing = true;
    }

    private void CheckPingState()
    {
        if(waitingPing)
        {
            int usedTime = DateTime.Now.Second * 1000 + DateTime.Now.Millisecond - pingAtTime.Second * 1000 - pingAtTime.Millisecond;
            if (usedTime >= 999)
            {
                UIManager.Instance.SetPingText(999);
            }
        }

    }
}
