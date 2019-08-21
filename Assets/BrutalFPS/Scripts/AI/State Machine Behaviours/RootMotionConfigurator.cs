using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink {
    [SerializeField]
    private int _rootPosition = 0;

    [SerializeField]
    private int _rootRotation = 0;

    private bool _rootMotionProcessed = false;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        if (_stateMachine) {
            _stateMachine.AddRootMotionRequest(_rootPosition, _rootRotation);
            _rootMotionProcessed = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        if (_stateMachine && _rootMotionProcessed)
            _stateMachine.AddRootMotionRequest(-_rootPosition, -_rootRotation);
        _rootMotionProcessed = false;
    }
}
