using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DemoManager : MonoBehaviour
{
    //Singleton
    static DemoManager instance;

    public static DemoManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(DemoManager)) as DemoManager;
            }
            return instance;
        }
    }

    public BattleBasicSetting basicSetting;
    //public PlayerInfo playerInfo;

    public string serverIP;
    public int serverPort=11000;
    public int myPort;


    public ClientManager clientManager;
    public ServerManager serverManager;
    //public UIManager uiManager;
    //public TCPClient tcpClient;
    //public TCPServer tcpServer;


    Battlefeild battlefeild;

    public enum CSMode { Host, PureClient ,Unchoose};
    public CSMode csMode=CSMode.Unchoose;

    public enum DemoRunningAt{ChooingMode,ServerInitial,ClientInitial,Playing,EndPlay}
    public DemoRunningAt demoState;

    public void ChooseHostOrClient()
    {
        demoState = DemoRunningAt.ChooingMode;
        csMode = CSMode.Unchoose;
        ServerManager.Instance.ClearServer();
        ClientManager.Instance.ClearScene();
        ClientManager.Instance.OpenChooseHostOrClientUI();
    }

    public void HostInitial(BattleBasicSetting battleBasicSetting,int myFactionOrder)
    {
        Debug.Log("as host");
        serverIP = NetworkUtils.GetLocalIPv4();
        demoState = DemoRunningAt.ServerInitial;
        csMode = CSMode.Host;
        ServerManager.Instance.CreateServer(battleBasicSetting);
        demoState = DemoRunningAt.ClientInitial;
        ClientManager.Instance.CreateClient("localhost",myFactionOrder);
        demoState = DemoRunningAt.Playing;
    }

    public void PureClientInitial(string serverIP,int myFactionOrder)
    {
        csMode = CSMode.PureClient;
        demoState = DemoRunningAt.ClientInitial;
        ClientManager.Instance.CreateClient(serverIP,myFactionOrder);
        demoState = DemoRunningAt.Playing;
    }


    private void Awake()
    {
    }

    void Start()
    {
        


        ChooseHostOrClient();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {

    }

}
