using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;

// Enumerations
public enum PlayerMoveStatus { NotMoving, Crouching, Walking, Running, NotGrounded, Landing }
public enum CurveControlledBobCallbackType { Horizontal, Vertical }

// Delegates
public delegate void CurveControlledBobCallback();

[System.Serializable]
public class CurveControlledBobEvent {
    public float Time = 0.0f;
    public CurveControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.Vertical;
}

[System.Serializable]
public class CurveControlledBob {
    [SerializeField]
    AnimationCurve _bobcurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f),
                                                   new Keyframe(1f, 0f), new Keyframe(1.5f, -1f),
                                                   new Keyframe(2f, 0f));

    // Variabili utilizzate per gestire il Bob Control da inspector
    [SerializeField]
    float _horizontalMultiplier = 0.01f;
    [SerializeField]
    float _verticalMultiplier = 0.02f;
    [SerializeField]
    float _verticaltoHorizontalSpeedRatio = 2.0f;
    [SerializeField]
    float _baseInterval = 1.0f;

    // Internals
    private float _prevXPlayHead;
    private float _prevYPlayHead;
    private float _xPlayHead;
    private float _yPlayHead;
    private float _curveEndTime;
    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize() {
        // Durata del Bob Curve nel tempo
        _curveEndTime = _bobcurve[_bobcurve.length - 1].time;
        _xPlayHead = 0.0f;
        _yPlayHead = 0.0f;
        _prevXPlayHead = 0.0f;
        _prevYPlayHead = 0.0f;
    }

    public void RegisterEventCallback(float time, CurveControlledBobCallback function, CurveControlledBobCallbackType type) {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        _events.Sort(
            delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2) {
                return (t1.Time.CompareTo(t2.Time));
            }
        );
    }

    public Vector3 GetVectorOffset(float speed) {
        _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * _verticaltoHorizontalSpeedRatio;

        if (_xPlayHead > _curveEndTime)
            _xPlayHead -= _curveEndTime;

        if (_yPlayHead > _curveEndTime)
            _yPlayHead -= _curveEndTime;

        // Process Events
        for (int i = 0; i < _events.Count; i++) {
            CurveControlledBobEvent ev = _events[i];
            if (ev != null) {
                if (ev.Type == CurveControlledBobCallbackType.Vertical) {
                    if ((_prevYPlayHead < ev.Time && _yPlayHead >= ev.Time) ||
                        (_prevYPlayHead > _yPlayHead && (ev.Time > _prevYPlayHead || ev.Time <= _yPlayHead))) {
                        ev.Function();
                    }
                }
                else {
                    if ((_prevXPlayHead < ev.Time && _xPlayHead >= ev.Time) ||
                        (_prevXPlayHead > _xPlayHead && (ev.Time > _prevXPlayHead || ev.Time <= _xPlayHead))) {
                        ev.Function();
                    }
                }
            }
        }

        float xPos = _bobcurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobcurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        //Aggiungere zPos per ottenere la camminata Chicken Style
        return new Vector3(xPos, yPos, 0f);
    }
}


