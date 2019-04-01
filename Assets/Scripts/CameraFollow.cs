using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
	[SerializeField] private GameObject entity;
	private Vector3 velocity;
	private float posX, posZ;
	private float smoothTimeY = 0.2f, smoothTimeX = 0.2f;
	// Use this for initialization
	void Start () {
		entity = GameObject.FindGameObjectWithTag ("Player");
	}
	
	// Update is called once per frame
	void Update () {
		posX = Mathf.SmoothDamp (transform.position.x, entity.transform.position.x, ref velocity.x, smoothTimeX);
		posZ = Mathf.SmoothDamp (transform.position.z, entity.transform.position.z, ref velocity.z, smoothTimeY);
		transform.position = new Vector3 (posX, transform.position.y , posZ);
	}
}
