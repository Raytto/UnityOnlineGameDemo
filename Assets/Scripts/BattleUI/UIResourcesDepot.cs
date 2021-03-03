using UnityEngine;
using System.Collections;

public class UIResourcesDepot : MonoBehaviour
{
    //Singleton
    static UIResourcesDepot instance;

    public static UIResourcesDepot Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(UIResourcesDepot)) as UIResourcesDepot;
            }
            return instance;
        }
    }
    public GameObject[] units;
    public GameObject[] movingUnits;
    public GameObject[] outpost;
    public GameObject[] warTowers;
    public GameObject[] lifeWarTower;
    public GameObject[] tiles;
    public GameObject lifeLine;
    public GameObject lightTowerLight;
    public GameObject lightTowerDark;
    public GameObject toptip;
    public GameObject mapTip;
    public GameObject ObjectText;


    //public GameObject towerPrefeb;
    //public GameObject towerLinkPrefeb;

    public GameObject mapBackground;
    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
