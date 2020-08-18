using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour                                                   
{
    public bool dispalyGridGizmos;

    public Transform player;
    public LayerMask unwalkableMask;                                                
    public Vector2 gridWorldSize;                                                   
    public float nodeRadius;    // Promień   
    public TerrainType[] walkableRegions;
    public int obstacleProximityPenalty = 10;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    int maxTerrainPenalty = 0;

    Node[,] grid;                                                                   
                                                                                    
    float nodeDiameter;     // Średnica                                             
    int gridSizeX, gridSizeY;

    int penaltyMax = int.MinValue;
    int penaltyMin = int.MaxValue;
                   
    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }
                                                                                    
    private void Awake()                                                            
    {                                                                               
        nodeDiameter = nodeRadius * 2;                                              
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);               
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
         
        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add(Mathf.RoundToInt(Mathf.Log(region.terrainMask.value,2)), region.terainPenalty);
            if (region.terainPenalty >= maxTerrainPenalty)
            {
                maxTerrainPenalty = region.terainPenalty;
            }
        }
                                                                                    
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

                int movementPenalty = 0;


                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100, walkableMask))
                {
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                    //Debug.Log(movementPenalty);
                }

                if (!walkable)
                {
                    movementPenalty += obstacleProximityPenalty;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize)                               // ni chuja nie mam pojęcia jak to działa https://www.youtube.com/watch?v=Tb-rM3wGwv4&list=PLFt_AvWsXl0cq5Umv3pMC9SPnKjfp9eGW&index=7
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = blurSize;                               // wielkość siatki do blurowania

        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++)                         // Horizontal Pass
        {
            for (int x = -kernelExtents; x <= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += Mathf.Clamp(grid[sampleX, y].movementPenalty, 0, maxTerrainPenalty);
            }

            for(int x = 1; x < gridSizeX; x++)
            {
                int newNode = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int oldNode = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[oldNode, y].movementPenalty + grid[newNode, y].movementPenalty;
            }
        }

        for (int x = 0; x < gridSizeX; x++)                         // Vertical Pass
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int bluredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = bluredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            {
                int newNode = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int oldNode = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, oldNode] + penaltiesHorizontalPass[x, newNode];
                bluredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x,y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = bluredPenalty;

                Debug.Log(bluredPenalty);

                if (bluredPenalty > penaltyMax)                     // Gizmos
                {
                    penaltyMax = bluredPenalty;
                }

                if (bluredPenalty < penaltyMin)
                {
                    penaltyMin = bluredPenalty;
                }
            }
        }
        Debug.Log($"Min:{penaltyMin}, Max:{penaltyMax}");
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


    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
     
        if (grid != null && dispalyGridGizmos)
        {
            foreach (Node n in grid)
            {

                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPenalty));
                Gizmos.color = (n.walkable) ? Gizmos.color : Color.red;      // ? = THEN, : = ELSE
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
                // Gizmos.color = (n.walkable) ? Color.green : Color.red;      // ? = THEN, : = ELSE
                // Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }

    [System.Serializable]
    public class TerrainType
    {
        public LayerMask terrainMask;
        public int terainPenalty;
    }
}