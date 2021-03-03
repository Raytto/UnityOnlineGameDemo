using UnityEngine;
using System.Collections;
using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Threading;  
using System.Text; 

public class AsynchronousClient : MonoBehaviour
{
    Thread tcpListenerThread;
    Socket client;
    // Use this for initialization
    public KeyCode actionbtn = KeyCode.N;
    private int lifeLimit=50;

    void Start()
    {

    }

    private void OnEnable()
    {
        Debug.Log("Client OnEnable");
        tcpListenerThread = new Thread(new ThreadStart(StartClient));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Name = "DemoClient";
        tcpListenerThread.Start();
    }

    private void OnDisable()
    {
        if (tcpListenerThread != null && tcpListenerThread.IsAlive)
        {
            if(client!=null&&client.Connected)
            {
                client.Shutdown(SocketShutdown.Both);  
                client.Close();  
                Debug.Log("Client Closed"); 
            }
            tcpListenerThread.Interrupt();
            tcpListenerThread.Abort();
        }
    }

    // Update is called once per frame
    void Update()
    {
        lifeLimit = 10;
        if (Input.GetKeyDown(actionbtn))
        {
            Send(actionbtn.ToString());
        }
    }

    // The port number for the remote device.  
    private const int port = 11000;  
  
    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone =   
        new ManualResetEvent(false);  
    private static ManualResetEvent sendDone =   
        new ManualResetEvent(false);  
    private static ManualResetEvent receiveDone =   
        new ManualResetEvent(false);  
  
    // The response from the remote device.  
    private String response = String.Empty;  
  
    private void StartClient() {  
        // Connect to a remote device.  
        try {
            // Establish the remote endpoint for the socket.  
            // The name of the   
            // remote device is "host.contoso.com".  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry("127.0.0.1");  
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);  
  
            // Create a TCP/IP socket.  
            client = new Socket(ipAddress.AddressFamily,  
                SocketType.Stream, ProtocolType.Tcp);  
  
            // Connect to the remote endpoint.  
            client.BeginConnect( remoteEP,   
                new AsyncCallback(ConnectCallback), client);  
            connectDone.WaitOne();  
  
            // Send test data to the remote device.  
            Send("This is a test<EOF>");  
            sendDone.WaitOne();

            while (lifeLimit>0)
            {
                // Receive the response from the remote device. 
                receiveDone.Reset();
                Receive();
                receiveDone.WaitOne();

                // Write the response to the console.  
                DealMessage(response);
                lifeLimit--;
                Thread.Sleep(100);
            }

  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    }  

    private void DealMessage(String msg)
    {
        Debug.Log("Client Response received : " + msg);
        msg = "";
    }  

    private void ConnectCallback(IAsyncResult ar) {  
        try {  
            // Retrieve the socket from the state object.  
            client = (Socket) ar.AsyncState;  
  
            // Complete the connection.  
            client.EndConnect(ar);  
  
            Debug.Log("Socket connected to "+client.RemoteEndPoint.ToString());
            //Send(client,"Test<EOF>");
  
            // Signal that the connection has been made.  
            connectDone.Set();  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    }  
  
    private void Receive() {  
        try {  
            // Create the state object.  
            StateObject state = new StateObject();  
            state.workSocket = client;  
  
            // Begin receiving the data from the remote device.  
            client.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,  
                new AsyncCallback(ReceiveCallback), state);  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    }  
  
    private void ReceiveCallback( IAsyncResult ar ) {
        Debug.Log("ReceiveCallback");
        try {  
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            StateObject state = (StateObject) ar.AsyncState;  
  
            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);  
  
            if (bytesRead > 0) {
                Debug.Log("get more data");
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer,0,bytesRead));  
  
                // Get the rest of the data.  
                client.BeginReceive(state.buffer,0,StateObject.BufferSize,0,  
                    new AsyncCallback(ReceiveCallback), state);  
            } else {  
                // All the data has arrived; put it in response.  
                if (state.sb.Length > 1) {  
                    response = state.sb.ToString();  
                }  
                // Signal that all bytes have been received.  
                Debug.Log("Receive Done");
                client.EndReceive(ar);
                receiveDone.Set();  
            }  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    }  
  
    private void Send(String data) {
        if (client == null || client.Connected == false)
        {
            Debug.Log("Can not Send due to no connection");
        }
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);  
  
        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,  
            new AsyncCallback(SendCallback), client);  
    }  
  
    private void SendCallback(IAsyncResult ar) {  
        try {  
            // Retrieve the socket from the state object.  
            //Socket client = (Socket) ar.AsyncState;  
  
            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);  
            Debug.Log("Sent "+bytesSent+" bytes to server.");  
  
            // Signal that all bytes have been sent.  
            sendDone.Set();  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    }  
}