[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour {
    public List<AudioSource> AudioSources = new List<AudioSource>();
    private int _audioToUse = 0;

    // Inspector Assigned Locomotion Settings
    [SerializeField]
    private float _walkSpeed = 2.0f;
    [SerializeField]
    private float _runSpeed = 4.5f;
    [SerializeField]
    private float _jumpSpeed = 7.5f;
    [SerializeField]
    private float _crouchSpeed = 1.0f;
    [SerializeField]
    private float _stickToGroundForce = 5.0f;
    [SerializeField]
    private float _gravityMultiplier = 2.5f;
    [SerializeField]
    private float _runStepLengthen = 0.75f;
    [SerializeField]
    private CurveControlledBob _headBob = new CurveControlledBob();
    [SerializeField]
    private GameObject _flashLight = null;

    // Use Standard Assets Mouse Look class for mouse input -> Camera Look Control
    [SerializeField]
    private MouseLook _mouseLook = new MouseLook();

    // Private internals
    private Camera _camera = null;
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private bool _isCrouching = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;
    private float _controllerHeight = 0.0f;

    // Timers
    private float _fallingTimer = 0.0f;

    private CharacterController _characterController = null;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NotMoving;

    // Public Properties
    public PlayerMoveStatus movementStatus { get { return _movementStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }


    float _dragMultiplier = 1.0f;
    float _dragMultiplierLimit = 1.0f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    float _npcStickness = 0.5f;

    public float dragMultiplierLimit {
        get { return _dragMultiplierLimit; }
        set { _dragMultiplierLimit = Mathf.Clamp01(value); }

    }

    public float dragMultiplier {
        get { return _dragMultiplier; }
        set { _dragMultiplier = Mathf.Min(value , _dragMultiplierLimit); }

    }

    public CharacterController characterController {
        get { return _characterController; }
    }

    protected void Start() {
        // Cache component references
        _characterController = GetComponent<CharacterController>();
        _controllerHeight = _characterController.height;

        _camera = Camera.main;
        _localSpaceCameraPos = _camera.transform.localPosition;

        _movementStatus = PlayerMoveStatus.NotMoving;

        // Reset timers
        _fallingTimer = 0.0f;

        // Setup Mouse Look Script
        _mouseLook.Init(transform, _camera.transform);

        // Initiate Head Bob Object
        _headBob.Initialize();
        //Chiamo l'evento "PlayFootStepSound" quando "CurveControlledBobCallbackType.Vertical" è a 1.5
        _headBob.RegisterEventCallback(1.5f, PlayFootStepSound, CurveControlledBobCallbackType.Vertical);

        if (_flashLight)
            _flashLight.SetActive(false);
    }

    protected void Update() {
        // Se siamo in caduta fa partire il timer
        if (_characterController.isGrounded) _fallingTimer = 0.0f;
        else _fallingTimer += Time.deltaTime;


        if (Time.timeScale > Mathf.Epsilon)
            _mouseLook.LookRotation(transform, _camera.transform);

        if (Input.GetButtonDown("Flashlight")) {
            if (_flashLight)
                _flashLight.SetActive(!_flashLight.activeSelf);
        }

        // Lettura pressione pulsante Jump
        if (!_jumpButtonPressed && !_isCrouching)
            _jumpButtonPressed = Input.GetButtonDown("Jump");

        if (Input.GetButtonDown("Crouch")) {
            _isCrouching = !_isCrouching;
            _characterController.height = _isCrouching == true ? _controllerHeight / 2.0f : _controllerHeight;
        }

        // Calcola lo Status del Character
        if (!_previouslyGrounded && _characterController.isGrounded) {
            if (_fallingTimer > 0.5f) {
                // TODO: Play Landing Sound
            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.Landing;
        }
        else if (!_characterController.isGrounded)
            _movementStatus = PlayerMoveStatus.NotGrounded;
        else if (_characterController.velocity.sqrMagnitude < 0.01f)
            _movementStatus = PlayerMoveStatus.NotMoving;
        else if (_isCrouching)
            _movementStatus = PlayerMoveStatus.Crouching;
        else if (_isWalking)
            _movementStatus = PlayerMoveStatus.Walking;
        else
            _movementStatus = PlayerMoveStatus.Running;

        _previouslyGrounded = _characterController.isGrounded;

        _dragMultiplier = Mathf.Min( _dragMultiplier + Time.deltaTime , _dragMultiplierLimit );
    }

    protected void FixedUpdate() {
        // Lettura input da axis
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool waswalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        // Setta la velocità del Player in base a se si sta camminando o correndo
        float speed = _isCrouching ? _crouchSpeed : _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

        // Normalizza l'input nel caso in cui si dovesse sforare l'1
        if (_inputVector.sqrMagnitude > 1) _inputVector.Normalize();

        // Si muove sempre frontalmente in base alla telecamera 
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2f, 1))
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        // Scaling del movimento in base alla velocità attuale
        _moveDirection.x = desiredMove.x * speed * _dragMultiplier;
        _moveDirection.z = desiredMove.z * speed * _dragMultiplier;

        if (_characterController.isGrounded) {
            // Applico una forza verso il basso in modo da riomanere attaccato al pavimento
            _moveDirection.y = -_stickToGroundForce;

            // Se il Jump Button è stato premuto applica la speed verso l'alto
            // e setta isJumping a true + ripristina lo sato del pulsante Jump
            if (_jumpButtonPressed) {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
                // TODO: Play Jumping Sound
            }
        }
        else {
            // Se non siamo a terra applica la gravità di Unity + modificatore
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        // Muove il Character Controller
        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.z);
        if (speedXZ.magnitude > 0.01f)
            _camera.transform.localPosition = _localSpaceCameraPos + _headBob.GetVectorOffset(speedXZ.magnitude * (_isCrouching || _isWalking ? 1.0f : _runStepLengthen));
        else
            _camera.transform.localPosition = _localSpaceCameraPos;
    }

    void PlayFootStepSound() {
        if (_isCrouching)
            return;

        AudioSources[_audioToUse].Play();
        _audioToUse = (_audioToUse == 0) ? 1 : 0;
    }

    public void DoStickiness() {
        _dragMultiplier = 1.0f - _npcStickness;
    }
    
}
