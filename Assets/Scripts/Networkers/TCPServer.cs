using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPServer : MonoBehaviour
{
    //Singleton
    static TCPServer instance;

    public static TCPServer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(TCPServer)) as TCPServer;
            }
            return instance;
        }
    }

    public int campsNum=4;
    private class MyNetworkClient
    {
        public int order;
        public Thread tcpListenerThread;
        public TcpClient connectedTcpClient;
        public TcpListener tcpListener;
    }

    private MyNetworkClient[] myNetworkClients;
    public List<FromClientMessage> fromClientMessages;

    public enum TCPServerState { Uncreated, Creating, Created };
    public TCPServerState tcpServerState;
    // Use this for initialization
    void OnEnable()
    {
        //StartServer();
    }

    void OnStart()
    {
        tcpServerState = TCPServerState.Uncreated;
    }

    public void StartServer()
    {
        tcpServerState = TCPServerState.Creating;
        fromClientMessages = new List<FromClientMessage>();
        myNetworkClients = new MyNetworkClient[campsNum+1];
        for (int i = 0; i < myNetworkClients.Length; i++)
        {
            MyNetworkClient myNetworkClient = new MyNetworkClient();
            myNetworkClient.order = i;
            //myNetworkClient.tcpListenerThreads = CreateNewListener(i);
            myNetworkClients[i] = myNetworkClient;
            CreateNewListener(i);
        }
        tcpServerState = TCPServerState.Created;
    }

    public void AddAMessage(MessageHead msgHead, byte[] msgContent,int order)
    {
        lock (fromClientMessages)
        {
            FromClientMessage aMessage = new FromClientMessage
            {
                messageHead = msgHead,
                messageContentBytes = msgContent,
                playerOrder=order
            };
            fromClientMessages.Add(aMessage);
        }
    }

    public FromClientMessage GetOutAMessage()
    {
        lock (fromClientMessages)
        {
            if (fromClientMessages.Count == 0)
            {
                return null;
            }
            FromClientMessage aMessage = fromClientMessages[0];
            fromClientMessages.RemoveAt(0);
            return aMessage;
        }
    }

    public KeyCode actionbtn = KeyCode.N;
    // Update is called once per frame
    void Update()
    {
    //    if (Input.GetKeyDown(actionbtn))
    //    {
    //        TestMsg testMsg = new TestMsg();
    //        testMsg.a = 100;
    //        testMsg.b = 303;
    //        byte[] msgContent = NetworkUtils.Serialize(testMsg);
    //        byte[] fullMsg = NetworkUtils.PackWithHead(MessageTypes.TestType, msgContent);
    //        SendMessageBytes(myNetworkClients[0].connectedTcpClient,fullMsg);
    //        Debug.Log("Server sent " + fullMsg.Length + " bytes to Client 0");
    //    }
    }

    void CreateNewListener(int order)
    {
        Debug.Log("server:create listener "+order);
        // Start TcpServer background thread        
        Thread tcpListenerThread = new Thread(new ThreadStart(() => ListenForIncommingMsg(order)));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Name = "DemoServerListener";
        myNetworkClients[order].tcpListenerThread = tcpListenerThread;
        tcpListenerThread.Start();
    }

    public void ClearAll()
    {
        if (myNetworkClients != null)
        {
            for (int i = 0; i < myNetworkClients.Length; i++)
            {
                Thread tcpListenerThread = myNetworkClients[i].tcpListenerThread;
                if (tcpListenerThread != null && tcpListenerThread.IsAlive)
                {
                    tcpListenerThread.Interrupt();
                    tcpListenerThread.Abort();
                    Debug.Log("Sever:tcpListenerThread abort at " + i);
                }
            }
            myNetworkClients = null;
        }
        tcpServerState = TCPServerState.Uncreated;
    }

    /// <summary>   
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests    
    /// </summary>  
    //private void ListenForIncommingRequests(int order)
    //{
    //    try
    //    {
    //        // Create listener on localhost port 8052.
    //        string ipAdress = "0.0.0.0";
    //        int port = 1994 + order;
    //        TcpListener tcpListener = new TcpListener(IPAddress.Parse(ipAdress), port);
    //        myNetworkClients[order].tcpListener = tcpListener;
    //        tcpListener.Start();
    //        Debug.Log("Sever:Server is listening :"+ipAdress+":"+port);
    //        Byte[] bytes = new Byte[1024];
    //        while (true)
    //        {
    //            using (TcpClient connectedTcpClient = tcpListener.AcceptTcpClient())
    //            {
    //                Debug.Log("Server:Client "+order+" connected");
    //                myNetworkClients[order].connectedTcpClient = connectedTcpClient;
    //                // Get a stream object for reading                  
    //                using (NetworkStream stream = connectedTcpClient.GetStream())
    //                {
    //                    int length;
    //                    // Read incomming stream into byte arrary.                      
    //                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
    //                    {
    //                        var incommingData = new byte[length];
    //                        Array.Copy(bytes, 0, incommingData, 0, length);
    //                        // Convert byte array to string message.                            
    //                        string clientMessage = Encoding.ASCII.GetString(incommingData);
    //                        //Debug.Log("Sever:client "+order+" message received as: " + clientMessage);
    //                        //Thread.Sleep(5000);
    //                        Thread.Sleep(10);
    //                    }


    //                }
    //            }
    //            Thread.Sleep(10);
    //        }
    //    }
    //    catch (SocketException socketException)
    //    {
    //        Debug.Log("Sever:SocketException " + socketException.ToString());
    //    }
    //}

    private void ListenForIncommingMsg(int order)
    {
        try
        {
            // Create listener on localhost port 8052.
            string ipAdress = "0.0.0.0";
            int port = 1994 + order;
            TcpListener tcpListener = new TcpListener(IPAddress.Parse(ipAdress), port);
            myNetworkClients[order].tcpListener = tcpListener;
            tcpListener.Start();
            Debug.Log("Sever:Server is listening :"+ipAdress+":" + port);
            byte[] headBytes = new byte[8];
            while (true)
            {
                using (TcpClient connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    Debug.Log("Server:Client " + order + " connected");
                    myNetworkClients[order].connectedTcpClient = connectedTcpClient;
                    // Get a stream object for reading                  
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary.                      
                        while ((length = stream.Read(headBytes, 0, headBytes.Length)) != 0 )
                        {
                            if(length!=8)
                            {
                                Debug.Log("Server:head length error:"+length);
                                continue;
                            }
                            MessageHead messageHead = NetworkUtils.ResolveMessageHead(headBytes);
                            if(messageHead==null)
                            {
                                Debug.Log("Server:head error ");
                                continue;
                            }
                            //Debug.Log("Server:message type:" + messageHead.messageType);
                            //Debug.Log("Server:message length:" + messageHead.messageLength);
                            byte[] contentMsgBytes;
                            if (messageHead.messageLength != 0)
                            {
                                contentMsgBytes = new byte[messageHead.messageLength];
                                //while (stream.Read(contentMsgBytes, 0, contentMsgBytes.Length) == 0)
                                //{
                                //    Debug.Log("Server :waiting message content");
                                //}
                                int offset = 0;
                                while (true)
                                {
                                    length = stream.Read(contentMsgBytes, offset, contentMsgBytes.Length - offset);
                                    offset += length;
                                    //Debug.Log("Client got" + clientName + ":"+length);
                                    if (offset == messageHead.messageLength)
                                        break;
                                    //Thread.Sleep(10);
                                }

                                //Debug.Log("Server :get content");
                            }
                            else
                            {
                                contentMsgBytes = null;
                            }
                            AddAMessage(messageHead,contentMsgBytes,order);
                            //Thread.Sleep(5000);
                            //Thread.Sleep(10);
                        }
                    }
                }
                Thread.Sleep(10);
            }
            //Debug.Log("Sever :threadLifeTime over ");
        }
        catch (SocketException socketException)
        {
            Debug.Log("Sever:SocketException " + socketException.ToString());
        }
    }
    /// <summary>   
    /// Send message to client using socket connection.     
    /// </summary>  
    private void SendMessageTCP(TcpClient connectedTcpClient,string serverMessage)
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing.             
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                //Debug.Log("Sever:Server sent his message");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Sever:Socket exception: " + socketException);
        }
    }

    private void SendMessageBytes(TcpClient connectedTcpClient,byte[] data)
    {
        if (connectedTcpClient == null)
        {
            return;
        }
        try
        {
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(data, 0, data.Length);
                //Debug.Log("Server sent his message");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Server:Socket SendMessageBytes exception: " + socketException);
        }
    }

    public void SendMessageObject(int playerOrder,uint messageType,System.Object msgO)
    {
        TcpClient connectedTcpClient = myNetworkClients[playerOrder].connectedTcpClient;
        byte[] msgContent = NetworkUtils.Serialize(msgO);
        byte[] fullMsg = NetworkUtils.PackWithHead(messageType, msgContent);
        //Debug.Log("Server Send Msg of length:"+msgContent.Length);
        SendMessageBytes(connectedTcpClient,fullMsg);
    }

    public void SendMessageObjectToAll(uint messageType,System.Object msgO)
    {
        byte[] msgContent = NetworkUtils.Serialize(msgO);
        byte[] fullMsg = NetworkUtils.PackWithHead(messageType, msgContent);
        for (int i = 0; i < myNetworkClients.Length; i++)
        {
            //Debug.Log("try send to :"+i);
            if (myNetworkClients[i] != null && myNetworkClients[i].connectedTcpClient != null && myNetworkClients[i].connectedTcpClient.Connected)
            {
                //Debug.Log("Server Send Msg of length:" + msgContent.Length);
                SendMessageBytes(myNetworkClients[i].connectedTcpClient, fullMsg);
                //Debug.Log("success send to :" + i);
            }
        }
    }
}