using UnityEngine;
using System.Collections;

public class ToptipUI : MonoBehaviour
{
    public GameObject TextObj;
    public GameObject the_image;

    private int lifeTime;
    //bool startToDis = false;
    // Use this for initialization
    void Start()
    {
        lifeTime = 450;
    }

    // Update is called once per frame
    void Update()
    {
        //if(startToDis)
        //{
            lifeTime--;
            if (lifeTime < 400)
            {
                Vector3 p = this.gameObject.transform.position;
                p.y += 0.4f;
                this.gameObject.transform.position = p;
                if (lifeTime == 0)
                {
                    Destroy(this.gameObject);
                }
            }
        //}
    }

    IEnumerator MyUpdateFromStart()
    {
        yield return new WaitForSeconds(3.0f);
        //this.startToDis = true;
    }
}
