using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//TreeEditor is editor only comment out before builds
//using TreeEditor;

public class MapManager : MonoBehaviour
{
    public GameObject nodeObject;
    public GameObject lineObject;
    public Transform canvasTransform;
    public Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    public List<LineRenderer> lines;
    private int gridHeight = 7;
    private int gridWidth = 10;

    // Start is called before the first frame update
    void Start()
    {
        // create node and add them to the grid
        for (int i = 0; i < gridHeight; i++)
        {
            for (int j = 0; j < gridWidth; j++)
            {
                Node node = Instantiate(nodeObject, canvasTransform).GetComponent<Node>();
                node.transform.localPosition = new Vector3((j - (gridWidth - 1) / 2)*100, (i - gridHeight / 2) * 100, 0);

                node.coord = new Vector2Int(j, i);
                node.nodeType = (NodeType)Random.Range(0, 3);
                node.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = node.nodeType.ToString()[0].ToString();
                
                nodes[new Vector2Int(j,i)] = node;
            }
        }

        // boss node
        Node bossNode = Instantiate(nodeObject, canvasTransform).GetComponent<Node>();
        bossNode.transform.localPosition = new Vector3(0, (gridHeight / 2 + 1) * 100, 0);

        bossNode.GetComponentInChildren<Button>().GetComponentInChildren<TextMeshProUGUI>().text = bossNode.nodeType.ToString()[0].ToString();

        int connectionSize;
        int firstRowConnectionCount = 0;

        while (firstRowConnectionCount < (gridWidth / 2)) // make sure the map will always have more than half of the node activated each run
        {
            foreach (KeyValuePair<Vector2Int, Node> p in nodes)
            {
                int nextRow = p.Key.y + 1;

                if (p.Key.y == 0) // first row
                {
                    p.Value.connectedNodes.Clear(); // reset

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
                            firstRowConnectionCount ++;
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
                            foreach(LineRenderer l in lines)
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
                            if(connectionSize == possibleConnectionCount)// connect to both nodes above and on the right
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
                            firstRowConnectionCount ++;
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
                if (p.Value.connectedNodes.Count > 0)
                {
                    foreach (Node n in p.Value.connectedNodes)
                    {
                        LineRenderer line = Instantiate(lineObject,canvasTransform).GetComponent<LineRenderer>();
                        line.positionCount = 2;
                        line.SetPosition(0, n.transform.position);
                        line.SetPosition(1, p.Value.transform.position);
                        lines.Add(line);
                    }
                }
                else
                {
                    Destroy(nodes[p.Key].gameObject);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
