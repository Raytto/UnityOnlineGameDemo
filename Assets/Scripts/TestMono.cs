using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;


public class TestMono : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        //TestMsg a = new TestMsg();
        //a.a = 1;
        //a.b = 11;
        //byte[] dataContent = NetworkUtils.Serialize(a); //序列化
        //byte[] dataCompelet = NetworkUtils.PackWithHead(MessageTypes.testType,dataContent);

        ////MemoryStream incomingStream = new MemoryStream(dataCompelet);
        ////BinaryReader binary = new BinaryReader(incomingStream, Encoding.UTF8);
        ////MessageHead messageHead = new MessageHead();
        ////messageHead.messageLength = binary.ReadUInt16();
        ////messageHead.messageType = binary.ReadUInt16();

        //TestMsg b =  NetworkUtils.Deserialize<TestMsg>(data);
        //Debug.Log(b.a);
        //Debug.Log(b.b);
        //Debug.Log(NetworkUtils.GetLocalIPv4());
    }

    // Update is called once per frame
    void Update()
    {

    }
}
