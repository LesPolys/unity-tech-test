using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private NavGridPathNode[] _currentPath = new NavGridPathNode[0];
    private int _currentPathIndex = 0;
    
    [SerializeField]
    private NavGrid _grid;
    [SerializeField]
    private float _speed = 10.0f;
    [SerializeField]
    private float turnSpeed = 10;
    [SerializeField]
	private float turnDst = 2;
    [SerializeField]
	private float stoppingDst = 5;

    const float minPathUpdateTime = .2f;
	const float pathUpdateMoveThreshold = .5f;

	Path path;

    public void Update()
    {
        // Check Input
        if (Input.GetMouseButtonUp(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo) && _grid.WorldPositionToNode(hitInfo.point).isWalkable)
            {
                _currentPath = _grid.GetPath(transform.position, hitInfo.point);
                OnPathFound(_currentPath);
            }
        }
    }

    //Convert Nodes to smooth path and have player follow it
	public void OnPathFound(NavGridPathNode[] waypoints) {
		path = new Path(waypoints, transform.position, turnDst, stoppingDst);

		StopCoroutine("FollowPath");
		StartCoroutine("FollowPath");
	}

    //Draw the Path
	public void OnDrawGizmos() {
		if (path != null) {
			path.DrawWithGizmos ();
		}
	}

    //Follow the smooted path
	private IEnumerator FollowPath() {

		bool followingPath = true;
		int pathIndex = 0;
		transform.LookAt (path.lookPoints [0]);

		float speedPercent = 1;

		while (followingPath) {
			Vector2 pos2D = new Vector2 (transform.position.x, transform.position.z);
			while (path.turnBoundaries [pathIndex].HasCrossedLine (pos2D)) {
				if (pathIndex == path.finishLineIndex) {
					followingPath = false;
					break;
				} else {
					pathIndex++;
				}
			}

			if (followingPath) {

				if (pathIndex >= path.slowDownIndex && stoppingDst > 0) {
					speedPercent = Mathf.Clamp01 (path.turnBoundaries [path.finishLineIndex].DistanceFromPoint (pos2D) / stoppingDst);
					if (speedPercent < 0.01f) {
						followingPath = false;
					}
				}

				Quaternion targetRotation = Quaternion.LookRotation (path.lookPoints [pathIndex] - transform.position);
				transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
				transform.Translate (Vector3.forward * Time.deltaTime * _speed * speedPercent, Space.Self);
			}

			yield return null;

		}
	}
}
