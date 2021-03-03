using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading; 

public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

    // Received data string.  
#pragma warning disable XS0001 // Find APIs marked as TODO in Mono
    public StringBuilder sb = new StringBuilder();
#pragma warning restore XS0001 // Find APIs marked as TODO in Mono
}