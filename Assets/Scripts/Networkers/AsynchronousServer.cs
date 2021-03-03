using UnityEngine;
using System.Collections;
using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading; 

public class AsynchronousServer : MonoBehaviour
{
    Thread tcpListenerThread;
    // Use this for initialization
    void Start()
    {

    }

    private void OnEnable()
    {
        Debug.Log("Server OnEnable");
        tcpListenerThread = new Thread(new ThreadStart(StartListening));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Name = "DemoServer";
        tcpListenerThread.Start();
    }

    private void OnDisable()
    {
        if(tcpListenerThread!=null&&tcpListenerThread.IsAlive)
        {
            tcpListenerThread.Interrupt();
            tcpListenerThread.Abort();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);  
  
    public AsynchronousServer() {  
    }  
  
    public static void StartListening() {  
        // Establish the local endpoint for the socket.  
        // The DNS name of the computer  
        // running the listener is "host.contoso.com".  
        //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);  
  
        // Create a TCP/IP socket.  
        Socket listener = new Socket(ipAddress.AddressFamily,  
            SocketType.Stream, ProtocolType.Tcp );  
  
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try {  
            listener.Bind(localEndPoint);  
            listener.Listen(100);  
  
            while (true) {  
                // Set the event to nonsignaled state.  
                allDone.Reset();  
  
                // Start an asynchronous socket to listen for connections.  
                Debug.Log("Waiting for a connection...");  
                listener.BeginAccept(   
                    new AsyncCallback(AcceptCallback),  
                    listener );  
  
                // Wait until a connection is made before continuing.  
                allDone.WaitOne();  
            }  
  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }

  
    }  
  
    public static void AcceptCallback(IAsyncResult ar) {  
        Debug.Log("AcceptCallback");
        // Signal the main thread to continue.  
        allDone.Set();  
  
        // Get the socket that handles the client request.  
        Socket listener = (Socket) ar.AsyncState;  
        Socket handler = listener.EndAccept(ar);  
  
        // Create the state object.  
        StateObject state = new StateObject();  
        state.workSocket = handler;  

        handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,  
            new AsyncCallback(ReadCallback), state);  
    }  
  
    public static void ReadCallback(IAsyncResult ar) {  
        Debug.Log("ReadCallback");
        String content = String.Empty;  
  
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject) ar.AsyncState;  
        Socket handler = state.workSocket;  
  
        // Read data from the client socket.   
        int bytesRead = handler.EndReceive(ar);  
  
        if (bytesRead > 0) {  
            // There  might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(  
                state.buffer, 0, bytesRead));  
  
            // Check for end-of-file tag. If it is not there, read   
            // more data.  
            content = state.sb.ToString();  
            if (content.IndexOf("<EOF>") > -1) {  
                // All the data has been read from the   
                // client. Display it on the console.  
                Debug.Log("Read "+content.Length+"bytes from socket. \n Data : "+content);  
                // Echo the data back to the client.  
                Send(handler, content);  
            } else {  
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,  
                new AsyncCallback(ReadCallback), state);  
            }  
        }  
    }  
  
    private static void Send(Socket handler, String data) {  
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);  
  
        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,  
            new AsyncCallback(SendCallback), handler);  
    }  
  
    private static void SendCallback(IAsyncResult ar) {  
        try {  
            // Retrieve the socket from the state object.  
            Socket handler = (Socket) ar.AsyncState;  
  
            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);  
            Debug.Log("Sent "+bytesSent+" bytes to client.");  
  
            handler.Shutdown(SocketShutdown.Both);  
            handler.Close();  
  
        } catch (Exception e) {  
            Debug.Log(e.ToString());  
        }  
    } 
}
