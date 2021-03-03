using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using static UnityEngine.UI.Dropdown;

public class PlayerManager : MonoBehaviour
{
    public GameObject playGroundNode;
    public GameObject cameraObject;
    public GameObject playingCanvas;
    public GameObject settingCanvas;
    public Scrollbar playScrollbar;
    public Dropdown serverChooser;

    public float cameraControlRate = 0.05f;

    enum PlayerState { Setting, Playing};

    PlayerState currentPlayerState;
    float playTimeAt;
    float lastPlayTimeAt;
    int logAt;
    float playSpeed=1f;

    GameObject mapObj;

    Dictionary<string,ABattleLog> logData;//use serverID as key.So require dataset without muti battle on a server
    Dictionary<string, Abuilding> buildingData;
    Dictionary<string, GameObject> LinkObjs;

    void LoadData()
    {
        Debug.Log("Start Read Files");
        string settingCSVPath = GameObject.Find("Setting_CSV_Path_text").GetComponent<Text>().text;
        string recordCSVPath = GameObject.Find("Setting_CSV_Path_Text").GetComponent<Text>().text;
        Debug.Log(recordCSVPath);
        CsvReader buildingCSV = new CsvReader();
        CsvReader recordCSV = new CsvReader();
        buildingCSV.ReadCSVWithHeadLine("/Users/pangruitao/Documents/Program_Files/Unity Project/battlefield_demo/PlayerData/mine_island_building.csv");
        recordCSV.ReadCSVWithHeadLine("/Users/pangruitao/Documents/Program_Files/Unity Project/battlefield_demo/PlayerData/mine_battle_log_0323.csv");

        Debug.Log("Start Deal building Data");
        buildingData = new Dictionary<string, Abuilding>();
        for (int i = 0; i < buildingCSV.rowNum;i++)
        {
            if (buildingCSV.data[i, 3] == "lighttower")
                continue;
            Abuilding abuilding = new Abuilding();
            abuilding.key = buildingCSV.data[i, 0];
            if(buildingCSV.data[i, 3]=="maincamp")
            {
                abuilding.camp = int.Parse(buildingCSV.data[i, 4]);
                abuilding.state = 2;
                abuilding.isMain = true;
            }else{
                abuilding.camp = 0;
                abuilding.state = 0;
                abuilding.isMain = false;
            }
            string[] ps = buildingCSV.data[i, 2].Split(',');
            abuilding.position_x = int.Parse(ps[2]);
            abuilding.position_y = int.Parse(ps[3]);
            abuilding.linkedBuildingIDs = new List<string>();
            string[] linkedIDs = buildingCSV.data[i, 5].Replace("{", "").Replace("}","").Split(',');
            for (int i2 = 0; i2 < linkedIDs.Length;i2++)
            {
                abuilding.linkedBuildingIDs.Add(linkedIDs[i2]);
                //Debug.Log(abuilding.key+" link to "+linkedIDs[i2]);
            }
            buildingData.Add(abuilding.key, abuilding);
        }

        Debug.Log("Start Deal record Data");
        logData = new Dictionary<string, ABattleLog>();
        string currentServer = "";
        ABattleLog currentBattleLog = null;
        for (int i = 0; i < recordCSV.rowNum;i++)
        {
            if(recordCSV.data[i,1]!=currentServer)
            {
                if(currentBattleLog!=null)
                {
                    logData.Add(currentBattleLog.serverID, currentBattleLog);
                }
                currentBattleLog = new ABattleLog
                {
                    serverID = recordCSV.data[i, 1],
                    battleStartTime = int.Parse(recordCSV.data[i, 0]),
                    buildingLogs = new List<ABuildingLog>()
                };
                currentServer = recordCSV.data[i, 1];
            }
            ABuildingLog aBuildingLog = new ABuildingLog();
            aBuildingLog.timeAt = int.Parse(recordCSV.data[i, 0]) - currentBattleLog.battleStartTime;
            aBuildingLog.key = recordCSV.data[i, 2];
            aBuildingLog.afterCamp = int.Parse(recordCSV.data[i, 6]);
            aBuildingLog.afterState=int.Parse(recordCSV.data[i, 5]);
            currentBattleLog.buildingLogs.Add(aBuildingLog);
        }
        if (currentBattleLog != null)
        {
            logData.Add(currentBattleLog.serverID, currentBattleLog);
        }
    }

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
        //Debug.Log("asdf,f,sa,we,qq".Split(',')[2]);
        playingCanvas.SetActive(false);
        settingCanvas.SetActive(true);
        //Debug.Log("Setting");
        currentPlayerState = PlayerState.Setting;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(currentPlayerState);
        if(currentPlayerState==PlayerState.Playing)
        {
            CameraUpdate();
            ShowUpdate();
        }
            
    }

    void ShowUpdate()
    {
        //Debug.Log(Time.deltaTime);
        if(GameObject.Find("PlayingStateText").GetComponent<Text>().text == "Pause")
        {
            return;
        }
        if(playTimeAt>=5400f)
        {
            return;
        }
        playTimeAt += Time.deltaTime * playSpeed;
        GameObject.Find("Time_Text").GetComponent<Text>().text=""+(int)(playTimeAt/3600)+":"+(int)((playTimeAt%3600) / 60)+ ":" + (int)(playTimeAt%60 );
        playScrollbar.value = Mathf.Clamp(playTimeAt / 5400f, 0f, 1f);
        while(true)
        {
            string serverID = serverChooser.options[serverChooser.value].text;
            if(!logData.ContainsKey(serverID))
            {
                Debug.Log("No ServerID:"+serverID);
                return;
            }
            ABattleLog theBattleLog = logData[serverID];
            if(logAt>=theBattleLog.buildingLogs.Count)
            {
                break;
            }
            if(theBattleLog.buildingLogs[logAt].timeAt<=playTimeAt)
            {
                Abuilding theBuilding=buildingData[theBattleLog.buildingLogs[logAt].key];
                UpdateABuilding(theBuilding,theBattleLog.buildingLogs[logAt]);
                //Debug.Log("Log "+logAt+" Done");
                logAt++;
            }else{
                break;
            }
        }
    }

    public void SpeedSet()
    {
        playSpeed = float.Parse(GameObject.Find("Speed_Text").GetComponent<Text>().text);
    }

    public void PlayingOrPause()
    {
        if(GameObject.Find("PlayingStateText").GetComponent<Text>().text == "Playing")
        {
            GameObject.Find("PlayingStateText").GetComponent<Text>().text = "Pause";
        }else{
            GameObject.Find("PlayingStateText").GetComponent<Text>().text = "Playing";
        }
    }

    public void ComfirmSetting()
    {
        LoadData();
        InitialPlaying();
    }

    void InitialPlaying()
    {
        playingCanvas.SetActive(true);
        settingCanvas.SetActive(false);

        foreach (string serverID in logData.Keys)
        {
            OptionData optionData=new OptionData();
            optionData.text = serverID;
            serverChooser.options.Add(optionData);
        }                   
        serverChooser.value = 0;
        string recordCSVPath = GameObject.Find("Server_chooser_Label_text").GetComponent<Text>().text=serverChooser.options[0].text;
        ChooseBattle();
        currentPlayerState = PlayerState.Playing;
    }

    public void ChooseBattle()
    {
        int battleSelect = serverChooser.value;
        currentPlayerState = PlayerState.Setting;
        Debug.Log("Choose:"+battleSelect);
        ClearMap();
        CreateMap();
        playTimeAt=0f;
        lastPlayTimeAt=-1f;
        logAt=0;
        GameObject.Find("PlayingStateText").GetComponent<Text>().text = "Playing";
        currentPlayerState = PlayerState.Playing;
        Transform ct = cameraObject.GetComponent<Transform>();
        Camera c = cameraObject.GetComponent<Camera>();
        c.orthographicSize = 100;
        ct.position = new Vector3(100,100,-10);
        Debug.Log("Choose:" + battleSelect+" success");
    }

    void ClearMap()
    {
        if(mapObj!=null)
            Destroy(mapObj);
        if(buildingData!=null)
        {
            foreach(Abuilding abuilding in buildingData.Values)
            {
                if(abuilding.itsGameObj!=null)
                {
                    Destroy(abuilding.itsGameObj);
                }
            }
        }
        if(LinkObjs!=null)
        {
            foreach(GameObject obj in LinkObjs.Values)
            {
                if(obj!=null)
                {
                    Destroy(obj);
                }
            }
        }
    }

    void CreateMap()
    {
        mapObj = Instantiate(UIResourcesDepot.Instance.mapBackground);
        Transform bt = mapObj.transform;
        bt.SetParent(playGroundNode.transform);
        bt.position = new Vector3(250 / 2, 250 / 2, 0);
        bt.localScale = new Vector3(250, 250);

        SpriteRenderer spriteRenderer = mapObj.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = LayerOrder.MapObjsLayer;

        LinkObjs = new Dictionary<string, GameObject>();
        foreach(Abuilding abuilding in buildingData.Values)
        {
            if(!abuilding.isMain)
                abuilding.camp = 0;
            UpdateABuilding(abuilding);
        }
    }

    void UpdateABuilding(Abuilding abuilding,ABuildingLog aBuildingLog=null)
    {
        if (!abuilding.isMain)
        {
            if (aBuildingLog != null)
            {
                abuilding.camp = aBuildingLog.afterCamp;
                abuilding.state = aBuildingLog.afterState;
            }
            else
            {
                abuilding.camp = 0;
                abuilding.state = 0;
            }
        }
        if(abuilding.itsGameObj!=null)
        {
            Destroy(abuilding.itsGameObj);
        }

        if(abuilding.state==0||abuilding.state==2||abuilding.isMain)
        {
            abuilding.itsGameObj = Instantiate(UIResourcesDepot.Instance.warTowers[abuilding.camp]);
        }else{
            //Debug.Log("Deal Life:"+abuilding.state+" of "+abuilding.key);
            abuilding.itsGameObj = Instantiate(UIResourcesDepot.Instance.lifeWarTower[abuilding.camp]);
            //abuilding.itsGameObj.name = "Life";
        }

        Transform tf= abuilding.itsGameObj.transform;
        tf.SetParent(playGroundNode.GetComponent<Transform>());
        tf.position = new Vector3(abuilding.position_x + 0.5f, abuilding.position_y + 0.5f, 0f);
        if (abuilding.state == 0 || abuilding.state == 2 || abuilding.isMain)
        {
            tf.localScale = new Vector3(5f, 5f, 5f);
        }else{
            tf.localScale = new Vector3(8f, 8f, 8f);
        }

        abuilding.itsGameObj.GetComponent<SpriteRenderer>().sortingOrder = LayerOrder.TowerLayer;
        foreach(string linkedID in abuilding.linkedBuildingIDs)
        {
            UpdateATowerLink(abuilding.key, linkedID);
        }
    }

    void UpdateATowerLink(string buildingID1, string buildingID2)
    {
        if(!buildingData.ContainsKey(buildingID1))
        {
            Debug.Log("no "+buildingID1);
            return;
        }
        if(!buildingData.ContainsKey(buildingID2))
        {
            Debug.Log("no " + buildingID2);
            return;
        }
        Abuilding building1 = buildingData[buildingID1];
        Abuilding building2 = buildingData[buildingID2];
        if(building1==null)
        {
            Debug.Log("building1==null");
        }
        if (building2 == null)
        {
            Debug.Log("building2==null");
        }
        string linkID1 = building1.key + "to" + building2.key;
        string linkID2 = building2.key + "to" + building1.key;
        if(LinkObjs.ContainsKey(linkID1))
        {
            Destroy(LinkObjs[linkID1]);
            LinkObjs.Remove(linkID1);
        }
        if (LinkObjs.ContainsKey(linkID2))
        {
            Destroy(LinkObjs[linkID2]);
            LinkObjs.Remove(linkID2);
        }
        GameObject linkObj=Instantiate(UIResourcesDepot.Instance.tiles[building1.camp == building2.camp ? building1.camp : 0]);
        Transform linkTf = linkObj.GetComponent<Transform>();
        Vector3 linkVector = new Vector3(building1.position_x,building1.position_y,0f) - new Vector3(building2.position_x, building2.position_y, 0f);
        linkTf.localScale = new Vector3(0.9f, Mathf.Sqrt(linkVector.x * linkVector.x + linkVector.y * linkVector.y), 1.0f);
        linkTf.localPosition = (new Vector3(building1.position_x+0.5f, building1.position_y+ 0.5f, 0f) + new Vector3(building2.position_x + 0.5f, building2.position_y + 0.5f, 0f)) / 2;
        float rotateAngle = Vector3.Angle(new Vector3(0, 1, 0), linkVector);
        if (linkVector.x > 0.001f || linkVector.x < -0.001f)
        {
            rotateAngle *= -Mathf.Abs(linkVector.x) / linkVector.x;
        }
        linkTf.rotation = Quaternion.Euler(new Vector3(0, 0, rotateAngle));
        linkTf.SetParent(playGroundNode.GetComponent<Transform>());
        linkObj.GetComponent<SpriteRenderer>().sortingOrder = LayerOrder.TowerLinksLayer;
        LinkObjs.Add(linkID1, linkObj);
        LinkObjs.Add(linkID2, linkObj);
    }

    void CameraUpdate()
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
}
