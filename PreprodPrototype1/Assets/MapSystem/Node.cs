using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum NodeType
{
    BATTLE,
    REST,
    EVENT,
    SHOP,
    BOSS
}

[System.Serializable]
public class NodeSaveData
{
    public int id;
    public NodeType nodeType;
    public bool isUnlocked;
    public bool isActivated;
    public int nodeLevel;
    public Vector2Int coord;
    public Vector3 pos;
    public List<int> connectedNode = new List<int>();
}

public class Node : MonoBehaviour
{
    public int id;
    public NodeType nodeType;
    public bool isUnlocked;
    public bool isActivated = false;
    public int nodeLevel;
    public Vector2Int coord;
    public Vector3 pos;
    public List<Node> connectedNodes = new List<Node>();
    private Button button;

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
        // increase level after picking a node
        int level = PlayerPrefs.GetInt("MapLevel") + 1;
        PlayerPrefs.SetInt("MapLevel", level);

        isActivated = true;

        // save map state
        MapManager map = FindFirstObjectByType<MapManager>();
        map.SaveMap();

        // save camera position
        Camera cam = Camera.main;
        Vector3 pos = cam.transform.position;

        PlayerPrefs.SetFloat("CamX", pos.x);
        PlayerPrefs.SetFloat("CamY", pos.y);
        PlayerPrefs.SetFloat("CamZ", pos.z);

        PlayerPrefs.Save();

        Debug.Log("loading scene!");

        switch(nodeType)
        {
            case NodeType.BATTLE:
                if (nodeLevel <= 1)
                {
                    SceneManager.LoadScene("JandreTest");
                }
                else
                {
                    int n = Random.Range(0, 2);
                    if (n == 0)
                    {
                        SceneManager.LoadScene("JandreTest");
                    }
                    else
                    {
                        SceneManager.LoadScene("01_Battle");
                    }
                }
                break;
            case NodeType.REST:
                SceneManager.LoadScene("RestScene"); // for debug purpose
                break;
            case NodeType.SHOP:
                SceneManager.LoadScene("MapScene"); // for debug purpose
                break; 
            case NodeType.EVENT:
                if (nodeLevel < 1)
                {
                    SceneManager.LoadScene("00_StoryScene");
                }
                else if (nodeLevel < 3)
                {
                    SceneManager.LoadScene("01_StoryScene");
                }
                else if (nodeLevel < 6)
                {
                    SceneManager.LoadScene("02_StoryScene");
                }
                else if (nodeLevel < 10)
                {
                    SceneManager.LoadScene("03_StoryScene");
                }
                PlayerPrefs.SetInt("HasCompanion", 1);
                break;
            default:
                break;
        }
    }

    public void ActivateButton()
    {
        button = GetComponentInChildren<Button>();
        button.interactable = true;
    }

    public void DeactivateButton()
    {
        button = GetComponentInChildren<Button>();
        button.interactable = false;
    }

    public void MarkPastButton()
    {
        button = GetComponentInChildren<Button>();
        ColorBlock butColor = button.colors;
        butColor.disabledColor = new Color(1.0f, 0.5f, 0.5f);
        button.colors = butColor;
        button.interactable = false;
        button.transform.Find("Icon").GetComponent<Image>().color = new Color(1.0f, 0.5f, 0.5f);
    }
}
