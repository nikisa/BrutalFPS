using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Include un AudioMixerGroup nell'AudioMixer di Unity.
// Contiene il nome del gruppo (che è anche il parametro di volume associato)
// il gruppo stesso e un IEnumerator per attenuare la traccia audio nel tempo
public class TrackInfo {
    public string Name = string.Empty;
    public AudioMixerGroup Group = null;
    public IEnumerator TrackFader = null;
}

// Fornisce funzionalità one-shot in pool con il priority system  
// contenendo anche lo Unity Audio Mixer per semplificare 
// l'utilizzo dei volumi nei gruppi audio
public class AudioManager : MonoBehaviour
{
    //Statics 
    private static AudioManager _instance = null;
    public static AudioManager instance {
        get {
            if (_instance==null) 
                _instance = (AudioManager)FindObjectOfType(typeof(AudioManager));
            return _instance;
        }
    }

    // Inspector
    [SerializeField]
    AudioMixer _mixer = null;

    // Private
    Dictionary<string, TrackInfo> _tracks = new Dictionary<string, TrackInfo>();

    private void Awake() {
        DontDestroyOnLoad(gameObject);

        if (!_mixer) return;

        // Unisco tutti i gruppi presenti nel mixer
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(string.Empty);

        // Creo la traccia del Mixer basando sul nome del Gruppo (Track -> AudioGroup)
        foreach (AudioMixerGroup group in groups) {
            TrackInfo trackInfo = new TrackInfo();
            trackInfo.Name = group.name;
            trackInfo.Group = group;
            trackInfo.TrackFader = null;
            _tracks[group.name] = trackInfo;
        }

    }

    private void Update() {
        
    }


    // Restituisce il volume dell'AudioMixerGroup assegnato alla traccia in ingresso.
    // AudioMixerGroup deve assolutamente esporre il proprio volume allo script
    // per fare in modo che funzioni e la variabile deve assolutamente avere lo stesso
    // nome del relativo gruppo
    public float GetTrackVolume(string track) {

        TrackInfo trackInfo;

        if (_tracks.TryGetValue(track , out trackInfo)) {
            float volume;
            _mixer.GetFloat(track, out volume);
            return volume;
        }
        return float.MinValue;
    }

    public AudioMixerGroup GetAudioMixerGroupFromTrackName(string name) {
        TrackInfo ti;
        if (_tracks.TryGetValue(name, out ti)) {
            return ti.Group;
        }
        return null;
    }

    // Setto il volume dell'AudioMixerGroup assegnato alla traccia che viene passata.
    // L'AudioMixerGroup deve assolutamente esporre il proprio volume allo script
    // per fare in modo che funzioni e la variabile deve assolutamente avere lo stesso
    // nome del relativo gruppo
    // Se viene assegnato un fade time , la coroutine verrà utilizzata per eseguire 
    // la dissolvenza
    public void SetTrackVolume(string track , float volume , float fadeTime = 0.0f) {

        if (!_mixer) return;

        TrackInfo trackInfo;

        if (_tracks.TryGetValue( track, out trackInfo )) {
            if (trackInfo.TrackFader != null)
                StopCoroutine(trackInfo.TrackFader);

            if (fadeTime == 0.0f)
                _mixer.SetFloat(track, volume);
            else {
                trackInfo.TrackFader = SetTrackVolumeInternal(track, volume, fadeTime);
                StartCoroutine(trackInfo.TrackFader);
            }
        }
    }

    // Utilizzato da SetTrackVolume per implementare , nel corso del tempo ,
    // una dissolvenza tra i volumi di una traccia 
    protected IEnumerator SetTrackVolumeInternal(string track, float volume, float fadeTime) {
        float startVolume = 0.0f;
        float timer = 0.0f;
        _mixer.GetFloat(track, out startVolume);

        while (timer < fadeTime && fadeTime > 0) {
            timer += Time.unscaledDeltaTime;
            _mixer.SetFloat(track, Mathf.Lerp(startVolume, volume, timer / fadeTime));
            yield return null;
        }

        _mixer.SetFloat(track, volume);
    }

}
