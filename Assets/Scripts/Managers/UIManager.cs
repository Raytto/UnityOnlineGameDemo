using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
//using System.Numerics;

public class UIManager : MonoBehaviour
{
    //Singleton
    static UIManager instance;

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(UIManager)) as UIManager;
            }
            return instance;
        }
    }

    public GameObject mapNode;
    public GameObject warTowersNode;
    public GameObject warTowerLinksNode;
    public GameObject outpostNode;
    public GameObject lightTowerNode;
    public GameObject unitsNode;
    public GameObject movingUnitsNode;
    public GameObject mapTipCanvasNode;
    public GameObject ObjTextCanvasNode;

    public GameObject CSDialog;
    public GameObject ingameUI;

    public GameObject cameraObject;
    public float cameraControlRate = 0.05f;

    public FoggyUI foggyUI;


    private float mapSizeX = 0f;
    private float mapSizeY = 0f;
    private GameObject tipSquare;

    //For Faction
    public List<FactionInfo> factionInfos;
    public int myFactionOrder;
    public GameObject myFactionImage;


    private Dictionary<string, GameObject> warTowerObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> warTowerLinksObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> warTowerSafeZoneObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> outpostObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> unitObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> movingUnitObjs = new Dictionary<string, GameObject>();
    private Dictionary<string, LightTowerUI> lightTowerUIs = new Dictionary<string, LightTowerUI>();
    private Dictionary<string, GameObject> objTexts = new Dictionary<string, GameObject>();
    private GameObject backgroundObj;

    private static class LayerOrder
    {
        public const int MapObjsLayer = 0;
        public const int TowerSafeZoneLayer = 1;
        public const int MapTipLayer = 2;
        public const int TowerLinksLayer = 3;
        public const int TowerLayer = 4;
        public const int OutpostLayer = 5;
        public const int UnitLayer = 6;
        public const int ChoosedObjLayer = 7;
    }

    // Use this for initialization
    void Start()
    {
        battleTimeAt = MDs.UnsetForPositive;
        clientStartTime = DateTime.Now;
        myFactionOrder = 0;

        factionInfos = new List<FactionInfo>();
        for (int i = 0; i <= 4; i++)
        {
            factionInfos.Add(new FactionInfo { factionOrder = i, factionScore = 0, factionTowerScore = 1 });
        }

    }

    // Update is called once per frame
    void Update()
    {
        CameraUpdate();
        TipSquareUpdate();
        ChooseObjUpdate();
        RightClickAction();
        UpdateTime();

        if (myFactionOrder != 0)
        {
            myFactionImage.SetActive(true);
            myFactionImage.transform.localPosition = new Vector3(-1.3f, 70f - myFactionOrder * 40f, 0);
        }
        else
        {
            myFactionImage.SetActive(false);
        }

        if (toptipInWaiting.Count > 0)
        {
            this.TryShowAToptip();
        }

    }

    public int battleTimeAt;//present battle at(millisecond)
    public int battleSpeedRate;
    public DateTime lastUpdateAtTime;
    public DateTime clientStartTime;

    private void UpdateTime()
    {
        if (battleTimeAt == MDs.UnsetForPositive)
        {
            return;
        }
        //Update Time
        DateTime timeNow = DateTime.Now;
        int pastTime = TimeUtils.GetTotalMilliseconds(timeNow, clientStartTime) - TimeUtils.GetTotalMilliseconds(lastUpdateAtTime, clientStartTime);
        lastUpdateAtTime = timeNow;
        battleTimeAt += pastTime * battleSpeedRate;
        SetTimeText(battleTimeAt);

        //Update Faction Score
        foreach (FactionInfo factionInfo in factionInfos)
        {
            factionInfo.factionScore += factionInfo.factionTowerScore * (battleTimeAt - factionInfo.lastUpdateBattleTime) / MySettings.towerScoreToRealScorePerTime;
            factionInfo.lastUpdateBattleTime = battleTimeAt;
            SetFactionText(factionInfo);
        }
        //Update WarTower state time
        //foreach(WarTowerUI in UIManager.Instance.wa)
        //Update MovingUnit position
        //Update Outpost Building time

        //Update WarTower
        foreach (GameObject warTowerObj in warTowerObjs.Values)
        {
            WarTowerUI warTowerUI = warTowerObj.GetComponent<WarTowerUI>();
            if (warTowerUI.towerState == MDs.State.Getting)
            {
                warTowerUI.stateChangeLeftTime -= battleTimeAt - warTowerUI.lastUpdateTime;
                warTowerUI.lastUpdateTime = battleTimeAt;
            }
            if (warTowerUI.towerState == MDs.State.Losing)
            {
                warTowerUI.stateChangeLeftTime -= battleTimeAt - warTowerUI.lastUpdateTime;
                warTowerUI.lastUpdateTime = battleTimeAt;
            }
        }

        //Update LightTower
        foreach (LightTowerUI lightTowerUI in lightTowerUIs.Values)
        {
            for (int i = 1; i <= 4; i++)
            {
                if (lightTowerUI.factionState[i] == MDs.LightState.Light)
                {
                    lightTowerUI.leftLightTime[i] -= battleTimeAt - lightTowerUI.lastUpdateTime;
                }
                if (lightTowerUI.coolDownTime[i] > 0)
                {
                    lightTowerUI.coolDownTime[i] -= battleTimeAt - lightTowerUI.lastUpdateTime;
                }
            }
            lightTowerUI.lastUpdateTime = battleTimeAt;
        }

        //Update OutPostState
        foreach (GameObject outpostObj in outpostObjs.Values)
        {
            OutpostUI outpostUI = outpostObj.GetComponent<OutpostUI>();
            if (outpostUI.isBuilding)
            {
                outpostUI.leftBuildTime -= battleTimeAt - outpostUI.lastUpdateTime;
                outpostUI.lastUpdateTime = battleTimeAt;
            }
        }

        //Update MovingUnitState
        foreach (GameObject movingUnitObj in movingUnitObjs.Values)
        {
            MovingUnitUI movingUnitUI = movingUnitObj.GetComponent<MovingUnitUI>();
            movingUnitUI.currentLength += (battleTimeAt - movingUnitUI.lastUpdateTime) * movingUnitUI.speed;
            movingUnitUI.lastUpdateTime = battleTimeAt;
            movingUnitUI.unitObj.transform.position = Vector3.Lerp(MovingUnitUI.LogicToUIPosition(movingUnitUI.startPosition), MovingUnitUI.LogicToUIPosition(movingUnitUI.endPosition), movingUnitUI.currentLength / movingUnitUI.totalLength);
        }
    }

    public GameObject ingameUINode;

    private List<string> toptipInWaiting = new List<string>();
    DateTime lastToptipTime = DateTime.Now;

    public void AddATopTip(string msg)
    {
        if (toptipInWaiting == null)
        {
            toptipInWaiting = new List<string>();
        }
        toptipInWaiting.Add(msg);
    }

    public void TryShowAToptip()
    {
        TimeSpan ts2 = DateTime.Now - lastToptipTime;
        if (ts2.TotalSeconds < 0.5f || toptipInWaiting == null || toptipInWaiting.Count == 0)
        { return; }
        lastToptipTime = DateTime.Now;
        string msg = toptipInWaiting[0];
        toptipInWaiting.RemoveAt(0);
        GameObject toptipObj = Instantiate(UIResourcesDepot.Instance.toptip);
        ToptipUI toptipUI = toptipObj.GetComponent<ToptipUI>();
        toptipUI.TextObj.GetComponent<Text>().text = msg;
        toptipObj.transform.SetParent(ingameUINode.transform);
        toptipObj.transform.localPosition = new Vector3(263f, 333f, 0f);
    }


    public void CreateAMapTip(string content,Vector2Int logicPosition)
    {
        //Debug.Log(content);
        GameObject mapTipObj=Instantiate(UIResourcesDepot.Instance.mapTip);
        mapTipObj.transform.SetParent(this.mapTipCanvasNode.transform);
        MapTip mapTip=mapTipObj.GetComponent<MapTip>();
        mapTip.textObj.GetComponent<Text>().text = content;
        mapTipObj.transform.position = MapTip.GetUIPostionByLogiclogicPosition(logicPosition);
        mapTip.backRect.transform.localScale = new Vector3(0.15f* content.Length, 0.15f , 1);
        //Debug.Log(content.Length);
    }

    private void CreateObjText(string id,string content,Vector2Int logicPosition)
    {
        GameObject objTextObj = Instantiate(UIResourcesDepot.Instance.ObjectText);
        objTextObj.transform.SetParent(this.ObjTextCanvasNode.transform);
        ObjectText objectText=objTextObj.GetComponent<ObjectText>();
        objectText.text.GetComponent<Text>().text = content;
        objTextObj.transform.position = ObjectText.GetUIPostionByLogiclogicPosition(logicPosition);
        objectText.backRect.transform.localScale = new Vector3(0.15f * content.Length, 0.15f, 1);
        if(objTexts.ContainsKey(id))
        {
            Destroy(objTexts[id]);
            objTexts.Remove(id);
        }
        objTexts.Add(id,objTextObj);
    }

    private void RemoveObjText(string id)
    {
        if (objTexts.ContainsKey(id))
        {
            Destroy(objTexts[id]);
            objTexts.Remove(id);
        }
    }

    public void ClickBackButton()
    {
        if (currentChoosingUI.GetType().ToString() == "MovingUnitUI")
        {
            MovingUnitUI movingUnitUI = (MovingUnitUI)currentChoosingUI;
            if (myFactionOrder == movingUnitUI.factionOrder)
            {
                MovingUnitMsg movingUnitMsg = new MovingUnitMsg
                {
                    updateWay = MovingUnitMsg.UpdateWay.CallBack,
                    id = movingUnitUI.id
                };
                TCPClient.Instance.SendMessageObject(MessageTypes.MovingUnitMsg, movingUnitMsg);
            }
        }
        if (currentChoosingUI.GetType().ToString() == "UnitUI")
        {
            UnitUI unitUI = (UnitUI)currentChoosingUI;
            if (myFactionOrder == unitUI.factionOrder)
            {
                UnitMsg unitMsg = new UnitMsg
                {
                    updateWay = UnitMsg.UpdateWay.CallBack,
                    id = unitUI.id
                };
                TCPClient.Instance.SendMessageObject(MessageTypes.UnitMsg, unitMsg);
            }
        }
    }

    private int rightClickActionDelayKey = 0;
    public void RightClickAction()
    {
        if (rightClickActionDelayKey > 0)
        {
            rightClickActionDelayKey--;
            return;
        }
        if (Input.GetMouseButtonDown(1) && Input.mousePosition.x > 200)
        {
            //Debug.Log("RightClickAction1");
            Vector2Int position = TileOfWorldPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));


            if (currentChoosingUI != null && currentChoosingUI.GetType().ToString() == "OutpostUI")
            {
                //Debug.Log("RightClickAction2");
                OutpostUI opUI = (OutpostUI)currentChoosingUI;

                if (opUI.factionOrder != myFactionOrder)
                {
                    return;
                }

                if (foggyUI.isUnderFoggy(position))
                {
                    CreateAMapTip("迷雾区域",position);
                    //Debug.Log("迷雾区域");
                    //AddATopTip("迷雾区域");
                    return;
                }
                //if (opUI.outTroopNum >= 2)
                //{
                //    CreateAMapTip("据点行军达到上限", position);
                //    //AddATopTip("据点行军达到上限");
                //    return;
                //}
                GameObject scrollbar = GameObject.Find("OutpostBuiltState_Scrollbar");
                if (scrollbar==null)
                {
                    Debug.Log("no scrollbar");
                    AddATopTip("no scrollbar error");
                    return;
                }
                if(scrollbar.GetComponent<Scrollbar>().value * opUI.unitNum <= 0)
                {
                    CreateAMapTip("据点行军数量需要大于0", position);
                    //AddATopTip("据点行军数量需要大于0");
                    return;
                }

                if (opUI.factionOrder == myFactionOrder)
                {
                    if (position == opUI.position)
                    {
                        return;
                    }

                    MovingUnitMsg movingUnitMsg = new MovingUnitMsg
                    {
                        updateWay = MovingUnitMsg.UpdateWay.Start,
                        belongToOutpostID = opUI.id,
                        UnitAmount = (int)(GameObject.Find("OutpostBuiltState_Scrollbar").GetComponent<Scrollbar>().value * opUI.unitNum),
                        endPositionX = position.x,
                        endPositionY = position.y
                    };
                    TCPClient.Instance.SendMessageObject(MessageTypes.MovingUnitMsg, movingUnitMsg);
                    //Debug.Log("RightClickAction3");
                    rightClickActionDelayKey = 20;
                }
            }
        }
    }

    private List<System.Object> TilePositionUIs(Vector2Int p)
    {
        List<System.Object> positionUIs = new List<System.Object>();
        if (warTowerObjs != null)
        {
            foreach (GameObject warTowerObj in warTowerObjs.Values)
            {
                WarTowerUI warTowerUI = warTowerObj.GetComponent<WarTowerUI>();
                if (warTowerUI.logicPosition == p)
                {
                    positionUIs.Add(warTowerUI);
                }
            }
        }

        if (outpostObjs != null)
        {
            foreach (GameObject outpostObj in outpostObjs.Values)
            {
                OutpostUI outpostUI = outpostObj.GetComponent<OutpostUI>();
                if (outpostUI.position == p)
                {
                    positionUIs.Add(outpostUI);
                }
            }
        }

        if (unitObjs != null)
        {
            foreach (GameObject unitObj in unitObjs.Values)
            {
                UnitUI unitUI = unitObj.GetComponent<UnitUI>();
                if (unitUI.position == p)
                {
                    positionUIs.Add(unitUI);
                }
            }
        }
        if(lightTowerUIs!=null)
        {
            foreach(LightTowerUI lightUI in lightTowerUIs.Values)
            {
                if(lightUI.position==p)
                {
                    positionUIs.Add(lightUI);
                }
            }

        }

        return positionUIs;
    }

    //private GameObject currentChoosingObj;
    private Vector2Int currentChoosingTile;
    private System.Object currentChoosingUI;
    private List<System.Object> currentChooingTileUIs;
    public GameObject leftDownUI;

    private void ChooseObjUpdate()
    {
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > 200)
        {
            Vector2Int position = TileOfWorldPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (currentChoosingUI == null)//no choosed
            {
                //currentChoosingTile = position;
                currentChooingTileUIs = TilePositionUIs(position);
                if (currentChooingTileUIs.Count > 0)//there is obj,then choose it
                {
                    currentChoosingTile = position;
                    ChooseObj(currentChooingTileUIs[0]);
                }
            }
            else if (currentChoosingTile != position)
            {//choose another tile
                UnchooseObj();
                currentChooingTileUIs = TilePositionUIs(position);
                if (currentChooingTileUIs.Count > 0)//there is obj,then choose it
                {
                    currentChoosingTile = position;
                    ChooseObj(currentChooingTileUIs[0]);
                }
                else
                {
                    currentChoosingUI = null;
                }
            }
            else if (currentChoosingTile == position)
            {//choose this tile,change to another obj in this tile
                System.Object lastChoosingUI = currentChoosingUI;
                UnchooseObj();
                currentChooingTileUIs = TilePositionUIs(position);
                if (currentChooingTileUIs.Count > 0)//there is obj,then choose it
                {
                    currentChoosingTile = position;
                    int index = currentChooingTileUIs.IndexOf(lastChoosingUI);
                    if (index == -1)
                    {
                        ChooseObj(currentChooingTileUIs[0]);
                    }
                    else
                    {
                        index = (index + 1) % currentChooingTileUIs.Count;
                        ChooseObj(currentChooingTileUIs[index]);
                    }

                }
            }
        }
        if (currentChoosingUI != null)
        {
            switch (currentChoosingUI.GetType().ToString())
            {
                case "OutpostUI":
                    UpdateChooseOutpostOutpostUIInfo((OutpostUI)currentChoosingUI);
                    break;
                case "WarTowerUI":
                    UpdateChooseWarTowerClickUIInfo((WarTowerUI)currentChoosingUI);
                    break;
                case "UnitUI":
                    UpdateUnitClickUIInfo((UnitUI)currentChoosingUI);
                    break;
                case "LightTowerUI":
                    UpdateLightTowerClickUIInfo((LightTowerUI)currentChoosingUI);
                    break;
                default:
                    break;
            }
        }
        else
        {
            //leftDownUI.SetActive(false);
        }
    }

    private void ChooseObj(System.Object o)
    {
        currentChoosingUI = o;
        switch (o.GetType().ToString())
        {
            case "OutpostUI":
                //Debug.Log("Choose an OutpostUI");
                outpostClickUI.SetActive(true);
                //if()
                break;
            case "WarTowerUI":
                warTowerClickUI.SetActive(true);
                break;
            case "UnitUI":
                UnitClickUI.SetActive(true);
                break;
            case "LightTowerUI":
                LightTowerClickUI.SetActive(true);
                break;
            default:
                break;
        }
    }

    public GameObject LightTowerClickUI;
    private void UpdateLightTowerClickUIInfo(LightTowerUI lightTowerUI)
    {
        if(lightTowerUI.factionState[myFactionOrder]==MDs.LightState.Light)
        {
            GameObject.Find("LightTowerClickUI_Text1").GetComponent<Text>().text = "点亮";
            GameObject.Find("LightTowerClickUI_Text2").GetComponent<Text>().text = "发光剩余时间:"+((int)(lightTowerUI.leftLightTime[myFactionOrder] / 3600000)).ToString() + ":" + ((int)((lightTowerUI.leftLightTime[myFactionOrder] % 3600000) / 60000)).ToString() + ":" + ((int)((lightTowerUI.leftLightTime[myFactionOrder] % 60000) / 1000)).ToString();
        }
        else
        {
            GameObject.Find("LightTowerClickUI_Text1").GetComponent<Text>().text = "未点亮";
            GameObject.Find("LightTowerClickUI_Text2").GetComponent<Text>().text = "";
        }

        if(lightTowerUI.coolDownTime[myFactionOrder]>0)
        {
            GameObject.Find("LightTowerClickUI_Text4").GetComponent<Text>().text ="下次点亮冷:"+((int)(lightTowerUI.coolDownTime[myFactionOrder] / 3600000)).ToString() + ":" + ((int)((lightTowerUI.coolDownTime[myFactionOrder] % 3600000) / 60000)).ToString() + ":" + ((int)((lightTowerUI.coolDownTime[myFactionOrder] % 60000) / 1000)).ToString();
        }
        else
        {
            GameObject.Find("LightTowerClickUI_Text4").GetComponent<Text>().text = "可点亮";
        }
    }

    public GameObject UnitClickUI;
    public GameObject UnitClickUIBackButton;

    private void UpdateUnitClickUIInfo(UnitUI unitUI)
    {
        //
        if (unitUI.factionOrder == 0)
        {
            GameObject.Find("UnitClickUI_NAME").GetComponent<Text>().text = "高积分野怪";
        }
        else
        {
            GameObject.Find("UnitClickUI_NAME").GetComponent<Text>().text = "玩家队伍";
        }

        GameObject.Find("UnitClickUI_Text1").GetComponent<Text>().text = "阵营:" + unitUI.factionOrder;
        GameObject.Find("UnitClickUI_Text2").GetComponent<Text>().text = "士兵数量:" + unitUI.UnitAmount;
        if (myFactionOrder == unitUI.factionOrder)
        {
            UnitClickUIBackButton.SetActive(true);
        }
        else
        {
            UnitClickUIBackButton.SetActive(false);
        }
    }

    public GameObject warTowerClickUI;

    private void UpdateChooseWarTowerClickUIInfo(WarTowerUI warTowerUI)
    {
        //warTowerClickUI.SetActive(true);
        GameObject.Find("WarTowerClickUI_Text1").GetComponent<Text>().text = "当前阵营:" + warTowerUI.FactionOrder;
        GameObject.Find("WarTowerClickUI_Text2").GetComponent<Text>().text = "分值:" + warTowerUI.towerValue;
        GameObject.Find("WarTowerClickUI_Text4").GetComponent<Text>().text = "等级:" + warTowerUI.towerLevel;


        switch (warTowerUI.towerState)
        {
            case MDs.State.Staying:
                int towerHaveNum = 0;
                foreach (GameObject warTowerObj in warTowerObjs.Values)
                {
                    WarTowerUI twarTowerUI = warTowerObj.GetComponent<WarTowerUI>();
                    if (twarTowerUI.FactionOrder == myFactionOrder)
                        towerHaveNum++;
                }
                int needTime = MySettings.TowerGettingTime(warTowerUI.towerLevel,towerHaveNum);
                GameObject.Find("WarTowerClickUI_Text3").GetComponent<Text>().text = "占领需要"+((int)(needTime / 3600000)).ToString() + ":" + ((int)((needTime % 3600000) / 60000)).ToString() + ":" + ((int)((needTime % 60000) / 1000)).ToString();
                break;
            case MDs.State.Losing:
                GameObject.Find("WarTowerClickUI_Text3").GetComponent<Text>().text = "丢失剩余:" + ((int)(warTowerUI.stateChangeLeftTime / 3600000)).ToString() + ":" + ((int)((warTowerUI.stateChangeLeftTime % 3600000) / 60000)).ToString() + ":" + ((int)((warTowerUI.stateChangeLeftTime % 60000) / 1000)).ToString();
                break;
            case MDs.State.Getting:
                GameObject.Find("WarTowerClickUI_Text3").GetComponent<Text>().text = "占领剩余:" + ((int)(warTowerUI.stateChangeLeftTime / 3600000)).ToString() + ":" + ((int)((warTowerUI.stateChangeLeftTime % 3600000) / 60000)).ToString() + ":" + ((int)((warTowerUI.stateChangeLeftTime % 60000) / 1000)).ToString();
                break;
            default:
                break;
        }
    }

    public GameObject outpostClickUI;
    public GameObject outposOutpostBuildingStatetClickUI;
    public GameObject outposOutpostBuiltStatetClickUI;
    public GameObject OutpostBuiltStateUI;
    private void UpdateChooseOutpostOutpostUIInfo(OutpostUI outpostUI)
    {
        if (!outpostObjs.ContainsValue(outpostUI.gameObject))
        {
            UnchooseObj();
            return;
        }

        if (outpostUI.isBuilding)
        {
            outposOutpostBuildingStatetClickUI.SetActive(true);
            outposOutpostBuiltStatetClickUI.SetActive(false);
            GameObject.Find("OutpostClickUI_Faction").GetComponent<Text>().text = "阵营:" + outpostUI.factionOrder;
            GameObject.Find("OutpostClickUI_HP").GetComponent<Text>().text = "血量:" + outpostUI.blood;
            outpostUI.leftBuildTime -= battleTimeAt - outpostUI.lastUpdateTime;
            outpostUI.lastUpdateTime = battleTimeAt;
            GameObject.Find("OutpostClickUI_BuildingLeft").GetComponent<Text>().text = "建造剩余:" + ((int)(outpostUI.leftBuildTime / 3600000)).ToString() + ":" + ((int)((outpostUI.leftBuildTime % 3600000) / 60000)).ToString() + ":" + ((int)((outpostUI.leftBuildTime % 60000) / 1000)).ToString();
        }
        else
        {
            outposOutpostBuildingStatetClickUI.SetActive(false);
            outposOutpostBuiltStatetClickUI.SetActive(true);
            GameObject.Find("OutpostClickUI_Faction").GetComponent<Text>().text = "阵营:" + outpostUI.factionOrder;
            GameObject.Find("OutpostClickUI_HP").GetComponent<Text>().text = "血量:" + outpostUI.blood;
            GameObject.Find("OutpostBuiltState_LeftUnitNum").GetComponent<Text>().text = "剩余士兵数量:" + outpostUI.unitNum;
            if (myFactionOrder == outpostUI.factionOrder)
            {
                OutpostBuiltStateUI.SetActive(true);
                GameObject.Find("OutpostBuiltState_OutTroopNum").GetComponent<Text>().text = "已出发队列数量:" + outpostUI.outTroopNum;
                GameObject.Find("OutpostBuiltState_ChoosedUnitNum").GetComponent<Text>().text = "已选士兵数量:" + (int)(GameObject.Find("OutpostBuiltState_Scrollbar").GetComponent<Scrollbar>().value * outpostUI.unitNum);
            }
            else
            {
                OutpostBuiltStateUI.SetActive(false);
            }
        }
    }

    private void UnchooseObj()
    {
        //Debug.Log("unchoose");
        switch (currentChoosingUI.GetType().ToString())
        {
            case "OutpostUI":
                //Debug.Log("Choose an OutpostUI");
                OutpostUI outpostUI = (OutpostUI)currentChoosingUI;
                outpostClickUI.SetActive(false);
                //if()
                break;
            case "WarTowerUI":
                WarTowerUI warTowerUI = (WarTowerUI)currentChoosingUI;
                warTowerClickUI.SetActive(false);
                break;
            case "UnitUI":
                UnitClickUI.SetActive(false);
                break;
            case "LightTowerUI":
                LightTowerClickUI.SetActive(false);
                break;
            default:
                break;
        }
        currentChoosingUI = null;

    }

    private void TipSquareUpdate()
    {
        if (tipSquare != null)
        {
            Transform bt = tipSquare.GetComponent<Transform>();
            bt.position = TileMidWorldPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            if (tipSquare.activeSelf && (bt.position.x > this.mapSizeX || bt.position.x < 0 || bt.position.y > this.mapSizeY || bt.position.y < 0))
            {
                tipSquare.SetActive(false);
            }
            else if (!tipSquare.activeSelf && (bt.position.x <= this.mapSizeX && bt.position.x >= 0 && bt.position.y <= this.mapSizeY && bt.position.y >= 0))
            {
                tipSquare.SetActive(true);
            }

            //Debug.Log("tip at :"+TileMidWorldPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
        }
    }

    private Vector2Int WorldToLogicPosition(Vector3 worldPosition)
    {
        return new Vector2Int((int)worldPosition.x, (int)worldPosition.y);
    }

    private Vector3 TileMidWorldPosition(Vector3 worldPosition)
    {
        return new Vector3((int)worldPosition.x + 0.5f, (int)worldPosition.y + 0.5f, 0f);
    }

    private Vector2Int TileOfWorldPosition(Vector3 worldPosition)
    {
        return new Vector2Int((int)worldPosition.x, (int)worldPosition.y);
    }

    public void UpdateMovingUnit(MovingUnitMsg msg)
    {
        if (msg.updateWay == MovingUnitMsg.UpdateWay.Update)
        {
            if (this.movingUnitObjs == null)
            {
                this.movingUnitObjs = new Dictionary<string, GameObject>();
            }
            if (this.movingUnitObjs.ContainsKey(msg.id))//if exist destroy
            {
                Destroy(this.movingUnitObjs[msg.id]);
                movingUnitObjs.Remove(msg.id);
            }

            GameObject movingUnitObj = Instantiate(UIResourcesDepot.Instance.movingUnits[msg.factionOrder]);

            MovingUnitUI movingUnitUI = movingUnitObj.GetComponent<MovingUnitUI>();

            movingUnitUI.id = msg.id;
            movingUnitUI.belongToOutpostID = msg.belongToOutpostID;
            movingUnitUI.UnitAmount = msg.UnitAmount;
            movingUnitUI.startPosition = new Vector2Int(msg.startPositionX, msg.startPositionY);
            movingUnitUI.endPosition = new Vector2Int(msg.endPositionX, msg.endPositionY);
            movingUnitUI.totalLength = msg.totalLength;
            movingUnitUI.currentLength = msg.currentLength;
            movingUnitUI.speed = msg.speed;
            movingUnitUI.factionOrder = msg.factionOrder;
            movingUnitUI.lastUpdateTime = msg.lastUpdateTime;

            //Debug.Log("UpdateMovingUnit:startPosition=" + movingUnitUI.startPosition+";movingUnitUI.endPositio"+movingUnitUI.endPosition);

            movingUnitObj.transform.SetParent(movingUnitsNode.transform);
            movingUnitObj.transform.position = Vector3.zero;
            //movingUnitObj.transform.position = Vector3.zero;
            //draw unit
            movingUnitUI.unitObj.transform.position = Vector3.Lerp(MovingUnitUI.LogicToUIPosition(movingUnitUI.startPosition), MovingUnitUI.LogicToUIPosition(movingUnitUI.endPosition), movingUnitUI.currentLength / movingUnitUI.totalLength);
            movingUnitUI.unitObj.transform.localScale = Vector3.one;
            SpriteRenderer unitSpriteRenderer = movingUnitUI.unitObj.GetComponent<SpriteRenderer>();
            unitSpriteRenderer.sortingOrder = LayerOrder.UnitLayer;

            //draw line

            Vector3 linkVector = MovingUnitUI.LogicToUIPosition(movingUnitUI.endPosition) - MovingUnitUI.LogicToUIPosition(movingUnitUI.startPosition);
            movingUnitUI.routeObj.transform.localScale = new Vector3(0.1f, Mathf.Sqrt(linkVector.x * linkVector.x + linkVector.y * linkVector.y), 1.0f);
            movingUnitUI.routeObj.transform.position = (MovingUnitUI.LogicToUIPosition(movingUnitUI.endPosition) + MovingUnitUI.LogicToUIPosition(movingUnitUI.startPosition)) / 2f;
            float rotateAngle = Vector3.Angle(new Vector3(0, 1, 0), linkVector);
            if (linkVector.x > 0.001f || linkVector.x < -0.001f)
            {
                rotateAngle *= -Mathf.Abs(linkVector.x) / linkVector.x;
            }
            movingUnitUI.routeObj.transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotateAngle));

            SpriteRenderer lineSpriteRenderer = movingUnitUI.routeObj.GetComponent<SpriteRenderer>();
            lineSpriteRenderer.sortingOrder = LayerOrder.TowerLinksLayer;



            movingUnitObjs.Add(msg.id, movingUnitObj);
        }
        else if (msg.updateWay == MovingUnitMsg.UpdateWay.Destory)
        {
            if (movingUnitObjs.ContainsKey(msg.id))//if exist destroy
            {
                Destroy(movingUnitObjs[msg.id]);
                movingUnitObjs.Remove(msg.id);
            }
        }
    }

    public void LightTowerUpdate(LightTowerMsg msg)
    {
        if (lightTowerUIs.ContainsKey(msg.id))
        {
            LightTowerUI oldLightTowerUI = lightTowerUIs[msg.id];
            foggyUI.RemoveCanSeeZone(oldLightTowerUI.position.x, oldLightTowerUI.position.y, oldLightTowerUI.position.x, oldLightTowerUI.position.y);
            if(oldLightTowerUI.factionState[myFactionOrder]==MDs.LightState.Light)
            {
                foggyUI.RemoveCanSeeZone(oldLightTowerUI.position.x - oldLightTowerUI.lightRadius,
                                     oldLightTowerUI.position.y - oldLightTowerUI.lightRadius,
                                     oldLightTowerUI.position.x + oldLightTowerUI.lightRadius,
                                     oldLightTowerUI.position.y + oldLightTowerUI.lightRadius);
            }
            Destroy(lightTowerUIs[msg.id].gameObject);
            lightTowerUIs.Remove(msg.id);
        }

        GameObject lightTowerObj;
        if (msg.factionState[myFactionOrder] == MDs.LightState.Light)
        {
            lightTowerObj = Instantiate(UIResourcesDepot.Instance.lightTowerLight);
        }
        else
        {
            lightTowerObj = Instantiate(UIResourcesDepot.Instance.lightTowerDark);
        }
        lightTowerObj.AddComponent<LightTowerUI>();
        LightTowerUI lightTowerUI = lightTowerObj.GetComponent<LightTowerUI>();
        lightTowerUI.id = msg.id;
        lightTowerUI.position = new Vector2Int(msg.positionX, msg.positionY);
        lightTowerUI.factionState = msg.factionState;
        lightTowerUI.coolDownTime = msg.coolDownTime;
        lightTowerUI.leftLightTime = msg.leftLightTime;
        lightTowerUI.lightRadius = msg.lightRadius;
        lightTowerUI.lastUpdateTime = msg.lastUpdateTime;

        lightTowerObj.transform.position = lightTowerUI.GetUIPosition();
        lightTowerObj.transform.localScale = Vector3.one;
        lightTowerObj.transform.SetParent(lightTowerNode.transform);
        SpriteRenderer spriteRenderer = lightTowerObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.TowerLayer;

        foggyUI.CreateCanSeeZone(lightTowerUI.position.x, lightTowerUI.position.y, lightTowerUI.position.x, lightTowerUI.position.y);
        if(lightTowerUI.factionState[myFactionOrder]==MDs.LightState.Light)
        {
            foggyUI.CreateCanSeeZone(lightTowerUI.position.x - lightTowerUI.lightRadius,
                                     lightTowerUI.position.y - lightTowerUI.lightRadius,
                                     lightTowerUI.position.x + lightTowerUI.lightRadius,
                                     lightTowerUI.position.y + lightTowerUI.lightRadius);
        }
        lightTowerUIs.Add(lightTowerUI.GetID(),lightTowerUI);
    }

    public void UnitUpdate(UnitMsg msg)
    {
        if (msg.updateWay == UnitMsg.UpdateWay.Update)
        {
            if (unitObjs == null)
            {
                unitObjs = new Dictionary<string, GameObject>();
            }
            if (unitObjs.ContainsKey(msg.id))//if exist destroy
            {
                Destroy(unitObjs[msg.id]);
                unitObjs.Remove(msg.id);
            }
            //Debug.Log("msg.factionOrder:"+msg.factionOrder);
            GameObject unitObj = Instantiate(UIResourcesDepot.Instance.units[msg.factionOrder]);
            unitObj.AddComponent<UnitUI>();

            UnitUI unitUi = unitObj.GetComponent<UnitUI>();

            unitUi.id = msg.id;
            unitUi.position = new Vector2Int(msg.positionX, msg.positionY);
            unitUi.belongToOutpostID = msg.belongToOutpostID;
            unitUi.UnitAmount = msg.UnitAmount;
            unitUi.factionOrder = msg.factionOrder;

            Transform tf = unitUi.GetComponent<Transform>();
            tf.SetParent(unitsNode.GetComponent<Transform>());
            tf.position = unitUi.GetUIPosition();
            tf.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = unitObj.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = LayerOrder.UnitLayer;

            unitObjs.Add(msg.id, unitObj);
            CreateObjText(msg.id, "士兵:" + unitUi.UnitAmount,unitUi.position);

        }
        else if (msg.updateWay == UnitMsg.UpdateWay.Destory)
        {
            if (unitObjs.ContainsKey(msg.id))//if exist destroy
            {
                Destroy(unitObjs[msg.id]);
                unitObjs.Remove(msg.id);
                RemoveObjText(msg.id);
            }
        }
    }

    public void UpdateOutpost(OutpostMsg msg)
    {
        if (msg.msgType == OutpostMsg.MsgType.Update)
        {
            if (outpostObjs == null)
            {
                outpostObjs = new Dictionary<string, GameObject>();
            }
            if (outpostObjs.ContainsKey(msg.outpostID))//if exist destroy
            {
                Destroy(outpostObjs[msg.outpostID]);
                outpostObjs.Remove(msg.outpostID);
            }
            else{
                if (msg.factionOrder == myFactionOrder)
                {
                    foggyUI.CreateCanSeeZone(msg.positionX - MySettings.outpostSeeDist,
                                             msg.positionY - MySettings.outpostSeeDist,
                                             msg.positionX + MySettings.outpostSeeDist,
                                             msg.positionY + MySettings.outpostSeeDist);
                }
            }

            GameObject outpostObj = Instantiate(UIResourcesDepot.Instance.outpost[msg.factionOrder]);
            outpostObj.AddComponent<OutpostUI>();

            OutpostUI outpostUI = outpostObj.GetComponent<OutpostUI>();

            outpostUI.id = msg.outpostID;
            outpostUI.isBuilding = msg.isBuilding;
            outpostUI.leftBuildTime = msg.leftBuildTime;
            outpostUI.lastUpdateTime = battleTimeAt;
            outpostUI.blood = msg.blood;
            outpostUI.unitNum = msg.unitNum;
            outpostUI.outTroopNum = msg.outTroopNum;
            outpostUI.position = new Vector2Int(msg.positionX, msg.positionY);
            outpostUI.factionOrder = msg.factionOrder;

            Transform tf = outpostObj.GetComponent<Transform>();
            tf.SetParent(outpostNode.GetComponent<Transform>());
            tf.position = outpostUI.GetUIPosition();
            tf.localScale = Vector3.one * Mathf.Max(1, (float)Math.Sqrt(msg.unitNum / MySettings.sizeOutpostValue));

            SpriteRenderer spriteRenderer = outpostObj.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = LayerOrder.OutpostLayer;

            outpostObjs.Add(msg.outpostID, outpostObj);

            CreateObjText(msg.outpostID, "士兵:" + outpostUI.unitNum+" 行军:"+outpostUI.outTroopNum, outpostUI.position);

        }
        else if (msg.msgType == OutpostMsg.MsgType.Destory)
        {
            
            if (outpostObjs.ContainsKey(msg.outpostID))//if exist destroy
            {
                OutpostUI outpostUI = outpostObjs[msg.outpostID].GetComponent<OutpostUI>();
                if (outpostUI.factionOrder == myFactionOrder)
                {
                    foggyUI.RemoveCanSeeZone(outpostUI.position.x - MySettings.outpostSeeDist,
                                             outpostUI.position.y - MySettings.outpostSeeDist,
                                             outpostUI.position.x + MySettings.outpostSeeDist,
                                             outpostUI.position.y + MySettings.outpostSeeDist);
                }
                Destroy(outpostObjs[msg.outpostID]);
                outpostObjs.Remove(msg.outpostID);

                RemoveObjText(msg.outpostID);
            }

        }
    }

    public void UpdateMap(MapMsg mapMsg)
    {
        this.mapSizeX = mapMsg.mapSizeX;
        this.mapSizeY = mapMsg.mapSizeY;
        //create map
        Destroy(backgroundObj);
        backgroundObj = Instantiate(UIResourcesDepot.Instance.mapBackground);
        Transform bt = backgroundObj.transform;
        bt.SetParent(mapNode.transform);
        bt.position = new Vector3(mapMsg.mapSizeX / 2, mapMsg.mapSizeY / 2, 0);
        bt.localScale = new Vector3(mapMsg.mapSizeX, mapMsg.mapSizeY);

        SpriteRenderer spriteRenderer = backgroundObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.MapObjsLayer;
        //Debug.Log("map created");

        foggyUI.InitialFoggy(mapMsg.mapSizeX, mapMsg.mapSizeY);
        //foggyUI.CreateCanSeeZone(10,10,20,20);
        //foggyUI.CreateCanSeeZone(50, 50, 60, 60);
        //foggyUI.RemoveCanSeeZone(50, 50, 55, 55);

        AddATopTip("成功进入战场");
    }

    public void CreateTipSquare(int myFactionOrder)
    {
        //create map tip
        if (tipSquare != null)
        {
            Destroy(tipSquare);
        }
        tipSquare = Instantiate(UIResourcesDepot.Instance.tiles[myFactionOrder]);
        Transform bt = tipSquare.GetComponent<Transform>();
        bt.SetParent(mapNode.GetComponent<Transform>());
        bt.position = TileMidWorldPosition(Input.mousePosition);
        bt.localScale = new Vector3(1f, 1f, 1f);
        SpriteRenderer spriteRenderer = tipSquare.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.MapTipLayer;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
    }



    public void UpdateAWarTower(WarTowerMsg warTowerMsg)
    {
        if (warTowerObjs.ContainsKey(warTowerMsg.towerID))//Update the info
        {
            WarTowerUI oldWarTowerUI = warTowerObjs[warTowerMsg.towerID].GetComponent<WarTowerUI>();
            if(oldWarTowerUI.FactionOrder==myFactionOrder&&warTowerMsg.FactionOrder!=myFactionOrder)
            {
                foggyUI.RemoveCanSeeZone(warTowerMsg.logicPositionX - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionX + MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY + MySettings.warTowerSeeDist);
            }
            if (oldWarTowerUI.FactionOrder!= myFactionOrder&&warTowerMsg.FactionOrder==myFactionOrder)
            {
                foggyUI.CreateCanSeeZone(warTowerMsg.logicPositionX - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionX + MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY + MySettings.warTowerSeeDist);
            }
            //Debug.Log("update:" + warTowerObjs);
            Destroy(warTowerObjs[warTowerMsg.towerID].gameObject);
            warTowerObjs.Remove(warTowerMsg.towerID);
        }
        else{
            if (warTowerMsg.FactionOrder == myFactionOrder)
            {
                foggyUI.CreateCanSeeZone(warTowerMsg.logicPositionX - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY - MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionX + MySettings.warTowerSeeDist,
                                         warTowerMsg.logicPositionY + MySettings.warTowerSeeDist);
            }
        }

        GameObject warTowerObj;
        if (warTowerMsg.isLifeTower)
        {
            warTowerObj = Instantiate(UIResourcesDepot.Instance.lifeWarTower[warTowerMsg.FactionOrder]);
        }
        else
        {
            warTowerObj = Instantiate(UIResourcesDepot.Instance.warTowers[warTowerMsg.FactionOrder]);
        }
        warTowerObj.AddComponent<WarTowerUI>();

        WarTowerUI warTowerUI = warTowerObj.GetComponent<WarTowerUI>();
        warTowerUI.id = warTowerMsg.towerID;
        warTowerUI.logicPosition = new Vector2Int(warTowerMsg.logicPositionX, warTowerMsg.logicPositionY);
        warTowerUI.isLifeTower = warTowerMsg.isLifeTower;
        warTowerUI.isBrithTower = warTowerMsg.isBrithTower;
        warTowerUI.FactionOrder = warTowerMsg.FactionOrder;
        warTowerUI.towerValue = warTowerMsg.towerValue;
        warTowerUI.towerState = warTowerMsg.towerState;
        warTowerUI.lastUpdateTime = warTowerMsg.lastUpdateTime;
        warTowerUI.stateChangeLeftTime = warTowerMsg.stateChangeLeftTime;
        warTowerUI.towerLevel = warTowerMsg.towerLevel;

        Transform tf = warTowerObj.transform;
        tf.SetParent(warTowersNode.GetComponent<Transform>());
        tf.position = WarTowerUI.LogicToUIPosition(new Vector2Int(warTowerMsg.logicPositionX, warTowerMsg.logicPositionY));
        tf.localScale = Vector3.one * Mathf.Max(1, Mathf.Sqrt(warTowerMsg.towerValue / MySettings.sizeTowerValue));

        tf.localScale = Vector3.one;
        if (warTowerMsg.isBrithTower)
        {
            tf.localScale *= 1.5f;
        }
        if (warTowerMsg.isLifeTower)
        {
            tf.localScale *= 1.5f;
        }


        SpriteRenderer spriteRenderer = warTowerObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.TowerLayer;

        warTowerObjs.Add(warTowerMsg.towerID, warTowerObj);
        CreateObjText(warTowerMsg.towerID, "等级" + warTowerUI.towerLevel, warTowerUI.logicPosition);

        foreach (string linkedTowerID in warTowerMsg.linkedTowerIDs)
        {
            if (warTowerObjs.ContainsKey(linkedTowerID))
            {
                UpdateATowerLink(warTowerUI.id, linkedTowerID);
            }
        }

        if(warTowerUI.isLifeTower)
        {
            UpdateLifeTowerSafeZone(warTowerUI);
        }
    }

    private void UpdateLifeTowerSafeZone(WarTowerUI warTowerUI)
    {
        //private Dictionary<string, GameObject> warTowerSafeZoneObjs = new Dictionary<string, GameObject>();

        GameObject warTowerSafeZoneObj;
        if (!warTowerUI.isLifeTower)
        {
            return;
        }
        if(warTowerSafeZoneObjs.ContainsKey(warTowerUI.id))//if exist,destroy it
        {
            warTowerSafeZoneObj = warTowerSafeZoneObjs[warTowerUI.id];
            Destroy(warTowerSafeZoneObj);
            warTowerSafeZoneObjs.Remove(warTowerUI.id);
        }
        if(warTowerUI.FactionOrder==0)//do not show safezone for no owner
        {
            return;
        }
        //create new
        warTowerSafeZoneObj=Instantiate(UIResourcesDepot.Instance.tiles[warTowerUI.FactionOrder]);
        warTowerSafeZoneObj.transform.position = warTowerUI.GetUIPosition();
        warTowerSafeZoneObj.transform.localScale = new Vector3(3f, 3f, 1);
        warTowerSafeZoneObj.transform.SetParent(warTowersNode.transform);

        SpriteRenderer spriteRenderer = warTowerSafeZoneObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.TowerSafeZoneLayer;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);

        warTowerSafeZoneObjs.Add(warTowerUI.id, warTowerSafeZoneObj);
    }

    private void UpdateATowerLink(string warTowerID, string warTowerID2)
    {
        GameObject linkedWarTower = warTowerObjs[warTowerID2];
        GameObject warTowerObj = warTowerObjs[warTowerID];

        if (linkedWarTower == null || warTowerObj == null)
        {
            Debug.Log("UpdateATowerLink Error:Cannot find GameObject linkedWarTower or GameObject warTowerObj by warTowerID=" + warTowerID + " and warTowerID2=" + warTowerID2);
        }

        WarTowerUI linkedWarTowerUI = linkedWarTower.GetComponent<WarTowerUI>();
        WarTowerUI warTowerUI = warTowerObj.GetComponent<WarTowerUI>();

        string warTowerLinkID = GetWarTowerLinkID(warTowerID, warTowerID2);

        GameObject linkObj;
        if (warTowerLinksObjs.ContainsKey(warTowerLinkID))//if exist,simplly remove it
        {
            linkObj = warTowerLinksObjs[warTowerLinkID];
            warTowerLinksObjs.Remove(warTowerLinkID);
            Destroy(linkObj);
        }

        linkObj = Instantiate(UIResourcesDepot.Instance.tiles[linkedWarTowerUI.FactionOrder == warTowerUI.FactionOrder ? warTowerUI.FactionOrder : 0]);
        Transform linkTf = linkObj.GetComponent<Transform>();
        Vector3 linkVector = warTowerUI.GetUIPosition() - linkedWarTowerUI.GetUIPosition();
        linkTf.localScale = new Vector3(0.3f, Mathf.Sqrt(linkVector.x * linkVector.x + linkVector.y * linkVector.y), 1.0f);
        linkTf.localPosition = (warTowerUI.GetUIPosition() + linkedWarTowerUI.GetUIPosition()) / 2;
        float rotateAngle = Vector3.Angle(new Vector3(0, 1, 0), linkVector);
        if (linkVector.x > 0.001f || linkVector.x < -0.001f)
        {
            rotateAngle *= -Mathf.Abs(linkVector.x) / linkVector.x;
        }
        linkTf.rotation = Quaternion.Euler(new Vector3(0, 0, rotateAngle));
        linkTf.SetParent(warTowerLinksNode.GetComponent<Transform>());
        linkObj.name = "Link_" + GetWarTowerLinkID(warTowerUI.id, linkedWarTowerUI.id);

        SpriteRenderer spriteRenderer = linkObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.TowerLinksLayer;

        warTowerLinksObjs.Add(warTowerLinkID, linkObj);

    }

    private string GetWarTowerLinkID(string taid, string tbid)
    {

        if (string.Compare(taid, tbid) > 0)
        {
            return taid + "_to_" + tbid;
        }
        else
        {
            return tbid + "_to_" + taid;
        }
    }

    public void CameraUpdate()
    {
        Transform ct = cameraObject.GetComponent<Transform>();
        Camera c = cameraObject.GetComponent<Camera>();
        c.orthographicSize *= (float)System.Math.Pow(1f - cameraControlRate * 4f, Input.GetAxis("Mouse ScrollWheel"));

        if (Input.GetKey(KeyCode.Q))
        {
            c.orthographicSize = c.orthographicSize * (1 - cameraControlRate);
        }

        if (Input.GetKey(KeyCode.E))
        {
            c.orthographicSize = c.orthographicSize / (1 - cameraControlRate);
        }

        if (Input.GetKey(KeyCode.W))
        {
            ct.position = new Vector3(ct.position.x, ct.position.y + c.orthographicSize * cameraControlRate, ct.position.z);
        }

        if (Input.GetKey(KeyCode.S))
        {
            ct.position = new Vector3(ct.position.x, ct.position.y - c.orthographicSize * cameraControlRate, ct.position.z);
        }

        if (Input.GetKey(KeyCode.A))
        {
            ct.position = new Vector3(ct.position.x - c.orthographicSize * cameraControlRate, ct.position.y, ct.position.z);
        }

        if (Input.GetKey(KeyCode.D))
        {
            ct.position = new Vector3(ct.position.x + c.orthographicSize * cameraControlRate, ct.position.y, ct.position.z);
        }

    }



    public void CleanAll()
    {

    }

    public void AskToChooseServer()
    {
        CleanAll();
        CSDialog.SetActive(true);
    }

    public void ClickAsHost()
    {
        Debug.Log("Clicked As Server");
        BattleBasicSetting basicSetting = new BattleBasicSetting
        {
            mapSizeX = int.Parse(GameObject.Find("MapSizeXText").GetComponent<Text>().text),
            mapSizeY = int.Parse(GameObject.Find("MapSizeXText").GetComponent<Text>().text),
            warTowerXnum = int.Parse(GameObject.Find("TowerNumXText").GetComponent<Text>().text),
            warTowerYnum = int.Parse(GameObject.Find("TowerNumYText").GetComponent<Text>().text),
            lightHouseXnum = int.Parse(GameObject.Find("LightNumXText").GetComponent<Text>().text),
            lightHouseYnum = int.Parse(GameObject.Find("LightNumYText").GetComponent<Text>().text),
            PlayerNum = int.Parse(GameObject.Find("CampNumText").GetComponent<Text>().text)
        };
        myFactionOrder = int.Parse(GameObject.Find("HostAsCampText").GetComponent<Text>().text);

        this.CSDialog.SetActive(false);
        DemoManager.Instance.HostInitial(basicSetting, myFactionOrder);
    }

    public void ClickAsClient()
    {
        string hostIP = GameObject.Find("ServerIPText").GetComponent<Text>().text;
        myFactionOrder = int.Parse(GameObject.Find("ClientAsCampText").GetComponent<Text>().text);
        DemoManager.Instance.PureClientInitial(hostIP, myFactionOrder);
        this.CSDialog.SetActive(false);
        GameObject.Find("ServerIPTextShow").GetComponent<Text>().text = "服务器IP:" + hostIP;
        GameObject.Find("SpeedRateForServer").SetActive(false);
        GameObject.Find("ConfirmSpeedButton").SetActive(false);
        //Debug.Log("Clicked As Client");
        //this.demoManager.serverIP = GameObject.Find("ServerPortText").GetComponent<Text>().text;
        //this.demoManager.serverPort = int.Parse(GameObject.Find("ServerPortText").GetComponent<Text>().text);
        //this.demoManager.myPort = int.Parse(GameObject.Find("ClientPortText").GetComponent<Text>().text);
        //this.demoManager.order = int.Parse(GameObject.Find("ClientAsCampText").GetComponent<Text>().text);
        //this.CSDialog.SetActive(false);
        //this.demoManager.CSMode = DemoDefinations.CSModePureClient;
        //this.demoManager. DemoDefinations.DemoStateClientInitial;
    }

    public void ShowInGameUI()
    {
        ingameUI.SetActive(true);
        if (DemoManager.Instance.csMode == DemoManager.CSMode.Host)
        {
            GameObject.Find("SpeedRateForServer").SetActive(true);
            GameObject.Find("ConfirmSpeedButton").SetActive(true);
        }
        else
        {
            GameObject.Find("SpeedRateForServer").SetActive(false);
            GameObject.Find("ConfirmSpeedButton").SetActive(false);
        }
    }

    public void ClickConfirmSpeedButton()
    {
        //Debug.Log("22222");
        if (DemoManager.Instance.demoState == DemoManager.DemoRunningAt.Playing)
        {
            ServerManager.Instance.battlefeild.battleSpeedRate = int.Parse(GameObject.Find("SpeedRateText").GetComponent<Text>().text);
            //Debug.Log("33333");
            ServerManager.Instance.BroadcastTimeAndSpeed();
        }
    }

    public void SetServerIPText(string ipStr)
    {
        GameObject.Find("ServerIPTextShow").GetComponent<Text>().text = "服务器局域网IP:" + ipStr;
    }

    public void SetPingText(int ping)
    {
        GameObject.Find("PingText").GetComponent<Text>().text = "延迟: " + ping + " ms";
    }

    private string MillisecondToString(int millisecond)
    {
        //Debug.Log("Time is "+millisecond);
        string s = ((int)(millisecond / 3600000)).ToString() + ":" + ((int)((millisecond % 3600000) / 60000)).ToString() + ":" + ((int)((millisecond % 60000) / 1000)).ToString() + ":" + ((int)((millisecond % 1000))).ToString();
        //Debug.Log("sTime is :" + s);
        return s;
    }

    public void SetTimeText(int millisecond)
    {
        //Debug.Log("TimeTextobject:"+GameObject.Find("TimeText"));
        GameObject.Find("TimeText").GetComponent<Text>().text = "战场时间:" + MillisecondToString(millisecond);
    }

    public void SetSpeedText(int speedRate)
    {
        if (DemoManager.Instance.csMode == DemoManager.CSMode.Host)
        {
            GameObject.Find("SpeedText").GetComponent<Text>().text = "速度:";
            GameObject.Find("SpeedRateText").GetComponent<Text>().text = speedRate.ToString();
        }
        if (DemoManager.Instance.csMode == DemoManager.CSMode.PureClient)
        {
            GameObject.Find("SpeedText").GetComponent<Text>().text = "速度:" + speedRate;
        }

    }

    public void SetFactionText(FactionInfo factionInfo)
    {
        switch (factionInfo.factionOrder)
        {
            case 1:
                GameObject.Find("Faction1ScoreText").GetComponent<Text>().text = "阵营1总分:" + ((int)factionInfo.factionScore).ToString();
                GameObject.Find("Faction1TowerScore").GetComponent<Text>().text = "战塔产值:" + ((int)factionInfo.factionTowerScore).ToString();
                break;
            case 2:
                GameObject.Find("Faction2ScoreText").GetComponent<Text>().text = "阵营2总分:" + ((int)factionInfo.factionScore).ToString();
                GameObject.Find("Faction2TowerScore").GetComponent<Text>().text = "战塔产值:" + ((int)factionInfo.factionTowerScore).ToString();
                break;
            case 3:
                GameObject.Find("Faction3ScoreText").GetComponent<Text>().text = "阵营3总分:" + ((int)factionInfo.factionScore).ToString();
                GameObject.Find("Faction3TowerScore").GetComponent<Text>().text = "战塔产值:" + ((int)factionInfo.factionTowerScore).ToString();
                break;
            case 4:
                GameObject.Find("Faction4ScoreText").GetComponent<Text>().text = "阵营4总分:" + ((int)factionInfo.factionScore).ToString();
                GameObject.Find("Faction4TowerScore").GetComponent<Text>().text = "战塔产值:" + ((int)factionInfo.factionTowerScore).ToString();
                break;
            default:
                break;
        }
    }
}
