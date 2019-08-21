using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInfo {
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}

public class GameManager : MonoBehaviour {
    //Inspector
    [SerializeField]
    private ParticleSystem _bloodParticles = null;

    //Statics
    private static GameManager _instance = null;

    public static GameManager instance {
        get {
            if (_instance == null) {
                _instance = (GameManager)FindObjectOfType(typeof(GameManager));
            }
            return _instance;
        }
    }

    //Private
    private Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();
    private Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>();

    //Properties
    public ParticleSystem bloodParticles { get { return _bloodParticles; } }

    //Salva la State Machine in ingresso nel Dictionary con la chiave che gli viene fornita
    public void RegisterAIStateMachine(int key, AIStateMachine stateMachine) {
        if (!_stateMachines.ContainsKey(key)) {
            _stateMachines[key] = stateMachine;
        }
    }


    //Restituisce una referenza dell'IA State Machine basandosi sull'ID d'istanza di un oggetto
    public AIStateMachine GetAIStateMachine(int key) {
        AIStateMachine machine = null;
        if (_stateMachines.TryGetValue(key, out machine)) {
            return machine;
        }

        return null;
    }

    //Salva il PlayerInfo in ingresso nel Dictionary con la chiave che gli viene fornita
    public void RegisterPlayerInfo(int key, PlayerInfo playerInfo) {
        if (!_stateMachines.ContainsKey(key)) {
            _playerInfos[key] = playerInfo;
        }
    }


    //Restituisce una referenza del PlayerInfo basandosi sull'ID d'istanza di un oggetto
    public PlayerInfo GetPlayerInfo(int key) {
        PlayerInfo info = null;
        if (_playerInfos.TryGetValue(key, out info)) {
            return info;
        }

        return null;
    }

}
