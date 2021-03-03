using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class MovingUnitMsg
{
    public enum UpdateWay { Update, Destory ,Start,CallBack};
    public UpdateWay updateWay=UpdateWay.Update;
    public string id="";
    public string belongToOutpostID="";
    public int factionOrder=0;
    public int UnitAmount=0;

    public int startPositionX=0;
    public int startPositionY=0;
    public int endPositionX=0;
    public int endPositionY=0;
    public float totalLength=0;
    public float currentLength=0;
    public int lastUpdateTime=0;
    public float speed=0;
}
