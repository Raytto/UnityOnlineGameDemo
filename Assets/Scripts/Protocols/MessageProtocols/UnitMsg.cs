using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class UnitMsg
{
    public enum UpdateWay { Update, Destory ,CallBack};
    public UpdateWay updateWay;
    public string id="";
    public int positionX;
    public int positionY;
    public string belongToOutpostID="";
    public int UnitAmount;
    public int factionOrder;
}
