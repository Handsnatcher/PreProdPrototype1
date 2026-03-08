using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TreeEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MapSaveData
{
    public List<NodeSaveData> savedNodes = new List<NodeSaveData>();
}

public class MapManager : MonoBehaviour
{
    public GameObject nodeObject;
    public GameObject lineObject;
    public Transform canvasTransform;
    public Transform linesTransform;
    public Transform nodesTransform;
    public Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    //public List<LineRenderer> lines;
    public List<UILine> lines;
    private int gridHeight = 15;
    private int gridWidth = 8;
    private string savePath => Path.Combine(Application.persistentDataPath, "MapData.json");

    // Start is called before the first frame update
    void Start()
    {
        // create node and add them to the grid
        //for (int i = 0; i < gridHeight; i++)
        //{
        //    for (int j = 0; j < gridWidth; j++)
        //    {
        //        Node node = Instantiate(nodeObject, canvasTransform).GetComponent<Node>();
        //        node.transform.localPosition = new Vector3((j - (gridWidth - 1) / 2)*100, (i - gridHeight / 2) * 100, 0);
        //        node.id = i * gridWidth + j;
        //        node.coord = new Vector2Int(j, i);
        //        node.nodeType = (NodeType)Random.Range(0, 3);
        //        node.nodeLevel = i + 1;
        //        node.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = node.nodeType.ToString()[0].ToString(); // node text(TODO: replace with image)
                
        //        nodes[new Vector2Int(j,i)] = node;
        //    }
        //}
        if (PlayerPrefs.GetInt("MapLevel") == 0) // new game
        {
            PlayerPrefs.SetInt("MapLevel", 1); // start first level
            CreateMap();

            Camera cam = Camera.main;
            cam.transform.position = new Vector3(0, 1, -10);
        }
        else // continue
        {
            LoadMap();

            float camX = PlayerPrefs.GetFloat("CamX");
            float camY = PlayerPrefs.GetFloat("CamY");
            float camZ = PlayerPrefs.GetFloat("CamZ");

            Camera cam = Camera.main;
            cam.transform.position = new Vector3(camX, camY, camZ);
        }

        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void CreateMap()
    {
        // create node and add them to the grid
        for (int i = 0; i < gridHeight; i++)
        {
            for (int j = 0; j < gridWidth; j++)
            {
                Node node = Instantiate(nodeObject, nodesTransform).GetComponent<Node>();
                node.transform.localPosition = new Vector3((j - (gridWidth - 1) / 2) * 100, (i) * 100, 0);
                node.id = i * gridWidth + j;
                node.coord = new Vector2Int(j, i);
                node.nodeType = (NodeType)Random.Range(0, 3);
                node.nodeLevel = i + 1;
                node.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = node.nodeType.ToString()[0].ToString(); // node text(TODO: replace with image)

                nodes[new Vector2Int(j, i)] = node;
            }
        }

        // boss node
        Node bossNode = Instantiate(nodeObject, canvasTransform).GetComponent<Node>();
        bossNode.id = gridHeight * gridWidth;
        bossNode.coord = new Vector2Int(0, gridHeight);
        bossNode.nodeType = NodeType.BOSS;
        bossNode.nodeLevel = gridHeight;
        nodes[new Vector2Int(0, gridHeight)] = bossNode;
        bossNode.transform.localPosition = new Vector3(0, (gridHeight + 1) * 100, 0);

        bossNode.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = bossNode.nodeType.ToString()[0].ToString();

        int connectionSize;
        int firstRowConnectionCount = 0;

        while (firstRowConnectionCount < (gridWidth / 2)) // make sure the map will always have more than half of the node activated each run
        {
            foreach (KeyValuePair<Vector2Int, Node> p in nodes)
            {
                int nextRow = p.Key.y + 1;
                // check if the level is unlocked
                //if (p.Value.nodeLevel > PlayerPrefs.GetInt("MapLevel"))
                //{
                //    p.Value.DeactivateButton();
                //}
                //else if (p.Value.nodeLevel < PlayerPrefs.GetInt("MapLevel"))
                //{
                //    if (p.Value.isActivated)
                //    {
                //        p.Value.MarkPastButton();
                //    }
                //}
                //else
                //{
                //    p.Value.isUnlocked = true;
                //    p.Value.ActivateButton();
                //}

                if (p.Key.y == 0) // first row
                {
                    p.Value.connectedNodes.Clear(); // reset
                    p.Value.isUnlocked = true; // unlock first row
                    p.Value.ActivateButton();

                    if (p.Key.x == 0) // first node in the row
                    {
                        connectionSize = Random.Range(0, 3);
                        if (connectionSize == 2) // connect both node above
                        {
                            // set to be connected node
                            Node n1 = nodes[new Vector2Int(0, nextRow)];
                            Node n2 = nodes[new Vector2Int(1, nextRow)];
                            // connect node to self
                            p.Value.connectedNodes.Add(n1);
                            p.Value.connectedNodes.Add(n2);
                            // connect self to node
                            n1.connectedNodes.Add(p.Value);
                            n2.connectedNodes.Add(p.Value);
                            firstRowConnectionCount++;
                        }
                        else if (connectionSize == 1) // connect random 1 node
                        {
                            // set to be connected node
                            Node n1 = nodes[new Vector2Int(Random.Range(0, 2), nextRow)];
                            // connect node to self
                            p.Value.connectedNodes.Add(n1);
                            // connect self to node
                            n1.connectedNodes.Add(p.Value);
                            firstRowConnectionCount++;
                        }
                    }
                    else if (p.Key.x == (gridWidth - 1)) // last node in the row
                    {
                        Node prevNode = nodes[new Vector2Int(p.Key.x - 1, p.Key.y)];
                        int highestX = prevNode.coord.x; // highest to be self node x by default for calculation purpose
                        // check prev node has connections
                        if (prevNode.connectedNodes.Count > 0)
                        {
                            foreach (Node n in prevNode.connectedNodes)
                            {
                                if (n.coord.x > highestX)
                                {
                                    highestX = n.coord.x;
                                }
                            }
                        }

                        int possibleConnectionCount = gridWidth - highestX; // -1 for grid +1 for connection
                        connectionSize = Random.Range(0, possibleConnectionCount + 1);

                        if (connectionSize == 2) // connect both node above
                        {
                            // set to be connected node
                            Node n1 = nodes[new Vector2Int(gridWidth - 1, nextRow)];
                            Node n2 = nodes[new Vector2Int(gridWidth - 2, nextRow)];
                            // connect node to self
                            p.Value.connectedNodes.Add(n1);
                            p.Value.connectedNodes.Add(n2);
                            // connect self to node
                            n1.connectedNodes.Add(p.Value);
                            n2.connectedNodes.Add(p.Value);
                            firstRowConnectionCount++;
                        }
                        else if (connectionSize == 1) // connect random 1 node
                        {
                            if (connectionSize == possibleConnectionCount)
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(gridWidth - 1, nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                            }
                            else
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(Random.Range(gridWidth - 2, gridWidth), nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                            }
                            firstRowConnectionCount++;
                        }

                        // RESET CODE: check if the first row have enough nodes activated otherwise restart mapping
                        if (firstRowConnectionCount < (gridWidth / 2))
                        {
                            firstRowConnectionCount = 0; // reset count
                            foreach (UILine l in lines)
                            {
                                Destroy(l);
                            }
                            lines.Clear();
                            break; // restart the pathing calculation
                        }
                    }
                    else if (p.Key.x > 0) // other nodes except the first one and last one
                    {
                        Node prevNode = nodes[new Vector2Int(p.Key.x - 1, p.Key.y)];
                        int highestX = prevNode.coord.x; // highest to be self node x by default for calculation purpose
                        // check prev node has connections
                        if (prevNode.connectedNodes.Count > 0)
                        {
                            foreach (Node n in prevNode.connectedNodes)
                            {
                                if (n.coord.x > highestX)
                                {
                                    highestX = n.coord.x;
                                }
                            }
                        }

                        int possibleConnectionCount = p.Key.x - highestX + 2; // +2 for max node possible while prevous node connected to the one above
                        connectionSize = Random.Range(0, possibleConnectionCount + 1);

                        if (connectionSize == 3) // connect all 3 nodes
                        {
                            // set to be connected node
                            Node n1 = nodes[new Vector2Int(p.Key.x - 1, nextRow)];
                            Node n2 = nodes[new Vector2Int(p.Key.x, nextRow)];
                            Node n3 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];
                            // connect node to self
                            p.Value.connectedNodes.Add(n1);
                            p.Value.connectedNodes.Add(n2);
                            p.Value.connectedNodes.Add(n3);
                            // connect self to node
                            n1.connectedNodes.Add(p.Value);
                            n2.connectedNodes.Add(p.Value);
                            n3.connectedNodes.Add(p.Value);
                            firstRowConnectionCount++;
                        }
                        else if (connectionSize == 2)
                        {
                            if (connectionSize == possibleConnectionCount)// connect to both nodes above and on the right
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(p.Key.x, nextRow)];
                                Node n2 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                p.Value.connectedNodes.Add(n2);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                                n2.connectedNodes.Add(p.Value);
                            }
                            else // connect 2 out of 3 nodes
                            {
                                int s = Random.Range(0, 3);
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(p.Key.x - 1, nextRow)];
                                Node n2 = nodes[new Vector2Int(p.Key.x, nextRow)];
                                Node n3 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];

                                switch (s)
                                {
                                    case 0:
                                        // connect node to self
                                        p.Value.connectedNodes.Add(n1);
                                        p.Value.connectedNodes.Add(n2);
                                        // connect self to node
                                        n1.connectedNodes.Add(p.Value);
                                        n2.connectedNodes.Add(p.Value);
                                        break;
                                    case 1:
                                        // connect node to self
                                        p.Value.connectedNodes.Add(n2);
                                        p.Value.connectedNodes.Add(n3);
                                        // connect self to node
                                        n2.connectedNodes.Add(p.Value);
                                        n3.connectedNodes.Add(p.Value);
                                        break;
                                    case 2:
                                        // connect node to self
                                        p.Value.connectedNodes.Add(n1);
                                        p.Value.connectedNodes.Add(n3);
                                        // connect self to node
                                        n1.connectedNodes.Add(p.Value);
                                        n3.connectedNodes.Add(p.Value);
                                        break;
                                    default:
                                        // connect node to self
                                        p.Value.connectedNodes.Add(n1);
                                        p.Value.connectedNodes.Add(n2);
                                        // connect self to node
                                        n1.connectedNodes.Add(p.Value);
                                        n2.connectedNodes.Add(p.Value);
                                        break;
                                }
                            }
                            firstRowConnectionCount++;
                        }
                        else if (connectionSize == 1) // connect random 1 node
                        {
                            // set to be connected node
                            Node n1 = nodes[new Vector2Int(Random.Range(highestX, p.Key.x + 2), nextRow)];
                            // connect node to self
                            p.Value.connectedNodes.Add(n1);
                            // connect self to node
                            n1.connectedNodes.Add(p.Value);
                            firstRowConnectionCount++;
                        }
                    }
                }
                else if (p.Key.y == (gridHeight - 1)) // last row
                {
                    if (p.Value.connectedNodes.Count > 0)
                    {
                        p.Value.connectedNodes.Add(bossNode);
                    }
                }
                else // in between rows
                {
                    if (p.Value.connectedNodes.Count > 0)
                    {
                        if (p.Key.x == 0) // first node in the row
                        {
                            connectionSize = Random.Range(1, 3);
                            if (connectionSize == 2) // connect both node above
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(0, nextRow)];
                                Node n2 = nodes[new Vector2Int(1, nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                p.Value.connectedNodes.Add(n2);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                                n2.connectedNodes.Add(p.Value);
                            }
                            else if (connectionSize == 1) // connect random 1 node
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(Random.Range(0, 2), nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                            }
                        }
                        else if (p.Key.x == (gridWidth - 1)) // last node in the row
                        {
                            Node prevNode = nodes[new Vector2Int(p.Key.x - 1, p.Key.y)];
                            int highestX = prevNode.coord.x; // highest to be self node x by default for calculation purpose
                            // check prev node has connections
                            if (prevNode.connectedNodes.Count > 0)
                            {
                                foreach (Node n in prevNode.connectedNodes)
                                {
                                    if (n.coord.x > highestX)
                                    {
                                        highestX = n.coord.x;
                                    }
                                }
                            }

                            int possibleConnectionCount = gridWidth - highestX; // -1 for grid +1 for connection
                            connectionSize = Random.Range(1, possibleConnectionCount + 1);

                            if (connectionSize == 2) // connect both node above
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(gridWidth - 1, nextRow)];
                                Node n2 = nodes[new Vector2Int(gridWidth - 2, nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                p.Value.connectedNodes.Add(n2);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                                n2.connectedNodes.Add(p.Value);
                            }
                            else if (connectionSize == 1) // connect random 1 node
                            {
                                if (connectionSize == possibleConnectionCount)
                                {
                                    // set to be connected node
                                    Node n1 = nodes[new Vector2Int(gridWidth - 1, nextRow)];
                                    // connect node to self
                                    p.Value.connectedNodes.Add(n1);
                                    // connect self to node
                                    n1.connectedNodes.Add(p.Value);
                                }
                                else
                                {
                                    // set to be connected node
                                    Node n1 = nodes[new Vector2Int(Random.Range(gridWidth - 2, gridWidth), nextRow)];
                                    // connect node to self
                                    p.Value.connectedNodes.Add(n1);
                                    // connect self to node
                                    n1.connectedNodes.Add(p.Value);
                                }
                            }
                        }
                        else if (p.Key.x > 0) // other nodes except the first one and last one
                        {
                            Node prevNode = nodes[new Vector2Int(p.Key.x - 1, p.Key.y)];
                            int highestX = prevNode.coord.x; // highest to be self node x by default for calculation purpose
                            // check prev node has connections
                            if (prevNode.connectedNodes.Count > 0)
                            {
                                foreach (Node n in prevNode.connectedNodes)
                                {
                                    if (n.coord.x > highestX)
                                    {
                                        highestX = n.coord.x;
                                    }
                                }
                            }

                            int possibleConnectionCount = p.Key.x - highestX + 2; // +2 for max node possible while prevous node connected to the one above
                            connectionSize = Random.Range(1, possibleConnectionCount + 1);

                            if (connectionSize == 3) // connect all 3 nodes
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(p.Key.x - 1, nextRow)];
                                Node n2 = nodes[new Vector2Int(p.Key.x, nextRow)];
                                Node n3 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                p.Value.connectedNodes.Add(n2);
                                p.Value.connectedNodes.Add(n3);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                                n2.connectedNodes.Add(p.Value);
                                n3.connectedNodes.Add(p.Value);
                            }
                            else if (connectionSize == 2)
                            {
                                if (connectionSize == possibleConnectionCount)// connect to both nodes above and on the right
                                {
                                    // set to be connected node
                                    Node n1 = nodes[new Vector2Int(p.Key.x, nextRow)];
                                    Node n2 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];
                                    // connect node to self
                                    p.Value.connectedNodes.Add(n1);
                                    p.Value.connectedNodes.Add(n2);
                                    // connect self to node
                                    n1.connectedNodes.Add(p.Value);
                                    n2.connectedNodes.Add(p.Value);
                                }
                                else // connect 2 out of 3 nodes
                                {
                                    int s = Random.Range(0, 3);
                                    // set to be connected node
                                    Node n1 = nodes[new Vector2Int(p.Key.x - 1, nextRow)];
                                    Node n2 = nodes[new Vector2Int(p.Key.x, nextRow)];
                                    Node n3 = nodes[new Vector2Int(p.Key.x + 1, nextRow)];

                                    switch (s)
                                    {
                                        case 0:
                                            // connect node to self
                                            p.Value.connectedNodes.Add(n1);
                                            p.Value.connectedNodes.Add(n2);
                                            // connect self to node
                                            n1.connectedNodes.Add(p.Value);
                                            n2.connectedNodes.Add(p.Value);
                                            break;
                                        case 1:
                                            // connect node to self
                                            p.Value.connectedNodes.Add(n2);
                                            p.Value.connectedNodes.Add(n3);
                                            // connect self to node
                                            n2.connectedNodes.Add(p.Value);
                                            n3.connectedNodes.Add(p.Value);
                                            break;
                                        case 2:
                                            // connect node to self
                                            p.Value.connectedNodes.Add(n1);
                                            p.Value.connectedNodes.Add(n3);
                                            // connect self to node
                                            n1.connectedNodes.Add(p.Value);
                                            n3.connectedNodes.Add(p.Value);
                                            break;
                                        default:
                                            // connect node to self
                                            p.Value.connectedNodes.Add(n1);
                                            p.Value.connectedNodes.Add(n2);
                                            // connect self to node
                                            n1.connectedNodes.Add(p.Value);
                                            n2.connectedNodes.Add(p.Value);
                                            break;
                                    }
                                }
                            }
                            else if (connectionSize == 1) // connect random 1 node
                            {
                                // set to be connected node
                                Node n1 = nodes[new Vector2Int(Random.Range(highestX, p.Key.x + 2), nextRow)];
                                // connect node to self
                                p.Value.connectedNodes.Add(n1);
                                // connect self to node
                                n1.connectedNodes.Add(p.Value);
                            }
                        }
                    }
                }

                // create line render
                //if (p.Value.connectedNodes.Count > 0)
                //{
                //    foreach (Node n in p.Value.connectedNodes)
                //    {
                //        LineRenderer line = Instantiate(lineObject, canvasTransform).GetComponent<LineRenderer>();
                //        line.positionCount = 2;
                //        line.SetPosition(0, n.transform.position);
                //        line.SetPosition(1, p.Value.transform.position);
                //        lines.Add(line);
                //    }
                //}
                //else
                //{
                //    Destroy(nodes[p.Key].gameObject);
                //}
                DrawMapPath(p.Value);
            }
        }

        SaveMap();
    }

    void DrawMapPath(Node n)
    {
        if (n.connectedNodes.Count > 0 || n.nodeType == NodeType.BOSS)
        {
            //foreach (Node node in n.connectedNodes)
            //{
            //    LineRenderer line = Instantiate(lineObject, canvasTransform).GetComponent<LineRenderer>();
            //    line.positionCount = 2;
            //    line.SetPosition(0, node.transform.position);
            //    line.SetPosition(1, n.transform.position);
            //    lines.Add(line);
            //}

            foreach (Node node in n.connectedNodes)
            {
                UILine line = Instantiate(lineObject, linesTransform).GetComponent<UILine>();
                line.pointA = node.GetComponentInChildren<Button>().GetComponent<RectTransform>();
                line.pointB = n.GetComponentInChildren<Button>().GetComponent<RectTransform>();

                lines.Add(line);
            }
        }
        else
        {
            Destroy(nodes[n.coord].gameObject);
        }

        // activate button
        if (n.nodeLevel > PlayerPrefs.GetInt("MapLevel")) // future route
        {
            n.DeactivateButton();
        }
        else if (n.nodeLevel < PlayerPrefs.GetInt("MapLevel")) // past route
        {
            if (n.isActivated)
            {
                n.MarkPastButton();
            }
            else
            {
                n.DeactivateButton();
            }
            n.isUnlocked = false;
        }
        else // current route
        {
            //n.isUnlocked = true;
            
            // only unlock node who has prevous node unlocked
            foreach (var node in n.connectedNodes)
            {
                if (node.isActivated)
                {
                    n.isUnlocked = true;
                }
            }
            // activate button
            if (n.isUnlocked)
            {
                n.ActivateButton();
            }
            else
            {
                n.DeactivateButton();
            }
        }
    }

    public void SaveMap()
    {
        MapSaveData mapData = new MapSaveData();

        // loop through nodes and add to save data
        foreach (var p in nodes)
        {
            NodeSaveData nodeData = new NodeSaveData();
            nodeData.id = p.Value.id;
            nodeData.nodeType = p.Value.nodeType;
            nodeData.isUnlocked = p.Value.isUnlocked;
            nodeData.nodeLevel = p.Value.nodeLevel;
            nodeData.isActivated = p.Value.isActivated;
            nodeData.coord = p.Value.coord;

            foreach (var n in p.Value.connectedNodes)
            {
                nodeData.connectedNode.Add(n.id);
            }

            mapData.savedNodes.Add(nodeData);
        }

        // save to file
        string json = JsonUtility.ToJson(mapData);
        File.WriteAllText(savePath, json);
    }

    public void LoadMap()
    {
        if (!File.Exists(savePath))
        {
            return;
        }

        string json = File.ReadAllText(savePath);
        MapSaveData mapData = JsonUtility.FromJson<MapSaveData>(json);

        nodes.Clear();

        Dictionary<int, Node> indexedNodes = new Dictionary<int, Node>();

        // create node
        foreach (var nodeData in mapData.savedNodes)
        {
            Node node = Instantiate(nodeObject, nodesTransform).GetComponent<Node>();

            node.id = nodeData.id;
            node.coord = nodeData.coord;
            node.isActivated = nodeData.isActivated;
            node.isUnlocked = nodeData.isUnlocked;
            node.nodeLevel = nodeData.nodeLevel;
            node.nodeType = nodeData.nodeType;
            node.connectedNodes = new List<Node>();

            if (node.nodeType == NodeType.BOSS)
            {
                node.transform.localPosition = new Vector3(0, (gridHeight + 1) * 100, 0);
            }
            else
            {
                node.transform.localPosition = new Vector3((node.coord.x - (gridWidth - 1) / 2) * 100, (node.coord.y) * 100, 0);
            }

            node.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = node.nodeType.ToString()[0].ToString();

            nodes[node.coord] = node;
            indexedNodes[node.id] = node;
        }

        // reconnect nodes
        foreach (var nodeData in mapData.savedNodes)
        {
            Node node = indexedNodes[nodeData.id];

            foreach (var id in nodeData.connectedNode)
            {
                if (indexedNodes.TryGetValue(id, out Node connectedNode))
                {
                    node.connectedNodes.Add(connectedNode);
                }
            }

            DrawMapPath(node);
        }
    }
}
