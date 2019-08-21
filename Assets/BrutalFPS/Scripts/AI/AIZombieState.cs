using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIZombieState : AIState {


    //Protected
    protected int _playerLayerMask = -1;
    protected int _bodyPartLayer = -1;
    protected int _visualLayerMask = -1;
    protected AIZombieStateMachine _zombieStateMachine = null;

    //Private
    private float minSatisfaction = 0.9f; //Sotto questa soglia lo zombie va a nutrirsi 


    //Calcolo le mask e i layer usati per il raycasting 
    void Awake() {
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part") + 1; //+1 così includo anche il default layer
        _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1;
        _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
    }


    public override void SetStateMachine(AIStateMachine stateMachine) {
        if (stateMachine.GetType() == typeof(AIZombieStateMachine)) {
            base.SetStateMachine(stateMachine);
            _zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }


    //Esamina la minaccia e la salva nella "Parent Machine" se la minaccia visiva o uditiva
    //che è stata trovata ha una priorità più alta
    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other) {
        if (_zombieStateMachine == null) {
            return;
        }

        if (eventType != AITriggerEventType.Exit) {
            //Prendo il tipo dell'attuale minaccia visiva che ho salvato
            AITargetType curType = _zombieStateMachine.VisualThreat.type;

            //Controllo che il collider entrato nel Sensor sia un Player
            if (other.CompareTag("Player")) {
                //Calcolo la distanza tra il sensor e il collider
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);

                //Se l'ultima minaccia salvata non è un Player o se questo player è più vicino rispetto
                //al player salvato come minaccia visiva cambio l'ordine di priorità
                if (curType != AITargetType.Visual_Player ||
                    (curType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.distance)) {
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _playerLayerMask)) {
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            }
            else if (other.CompareTag("Flashlight") && curType != AITargetType.Visual_Player) {
                BoxCollider flashLightTrigger = (BoxCollider)other;
                float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, flashLightTrigger.transform.position);
                float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                float aggrFactor = distanceToThreat / zSize;

                if (aggrFactor <= _zombieStateMachine.sight && aggrFactor <= _zombieStateMachine.intelligence) {
                    _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Light, other, other.transform.position, distanceToThreat);
                }
            }
            else if (other.CompareTag("AI Sound Emitter")) {
                SphereCollider soundTrigger = (SphereCollider)other;
                if (soundTrigger == null) return;

                //Prendo la posizione dell'Agent Sensor
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;

                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out soundPos, out soundRadius);

                //Calcolo QUANTO siamo nel raggio sonoro
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;


                //Calcolo un fattore distanza che corrisponde a 1 quando è al limite del raggio sonoro , 
                //0 quando è al centro
                float distanceFactor = (distanceToThreat / soundRadius);

                //Calcolo il distanceFactor in base alla capacità uditiva dell'Agent
                distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.hearing);

                //Controllo se è troppo distante
                if (distanceFactor > 1.0f) return;

                //Se la sorgente è udibile e più vicina rispetto a l'ultima che è stata salvata
                if (distanceToThreat < _zombieStateMachine.AudioThreat.distance) {
                    //Setto la sorgente più vicina
                    _zombieStateMachine.AudioThreat.Set(AITargetType.Audio, other, soundPos, distanceToThreat);
                }

            }
            else
                    // Resgistro la minaccia visiva più vicina
                    if (other.CompareTag("AI Food") && curType != AITargetType.Visual_Player
                        && curType != AITargetType.Visual_Light
                        && _zombieStateMachine.satisfaction <= minSatisfaction
                        && _zombieStateMachine.AudioThreat.type == AITargetType.None) {
                //Calcolo quanto è distante la minaccia
                float distanteToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.sensorPosition);

                //Controllo se la distanza è minore di qualsiasi copsa sia stata salvata in prcedenza
                if (distanteToThreat < _zombieStateMachine.VisualThreat.distance) {
                    //Se sì controllo che sia interna al FOV e al relativo range del raggio visivo
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _visualLayerMask)) {
                        //Setto il target più vicino
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food, other, other.transform.position, distanteToThreat);
                    }
                }
            }
        }
    }


    /*
     * Testa il collider in ingresso con il FOV dello zombie e usa il LayerMask in ingresso 
     * per testare la traiettoria visiva
     */
    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1) {
        hitInfo = new RaycastHit();

        if (_zombieStateMachine == null) return false;

        //Calcola l'angolo tra l'origine del Sensor e la direzione del collider
        Vector3 head = _stateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);


        //Se l'angolo è maggiore della metà del FOV allora siamo fuori dal cono visivo
        // e restituisco false (non sono visibile)
        if (angle > _zombieStateMachine.fov * 0.5f)
            return false;

        //Faccio partire un raycast dal Sensor nella direzione del collider moltiplicata per 
        //la distanza del raggio del Sensor scalata per il valore di "abilità visiva" dello zombie
        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

        //Trova il collider più vicino che non sia una parte del corpo della IA stessa.
        float closestColliderDistance = float.MaxValue;
        Collider closestCollider = null;

        for (int i = 0; i < hits.Length; i++) {
            RaycastHit hit = hits[i];

            //Questo "hit" è più vicino di qualsiasi altro salvato in precedenza?
            if (hit.distance < closestColliderDistance) {
                //Se "hit" è nella parte di Layer "Body"
                if (hit.transform.gameObject.layer == _bodyPartLayer) {
                    //Controlliamo che non sia una parte di corpo dello zombie stesso
                    if (_stateMachine != GameManager.instance.GetAIStateMachine(hit.rigidbody.GetInstanceID())) {
                        //Salvo: Collider , distanza e hit info
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else {
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        if (closestCollider && closestCollider.gameObject == other.gameObject) return true;


        return false;
    }
}
