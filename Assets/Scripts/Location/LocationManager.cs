using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocationManager : MonoBehaviour
{

    public static event Action<PointOfInterest> OnNewPointOfInterest;
    public static event Action<List<GameObject>, PointOfInterest> OnLocationLoaded;

    private static LocationManager instance;
    public static LocationManager Instance { get { return instance; } }

    private Character[] characters;



    [SerializeField] private PointOfInterest selectedPoint = null;
    public PointOfInterest SelectedPoint { get { return selectedPoint; } }

    private void Start()
    {
        ChooseLocation();
        LoadLocation();
    }
    [SerializeField] Points[] points;
    public Points[] PointsOfInterest { get { return points; } }

    [System.Serializable]
    public struct Points
    {
        public string name;
        public PointOfInterest pointOfInterest;
        [Range(0, 100)] public float chanceRate;
    }
    public void ChooseLocation()
    {
        if (PointsOfInterest == null || PointsOfInterest.Length == 0) return;

        // Calculate the total chance
        float totalChance = PointsOfInterest.Sum(point => point.chanceRate);

        // Choose a random value between 0 and totalChance
        float value = UnityEngine.Random.value * totalChance;
        float cumulativeChance = 0;
        foreach (var point in PointsOfInterest)
        {
            cumulativeChance += point.chanceRate;
            if (value < cumulativeChance)
            {
                selectedPoint = point.pointOfInterest;
                break;
            }
        }

        // Callback for prompt and other scripts
        OnNewPointOfInterest?.Invoke(selectedPoint);
    }

    public void LoadLocation()
    {
        // get chosen location
        // setup location, spawn characters
        List<GameObject> charactersToSpawn = CharacterManager.Instance.characterPrefabs.ToList();
        List<GameObject> spawnedCharacters = new List<GameObject>();
        foreach (GameObject character in charactersToSpawn)
        {
            Transform spawnPoint = null;
            if (character.TryGetComponent(out Character characterScript))
            {
                if (characterScript.type == CharacterType.Squidward)
                    spawnPoint = SelectedPoint.specialSpawnpoints.squidwardPoint != null ? SelectedPoint.specialSpawnpoints.squidwardPoint : SelectedPoint.GetAvailableSpawnpoint();
                else if (characterScript.type == CharacterType.MrKrabs)
                    spawnPoint = SelectedPoint.specialSpawnpoints.mrKrabsPoint != null ? SelectedPoint.specialSpawnpoints.mrKrabsPoint : SelectedPoint.GetAvailableSpawnpoint();

                else spawnPoint = SelectedPoint.GetAvailableSpawnpoint();
            }
            else Debug.LogError("No character script found!");
            GameObject newCharacter = Instantiate(character, spawnPoint.position, spawnPoint.rotation);
            spawnedCharacters.Add(newCharacter);

        }
        //Callback once its done.
        Debug.Log(OnLocationLoaded == null);

        OnLocationLoaded?.Invoke(spawnedCharacters, SelectedPoint);
    }

    public bool RandomChance(float chance = 50)
    {
        chance /= 100;
        if (UnityEngine.Random.value < chance) return true;
        return false;
    }


    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }
}
