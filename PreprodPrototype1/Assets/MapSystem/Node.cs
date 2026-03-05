using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NodeType
{
    BATTLE,
    REST,
    SHOP,
    EVENT
}

public class Node : MonoBehaviour
{
    public NodeType nodeType;
    public bool isUnlocked;
    public int nodeLevel;
    public Vector2Int coord;
    public List<Node> connectedNodes = new List<Node>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene()
    {
        switch(nodeType)
        {
            case NodeType.BATTLE:
                SceneManager.LoadScene("JandreTest");
                break;
            case NodeType.REST:
                break;
            case NodeType.SHOP:
                break; 
            case NodeType.EVENT:
                break;
            default:
                break;
        }
    }
}
