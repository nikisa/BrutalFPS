using UnityEngine;
using System.Collections;


// Patrolling Behaviour generico per Zombie

public class AIZombieState_Patrol1 : AIZombieState {
    // Inpsector Assigned 
    [SerializeField]
    float _turnOnSpotThreshold = 80.0f;
    [SerializeField]
    float _slerpSpeed = 5.0f;

    [SerializeField]
    [Range(0.0f, 3.0f)]
    float _speed = 1.0f;


    public override AIStateType GetStateType() {
        return AIStateType.Patrol;
    }


    public override void OnEnterState() {
        Debug.Log("Entering Patrol State");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // Configurazione State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // Set Destination
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        // Mi assicuro che il navAgent sia acceso
        _zombieStateMachine.navAgent.Resume();
    }



    public override AIStateType OnUpdate() {
        // La minaccia visiva è il Player?
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // Il suono è la terza priorità più alta
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // Se vede un cadavere e il livello di satisfaction è basso , lo raggiungo
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food) {
            // Valuto se raggiungere il cadavere in rapporto fame-distanza
            if ((1.0f - _zombieStateMachine.satisfaction) > (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius)) {
                _stateMachine.SetTarget(_stateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        if (_zombieStateMachine.navAgent.pathPending) {
            _zombieStateMachine.speed = 0;
            return AIStateType.Patrol;
        }

        else
            _zombieStateMachine.speed = _speed;


        // Calcola l'angolo per arrivare al target
        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        // Se è troppo grande, passa da Patrol ad Altered
        if (angle > _turnOnSpotThreshold) {
            return AIStateType.Alerted;
        }


        // Se non viene utilizzata la root rotation, assicura di mantenere gli zombi ruotati e rivolti nella giusta direzione.
        if (!_zombieStateMachine.useRootRotation) {
            // Genera un nuovo Quaternion che rappresenta la rotazione che dovrà avere
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);

            // Ruota nel tempo verso quella nuova rotazione 
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        // Se per qualsiasi motivo il NavAgent ha perso il suo percorso ,
        // chiamo la funzione NextWaypoint() così da settarne uno nuovo e
        // assegnarne il Path al NavAgent
        if (_zombieStateMachine.navAgent.isPathStale ||
            (!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) ||
            _zombieStateMachine.navAgent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete) {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }


        // Sta nello State Patrol
        return AIStateType.Patrol;
    }


    // Chiamata dalla Parent StateMachine quando lo zombie ha raggiunto il suo bersaglio ed
    // è entrato nel suo target trigger
    public override void OnDestinationReached(bool isReached) {
        if (_zombieStateMachine == null || !isReached)
            return;

        // Seleziona il prossimo Waypoint del Waypoint Network
        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
    }


    /*public override void 		OnAnimatorIKUpdated()	
    {
        if (_zombieStateMachine == null)
            return;

        _zombieStateMachine.animator.SetLookAtPosition ( _zombieStateMachine.targetPosition + Vector3.up );
        _zombieStateMachine.animator.SetLookAtWeight (0.55f );
    }*/
}
