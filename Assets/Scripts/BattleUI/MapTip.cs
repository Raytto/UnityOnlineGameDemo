using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MapTip : MonoBehaviour
{
    public GameObject textObj;
    public GameObject backRect;
    // Use this for initialization
    private int lifeTime=500;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime--;
        if (lifeTime < 400)
        {
            //Debug.Log(textObj.GetComponent<Text>().text);
            Vector3 p = this.gameObject.transform.position;
            p.y += 0.001f;
            this.gameObject.transform.position = p;
            if (lifeTime == 0)
            {
                Destroy(this.gameObject);
            }
            backRect.GetComponent<SpriteRenderer>().color = new Color(0.9150943f, 0.5787933f, 0f, lifeTime / 500f);
            textObj.GetComponent<Text>().color=new Color(0.1960784f, 0.1960784f, 0.1960784f, lifeTime / 500f);
        }
    }

    public static Vector3 GetUIPostionByLogiclogicPosition(Vector2Int logicPostion)
    {
        return new Vector3(logicPostion.x+0.5f,logicPostion.y+0.7f,0f);
    }

}
