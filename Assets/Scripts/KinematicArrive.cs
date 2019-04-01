using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Author: Taylor Conners
public class KinematicArrive : MonoBehaviour {
	
	#region data
	private KinematicSteering steering;
	private const float timeToTarget = 0.1f;
	//GameObject obj is the reference to the "Leader" GameObject of this entity.
	[SerializeField] private GameObject obj;
	[SerializeField] private float maxSpeed;
	[SerializeField] private float turnSpeed;
	private float radius, timer;
	[SerializeField] private bool pathBlocked, isPathable;
	//distance: distance from leader object; 
	//angle: angle relative to the leader objects transform.forward
	//rotationOffset: direction to rotate relative to the leader's forward vector
	[SerializeField] private float distance, angle, rotationOffset;
	[SerializeField] private Vector3 offsetPosition;
	private Vector3 mouseClickLocation, eventualLocation, currentTarget;
	public Vector3 target, tempTarget;
	#endregion

	void Start () {
		timer = .1f;
		//Radius is set in code for ease across all entities
		radius = 0f;
		steering = getSteering (steering, target);
		if (steering != null)
			setOrientations (steering, target);
	}
		
	void Update () {
		mouseClickLocation = GameController.instance.getMouseVector ();
		//The GameObject tagged "Leader" is the GameObject which all other entities follow with their steering.
		if (this.tag.Equals ("Leader")) {
			return;
			//target = GameController.instance.getMouseVector ();
		}
		else {
			//If this is not the leader, then calculate the offset position relative to the leader.
			//obj is a reference to the "Leader" GameObject
			offsetPosition = new Vector3 (obj.transform.forward.x, 0f, obj.transform.forward.z) * -distance;
			offsetPosition = Quaternion.AngleAxis (angle, obj.transform.up) * offsetPosition;
			offsetPosition.y = 0f;
			target = new Vector3 (obj.transform.position.x + offsetPosition.x, 2f,
				obj.transform.position.z + offsetPosition.z);
			eventualLocation = new Vector3 (mouseClickLocation.x + offsetPosition.x, 2f,
				mouseClickLocation.z + offsetPosition.z);
			//Checks if the target is in a pathable location (not an obstacle)
			checkTargetPathable (eventualLocation);
		}
		steering = getSteering (steering, target);
		setOrientations (steering, target);
		//If the path is blocked by an obstacle, the protocol in OnCollisionStay will trigger and function for
		//.35f seconds at a time while the path remains blocked.
		if (pathBlocked) { 
			timer -= Time.deltaTime;
			if (timer <= 0f) {
				timer = .1f;
				pathBlocked = false;
			}
		}
	}

	/// <summary>
	/// Gets the steering. Sets steering velocity and steering rotation.
	/// </summary>
	/// <returns>The steering.</returns>
	public KinematicSteering getSteering(KinematicSteering steering, Vector3 goal){
		if (steering == null)
			steering = new KinematicSteering ();

		//If the path is not blocked, continue pathing towards the intended target.
		if (!pathBlocked) {
			steering.velocity = goal - this.transform.position;
		} else {
			//tempTarget is assigned in avoidObstacles and is relative to a detected obstacle.
			steering.velocity = tempTarget - this.transform.position;
		}

		//If the player has hit the satisfaction radius, stop the player and return steering.
		if (steering.velocity.magnitude <= radius) {
			steering.velocity = Vector3.zero;
			setOrientations (steering, target);
			return steering;
		}

		/*if (this.tag == "Leader")
			steering.velocity = steering.velocity.normalized * 15;*/

		steering.velocity /= timeToTarget;

		//Limit player speed moving between two points.
		if (steering.velocity.magnitude > maxSpeed) {
			steering.velocity.Normalize ();
			steering.velocity *= maxSpeed;
		}
			
		return steering;
	}

	/// <summary>
	/// Intended to detect obstacles before touching them in an attempt to steer away.
	/// This did not work as intended, and its functionality is working in OnCollisionStay
	/// THIS IS NOT CURRENTLY BEING UTILIZED
	/// </summary>
	void avoidObstacles(){
		//Ray ray = new Ray (transform.position, steering.velocity);
		Ray ray = new Ray (transform.position, transform.forward);
		Ray ray2 = new Ray (transform.position, eventualLocation);
		RaycastHit hit;
		float distance = Vector3.Distance (eventualLocation, transform.position);
		LayerMask obstacle = (1 << LayerMask.NameToLayer ("Obstacle"));
		//If the entity is moving:

		if (steering.velocity.normalized.x != 0f || steering.velocity.normalized.z !=0f) { 
			//If the path is currently blocked
			if (pathBlocked) {
				//If there is a line of sight between this object and its goal
//				if (!Physics.Raycast(transform.position, transform.forward, 5f, obstacle)/*!Physics.Raycast (ray, out hit, distance * 5f, obstacle)*/) {
//					Debug.DrawRay(this.transform.position, eventualLocation, Color.red, 5f);
//					pathBlocked = false;
//				}
				if (!Physics.SphereCast (ray2, 1f, 1f, obstacle))
					pathBlocked = false;
			}
			//If there is an obstacle between the entitiy and its destination:
			//Debug.DrawRay (this.transform.position, steering.velocity * 2f , Color.red, 0.05f);
			//Debug.DrawRay(new Vector3(eventualLocation.x, eventualLocation.y + 10f, eventualLocation.z), Vector3.down * 10f, Color.red, 1f);
			if (Physics.Raycast (ray, out hit, 1f, obstacle) && !pathBlocked) {
				pathBlocked = true;
				//If this is not the leader object that all entities are following:
				if (!this.tag.Equals ("Leader")) {
					//The distance vector between this entity and the obstacle.
					Vector3 obstacleDistance = hit.transform.position - this.transform.position;
					if (Vector3.Dot (this.transform.right, obstacleDistance) < 0) {
						tempTarget = hit.point + obj.transform.right * 10f + -obj.transform.forward;
						//Debug.DrawRay (new Vector3 (tempTarget.x, tempTarget.y + 10f, tempTarget.z), Vector3.down * 10f, Color.red, 1f);
					} else {
						tempTarget = hit.point + -obj.transform.right * 10f + -obj.transform.forward;
						//Debug.DrawRay (new Vector3 (tempTarget.x, tempTarget.y + 10f, tempTarget.z), Vector3.down * 10f, Color.red, 1f);
					}
				}
			} else {
				//pathBlocked = false;
			}
		}
	}
		
