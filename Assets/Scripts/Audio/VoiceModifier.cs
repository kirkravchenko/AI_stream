using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioDistortionFilter)), RequireComponent(typeof(AudioReverbFilter)), RequireComponent(typeof(AudioEchoFilter)), RequireComponent(typeof(AudioHighPassFilter)), RequireComponent(typeof(AudioLowPassFilter)), RequireComponent(typeof(AudioSource))]
public abstract class VoiceModifier : MonoBehaviour
{
    [Header("Settings")]
    public float volume = 1.0f;
    public float pitch = 1.0f;

    public Randomization randomization;


    [Space(15)]
    public FilterToggles filterToggles;
    [Space(10)]
    public Filters filters;
    public void ToggleLowPassFilter(bool enable)
    {
        filterToggles.lowPass = enable;
        RefreshFilters();
    }
    public void ToggleHighPassFilter(bool enable)
    {
        filterToggles.highPass = enable;
        RefreshFilters();
    }
    public void SetReverbPreset(ReverbPreset preset)
    {
        filterToggles.reverbPreset = preset;
        RefreshFilters();
    }
    public void SetDistortionPreset(DistortionPreset preset)
    {
        filterToggles.distortion = preset;
        RefreshFilters();
    }

    public void InitFilters()
    {
        //Add/Remove Filters and apply settings 
        RefreshFilters();
        filters.reverbFilter.enabled = true;
        filters.distortionFilter.enabled = true;


    }
    private void RefreshFilters()
    {
        filters.reverbFilter.reverbPreset = GetReverbPreset();

        if (filters.reverbFilter.reverbPreset == AudioReverbPreset.User)
        {
            treeDomeReverb = new CustomReverb();
            LoadCustomFilter(treeDomeReverb);
        }

        filters.distortionFilter.distortionLevel = GetDistortionLevel();

        filters.echoFilter.enabled = filterToggles.echo;

        filters.highPassFilter.enabled = filterToggles.highPass;
        filters.lowPassFilter.enabled = filterToggles.lowPass;
    }

    public float GetVolume()
    {
        float volume = this.volume;
        if (randomization.randomizeVolume)
        {
            float randomVolume_min = Mathf.Clamp01(randomization.volumeRange.x);
            float randomVolume_max = Mathf.Clamp01(randomization.volumeRange.y);
            volume += UnityEngine.Random.Range(randomVolume_min, randomVolume_max);
        }

        return volume;
    }
    public float GetPitch()
    {
        return randomization.randomizePitch ? pitch + UnityEngine.Random.Range(randomization.pitchRange.x, randomization.pitchRange.y) : pitch;
    }



    private CustomReverb treeDomeReverb;
    private class CustomReverb
    {
        AudioReverbPreset customReverb = AudioReverbPreset.User;
        public float dryLevel = 0f;
        public float room = -1776f;
        public float roomHF = -4589f;
        public float roomLF = 0;
        public float decayTime = 1.7f;
        public float decayHFRatio = 0.1f;
        public float reflectionsLEvel = 1000;
        public float reflectionsDelay = 0.1f;
        public float reverbLevel = 756f;
        public float reverbDelay = 0.0021f;
        public float HFReference = 5000f;
        public float LFReference = 250f;
        public float diffusion = 80f;
        public float density = 60f;

    }
    private void LoadCustomFilter(CustomReverb treeDomeReverb)
    {
        filters.reverbFilter.dryLevel = treeDomeReverb.dryLevel;
        filters.reverbFilter.room = treeDomeReverb.room;
        filters.reverbFilter.roomHF = treeDomeReverb.roomHF;
        filters.reverbFilter.roomLF = treeDomeReverb.roomLF;
        filters.reverbFilter.decayTime = treeDomeReverb.decayTime;
        filters.reverbFilter.decayHFRatio = treeDomeReverb.decayHFRatio;
        filters.reverbFilter.reflectionsLevel = treeDomeReverb.reflectionsLEvel;
        filters.reverbFilter.reflectionsDelay = treeDomeReverb.reflectionsDelay;
        filters.reverbFilter.reverbLevel = treeDomeReverb.reverbLevel;
        filters.reverbFilter.reverbDelay = treeDomeReverb.reverbDelay;
        filters.reverbFilter.hfReference = treeDomeReverb.HFReference;
        filters.reverbFilter.lfReference = treeDomeReverb.LFReference;
        filters.reverbFilter.diffusion = treeDomeReverb.diffusion;
        filters.reverbFilter.density = treeDomeReverb.density;
    }



    private float GetDistortionLevel()
    {
        return filterToggles.distortion switch
        {
            DistortionPreset.Loud => 0.45f,
            DistortionPreset.Louder => 0.75f,
            DistortionPreset.Earrape => 0.9f,
            _ => 0f,
        };
    }
    private AudioReverbPreset GetReverbPreset()
    {
        return filterToggles.reverbPreset switch
        {
            ReverbPreset.Default => AudioReverbPreset.City,
            ReverbPreset.KrustyKrab => AudioReverbPreset.Livingroom,
            ReverbPreset.Underground => AudioReverbPreset.Stoneroom,
            ReverbPreset.FewerDream => AudioReverbPreset.Psychotic,
            ReverbPreset.Backrooms => AudioReverbPreset.Alley,
            ReverbPreset.TreeDome => AudioReverbPreset.User,
            _ => AudioReverbPreset.Off,
        };
    }

    #region Structs/Enums
    [System.Serializable]
    public struct Filters
    {
        public AudioDistortionFilter distortionFilter;
        public AudioEchoFilter echoFilter;
        public AudioReverbFilter reverbFilter;
        public AudioHighPassFilter highPassFilter;
        public AudioLowPassFilter lowPassFilter;
    }


    [System.Serializable]
    public struct FilterToggles
    {
        public ReverbPreset reverbPreset;
        public DistortionPreset distortion;
        public bool echo;

        public bool highPass;
        public bool lowPass;
    }
    [System.Serializable]
    public enum DistortionPreset
    {
        Normal,
        Loud,
        Louder,
        Earrape
    }

    [System.Serializable]
    public struct Randomization
    {
        public bool randomizePitch;
        public bool randomizeStartTime;
        public bool randomizeVolume;
        public Vector2 pitchRange;
        public Vector2 volumeRange;
    }
    public enum ReverbPreset
    {
        None,
        Default,
        KrustyKrab,
        Underground,
        TreeDome,
        Backrooms,
        FewerDream,
    }
    #endregion
}
