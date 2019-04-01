using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class AstarNode {

	private int row, col, f, g, h, type;
	public bool inClosedList {get; set;}
	public bool inOpenList {get; set;}
	public bool start {get; set;}
	public bool goal {get; set;}
	public bool onPath {get; set;}
	public bool visited {get; set;}
	public bool isOccupied { get; set; }
	private Vector3 location;
	private AstarNode parent;
	public GameObject nodeSquare{ get; set; }
	[SerializeField] private List<AstarNode> neighbors = new List<AstarNode> ();

	public void reset() {
		onPath = visited = inOpenList = inClosedList = false;
		parent = null;
	}

	#region constructors
	public AstarNode(){}
	public AstarNode(int r, int c, Vector3 loc) {
		row = r;
		col = c;
		location = loc;
	}

	public AstarNode(int r, int c, Vector3 loc, GameObject obj, List<AstarNode> neighborNodes){
		row = r;
		col = c;
		location = loc;
		nodeSquare = obj;
		neighbors = neighborNodes;
	}

	public AstarNode(int r, int c, Vector3 loc, GameObject obj) {
		row = r;
		col = c;
		location = loc;
		nodeSquare = obj;
	}
	#endregion

	#region overrides
	public bool compareTo(AstarNode other) {
		if (row == other.getRow () && col == other.getCol ())
			return true;
		return false;
	}

	public string toString(){
		return "Node: " + row + "_" + col + ", inOpenList: " + inOpenList + ", inClosedList: " + inClosedList + ", goal?: " + goal + ", start?: " + start;
	}
	#endregion

	#region setters
	public void setParameters(int r, int c, Vector3 loc) {
		row = r;
		col = c;
		location = loc;
	}

	public void setF() {
		f = g + h;
	}

	public void setG(int val) {
		g = val;
	}

	public void setH(int val) {
		h = val;
	}

	public void setParent (AstarNode n) {
		parent = n;
	}

	public void setLoaction (Vector3 loc) {
		location = loc;
	}
	#endregion

	#region getters

	public int getF(){
		return f;
	}

	public int getH(){
		return h;
	}

	public int getG(){
		return g;
	}

	public int getRow(){
		return row;
	}

	public int getCol(){
		return col;
	}

	public int getType(){
		return type;
	}

	public AstarNode getParent(){
		return parent;
	}

	public List<AstarNode> getList(){
		return neighbors;
	}

	public Vector3 getLocation() {
		return location;
	}
	#endregion

}
