using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class OutpostMsg
{
    public enum MsgType{Update,Destory};
    public MsgType msgType;

    public string outpostID;
    //for Update
    public bool isBuilding=false;
    public int leftBuildTime=0;
    public int blood=0;
    public int unitNum=0;
    public int outTroopNum=0;
    public int factionOrder = -1;
    public int positionX=0;
    public int positionY=0;
    //for 
}
