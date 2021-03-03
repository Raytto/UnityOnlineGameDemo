using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    //Singleton
    static TCPClient instance;

    public static TCPClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(TCPClient)) as TCPClient;
            }
            return instance;
        }
    }

    public enum TCPClientState { Disconnected, TryToConnect, Connected };
    public TCPClientState tcpClientState;
    public int order = 0;
    public string clientName = "aa";
    public KeyCode actionbtn = KeyCode.N;
    private string serverIP="localhost";
    private int serverPort=1994;   
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    //private int threadLiftTime=50;


    public List<FromServerMessage> fromServerMessages;
    // Use this for initialization  
    private void OnEnable()
    {
        //serverPort = 8050 + order;
        //Debug.Log("Client: "+order+"client start to connect to server :"+serverIP+":"+serverPort);
        //ConnectToTcpServer();
    }

    public void StartClient(string serverIP)
    {
        tcpClientState = TCPClientState.Disconnected;
        fromServerMessages = new List<FromServerMessage>();
        this.serverPort = 1994 + order;
        Debug.Log("Client: " + order + "client start to connect to server :" + serverIP + ":" + serverPort);
        this.serverIP = serverIP;
        //this.serverPort = serverPort;
        ConnectToTcpServer();
    }

    public void AddAMessage(MessageHead msgHead,byte[] msgContent)
    {
        lock(fromServerMessages)
        {
            FromServerMessage aMessage = new FromServerMessage
            {
                messageHead=msgHead,
                messageContentBytes=msgContent
            };
            fromServerMessages.Add(aMessage);
        }
    }

    public FromServerMessage GetOutAMessage()
    {
        lock (fromServerMessages)
        {
            if(fromServerMessages.Count==0)
            {
                return null;
            }
            FromServerMessage aMessage = fromServerMessages[0];
            fromServerMessages.RemoveAt(0);
            return aMessage;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(actionbtn))
        //{
        //    //SendMessageBytes(actionbtn.ToString()+"   abcdeff<EOF>");
        //    TestMsg testMsg = new TestMsg();
        //    testMsg.a = 100;
        //    testMsg.b = 303;
        //    byte[] msgContent = NetworkUtils.Serialize(testMsg);
        //    byte[] fullMsg = NetworkUtils.PackWithHead(MessageTypes.TestType, msgContent);
        //    SendMessageBytes(fullMsg);
        //    Debug.Log("Client: " + order + "sent " + fullMsg.Length + " bytes to Server");
        //}
    }

    public void ClearAll()
    {
        if (socketConnection != null && socketConnection.Connected)
        {
            socketConnection.Close();
        }
        if (clientReceiveThread != null && clientReceiveThread.IsAlive)
        {
            clientReceiveThread.Interrupt();
            clientReceiveThread.Abort();
            Debug.Log("Sever:tcpListenerThread abort");
        }
        tcpClientState = TCPClientState.Disconnected;
    }

    private void OnDisable()
    {
        //ClearAll();
    }


    private void ConnectToTcpServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(StartAConnection));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Name = "DemoClientListener"+clientName;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("Client" + clientName + ":On client connect exception " + e);
        }
    }
    /// <summary>   
    /// Runs in background clientReceiveThread; Listens for incomming data.     
    /// </summary>     
    private void StartAConnection()
    {
        try
        {
            Debug.Log("StartAConnection:"+this.serverIP+":"+this.serverPort);
            socketConnection = new TcpClient(this.serverIP, this.serverPort);
            tcpClientState = TCPClientState.Connected;
            Debug.Log("Get socketConnection");
            byte[] headMsgBytes = new byte[8];
            while (true)
            {
                // Get a stream object for reading              
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incomming stream into byte arrary.                  
                    while ((length = stream.Read(headMsgBytes, 0, headMsgBytes.Length)) != 0)
                    {
                        //Debug.Log("Client " + clientName + ":get a message head");
                        MessageHead messageHead = NetworkUtils.ResolveMessageHead(headMsgBytes);
                        if(messageHead==null)
                        {
                            Debug.Log("Client " + clientName + ":Error in message head");
                        }
                        //Debug.Log("Client " + clientName + ":message type:"+messageHead.messageType);
                        //Debug.Log("Client " + clientName + ":message length:" +messageHead.messageLength);
                        byte[] contentMsgBytes;
                        if (messageHead.messageLength != 0)
                        {
                            contentMsgBytes = new byte[messageHead.messageLength];
                            //Debug.Log("Client get Msg of length:" + messageHead.messageLength);
                            int offset = 0;
                            while(true)
                            {
                                length = stream.Read(contentMsgBytes, offset, contentMsgBytes.Length-offset);
                                offset += length;
                                //Debug.Log("Client got" + clientName + ":"+length);
                                if (offset == messageHead.messageLength)
                                    break;
                                //Thread.Sleep(10);
                            }
                            //Debug.Log("get content length of :"+length);
                        }
                        else
                        {
                            contentMsgBytes = null;
                        }
                        AddAMessage(messageHead,contentMsgBytes);
                    }
                }
                //Thread.Sleep(10);
            }
            //tcpClientState = TCPClientState.Disconnected;
            //Debug.Log("Client " + clientName + ":threadLiftTime over ");
        }
        catch (SocketException socketException)
        {
            Debug.Log("Client" + clientName + ":Socket exception: " + socketException);
        }
    }
    /// <summary>   
    /// Send message to server using socket connection.     
    /// </summary>  
    private void SendMessageString(string clientMessage)
    {
        if (socketConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                // Convert string message to byte array.                 
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                // Write byte array to socketConnection stream.                 
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                //Debug.Log("Client" + clientName + ":Client sent his message:"+clientMessage);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Client" + clientName + ":Socket exception: " + socketException);
        }
    }

    private void SendMessageBytes(byte[] data)
    {
        if (socketConnection == null)
        {
            return;
        }
        try
        {         
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(data, 0, data.Length);
                //Debug.Log("Client" + clientName + ":Client sent his message");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Client" + clientName + ":Socket SendMessageBytes exception: " + socketException);
        }
    }

    public void SendMessageObject(uint messageType,System.Object msgO)
    {
        byte[] msgContent = NetworkUtils.Serialize(msgO);
        byte[] fullMsg = NetworkUtils.PackWithHead(messageType, msgContent);
        SendMessageBytes(fullMsg);
    }

}