using UnityEngine;
using System.Collections;

public class SmoothCameraMount : MonoBehaviour 
{
	public Transform Mount = null;
	public float Speed = 5.0f;

	void Start () {
	
	}
	
	void LateUpdate () 
	{
		transform.position = Vector3.Lerp( transform.position, Mount.position, Time.deltaTime * Speed );
		transform.rotation = Quaternion.Slerp( transform.rotation, Mount.rotation, Time.deltaTime * Speed );
	}
}
