using UnityEngine;
using System.Collections;

public class ObjectText : MonoBehaviour
{
    public GameObject text;
    public GameObject backRect;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public static Vector3 GetUIPostionByLogiclogicPosition(Vector2Int logicPosition)
    {
        return new Vector3(logicPosition.x + 0.5f, logicPosition.y + 0.5f, 0f);
    }
}
