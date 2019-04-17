using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class Astar : MonoBehaviour {
	#region data

	public GameObject nodeObject;
	[SerializeField] private GameObject player;
	[SerializeField] private MeshRenderer room;
	private Vector3[] minMax;
	private Vector3[,] nodeGrid;
	private Vector3 playerPosition;
	private int length, height, minNodeDistance;
	private float xInc, yInc;
	private bool hitsNotNull, hitsBoundaries, hitDistance;
	LayerMask Background;

	private List<string> keys;
	private List<AstarNode> nodes;

	public AstarUser user;

	#endregion

	void Awake () {
		// Ignore Ground or Units when detecting AstarNode occupation by obstacles. Used in Astar.DetectNodeOccupation method.
		Background = ~(1 << LayerMask.NameToLayer ("Ground") | 1 << LayerMask.NameToLayer("Units"));
		nodes = new List<AstarNode> ();
		minNodeDistance = 2;

	}

	/// <summary>
	/// Coroutine that decomposes the scene to create a grid of AstarNode objects for pathing. This is called once from Start() of GameController singleton.
	/// </summary>
	/// <returns>
	/// IEnumerator start() of the AstarUser reference 'user'.
	/// </returns>
	public IEnumerator decompose(){
		reset ();
		createGrid ();
		createNodes ();
		yield return StartCoroutine (user.start ());
	}

	/// <summary>
	/// Populates the nodeGrid[,] Vector3 array with evenly spaced Vector3s that span the surface of the scene's ground mesh. Currently only tested on Plane mesh. minNodeDistance specifies the distance between every node and is set through the editor.
	/// </summary>
	void createGrid(){
		minMax = getRoomExtentsWorld ();
		length = (int)((minMax [1].x - minMax [0].x) / minNodeDistance);
		height = (int)((minMax [1].z - minMax [0].z) / minNodeDistance);
		nodeGrid = new Vector3[length, height];

		//grid iterators
		Vector3 min = minMax [0];
		Vector3 max = minMax [1];
		int i = 0; int j = 0;

		while (min.x < max.x){
			while (min.z < max.z) {
				nodeGrid [i, j] = new Vector3 (min.x + 1f, 0f, min.z + 1f); 
				j++; 
				min.z += minNodeDistance;
			}
			min.x += minNodeDistance;
			min.z = minMax [0].z;
			i++; 
			j = 0;
		}
	}

	/// <summary>
	/// Populates the list of AstarNode objects by determining if the position stored in the nodeGrid[,] array of Vector3s is a pathable position.
	/// </summary>
	void createNodes() {
		// Change nodes from a list to a hashmap
		for (int i = 0; i < nodeGrid.GetLength(0); i++){
			for (int j = 0; j < nodeGrid.GetLength(1); j++){
				Collider[] c = Physics.OverlapSphere (nodeGrid[i,j], .75f, Background);
				if (c.Length == 0) {
					nodes.Add (new AstarNode ());
					nodes [nodes.Count - 1].setParameters (i, j, nodeGrid [i, j]);
				}
			}
		}
	}

	/// <summary>
	/// Returns a Vector3[] that holds the minimum and maximum extents of the ground mesh.
	/// </summary>
	/// <returns>Vector3[2] where 0th element is minimum position and 1th element is maximum position of the Plane mesh (room) extents.</returns>
	Vector3[] getRoomExtentsWorld() {
		Vector3[] array = new Vector3[2];
		array [0] = room.bounds.min;
		array [1] = room.bounds.max;
		//ensure even numbers for vector elements
		foreach (Vector3 v in array) {
			if (v.x % 2 != 0) v.x--;
			if (v.z % 2 != 0) v.z--;
		}
		return array;
	}

	/// <summary>
	/// Detects whether or not a node is occupied by an obstacle.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void detectNodeOccupation(List<AstarNode> roomNodes) {
		foreach (AstarNode n in roomNodes) {
			Collider[] c = Physics.OverlapSphere (n.getLocation (), .75f, Background);
			if (c.Length != 0) {
				n.isOccupied = true;
			} else
				n.isOccupied = false;
		}
	}

	/// <summary>
	/// Instantiates gameobjects to visually represent the nodes of the AstarNode list.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void createNodeMarkers(List<AstarNode> roomNodes) {
		foreach (AstarNode n in roomNodes) {
			n.nodeSquare = GameObject.Instantiate (nodeObject, n.getLocation (), Quaternion.identity);
			n.nodeSquare.SetActive (false);
			n.nodeSquare.layer = 2;
		}
	}

	/// <summary>
	/// Gets a hard copy of the the list of AstarNode objects associated with the scene.
	/// </summary>
	/// <returns>A clone of the 'nodes' list.</returns>
	public List<AstarNode> getNodeListClone() {
		List<AstarNode> temp = new List<AstarNode> ();
		temp = nodes.ConvertAll (node => new AstarNode (node.getRow (), node.getCol (), node.getLocation (), node.nodeSquare ));
		return temp;
	}

	/// <summary>
	/// Sets the H costs of each node. The H heuristic is an estimation of the cost to reach the goal node from each node. If the node is currently occupied by an obstacle, the H cost is set to 5000 to ensure that the leader never paths to that node.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	/// <param name="goal">Goal node.</param>
	public void setHCosts(List<AstarNode> roomNodes, AstarNode goal) {
		foreach (AstarNode n in roomNodes) {
			if (n.isOccupied){
				n.setH (5000);
				continue;
			}
			n.setH ((Mathf.Abs (n.getRow () - goal.getRow ()) + Mathf.Abs(n.getCol() - goal.getCol())) * 10);
		}
	}

	/// <summary>
	/// Sets the neighboring nodes of each node in the supplied roomNodes list. 
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	/// <param name="nodeLookup">Structure which allows for O(1) lookup of neighboring nodes given a 'row' and 'column'.</param>
	public void setNeighbors(List<AstarNode> roomNodes, Dictionary<int, Dictionary<int, AstarNode>> nodeLookup) {
		Dictionary<int, AstarNode> columnMap;
		foreach (AstarNode n in roomNodes) {
			int row = n.getRow (); 
			int col = n.getCol (); 
			AstarNode neighbor;
			for (int i = -1; i < 2; i++) {
				for (int j = -1; j < 2; j++) {
					if(i == 0 && j == 0)
						continue;
					if(nodeLookup.TryGetValue(row + i, out columnMap)) {
						if(columnMap.TryGetValue(col + j, out neighbor)) {
							if (neighbor != null) {
								RaycastHit hit;
								Ray ray = new Ray (neighbor.getLocation (), n.getLocation ());
								// check that the node can be reached from the current node
								if (!Physics.Raycast (ray, out hit, Vector3.Distance (n.getLocation (), neighbor.getLocation ()), Background)){
									n.getList ().Add (neighbor);
								} 
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Colors the nodeObjects to better visualize the Astar path.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void colorNodes(List<AstarNode> roomNodes) {
		if (roomNodes.Count != 0) {
			foreach (AstarNode n in roomNodes) {
				if (n.nodeSquare != null){
					if (n.start) {
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.red);
					} else if (n.goal) {
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.blue);
					} else if (n.onPath) { 
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.magenta);
					} else if (n.inOpenList) {
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.yellow);
					} else if (n.inClosedList) {
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.white);
					} else if (n.isOccupied) {
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.clear);
					} else
						n.nodeSquare.GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.black);
				}
			}
		}
	}

	/// <summary>
	/// Clears the 'nodes' list and destroys all objects with tag 'nodeObject' in the scene.
	/// </summary>
	void reset() {
		nodes.Clear ();
		destroyObjects ("nodeObject");
	}

	/// <summary>
	/// Finds all objects in a scene with matching Tags and destroys them.
	/// </summary>
	/// <param name="tag">Tag name.</param>
	void destroyObjects(string tag) {
		GameObject[] objs = GameObject.FindGameObjectsWithTag (tag);
		foreach(GameObject obj in objs) {
			Destroy (obj);
		}
	}

	/// <summary>
	/// Visualize the roomNodes and Astar path.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void visualize(List<AstarNode> roomNodes) {
		createNodeMarkers (roomNodes);
		showNodes (roomNodes);
		colorNodes (roomNodes);
	}

	/// <summary>
	/// Shows the nodeObjects associated with each node to visualize the grid.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void showNodes(List<AstarNode> roomNodes) {
		foreach (AstarNode n in roomNodes) {
			n.nodeSquare.gameObject.SetActive (true);
		}
	}
}
