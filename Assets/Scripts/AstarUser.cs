using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class AstarUser : MonoBehaviour {
	private Rigidbody rb;
	private List<AstarNode> roomNodes, openList, closedList, path;
	[SerializeField] private Astar controller;
	private AstarNode startNode, targetNode;
	private bool foundGoal;
	private float radius;
	[SerializeField] private KinematicArrive kinArrive;
	private KinematicArrive.KinematicSteering steering;
	[SerializeField]private float speed;
	Vector3 pos;
	private IEnumerator co;

	void Awake() {
		openList = new List<AstarNode> ();
		closedList = new List<AstarNode> ();
		path = new List<AstarNode> ();
	}

	/// <summary>
	/// Retrieves the initial and necessary references for the A* search.
	/// </summary>
	public IEnumerator start() {
		radius = 0.5f; //how close the invisible leader will get to a node before pathing to the next node in the 'path' AstarNode list
		co = move (0);
		rb = GetComponent<Rigidbody> ();
		roomNodes = controller.getNodeListClone ();
		controller.setNeighbors (roomNodes);
		controller.visualize (roomNodes);
		yield return null;
	}

	/// <summary>
	/// Coroutine called from the GameController instance when the user left-clicks on the ground mesh in the scene.
	/// It is passed the Vector3 position of where the user clicked on the ground mesh.
	/// 
	/// 'co' is a 'move' coroutine, which is defined at the bottom of this script.
	/// It is using a 'while' loop to function as an instanced FixedUpdate method, so it must
	/// be stopped and restarted every time this is called, or else multiple instances of the
	/// movement algorithm are running at the same time for the invisible leader.
	/// </summary>
	/// <param name="target">Target.</param>
	public IEnumerator updater(Vector3 target) {
		StopCoroutine(co);
		co = move (0);
		controller.detectNodeOccupation (roomNodes);
		//foreach (AstarNode n in roomNodes) n.reset (); 
		startNode = setStartNode (startNode);
		targetNode = setTargetNode (target, targetNode);
		preSearch (); search (startNode); //sets new H costs, clears all lists, and resets node parameters
		controller.colorNodes (roomNodes); //re-color node objects to reflect new path
		yield return StartCoroutine (co); //move the invisible leader
	}

	/// <summary>
	/// Sets the start node as the node closest to the invisible leader.
	/// </summary>
	/// <returns>The start node.</returns>
	/// <param name="s">S.</param>
	AstarNode setStartNode(AstarNode s) {
		foreach (AstarNode n in roomNodes) {
			if (s == null) {
				s = n; s.start = true; continue;
			}
			float newVal = Vector3.Distance (n.getLocation (), this.transform.position);
			float oldVal = Vector3.Distance (s.getLocation (), this.transform.position);
			if (Mathf.Abs(newVal) < Mathf.Abs(oldVal)) {
				s.start = false; n.start = true;
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
				t = n; t.goal = true; continue;
			}
			float newVal = Vector3.Distance (n.getLocation (), target);
			float oldVal = Vector3.Distance (t.getLocation (), target);
			if (Mathf.Abs(newVal) < Mathf.Abs(oldVal)) {
				t.goal = false; n.goal = true;
				t = n;
			}
		}
		return t;
	}

	void FixedUpdate(){}

	/// <summary>
	/// Astar search algorithm. Recursively searches the list of AstarNodes for the fastest path between
	/// the current AstarNode and the goal AstarNode.
	/// </summary>
	/// <param name="current">Current.</param>
	void search(AstarNode current) {
		if (current.goal) { //if we have found the path
			closedList.Add (current);
			current.inClosedList = true;
			createPath (current);
			path.Reverse ();
			foundGoal = true;
			return;
		}
		//for each neighbor of the current node, set F and G and add to openList
		foreach (AstarNode n in current.getList()) {
			//if a node is already in the open list, check new route heuristic
			if (n.inOpenList) {
				int tempG;
				if (current.getRow () == n.getRow () || current.getCol () == n.getCol ()) tempG = 10;
				else tempG = 14;
				if (n.getF () < (n.getH () + current.getG () + tempG)) continue;
				else { //new route to node is faster than previous route
					n.setParent (current);
					n.setG (current.getG () + tempG);
					n.setF ();
					continue;
				}
			}
			//if a node is in the closed list, continue
			if (n.inClosedList) continue;
			//for an unchecked, pathable neighbor
			n.inOpenList = true; n.setParent(current);
			if (current.getRow () == n.getRow () || current.getCol () == n.getCol ()) n.setG (current.getG () + 10);
			else n.setG (current.getG () + 14);
			n.setF ();
			openList.Add (n);
		}
		//sort openList so that first node has lowest F score
		openList.Sort(compareF);
		current.inOpenList = false; current.inClosedList = true;
		closedList.Add (current);
		current = openList [0];
		openList.RemoveAt (0);
		search (current);
	}

	/// <summary>
	/// Creates the path to be followed by recursively passing itself the parent node of the current node until
	/// there is no parent node of the current node.
	/// </summary>
	/// <param name="n">N.</param>
	void createPath(AstarNode n) {
		path.Add (n);
		n.inClosedList = false;
		n.onPath = true;
		if (n.getParent () != null) createPath (n.getParent ());
	}

	/// <summary>
	/// Compares the F costs of two AstarNode objects.
	/// </summary>
	/// <returns>The f.</returns>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	int compareF(AstarNode a, AstarNode b) {
		if (a.getF () > b.getF ())
			return 1;
		return -1;
	}

	/// <summary>
	/// Resets certain valuse within each AstarNode in the roomNodes list, sets new H
	/// costs for each node, and clears all the lists involved in pathing.
	/// </summary>
	void preSearch() {
		controller.setHCosts (roomNodes, targetNode);
		foundGoal = false;
		closedList.Clear (); openList.Clear (); path.Clear ();
		foreach (AstarNode n in roomNodes) n.reset ();
	}

	/// <summary>
	/// Coroutine that handles moving the invisible leader along its path.
	/// The 'while' loop contained within runs in lock-step with FixedUpdate().
	/// </summary>
	/// <param name="current">Current.</param>
	IEnumerator move(int current) {
		while (current % path.Count != 0 || (current == 0 && path.Count != 0)) { //while the leader is not at its goal
			//if the leader is not at the start node and this is the first iteration, grab the second node
			if (this.transform.position != startNode.getLocation () && current == 0) current++;
			if (path.Count != 0 && path [current] != null) { 
				pos = Vector3.MoveTowards (transform.position, path [current].getLocation (), speed * Time.fixedDeltaTime);
				transform.position = pos;
				Quaternion rotation = new Quaternion ();
				if ((path [current].getLocation () - transform.position) != Vector3.zero) {
					rotation.SetLookRotation ((path [current].getLocation () - transform.position).normalized);
					this.transform.rotation = Quaternion.Euler (new Vector3 (0f, rotation.eulerAngles.y, 0f));
				}
				if (Vector3.Distance (transform.position, path [current].getLocation ()) <= radius) //if the leader is sufficiently close to a node, increment current
					current++;
				yield return new WaitForFixedUpdate ();
			} else {
				yield return null;
			}
		}
		yield return null;
	}
}
