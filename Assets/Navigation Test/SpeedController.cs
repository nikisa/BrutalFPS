using UnityEngine;
using System.Collections;

public class SpeedController : MonoBehaviour 
{
	// Public
	public float Speed = 0.0f;

	// Private
	private Animator _controller = null;

	void Start () 
	{
		_controller = GetComponent<Animator>();
	}
	
	void Update () 
	{
		_controller.SetFloat( "Speed", Speed );
	}
}
