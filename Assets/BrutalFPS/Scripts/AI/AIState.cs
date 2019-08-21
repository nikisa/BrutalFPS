using UnityEngine;
using System.Collections;

//La classe base di tutti gli stati relativi alla IA .
public abstract class AIState : MonoBehaviour {
    // Public Method
    // Chiamato dalla Parent State Machine per assegnare la sua referenza
    public virtual void SetStateMachine(AIStateMachine stateMachine) { _stateMachine = stateMachine; }

    // Default Handlers
    public virtual void OnEnterState() { }
    public virtual void OnExitState() { }
    public virtual void OnAnimatorIKUpdated() { }
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other) { }
    public virtual void OnDestinationReached(bool isReached) { }

    // Abtract Methods
    public abstract AIStateType GetStateType();
    public abstract AIStateType OnUpdate();

    // Protected Fields
    protected AIStateMachine _stateMachine;


    public virtual void OnAnimatorUpdated() {

        /*
         *Divido il numero di metri che il root motion ha aggiornato per deltaTime 
         *così ottengo il valore di metri al secondo. 
         *Assegno poi questo alla velocità del nav agent. 
         */
        if (_stateMachine.useRootPosition)
            _stateMachine.navAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;

        // Prendo la Root Rotation dall'animator e l'assegno alla rotazione della transform
        if (_stateMachine.useRootRotation)
            _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;

    }

    // Converte posizione e raggio dello Sphere Collider nel World Space prendendo
    // in considerazione lo scaling gerarchico
    public static void ConvertSphereColliderToWorldSpace(SphereCollider col, out Vector3 pos, out float radius) {
        // Default Values
        pos = Vector3.zero;
        radius = 0.0f;

        if (col == null)
            return;

        // Calcola la World Space Position del centro della Sphere
        pos = col.transform.position;
        pos.x += col.center.x * col.transform.lossyScale.x;
        pos.y += col.center.y * col.transform.lossyScale.y;
        pos.y += col.center.z * col.transform.lossyScale.z;

        // Calcola il World Space Radius del centro della Sphere
        radius = Mathf.Max(col.radius * col.transform.lossyScale.x,
                            col.radius * col.transform.lossyScale.y);

        radius = Mathf.Max(radius, col.radius * col.transform.lossyScale.z);
    }

    // Restituisce in gradi l'angolo tra i due vettori 
    public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector) {
        if (fromVector == toVector)
            return 0.0f;

        float angle = Vector3.Angle(fromVector, toVector);
        Vector3 cross = Vector3.Cross(fromVector, toVector);
        angle *= Mathf.Sign(cross.y);
        return angle;
    }
}
