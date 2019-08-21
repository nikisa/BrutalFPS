using UnityEngine;
using System.Collections;


public class AIDamageTrigger : MonoBehaviour {
    // Variabili inspector 
    [SerializeField]
    string _parameter = "";
    [SerializeField]
    int _bloodParticlesBurstAmount = 10;
    [SerializeField]
    float _damageAmount = 0.1f;


    // Variabili private
    AIStateMachine _stateMachine = null;
    Animator _animator = null;
    int _parameterHash = -1;
    GameManager _gameManager = null;


    void Start() {
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();

        if (_stateMachine != null)
            _animator = _stateMachine.animator;

        _parameterHash = Animator.StringToHash(_parameter);

        _gameManager = GameManager.instance;
    }


    void OnTriggerStay(Collider col) {

        if (!_animator)
            return;

        // Se la collisione avviene con il Player e il parametro è settato per fare danno
        if (col.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f) {
            if (GameManager.instance && GameManager.instance.bloodParticles) {
                ParticleSystem system = GameManager.instance.bloodParticles;

                // Roba temporanea
                system.transform.position = transform.position;
                system.transform.rotation = Camera.main.transform.rotation;

                var settings = system.main;
                settings.simulationSpace = ParticleSystemSimulationSpace.World;
                system.Emit(_bloodParticlesBurstAmount);
            }

            //Il Player subisce danno
            if (_gameManager != null) {
                PlayerInfo info = _gameManager.GetPlayerInfo(col.GetInstanceID());
                if (info != null && info.characterManager != null) {
                    info.characterManager.TakeDamage(_damageAmount);
                }
            }
        }
    }
}
