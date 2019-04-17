using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class AstarUser : MonoBehaviour {
	#region data
	private Rigidbody rb;
	private List<AstarNode> roomNodes, openList, closedList;
	private Stack<AstarNode> stackPath;
	[SerializeField] private Astar controller;
	private AstarNode startNode, targetNode;
	private bool foundGoal;
	private float radius;
	[SerializeField] private KinematicArrive kinArrive;
	private KinematicArrive.KinematicSteering steering;
	[SerializeField]private float speed;
	private Vector3 pos;
	private IEnumerator co;
	private Dictionary<int, Dictionary<int, AstarNode>> nodeLookup;
	#endregion

	void Awake() {
		nodeLookup = new Dictionary<int, Dictionary<int, AstarNode>>();
		openList = new List<AstarNode> ();
		closedList = new List<AstarNode> ();
		stackPath = new Stack<AstarNode> ();
	}

	/// <summary>
	/// Retrieves the initial and necessary references for the A* search.
	/// </summary>
	public IEnumerator start() {
		Dictionary<int, AstarNode> columnMap;
		radius = 0.5f; //how close the invisible leader will get to a node before pathing to the next node.
		co = move (true);
		rb = GetComponent<Rigidbody> ();
		roomNodes = controller.getNodeListClone ();
		
		for(int i = 0; i < roomNodes.Count; i++) {
			if(nodeLookup.TryGetValue(roomNodes[i].getRow(), out columnMap) == false) {
				columnMap = new Dictionary<int, AstarNode>();
				nodeLookup.Add(roomNodes[i].getRow(), columnMap);
			}
			columnMap.Add(roomNodes[i].getCol(), roomNodes[i]);
		}

		controller.setNeighbors (roomNodes, nodeLookup);
		controller.visualize (roomNodes);
		yield return null;
	}

	/// <summary>
	/// Coroutine called from the GameController instance when the user left-clicks on the ground mesh in the scene.
	/// </summary>
	/// <param name="target">The Vector3 position of a user's click on the floor mesh.</param>
	public IEnumerator updater(Vector3 target) {
		StopCoroutine(co);
		co = move (true);
		controller.detectNodeOccupation (roomNodes);
		startNode = setStartNode (startNode);
		targetNode = setTargetNode (target, targetNode);
		preSearch (); 
		search (startNode); 
		controller.colorNodes (roomNodes); //re-color node objects to reflect new path
		yield return StartCoroutine (co);
	}

	/// <summary>
	/// Sets the start node as the node closest to the invisible leader.
	/// TODO: Implement better algorithm than linear search.
	/// </summary>
	/// <returns>The start node.</returns>
	/// <param name="s">S.</param>
	AstarNode setStartNode(AstarNode s) {
		foreach (AstarNode n in roomNodes) {
			if (s == null) {
				s = n; 
				s.start = true; 
				continue;
			}
			float newVal = Vector3.Distance (n.getLocation (), this.transform.position);
			float oldVal = Vector3.Distance (s.getLocation (), this.transform.position);
			if (Mathf.Abs(newVal) < Mathf.Abs(oldVal)) {
				s.start = false; 
				n.start = true;
				s = n;
			}
		}
		return s;
	}

	/// <summary>
	/// Sets the target node as the node closest to the user's location selection on the ground mesh.
	/// </summary>
	/// <returns>The target node.</returns>
	/// <param name="target">Target.</param>
	/// <param name="t">T.</param>
	AstarNode setTargetNode(Vector3 target, AstarNode t) {
		foreach (AstarNode n in roomNodes) {
			if (n.isOccupied)
				continue;
			if (t == null) {
				t = n; 
				t.goal = true; 
				continue;
			}
			float newVal = Vector3.Distance (n.getLocation (), target);
			float oldVal = Vector3.Distance (t.getLocation (), target);
			if (Mathf.Abs(newVal) < Mathf.Abs(oldVal)) {
				t.goal = false; 
				n.goal = true;
				t = n;
			}
		}
		return t;
	}

	void FixedUpdate(){}

	/// <summary>
	/// Resets certain valuse within each AstarNode in the roomNodes list, sets new H
	/// costs for each node, and clears all the lists involved in pathing.
	/// </summary>
	void preSearch() {
		controller.setHCosts (roomNodes, targetNode);
		foundGoal = false;
		closedList.Clear (); 
		openList.Clear ();
		stackPath.Clear();
		foreach (AstarNode n in roomNodes) 
			n.reset ();
	}

	/// <summary>
	/// Astar search algorithm (recursive).
	/// </summary>
	/// <param name="current">Current node; initially startNode.</param>
	void search(AstarNode current) {
		if (current.goal) {
			closedList.Add (current);
			current.inClosedList = true;
			createPath (current);
			foundGoal = true;
			return;
		}

		// for each neighbor of the current node, set F and G and add to openList
		foreach (AstarNode n in current.getList()) {
			// if a node is already in the open list, check new route heuristic
			if (n.inOpenList) {
				int tempG;
				if (current.getRow () == n.getRow () || current.getCol () == n.getCol ()) 
					tempG = 10;
				else tempG = 14;
				int possibleRouteF = n.getH () + current.getG () + tempG;
				if (n.getF () < possibleRouteF) 
					continue;
				else { // new route to node is faster than previous route
					n.setParent (current);
					n.setG (current.getG () + tempG);
					n.setF ();
					continue;
				}
			}

			if (n.inClosedList) continue;

			// for an unchecked, pathable neighbor
			n.inOpenList = true; 
			n.setParent(current);
			if (current.getRow () == n.getRow () || current.getCol () == n.getCol ()) 
				n.setG (current.getG () + 10);
			else 
				n.setG (current.getG () + 14);
			n.setF ();

			openList.Add (n);
		}

		current.inOpenList = false; 
		current.inClosedList = true;
		closedList.Add (current);

		//sort openList so that first node has lowest F score
		openList.Sort(compareF);
		current = openList [0];
		openList.RemoveAt (0);
		search (current);
	}

	/// <summary>
	/// Creates the path to be followed by recursively passing itself the parent node of the current node.
	/// </summary>
	/// <param name="n">N.</param>
	void createPath(AstarNode n) {
		stackPath.Push(n);
		n.inClosedList = false;
		n.onPath = true;
		if (n.getParent () != null) 
			createPath (n.getParent ());
	}

	/// <summary>
	/// Compares the F costs of two AstarNode objects.
	/// </summary>
	/// <returns>The f.</returns>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	int compareF(AstarNode a, AstarNode b) {
		if (a.getF () >= b.getF ())
			return 1;
		return -1;
	}

	/// <summary>
	/// Coroutine that handles moving the invisible leader along its path.
	/// </summary>
	/// <param name="current">Current.</param>
	IEnumerator move(bool firstNode) {
		AstarNode currTarget;
		while(stackPath.Count != 0) {

			// Makes movement look smoother.
			if(this.transform.position != startNode.getLocation() && firstNode) {
				firstNode = false;
				stackPath.Pop();
			}

			currTarget = stackPath.Peek();
			transform.position = Vector3.MoveTowards (transform.position, currTarget.getLocation (), speed * Time.fixedDeltaTime);
			Quaternion rotation = new Quaternion ();

			if(currTarget.getLocation() - transform.position != Vector3.zero) {
				rotation.SetLookRotation ((currTarget.getLocation () - transform.position).normalized);
				this.transform.rotation = Quaternion.Euler (new Vector3 (0f, rotation.eulerAngles.y, 0f));
			}

			if (Vector3.Distance (transform.position, currTarget.getLocation ()) <= radius)
				stackPath.Pop();
				
			yield return new WaitForFixedUpdate ();
			 
		}
		yield return null;
	}
}
