using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentNoRootMotion : MonoBehaviour 
{
	// Inspector
	public AIWaypointNetwork WaypointNetwork = null;
	public int CurrentIndex = 0;
	public bool HasPath = false;
	public bool	PathPending	 = false;
	public bool PathStale = false;
	public NavMeshPathStatus PathStatus = NavMeshPathStatus.PathInvalid;
	public AnimationCurve JumpCurve = new AnimationCurve();

	// Private 
	private NavMeshAgent _navAgent = null;
	private Animator _animator = null;
	private float _originalMaxSpeed = 0;

	void Start () 
	{
	    _navAgent = GetComponent<NavMeshAgent>();
		_animator = GetComponent<Animator>();

		if (_navAgent)
			_originalMaxSpeed = _navAgent.speed;

		
		if (WaypointNetwork==null) return;

		// Setto il primo Waypoint
		SetNextDestination ( false );
	}


    // Do la possibilità di incrementare l'indice del 
    // waypoint attuale per poi settare la prossima destinazione
	// che verrà raggiunta (spero) dall'agent

	void SetNextDestination ( bool increment )
	{

		if (!WaypointNetwork) return;

		// Calcolo di quanto dev'essere incrementato l'index del Waypoint
		int incStep = increment?1:0;
		Transform nextWaypointTransform = null;

        // Calcolo l'index del prossimo Waypoint 
        int nextWaypoint = (CurrentIndex+incStep>=WaypointNetwork.Waypoints.Count)?0:CurrentIndex+incStep;
		nextWaypointTransform = WaypointNetwork.Waypoints[nextWaypoint];

		// Assumo di avere il waypoint con una transform "valida"
		if (nextWaypointTransform!=null)
		{
			// Aggiorni l'index del Waypoint attuale 
            // e assegno la sua position al NavMeshAgent
			CurrentIndex = nextWaypoint;
			_navAgent.destination = nextWaypointTransform.position;
			return;
		}

		// Se non trovo un Waypoint valido cerco subito quello successivo
		CurrentIndex=nextWaypoint;
	}


	void Update () 
	{
		int turnOnSpot;

		// Rendo visibili queste variabili nell'inspector per vedere come si
        // comporta lo stato del NavMeshAgent
		HasPath = _navAgent.hasPath;
		PathPending = _navAgent.pathPending;
		PathStale = _navAgent.isPathStale;
		PathStatus = _navAgent.pathStatus;

		// Eseguo il prodotto matriciale tra i vettori forward e speed (spazio tempo)
        // Se entrambi i valori dei due input corrispondono la magnitude del vettore risultante
        // sarà Sin(theta) dove Theta è l'angolo tra i vettori
		Vector3 cross = Vector3.Cross( transform.forward, _navAgent.desiredVelocity.normalized);

		// Se Y è negativo allora la anche rotazione è negativa
		float horizontal = (cross.y<0)? -cross.magnitude : cross.magnitude;

		horizontal = Mathf.Clamp( horizontal * 2.32f, -2.32f, 2.32f );
 
		if ( _navAgent.desiredVelocity.magnitude< 1.0f && Vector3.Angle( transform.forward, _navAgent.steeringTarget - transform.position) > 10.0f )
		{
			// Ferma il navAgent e assegna -1 o +1 a turnOnSpot in base a horizontal
			_navAgent.speed = 0.1f;
			turnOnSpot = (int)Mathf.Sign( horizontal );
		}
		else
		{
			// Altrimenti l'angolo è ridotto e omposto la velocità del navAgent
            // su Normal e resetto turnOnSpot
			_navAgent.speed = _originalMaxSpeed;
			turnOnSpot = 0;
		}

		// Setto i dati che sono stati calcolati nei parametri dell'animator
		_animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime );
		_animator.SetFloat("Vertical", _navAgent.desiredVelocity.magnitude , 0.1f, Time.deltaTime); 
		_animator.SetInteger("TurnOnSpot", turnOnSpot );


		// Se non c'è un Path e PathPending è false allora imposto il Waypoint successivo come target
        // altrimenti se il Path è vecchio rigenero tutto il percorso
		if ( ( _navAgent.remainingDistance<=_navAgent.stoppingDistance && !PathPending) || PathStatus==UnityEngine.AI.NavMeshPathStatus.PathInvalid /*|| PathStatus==NavMeshPathStatus.PathPartial*/)
		{
			SetNextDestination ( true );
		}
	    else if (_navAgent.isPathStale)
			SetNextDestination ( false );
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
