using System;

public static class MessageTypes
{
    public const uint TestType = 1;//with content
    public const uint ConnectMsg=2;//with content
    public const uint PingMsg = 3;//with content 

    public const uint ReqForTimeAndSpeed = 4;//without content
    public const uint TimeAndSpeedMsg = 5;//with content

    public const uint ReqForAllBattleInfo = 6;//without content
    public const uint AllBattleInfoMsg = 7;//with content

    //unit message
    public const uint UnitMsg = 8;//with content

    public const uint OutpostMsg = 9;//with content

    public const uint MovingUnitMsg = 10;//with content

    public const uint CreateMovingUnitFromOutpostReq = 11;//with content

    public const uint CallBackMovingUnitReq = 12;//with content

    public const uint FactionMsg = 13;//with content

    public const uint WarTowerMsg = 14;//with content

    public const uint TopTipMsg = 15;//with content

    public const uint LightTowerMsg=16;//with content

    public const uint MapTipMsg = 17;//with content
}

