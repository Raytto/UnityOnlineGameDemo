using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Collections.Generic;
using System;

public static class NetworkUtils
{
    public static byte[] Serialize(object obj)
    {
        if (obj == null || !obj.GetType().IsSerializable)
            return null;
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, obj);
            byte[] data = stream.ToArray();
            return data;
        }
    }

    public static T Deserialize<T>(byte[] data) where T : class
    {
        if (data == null || !typeof(T).IsSerializable)
            return null;
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream(data))
        {
            object obj = formatter.Deserialize(stream);
            return obj as T;
        }
    }

    public static string GetLocalIPv4()
    {
        IPAddress ipAddr = Dns.Resolve(Dns.GetHostName()).AddressList[0];

        return ipAddr.ToString();
        //return "unknown";
    }

    //public static byte[] PackWithHead(ushort messageType, byte[] data = null)
    //{
    //    List<byte> list = new List<byte>();
    //    if (data != null)
    //    {
    //        list.AddRange(BitConverter.GetBytes((ushort)data.Length));
    //        list.AddRange(BitConverter.GetBytes((ushort)messageType));           
    //        list.AddRange(data);                                           
    //    }
    //    else
    //    {
    //        list.AddRange(BitConverter.GetBytes((ushort)0));                        
    //        list.AddRange(BitConverter.GetBytes((ushort)messageType));                     
    //    }
    //    return list.ToArray();
    //}

    public static byte[] PackWithHead(uint messageType, byte[] data = null)
    {
        List<byte> list = new List<byte>();
        if (data != null)
        {
            list.AddRange(BitConverter.GetBytes((uint)data.Length));
            list.AddRange(BitConverter.GetBytes((uint)messageType));
            list.AddRange(data);                                           
        }
        else
        {
            list.AddRange(BitConverter.GetBytes((uint)0));                      
            list.AddRange(BitConverter.GetBytes((uint)messageType));               
        }
        return list.ToArray();
    }

    public static MessageHead ResolveMessageHead(byte[] data)
    {
        MessageHead messageHead = new MessageHead();
        if(data.Length!=8)
        {
            //Debug.Log("Head Lengh Error");
            return null;
        }
        using (MemoryStream stream = new MemoryStream(data))
        {
            BinaryReader binary = new BinaryReader(stream, Encoding.UTF8);
            try
            {
                messageHead.messageLength = binary.ReadUInt32();
                messageHead.messageType = binary.ReadUInt32();
                return messageHead;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}