using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Author: Taylor Conners
 * The GameController class is a singleton that attaches itself to the MainCamera of a scene.
 * */
public class GameController : MonoBehaviour {
	#region data
	private static GameController g_Instance = null;
	private LayerMask Ground, Obstacle;
	private Vector3 mouseVector;
	private float distance = 50f;
	[SerializeField] private GameObject leader;
	public GameObject cube;
	public Astar astar;
	public AstarUser leaderAstar;
	#endregion
	
	//If no instance of GameController exists in scene, force an instance of Gamecontroller.
	public static GameController instance {
		get {
			if (g_Instance == null) {
				g_Instance = GameObject.FindGameObjectWithTag ("MainCamera").GetComponent<GameController> ();
			}
			if (g_Instance == null) {
				g_Instance = GameObject.FindGameObjectWithTag ("MainCamera").AddComponent (typeof(GameController)) as GameController;
			}
			if (g_Instance == null) {
				GameObject obj = new GameObject ("GameController");
				g_Instance = obj.AddComponent (typeof(GameController)) as GameController;
				Debug.Log ("No main camera exists in scene; GameController was generated automatically.");
			}
			return g_Instance;
		}
	}

	void Awake() {
		leader = GameObject.FindGameObjectWithTag ("Leader");
	}

	void Start () {
		Ground = (1 << LayerMask.NameToLayer ("Ground"));
		Obstacle = (1 << LayerMask.NameToLayer ("Obstacle"));
		mouseVector = new Vector3 (0f, 0f, 0f);
		StartCoroutine (astar.decompose ());
	}

	void Update () {
		if (Input.GetMouseButtonDown (0)) { 
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			bool hitObs = false;
			if (Physics.Raycast (ray, out hit, distance, Obstacle))
				hitObs = true;
			if (Physics.Raycast(ray, out hit, distance, Ground) && !hitObs){
				mouseVector = hit.point;
				StartCoroutine (leaderAstar.updater (mouseVector)); //perform Astar search and move the invisible leader
			}
		}

		//If the user right clicks on the ground, an obstacle is created.
		//If the user right clicks on an obstacle, the obstacle is destroyed.
		if (Input.GetMouseButtonDown (1)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			bool hitObs = false;
			if (Physics.Raycast (ray, out hit, distance, Obstacle)) {
				if (hit.transform.tag.Equals ("Obstacle")) {
					Destroy (hit.collider.gameObject);
					//StartCoroutine (astar.decompose ());
				}
				hitObs = true;
			}
			if (Physics.Raycast (ray, out hit, distance, Ground) && !hitObs) { //if the user clicked on the ground
				GameObject newObstacle = GameObject.Instantiate (cube, new Vector3 (hit.point.x,
					2f, hit.point.z), Quaternion.identity) as GameObject;
				//StartCoroutine (astar.decompose ());
			}
		}
	}

	public Vector3 getMouseVector(){
		return mouseVector;
	}

	//Remove instance of GameController when application quits.
	void OnApplicationQuit() {
		g_Instance = null;
	}
}
