using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NavGridPathNode: IHeapItem<NavGridPathNode> {
	
	public bool isWalkable;
	public Vector3 Position;
	public int gridX;
	public int gridY;
	public int gCost;
	public int hCost;
	public NavGridPathNode parent;
	int heapIndex;
	
	public NavGridPathNode(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridY) {
		isWalkable = _isWalkable;
		Position = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
	}

	public int fCost {
		get {
			return gCost + hCost;
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo(NavGridPathNode nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
		if (compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}