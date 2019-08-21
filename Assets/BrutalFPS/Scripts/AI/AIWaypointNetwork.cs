using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Mostra la modalità in cui il Custom Inspector può stare nel AIWaypointNetwork component 
public enum PathDisplayMode { None, Connections, Paths }


// Contiene una lista di Waypoint. Ogni waypoint
// è una referenza ad una transform.
// Possiede anche le impostazioni per il Custom Inspector

public class AIWaypointNetwork : MonoBehaviour {
    [HideInInspector]
    public PathDisplayMode DisplayMode = PathDisplayMode.Connections;   // Modalità attuale 
    [HideInInspector]
    public int UIStart = 0;                                         // Start wayopoint index per Paths mode
    [HideInInspector]
    public int UIEnd = 0;                                           // End waypoint index per Paths mode

    // List of Transform references
    public List<Transform> Waypoints = new List<Transform>();

}
