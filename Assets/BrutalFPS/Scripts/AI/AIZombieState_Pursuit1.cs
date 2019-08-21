using UnityEngine;
using System.Collections;
using UnityEngine.AI;


// Zombie State utilizzato per inseguire un target
public class AIZombieState_Pursuit1 : AIZombieState {
    [SerializeField]
    [Range(0, 10)]
    private float _speed = 1.0f;
    [SerializeField]
    private float _slerpSpeed = 5.0f;
    [SerializeField]
    private float _repathDistanceMultiplier = 0.035f;
    [SerializeField]
    private float _repathVisualMinDuration = 0.05f;
    [SerializeField]
    private float _repathVisualMaxDuration = 5.0f;
    [SerializeField]
    private float _repathAudioMinDuration = 0.25f;
    [SerializeField]
    private float _repathAudioMaxDuration = 5.0f;
    [SerializeField]
    private float _maxDuration = 40.0f;

    // Private Fields
    private float _timer = 0.0f;
    private float _repathTimer = 0.0f;

    // Mandatory Overrides
    public override AIStateType GetStateType() { return AIStateType.Pursuit; }

    // Default Handlers
    public override void OnEnterState() {
        Debug.Log("Entering Pursuit State");

        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // Configurazione State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // Timer che setterà per quanto tempo lo zombie insegue il beresaglio
        _timer = 0.0f;
        _repathTimer = 0.0f;


        // Set path
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.targetPosition);
        _zombieStateMachine.navAgent.isStopped = false;

    }

    public override AIStateType OnUpdate() {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;

        if (_timer > _maxDuration)
            return AIStateType.Patrol;

        // Se sta inseguendo il Player ed entra nel melee trigger , esegui Attack
        if (_stateMachine.targetType == AITargetType.Visual_Player && _zombieStateMachine.inMeleeRange) {

            return AIStateType.Attack;
        }

        // In caso contrario, si tratta della navigazione verso aree di interesse, utilizza allora la soglia target standard
        if (_zombieStateMachine.isTargetReached) {
            switch (_stateMachine.targetType) {

                // Se raggiunge la fonte
                case AITargetType.Audio:
                case AITargetType.Visual_Light:
                    _stateMachine.ClearTarget();
                    return AIStateType.Alerted;		// Passa ad alert e cerca il nuovo target

                case AITargetType.Visual_Food:
                    return AIStateType.Feeding;
            }
        }


        // Se per qualsiasi motivo il NavAgent ha perso il suo percorso, 
        // chiama e poi passa allo stato Alerted in modo da provare ad acquisire nuovamente il bersaglio 
        // oppure rinuncia e riprende il pattugliamento
        if (_zombieStateMachine.navAgent.isPathStale ||
            (!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) ||
            _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete) {
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.navAgent.pathPending)
            _zombieStateMachine.speed = 0;
        else {
            _zombieStateMachine.speed = _speed;


            // Se siamo vicini al bersaglio che era un Player ed è ancora nel campo visivo ,
            // allora continua a girarsi in direzione del player
            if (!_zombieStateMachine.useRootRotation && _zombieStateMachine.targetType == AITargetType.Visual_Player && _zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player && _zombieStateMachine.isTargetReached) {
                Vector3 targetPos = _zombieStateMachine.targetPosition;
                targetPos.y = _zombieStateMachine.transform.position.y;
                Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
                _zombieStateMachine.transform.rotation = newRot;
            }
            // Aggiorna lentamente la rotation in modo che corrisponda alla rotation desiderata 
            // dai NavAgent solo se non segue il Player e si trova molto vicino
            else if (!_stateMachine.useRootRotation && !_zombieStateMachine.isTargetReached) {
                // Genera un nuovo Quaternion che rappresenta la rotation da ottenere
                Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);

                // Ruota lentamente nel tempo verso la nuova rotation
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
            }
            else if (_zombieStateMachine.isTargetReached) {
                return AIStateType.Alerted;
            }
        }

        // La minaccia visiva è un Player?
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player) {
            // Se la minaccia è la stessa ma la posizione è diversa 
            // perché si sposta in continaziuone
            if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position) {
                // Riassegna il Path più frequentemente man mano che si avvicina al Path(dovrebbe risparmiare alcuni cicli della CPU)
                if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer) {
                    // Repath dell'agent
                    _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                    _repathTimer = 0.0f;
                }
            }
            // Setto il target attuale
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);

            // Rimane in stato di ricerca
            return AIStateType.Pursuit;
        }

        // Se il target corrisponde all'ultimo avvistamento del Player, 
        // allora rimane in Pursuit e nient'altro può fare override
        if (_zombieStateMachine.targetType == AITargetType.Visual_Player)
            return AIStateType.Pursuit;




        // Se la minaccia visiva corrisponde alla luce del Player
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light) {
            // e il target attuale è di priorità inferiore , passa in Alerted
            // e prova a trovare la causa della luce
            if (_zombieStateMachine.targetType == AITargetType.Audio || _zombieStateMachine.targetType == AITargetType.Visual_Food) {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Alerted;
            }
            else if (_zombieStateMachine.targetType == AITargetType.Visual_Light) {
                // ID univoco del collider del target
                int currentID = _zombieStateMachine.targetColliderID;

                // Se corrisponde alla luce
                if (currentID == _zombieStateMachine.VisualThreat.collider.GetInstanceID()) {
                    // Se la minaccia è la stessa ma la posizione è diversa 
                    // perché si sposta in continaziuone
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.VisualThreat.position) {
                        // Riassegna il Path più frequentemente man mano che si avvicina al Path(dovrebbe risparmiare alcuni cicli della CPU)
                        if (Mathf.Clamp(_zombieStateMachine.VisualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer) {
                            // Repath dell'Agent
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.VisualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Pursuit;
                }
                else {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                    return AIStateType.Alerted;
                }
            }
        }
        else if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio) {

            if (_zombieStateMachine.targetType == AITargetType.Visual_Food) {
                _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                return AIStateType.Alerted;
            }
            else if (_zombieStateMachine.targetType == AITargetType.Audio) {
                // ID univoco del collider del target
                int currentID = _zombieStateMachine.targetColliderID;

                // Se corrisponde al suono
                if (currentID == _zombieStateMachine.AudioThreat.collider.GetInstanceID()) {
                    // Se la minaccia è la stessa ma la posizione è diversa 
                    // perché si sposta in continaziuone
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.AudioThreat.position) {
                        //  Riassegna il Path più frequentemente man mano che si avvicina al Path(dovrebbe risparmiare alcuni cicli della CPU)
                        if (Mathf.Clamp(_zombieStateMachine.AudioThreat.distance * _repathDistanceMultiplier, _repathAudioMinDuration, _repathAudioMaxDuration) < _repathTimer) {
                            // Repath dell'agent
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.AudioThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Pursuit;
                }
                else {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
                    return AIStateType.Alerted;
                }
            }
        }

        // Default
        return AIStateType.Pursuit;
    }
}