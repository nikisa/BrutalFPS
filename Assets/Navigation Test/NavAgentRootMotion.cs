using UnityEngine;
using System.Collections;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentRootMotion : MonoBehaviour 
{
	// Variabili visibili da Inspector 
	public AIWaypointNetwork WaypointNetwork = null;
	public int CurrentIndex	 = 0;
	public bool HasPath = false;
	public bool	PathPending	= false;
	public bool PathStale = false;
	public NavMeshPathStatus PathStatus = NavMeshPathStatus.PathInvalid;
	public AnimationCurve JumpCurve = new AnimationCurve();
	public bool MixedMode = true;

	// Private 
	private NavMeshAgent _navAgent = null;
	private Animator _animator = null;
	private float _smoothAngle = 0.0f;

	void Start () 
	{
		_navAgent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		// Disattivo l'auto-update della rotation
		_navAgent.updateRotation = false;

		if (WaypointNetwork==null) return;

		SetNextDestination ( false );
	}

    // Do la possibilità di incrementare l'indice del 
    // waypoint attuale per poi settare la prossima destinazione
    // che verrà raggiunta dall'agent
    void SetNextDestination ( bool increment )
	{		
		if (!WaypointNetwork) return;

		int incStep = increment?1:0;
		Transform nextWaypointTransform = null;

        // Calcolo di quanto dev'essere incrementato l'index del Waypoint
        int nextWaypoint = (CurrentIndex+incStep>=WaypointNetwork.Waypoints.Count)?0:CurrentIndex+incStep;
		nextWaypointTransform = WaypointNetwork.Waypoints[nextWaypoint];

		if (nextWaypointTransform!=null)
		{
            // Aggiorno l'index del Waypoint attuale 
            // e assegno la sua position al NavMeshAgent
            CurrentIndex = nextWaypoint;
			_navAgent.destination = nextWaypointTransform.position;
			return;
		}

        // Se non trovo un Waypoint valido cerco subito quello successivo
        CurrentIndex = nextWaypoint;
	}


	void Update () 
	{

        // Rendo visibili queste variabili nell'inspector per vedere come si
        // comporta lo stato del NavMeshAgent
        HasPath = _navAgent.hasPath;
		PathPending = _navAgent.pathPending;
		PathStale	= _navAgent.isPathStale;
		PathStatus	= _navAgent.pathStatus;

		// Converto la l'agent speed in local space
		Vector3 localDesiredVelocity = transform.InverseTransformVector( _navAgent.desiredVelocity);

        // Ottengo l'angolo in gradi per quanto devo girare per raggiungere la DesiredVelocity
        float angle = Mathf.Atan2( localDesiredVelocity.x , localDesiredVelocity.z ) * Mathf.Rad2Deg;

		_smoothAngle = Mathf.MoveTowardsAngle( _smoothAngle, angle, 80.0f * Time.deltaTime );

		// La Speed è la desiredVelocity proiettata dul nostro Forward Vector
		float speed = localDesiredVelocity.z;

		// Setto i parametri dell'animator
		_animator.SetFloat("Angle", _smoothAngle );
		_animator.SetFloat("Speed", speed , 0.1f, Time.deltaTime );

		if ( _navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon )
		{
			if (!MixedMode ||
				(MixedMode && Mathf.Abs(angle)<80.0f && _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion") ))
			{
				Quaternion  lookRotation = Quaternion.LookRotation( _navAgent.desiredVelocity, Vector3.up );
				transform.rotation = Quaternion.Slerp( transform.rotation, lookRotation, 5.0f * Time.deltaTime );
			}
		}


        // Se non c'è un Path e PathPending è false allora imposto il Waypoint successivo come target
        // altrimenti se il Path è vecchio rigenero tutto il percorso
        if ( ( _navAgent.remainingDistance<=_navAgent.stoppingDistance && !PathPending) || PathStatus==NavMeshPathStatus.PathInvalid )
		{
			SetNextDestination ( true );
		}
			else
		if (_navAgent.isPathStale)
			SetNextDestination ( false );

	}

	void OnAnimatorMove()
	{
		// Se sono in Mixed Mode e non sono in Locomotion State allora applico la Root Rotation
		if (MixedMode && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))
			transform.rotation = _animator.rootRotation;

		// Override dell'Agent's velocity con la velocità del root motion
		_navAgent.velocity = _animator.deltaPosition / Time.deltaTime;
	}


	IEnumerator Jump ( float duration )
	{
	    OffMeshLinkData data = _navAgent.currentOffMeshLinkData;

		Vector3 startPos = _navAgent.transform.position;

        // L'End position viene recuperata da OffMeshLink data e regolata per l'offset base dell'Agent
        Vector3 endPos = data.endPos + ( _navAgent.baseOffset * Vector3.up);

		float time = 0.0f;

		while ( time<= duration )
		{
			float t = time/duration;

            // Calcolo un LERP tra la Start Position e la End Position e regolo l'altezza in base alla
            // valutazione del tempo t sulla JumpCurve
            _navAgent.transform.position = Vector3.Lerp( startPos, endPos, t ) + (JumpCurve.Evaluate(t) * Vector3.up) ;

			time += Time.deltaTime;
			yield return null;
		}

        // Ancora prima di completare il colegamento
        // mi assicuro che l'Agent si esattamente sulla posizione finale
        _navAgent.transform.position = endPos;

        // informa l'Agent che può riprendere il controllo
        _navAgent.CompleteOffMeshLink();
	}
}
