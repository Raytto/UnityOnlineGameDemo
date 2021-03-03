using UnityEngine;
using System.Collections;

public class FoggyUI : MonoBehaviour
{
    Texture2D texture=null;
    int[,] canSeeNum;
    int sizeX;
    int sizeY;
    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitialFoggy(int sizeX,int sizeY)
    {
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.transform.position = Vector3.zero;
        this.transform.localScale = new Vector3(100,100,0);
        texture = new Texture2D(sizeX, sizeY);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, sizeX, sizeY), Vector2.zero);
        GetComponent<SpriteRenderer>().sprite = sprite;
        GetComponent<SpriteRenderer>().sortingOrder = 10;

        canSeeNum = new int[sizeX, sizeY];

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++) //Goes through each pixel
            {
                canSeeNum[x, y] = 0;
                Color pixelColour;
                pixelColour = new Color(0.8f, 0.8f, 0.8f, 1);
                texture.SetPixel(x, y, pixelColour);
            }
        }



        texture.Apply();
    }

    public void CreateCanSeeZone(int x0,int y0,int x1,int y1)
    {
        //Debug.Log("CreateCanSeeZone:X"+x0+","+y0+","+x1+","+y1);
        for (int y = Mathf.Max(y0, 0); y <= Mathf.Min(y1, sizeY - 1); y++)
        {
            for (int x = Mathf.Max(x0, 0); x <= Mathf.Min(x1, sizeX - 1); x++) //Goes through each pixel
            {
                canSeeNum[x, y]++;
                if(canSeeNum[x, y]>0)
                {
                    texture.SetPixel(x, y, new Color(0.8f, 0.8f, 0.8f, 0));
                }
            }
        }
        texture.Apply();
    }

    public void RemoveCanSeeZone(int x0, int y0, int x1, int y1)
    {
        //Debug.Log("RemoveCanSeeZone:X" + x0 + "," + y0 + "," + x1 + "," + y1);
        for (int y = Mathf.Max(y0,0); y <= Mathf.Min(y1,sizeY-1); y++)
        {
            for (int x = Mathf.Max(x0, 0); x <= Mathf.Min(x1, sizeX - 1); x++) //Goes through each pixel
            {
                canSeeNum[x, y]--;
                if (canSeeNum[x, y] <= 0)
                {
                    texture.SetPixel(x, y, new Color(0.8f, 0.8f, 0.8f, 1));
                }
            }
        }
        texture.Apply();
    }

    public bool isUnderFoggy(Vector2Int p)
    {
        if(canSeeNum[p.x,p.y]>0)
        {
            return false;
        }
        else 
        {
            return true;
        }
    }
}
