using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Grid : MonoBehaviour {
    public bool displayGridGizmos;

    public Transform robot;
    public LayerMask obstacleMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public TerrainType[] traversableRegions;
    public int obstacleProximityPenalty = 10;
    LayerMask traversableMask;
    Dictionary<int, int> traversableRegionsDictionary = new Dictionary<int, int>();

    Node[,]  grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    void Awake() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach (TerrainType region in traversableRegions) {
            traversableMask.value |= region.terrainMask.value;
            traversableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();
    }

    public int MaxSize {
        get {
            return gridSizeX * gridSizeY;
        }
    }

    void CreateGrid() {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool traversable = !Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask);

                int movementPenalty = 0;

                Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 100, traversableMask)) {
                    traversableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if (!traversable) {
                    movementPenalty += obstacleProximityPenalty;
                }

                grid[x, y] = new Node(traversable, worldPoint, x, y, movementPenalty);
            }
        }

        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize) {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalPass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalPass = new int[gridSizeX, gridSizeY];

        for (int y = 0; y < gridSizeY; y++) {
            for (int x = -kernelExtents; x <= kernelExtents; x++) {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].movementPenalty;
            }

            for (int x = 1; x < gridSizeX; x++) {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - grid[removeIndex, y].movementPenalty + grid[addIndex, y].movementPenalty;
            }
        }

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = -kernelExtents; y <= kernelExtents; y++) {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].movementPenalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++) {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].movementPenalty = blurredPenalty;

                if (blurredPenalty > penaltyMax) {
                    penaltyMax = blurredPenalty;
                }
                if (blurredPenalty < penaltyMin) {
                    penaltyMin = blurredPenalty;
                }
            }
        }
    }

    public List<Node> GetNeighbors(Node node) {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    public Node GetClosestTraversableNode(Node node) {
        int maxRadius = Mathf.RoundToInt((float)Math.Sqrt(gridSizeX * gridSizeX + gridSizeY * gridSizeY));

        List<Node> closestNodes = new List<Node>();
        for (int r = 0; r <= maxRadius; r++) {
            for (int x = 0; x <= r; x++) {
                int positiveX = Math.Clamp(node.gridX + x, 0, gridSizeX-1);
                int negativeX = Math.Clamp(node.gridX - x, 0, gridSizeX-1);
                int positiveY = Math.Clamp(node.gridY + r - x, 0, gridSizeY-1);
                int negativeY = Math.Clamp(node.gridY - r + x, 0, gridSizeY-1);

                if (grid[positiveX, positiveY].traversable) closestNodes.Add(grid[positiveX, positiveY]);
                if (grid[positiveX, negativeY].traversable) closestNodes.Add(grid[positiveX, negativeY]);
                if (grid[negativeX, positiveY].traversable) closestNodes.Add(grid[negativeX, positiveY]);
                if (grid[negativeX, negativeY].traversable) closestNodes.Add(grid[negativeX, negativeY]);
            }

            if (closestNodes.Count > 0) break;
        }

        Node closestNode = closestNodes[0];
        int minDistance = Pathfinding.GetDistance(closestNode, node);
        for (int i = 1; i < closestNodes.Count; i++) {
            int distance = Pathfinding.GetDistance(node, closestNodes[i]);
            if (distance < minDistance) {
                minDistance = distance;
                closestNode = closestNodes[i];
            }
        }

        return closestNode;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY-1) * percentY);

        return grid[x, y];
    }

    // public List<Node> path;
    void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        // if (grid != null) {
        //     foreach (Node node in grid) {
        //         if (node.movementPenalty > 0) {
        //             Gizmos.color = Color.green;
        //             Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - .1f));
        //         }
        //     }
        // }

        // if (onlyDisplayPathGizmos) {
        //     if (path != null) {
        //         foreach (Node node in path) {
        //             Gizmos.color = Color.magenta;
        //             Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - .1f));
        //         }
        //     }
        // } else {
            if (grid != null && displayGridGizmos) {
                Node robotNode = NodeFromWorldPoint(robot.position);
                foreach (Node node in grid) {

                    Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, node.movementPenalty));

                    Gizmos.color = node.traversable ? Gizmos.color : Color.red;
                    // if (node == robotNode) {
                    //     Gizmos.color = Color.cyan;
                    // }
                    // if (path != null) {
                    //     if (path.Contains(node)) {
                    //         Gizmos.color = Color.black;
                    //     }
                    // }
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter));
                }
            }
        //}
    }

    [Serializable]
    public class TerrainType {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }

    // ADDED .93 + .2 TO ALL PATHFINDING OBSTACLES
}
