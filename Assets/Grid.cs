using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour                                                   
{
    public Transform player;
    public LayerMask unwalkableMask;                                                
    public Vector2 gridWorldSize;                                                   
    public float nodeRadius;    // Promień                                          
    Node[,] grid;                                                                   
                                                                                    
    float nodeDiameter;     // Średnica                                             
    int gridSizeX, gridSizeY;                                                       
                                                                                    
                                                                                    
    private void Start()                                                            
    {                                                                               
        nodeDiameter = nodeRadius * 2;                                              
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);               
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);               
                                                                                    
        CreateGrid();                                                               
    }                                                                               


    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for(int x = 0; x < gridSizeX; x++)
        {
            for(int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);     // Ustalenie punktu w przestrzenieni każdej komórki/Node (Środka)
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node centerNode)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int neighbourX = centerNode.gridX + x;
                int neighbourY = centerNode.gridY + y;

                if (neighbourX >= 0 && neighbourX < gridSizeX && neighbourY >= 0 && neighbourY < gridSizeY)
                {
                    neighbours.Add(grid[neighbourX, neighbourY]);
                }
            }
        }

        return neighbours;
    }

    public List<Node> path;
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null)
        {
            foreach(Node n in grid)
            {
                Node playerNode = NodeFromWorldPoint(player.position);
                Gizmos.color = (n.walkable) ? Color.green : Color.red;      // ? = THEN, : = ELSE
                if (path != null)
                {
                    if (playerNode == n)
                    {
                        Gizmos.color = Color.magenta;
                    }
                    else if (path.Contains(n))
                    {
                        Gizmos.color = Color.blue;
                    }
                }
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}