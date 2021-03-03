using System;
using UnityEngine;

public static class MySettings
{
    //for initial
    public static int npcNumInArea100 = 0;
    public static int initialOutpostNum = 6;

    public static int npcScorePerUnit = 100;
    public static int pvpScorePerUnit = 1;

    public static int outpostBuildTime = 30000;

    public static int outpostInitialBlood = 100;
    public static int outpostLoseBloodPerAttack = 40;

    public static int warTowerLosingTIme = 60000;

    public static float towerScoreToRealScorePerTime = 30000f;//per millisecond
    public static float warTowerSnatchRate = 60f;

    public static float unitMovingSpeed = 0.0003f*4f;//per millisecond
    public static int outpostTroopLimit = 3;

    private static int MaxTowerLevel(int towerNumX,int towerNumY)
    {
        return (int)((towerNumX + towerNumY) / 2);
    }

    public static int TowerLevel(Vector2Int p,BattleBasicSetting basicSetting)
    {
        int maxTowerLevel = MaxTowerLevel(basicSetting.warTowerXnum,basicSetting.warTowerYnum);
        Vector2 c = new Vector2(basicSetting.mapSizeX / 2f, basicSetting.mapSizeY / 2f);
        float distanceToCenter = Mathf.Sqrt((p.x - c.x) * (p.x - c.x) + (p.y - c.y) * (p.y - c.y));
        float maxDistance = Mathf.Sqrt(basicSetting.mapSizeX * basicSetting.mapSizeX + basicSetting.mapSizeY * basicSetting.mapSizeY) / 2;
        int level = Mathf.Max(1,(int)(maxTowerLevel*(1f - distanceToCenter / maxDistance)));
        return level;
    }

    public static float TowerValue(int towerLevel)
    {
        return (int)Mathf.Pow(towerLevel,1.5f);
    }

    //about outpost recover
    public static int recoverAnOutpostPer = 4000;
    public static float recoverRateOfPower = 0.04f;

    //about outpost show out
    public static int newOutpostPer = 180000;

    //about tower getting time
    private static float TowerGettingTimeLevelRate(int towerLevel)
    {
        return Mathf.Pow( 1.2f,towerLevel-1f);
    }

    private static float TowerGettingTimeHavingRate(int alreadyHaveNum)
    {
        return Mathf.Pow(1.15f, alreadyHaveNum-1f);
    }

    public static int warTowerBasicGettingTIme = 15000;
    public static int TowerGettingTime(int towerLevel,int alreadyHaveNum)
    {
        return (int)(warTowerBasicGettingTIme * TowerGettingTimeLevelRate(towerLevel)*TowerGettingTimeHavingRate(alreadyHaveNum));
    }

    //light tower
    public static int lightTowerLightTime = 300000;
    public static int lightTowerCoolDownTime = 600000;

    //for client
    public static int warTowerSeeDist = 20;
    public static int lightTowerSeeDist = 33;
    public static int unitSeeDist = 6;
    public static int outpostSeeDist = 8;
    public static int sizeTowerValue = 50;
    public static int sizeOutpostValue = 150;
}

