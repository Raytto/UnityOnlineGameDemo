using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class CreateMovingUnitFromOutpostReq
{
    public string startOutpostID;
    public int unitAmount;
    public int toPositionX;
    public int toPositionY;
}
