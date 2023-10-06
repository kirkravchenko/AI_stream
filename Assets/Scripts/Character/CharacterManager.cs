using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterManager : MonoBehaviour
{

    #region properties
    private static CharacterManager instance;
    public static CharacterManager Instance { get { return instance; } }
    public GameObject[] characterPrefabs;
    private List<GameObject> spawnedCharacters;
    [SerializeField] 
    private Dictionary<CharacterType, Character> charactersDictionary = new Dictionary<CharacterType, Character>();
    private PointOfInterest activePoe;
    public PointOfInterest GetActivePoe { get { return activePoe; } }
    #endregion

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LocationManager.OnLocationLoaded += SetupCharacters;
    }

    private void SetupCharacters(
        List<GameObject> characters, PointOfInterest poe
    )
    {
        spawnedCharacters = characters;
        Setup(poe);
    }

    private void Setup(PointOfInterest poe)
    {
        activePoe = poe;
        foreach (GameObject spawnedCharacter in spawnedCharacters)
        {
            if (
                spawnedCharacter
                    .TryGetComponent(out Character character)
            )
            {
                charactersDictionary.Add(character.type, character);
                if (character.TryGetComponent(out AudioVoice voice))
                {
                    voice.SetReverbPreset(poe.audioEffect);
                }
            }
        }

        //Assign correct characters paths to created paths of selected Point of Interest
        foreach (PointOfInterest.PredeterminedPath predeterminedPath in poe.predeterminedPaths)
        {
            for (int i = 0; i < charactersDictionary.Count; i++)
            {
                if (charactersDictionary.ContainsKey(predeterminedPath.character))
                {
                    // CharacterTypes matches , i.e CharacterType.Spongebob == CharacterType.Spongebob
                    Character character = GetCharacterByType(predeterminedPath.character);
                    character.SetPredeterminedTransform(predeterminedPath.targetPositions);
                    break;
                }
            }
        }
    }

    public void SetAudioDistortionFilterForSpeakingCharacter(
        VoiceModifier.DistortionPreset distortionPreset = 
            VoiceModifier.DistortionPreset.Normal
    )
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.isSpeaking)
            {
                if (character
                    .TryGetComponent(out VoiceModifier voice)
                )
                {
                    voice.SetDistortionPreset(distortionPreset);
                    return;
                }
            }
        }
    }

    public void SetAudioReverbFilterForSpeakingCharacter(
        VoiceModifier.ReverbPreset reverbPreset = 
            VoiceModifier.ReverbPreset.Default
    )
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.isSpeaking)
            {
                if (character
                    .TryGetComponent(out VoiceModifier voice)
                )
                {
                    voice.SetReverbPreset(reverbPreset);
                    return;
                }
            }
        }
    }

    public void EnableLowPassFilterForSpeakingCharacter()
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.isSpeaking)
            {
                if (character
                    .TryGetComponent(out VoiceModifier voice)
                )
                {
                    voice.ToggleLowPassFilter(true);
                    return;
                }
            }
        }
    }

    public void ResetSpeakingFlagForCharacters()
    {
        foreach (Character character in charactersDictionary.Values)
            character.isSpeaking = false;
    }

    public void ResetCharactersVoiceFilters()
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.TryGetComponent(out VoiceModifier voice))
            {
                if (character.type == CharacterType.Squidward)
                    voice.SetDistortionPreset(
                        VoiceModifier.DistortionPreset.Earrape
                    );
                else
                    voice.SetDistortionPreset(
                        VoiceModifier.DistortionPreset.Normal
                    );
                voice.SetReverbPreset(activePoe.audioEffect);
                voice.ToggleHighPassFilter(false);
                voice.ToggleLowPassFilter(false);
            }
        }
    }

    public Character GetSpeakingCharacter()
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.isSpeaking)
            {
                return character;
            }
        }
        return null;
    }

    public Character GetCharacterByName(string name)
    {
        foreach (Character character in charactersDictionary.Values)
        {
            if (character.characterData.name == name) return character;
        }
        return null;
    }

    public CharacterType GetCharacterTypeByName(string name)
    {
        Character character = GetCharacterByName(name);
        return GetCharacterType(character);
    }

    public Character GetCharacterByType(CharacterType type)
    {
        if (charactersDictionary.ContainsKey(type))
            return charactersDictionary[type];
        return null;
    }

    public CharacterType GetCharacterType(Character character)
    {
        foreach (
            KeyValuePair<CharacterType, Character> pair in 
            charactersDictionary
        )
        {
            if (pair.Value == character)
            {
                return pair.Key;
            }
        }
        return CharacterType.None;
    }

    private void OnDestroy()
    {
        LocationManager.OnLocationLoaded -= SetupCharacters;
    }
}