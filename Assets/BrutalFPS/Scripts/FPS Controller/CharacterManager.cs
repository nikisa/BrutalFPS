﻿using UnityEngine;
using System.Collections;

public class CharacterManager : MonoBehaviour {
    // Assegnamenti da Inspector 
    [SerializeField] private CapsuleCollider _meleeTrigger = null;
    [SerializeField] private CameraBloodEffect _cameraBloodEffect = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _health = 100.0f;
    [SerializeField] private AISoundEmitter _soundEmitter = null;
    [SerializeField] private float _walkRadius = 0.0f;
    [SerializeField] private float _runRadius = 7.0f;
    [SerializeField] private float _landingRadius = 12.0f;
    [SerializeField] private float _bloodRadiusScale = 6.0f;

    // Private
    private Collider _collider = null;
    private FPSController _fpsController = null;
    private CharacterController _characterController = null;
    private GameManager _gameSceneManager = null;
    private int _aiBodyPartLayer = -1;

    void Start() {
        _collider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameManager.instance;
        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        if (_gameSceneManager != null) {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = _collider;
            info.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), info);
        }
    }

    public void TakeDamage(float amount) {
        _health = Mathf.Max(_health - (amount * Time.deltaTime), 0.0f);

        if (_fpsController) {
            _fpsController.dragMultiplier = 0.0f;
        }

        if (_cameraBloodEffect != null) {
            _cameraBloodEffect.minBloodAmount = (1.0f - _health / 100.0f);
            _cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 1.0f);
        }
    }

    public void DoDamage(int hitDirection = 0) {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        // Local Variables
        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        isSomethingHit = Physics.Raycast(ray, out hit, 1000.0f, 1 << _aiBodyPartLayer);

        if (isSomethingHit) {
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if (stateMachine) {
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 50, hit.rigidbody, this, 0);
            }
        }

    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            DoDamage();
        }

        if (_fpsController || _soundEmitter != null) {
            float newRadius = Mathf.Max(_walkRadius, (100.0f - _health) / _bloodRadiusScale);
            switch (_fpsController.movementStatus) {
                case PlayerMoveStatus.Landing: newRadius = Mathf.Max(newRadius, _landingRadius); break;
                case PlayerMoveStatus.Running: newRadius = Mathf.Max(newRadius, _runRadius); break;
            }

            _soundEmitter.SetRadius(newRadius);

            _fpsController.dragMultiplierLimit = Mathf.Max(_health / 100.0f, 0.25f);
        }

    }
}
