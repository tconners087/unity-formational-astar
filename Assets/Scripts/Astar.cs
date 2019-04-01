using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class Astar : MonoBehaviour {
	#region data
	public GameObject nodeObject;
	//private Vector3 rayStart;
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
	private Dictionary<string, List<AstarNode>> occupiedDict;

	public AstarUser user;
	#endregion

	void Awake () {
		//when raycasting against background, the Ground or Units will be ignored
		Background = ~(1 << LayerMask.NameToLayer ("Ground") | 1 << LayerMask.NameToLayer("Units"));
		nodes = new List<AstarNode> ();
		minNodeDistance = 2;

	}

	/// <summary>
	/// Coroutine that decomposes the scene to create a grid of AstarNode objects for pathing.
	/// It returns the start() Coroutine of the AstarUser reference 'user'.
	/// This is called once from Start() of GameController singleton.
	/// </summary>
	public IEnumerator decompose(){
		//yield return new WaitForSeconds (0.1f);
		reset ();
		createGrid ();
		createNodes ();
		yield return StartCoroutine (user.start ());
	}

	/// <summary>
	/// Populates the nodeGrid[,] Vector3 array with evenly spaced Vector3s that span the surface of the scene's ground mesh.
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
				j++; min.z += minNodeDistance;
			}
			min.x += minNodeDistance;
			min.z = minMax [0].z;
			i++; j = 0;
		}
	}

	/// <summary>
	/// Returns a Vector3[] that holds the minimum and maximum extents of the ground mesh.
	/// The minimum is stored at index 0 and the maximum is stored at index 1.
	/// </summary>
	/// <returns>The room extents world.</returns>
	Vector3[] getRoomExtentsWorld() {
		//holds the minimum and maximum extents of the ground mesh
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
				n.setIsOccupied (true);
			} else
				n.setIsOccupied (false);
		}
	}

	/// <summary>
	/// Populates the list of AstarNode objects by determining if the position stored
	/// in the nodeGrid[,] array of Vector3s is a pathable position.
	/// </summary>
	void createNodes() {
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
	/// Instantiates gameobjects to visually represent the nodes of the AstarNode list.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void createNodeMarkers(List<AstarNode> roomNodes) {
		foreach (AstarNode n in roomNodes) {
			n.setNodeObj (GameObject.Instantiate (nodeObject, n.getLocation (), Quaternion.identity));
			n.getObject ().SetActive (false);
			n.getObject ().layer = 2;
		}
	}

	/// <summary>
	/// Gets a hard copy of the the list of AstarNode objects associated with the scene.
	/// </summary>
	/// <returns>The node list clone.</returns>
	public List<AstarNode> getNodeListClone() {
		List<AstarNode> temp = new List<AstarNode> ();
		temp = nodes.ConvertAll (node => new AstarNode (node.getRow (), node.getCol (), node.getLocation (), node.getObject ()));
		return temp;
	}

	/// <summary>
	/// Sets the H costs of each node. If the node is currently occupied by an obstacle,
	/// he H cost is set to 5000 to ensure that the leader never paths to that node.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	/// <param name="goal">Goal.</param>
	public void setHCosts(List<AstarNode> roomNodes, AstarNode goal) {
		foreach (AstarNode n in roomNodes) {
			if (n.getIsOccupied()){
				n.setH (5000);
				continue;
			}
			n.setH ((Mathf.Abs (n.getRow () - goal.getRow ()) + Mathf.Abs(n.getCol() - goal.getCol())) * 10);
		}
	}

	/// <summary>
	/// Sets the neighboring nodes of each node in the supplied roomNodes list.
	/// This method currently kills performance when decomposing a scene and 
	/// causes the application to hang momentarily when decomposing. In the future,
	/// I should find a better way to determine of a neighboring node exists.
	/// I think "neighbor = roomNodes.Find(node => node.getRow...);" is the problem.
	/// </summary>
	/// <param name="roomNodes">Room nodes.</param>
	public void setNeighbors(List<AstarNode> roomNodes) {
		foreach (AstarNode n in roomNodes) {
			int row = n.getRow (); int col = n.getCol (); AstarNode neighbor;
			for (int i = -1; i < 2; i++) {
				for (int j = -1; j < 2; j++) { //check all surrounding nodes
					neighbor = roomNodes.Find (node => node.getRow () == (row + i) && node.getCol () == (col + j));
					if (neighbor != null && !neighbor.compareTo(n)) { //if sought neighbor exists and is not n
						RaycastHit hit;
						Ray ray = new Ray (neighbor.getLocation (), n.getLocation ());
						//check that the node can be reached from the current node (no obstacle in the way)
						if (!Physics.Raycast (ray, out hit, Vector3.Distance (n.getLocation (), neighbor.getLocation ()), Background)){
							n.getList ().Add (neighbor);
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
				if (n.getObject() != null){
					if (n.getStart ()) {
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.red);
					} else if (n.getGoal ()) {
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.blue);
					} else if (n.getOnPath ()) { 
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.magenta);
					} else if (n.getInOpenList ()) {
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.yellow);
					} else if (n.getInClosedList ()) {
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.white);
					} else if (n.getIsOccupied ()) {
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.clear);
					} else
						n.getObject ().GetComponent<MeshRenderer> ().material.SetColor ("_Color", Color.black);
				}
			}
		}
	}

	void reset() {
		nodes.Clear ();
		destroyObjects ("nodeObject");
	}

	void destroyObjects(string s) {
		GameObject[] objs = GameObject.FindGameObjectsWithTag (s);
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
			n.getObject ().gameObject.SetActive (true);
		}
	}
}
