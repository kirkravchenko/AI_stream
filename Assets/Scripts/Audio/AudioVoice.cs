using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVoice : VoiceModifier
{
    public event Action OnAudioPlayed;
    public event Action OnAudioFinish;
    public bool randomDistortion = false;
    [Range(0, 100)] public float overwriteDistortionChance = 0; 
    [Header("References")]
    [SerializeField] AudioSource m_Source;
    public AudioSource Source { get { return m_Source; } }
    [SerializeField] AudioClip m_Clip;
    private bool m_IsPlaying;
    DistortionPreset defaultDistortion;
    ReverbPreset defaultRefebs;

    private void Awake()
    {
        Play();
        defaultDistortion = filterToggles.distortion;
        defaultRefebs = ReverbPreset.Default;
    }

    //Modify audio and play it
    [ContextMenu("DEBUG/PLAY AUDIO CLIP")]
    public void Play(AudioClip clip = null)
    {
        if (clip != null) Init(clip);
        if (!CanPlay()) return;
        m_Source.Play();
        //Raise an event, might be usefull in the future
        OnAudioPlayed?.Invoke();
    }
    
    public void Init(
        AudioClip clip, 
        ReverbPreset reverbPreset = ReverbPreset.None,
        DistortionPreset distortionPreset = DistortionPreset.Normal
    )
    {
        m_Clip = clip;
        if (reverbPreset != ReverbPreset.None)
              filterToggles.reverbPreset = reverbPreset;
        if (distortionPreset != DistortionPreset.Normal)
        {
            if (randomDistortion)
            {
                int rand = UnityEngine.Random.Range(0, 101);
                if (rand < overwriteDistortionChance)
                    filterToggles.distortion = RandomDistortion();
            }
            else
                filterToggles.distortion = defaultDistortion;
        }

        InitFilters();

        m_Source.clip = m_Clip;
        m_Source.volume = GetVolume();
        m_Source.pitch = GetPitch();
    }

    private DistortionPreset RandomDistortion()
    {
        int rand = UnityEngine.Random.Range(0, 4);
        return (DistortionPreset)rand;
    }
    
    private void Update()
    {
        if (m_Source == null) return;

        //Check if audio finished playing
        if (m_Source.isPlaying == false && m_IsPlaying == true)
        {
            //Raise an event when the audio is finished, might be useful to move on to next character
           // Debug.Log("Audio finished playing.");
            OnAudioFinish?.Invoke();
        }
        m_IsPlaying = m_Source.isPlaying;

    }

    private bool CanPlay()
    {
        //Check if we have audio to play etc...
        if (m_Source == null)
        {
            m_Source = GetComponent<AudioSource>();
            if (!m_Source)
            {
                Debug.LogError("Missing reference to AudioSource component!");
                return false;
            }
        }
        if (m_Clip == null)
        {
           // Debug.LogWarning("You want to play audio but there's no audioClip assigned!");
            return false;
        }

        return true;
    }

    //Grab references and reload filters on runtime
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (m_Source == null) m_Source = GetComponent<AudioSource>();
        filters.distortionFilter = GetComponent<AudioDistortionFilter>();
        filters.reverbFilter = GetComponent<AudioReverbFilter>();
        filters.echoFilter = GetComponent<AudioEchoFilter>();
        filters.highPassFilter = GetComponent<AudioHighPassFilter>();
        filters.lowPassFilter = GetComponent<AudioLowPassFilter>();

        InitFilters();
    }
#endif
}
