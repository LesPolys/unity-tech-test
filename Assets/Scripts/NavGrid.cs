using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/*
Simple A* explanation:

Start: You start at a specific point in the maze (the “start node”).

Goal: You know where you want to end up (the “goal node”).

Neighbors: From your current position, you can move to several other spots (these are your “neighbors”). In a 2D grid, usually, you can move up, down, left, or right.

G Cost: For each neighbor, you calculate the cost to move there from the start. This is known as the “G cost”. Usually, moving one square might have a cost of 1.

H Cost: You also estimate the cost to move from each neighbor to the goal. This is known as the “H cost” or “heuristic”. 
A simple way to calculate this might be the “Manhattan distance” - how many squares left/right and up/down you would need to move to get to the goal in a straight line.

F Cost: You add the G cost and H cost together to get the “F cost”.

Choosing Paths: You then move to the neighbor with the lowest F cost.

Repeat: You repeat this process, always moving to the node with the lowest F cost, until you reach the goal.
*/

public class NavGrid : MonoBehaviour
{
    public Transform playerTransform;
    public LayerMask unwalkableMask;
    public Vector2 gridSize;
    public float nodeRadius;
    
    public List<NavGridPathNode> path;

    NavGridPathNode[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;
    public Transform seeker;

    void Start(){
        nodeDiameter = nodeRadius *2;
        gridSizeX = Mathf.RoundToInt(gridSize.x/nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridSize.y/nodeDiameter);
        CreateGrid(); 
    }

    //Create a grid of nodes, nodes are not walkable if the contact an object with the unwalkable mask
    void CreateGrid(){
        grid = new NavGridPathNode[gridSizeX,gridSizeY];
        Vector3 startingCorner = transform.position - Vector3.right * gridSize.x/2 - Vector3.forward * gridSize.y/2;

        for(int x = 0; x < gridSizeX; x++){
            for(int y = 0; y < gridSizeY; y++){
                Vector3 point = startingCorner + Vector3.right * (x * nodeDiameter + nodeRadius)  + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(point, nodeRadius,unwalkableMask));
                grid[x,y] = new NavGridPathNode(walkable, point, x, y); 
            }
        }
    }

    //Gets the nodes adjacent to the one provided, used for path calculation
    public List<NavGridPathNode> GetAdjacentNodes(NavGridPathNode node){
 		List<NavGridPathNode> adjacent = new List<NavGridPathNode>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0)
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					adjacent.Add(grid[checkX,checkY]);
				}
			}
		}

		return adjacent;
    }

    //conversion from transform pos of grid or player or raycast into a node on the grid
    public NavGridPathNode WorldPositionToNode(Vector3 position){
        float percentX = (position.x + gridSize.x/2) / gridSize.x;
        float percentY = (position.z + gridSize.y/2) / gridSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x,y];
    }

    /// <summary>
    /// Given the current and desired location, return a path to the destination
    /// </summary>
    public NavGridPathNode[] GetPath(Vector3 origin, Vector3 destination){
	    NavGridPathNode startNode = WorldPositionToNode(origin);
		NavGridPathNode targetNode = WorldPositionToNode(destination);

		Heap<NavGridPathNode> openSet = new Heap<NavGridPathNode>(gridSizeX * gridSizeY);
		HashSet<NavGridPathNode> closedSet = new HashSet<NavGridPathNode>();
		openSet.Add(startNode);

		while (openSet.Count > 0) {
			NavGridPathNode node = openSet.RemoveFirst();
			closedSet.Add(node);

			if (node == targetNode) {
				return RetracePath(startNode,targetNode);
			}

			foreach (NavGridPathNode adjacent in GetAdjacentNodes(node)) {
				if (!adjacent.isWalkable || closedSet.Contains(adjacent)) {
					continue;
				}

				int newCostToNeighbour = node.gCost + GetDistance(node, adjacent);
				if (newCostToNeighbour < adjacent.gCost || !openSet.Contains(adjacent)) {
					adjacent.gCost = newCostToNeighbour;
					adjacent.hCost = GetDistance(adjacent, targetNode);
					adjacent.parent = node;

					if (!openSet.Contains(adjacent))
						openSet.Add(adjacent);
				}
			}
		}
        return null;
    }

    //After path calculated. we need to flip the path so it goes from start to end.
    NavGridPathNode[] RetracePath(NavGridPathNode startNode, NavGridPathNode endNode){
        List<NavGridPathNode> _path = new List<NavGridPathNode>();
        NavGridPathNode currentNode = endNode;

        while(currentNode != startNode){
            _path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        _path.Reverse();
        path = _path;
        return _path.ToArray();
    }

    //used for a* calculation
    int GetDistance(NavGridPathNode a, NavGridPathNode b){
        int distX = Mathf.Abs(a.gridX - b.gridX);
        int distY = Mathf.Abs(a.gridY - b.gridY);

        if(distX > distY){
            return 14*distY + 10*(distX-distY);
        }else{
            return 14*distX + 10*(distY-distX);
        }
    }

    //debug draw for grid
    void OnDrawGizmos(){
        Gizmos.DrawWireCube(transform.position,new Vector3(gridSize.x,1,gridSize.y));

        if(grid != null){

            NavGridPathNode playerNode = WorldPositionToNode(playerTransform.position);

            foreach(NavGridPathNode n in grid){
                Gizmos.color = (n.isWalkable) ? Color.white : Color.red; 
                if(playerNode == n){
                  Gizmos.color = Color.cyan;  
                }

                if(path!=null){
                    if(path.Contains(n)){
                         Gizmos.color = Color.green;  
                    }
                }
                Gizmos.DrawCube(n.Position, Vector3.one * (nodeDiameter - .1f));
            }
        }    
    }
}