	//This is where obstacle avoidance is currently being handled.
	void OnCollisionStay(Collision c){
		//If this is an obstacle and the entity is sufficiently far away from its intended target.
		if (c.collider.tag == "Obstacle" && (eventualLocation - this.transform.position).magnitude > 2f){
			//If the obstacle is in front of the entity or perpendicular to the entity.
			if (Vector3.Dot (obj.transform.forward, (c.transform.position - this.transform.position)) > 0 ||
				Vector3.Dot(obj.transform.forward,(c.transform.position-this.transform.position)) == 0) {
				pathBlocked = true;
				//This is set permanently to "true" while the flag "isPathable" set in checkTargetPathable is not working
				//It works well enough when always true, but I will revisit in the future if it gives me any trouble.
				if (true) {
					//Give the entity a temporary target to the right or left of the obstacle.
					if (Vector3.Dot (obj.transform.right, (c.transform.position - this.transform.position)) < 0) {
						tempTarget = c.transform.position + (obj.transform.right * 2f) + obj.transform.forward * 2f;
					} else {
						tempTarget = c.transform.position + (-obj.transform.right * 2f) + obj.transform.forward * 2f;
					}
				}
				//never currently used.
				else {
					target = target + this.transform.position;
				}
			}
		}
	}

	/// <summary>
	/// Applies KinematicSteering velocity to a RigidBody and rotates an object to look in the direction it's moving
	/// </summary>
	/// <param name="steering">Steering.</param>
	public void setOrientations(KinematicSteering steering, Vector3 goal){
		if (pathBlocked)
			currentTarget = tempTarget;
		else
			currentTarget = goal;
		this.GetComponent<Rigidbody> ().velocity = steering.velocity;
		//Checks to see if magnitude of vector is > 1.6f, because if it's not, the player will turn to look up given a sufficiently small radius.
		//x of each Vector3 is 0 so that the sprite will not constantly look at the floor
		if (steering.velocity.magnitude > 1.6f) {
			//If this is not the "Leader", SetLookRotation in the directrion of the Leader's forward vector
			if (!tag.Equals ("Leader")) {
				//NOTE: might have to change the way LookRotation is working if entity must always face movement direction
				steering.rotation.SetLookRotation (obj.transform.forward);
				//if (pathBlocked)
				//	steering.rotation.SetLookRotation (steering.velocity);
				//else
				//	steering.rotation.SetLookRotation (obj.transform.forward);
			}else
				steering.rotation.SetLookRotation ((currentTarget - transform.position).normalized);
			this.transform.rotation = Quaternion.Slerp
			(Quaternion.Euler (new Vector3 (0f, this.transform.rotation.eulerAngles.y, 0f)),
					Quaternion.Euler (new Vector3 (0f, steering.rotation.eulerAngles.y + rotationOffset, 0f)), Time.deltaTime * turnSpeed);
		}
	}

	/// <summary>
	/// Checks to see if a given Vector3 is in a pathable location.
	/// </summary>
	/// <param name="t">T.</param>
	void checkTargetPathable(Vector3 t){
		//Vector3 actual;
		Ray ray = new Ray (t + (Vector3.up * 5f), Vector3.down);
		LayerMask Obstacle = (1 << LayerMask.NameToLayer ("Obstacle"));
		if (Physics.SphereCast (ray, 2f, 10f, Obstacle)) {
			isPathable = false;
		} else {
			isPathable = true;
		}
	}

	public Vector3 getTarget(){
		return target;
	}

	/// <summary>
	/// A class to store a Kinemating Steering Velocity and Rotation.
	/// </summary>
	public class KinematicSteering {
		public Vector3 velocity;
		public Quaternion rotation;

		public KinematicSteering() {
			velocity = new Vector3(0f,0f,0f);
			rotation = new Quaternion();
		}
			
	}




}
