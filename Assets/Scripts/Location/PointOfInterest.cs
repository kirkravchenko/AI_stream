using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class PointOfInterest : MonoBehaviour
{
    public PointName pointName;

    public VoiceModifier.ReverbPreset audioEffect;
    public Transform cameraLocation;

    public Transform[] normalSpawnPoints;

    public NpcSpawnPoint[] npcSpawnPoints;


    public SpecialSpawnpoints specialSpawnpoints;

    public PredeterminedPath[] predeterminedPaths;
    [System.Serializable]
    public struct PredeterminedPath
    {
        public CharacterType character;
        public Transform[] targetPositions;
    }
    public CameraSettings cameraSettings;
    [System.Serializable]
    public struct CameraSettings
    {
        public Vector3 followOffset;
        public float nearClipPlane;
    }


    [Header("Editor settings")]
    [SerializeField] Vector3 areaSize;

    private List<Transform> avialableSpawnPoints;

    [System.Serializable]
    public struct SpecialSpawnpoints
    {
        public Transform squidwardPoint;
        public Transform mrKrabsPoint;
        public Transform locationSpecialPoint;
    }
    [System.Serializable]
    public enum PointName
    {
        None,
        Test_1,
        Restaurant,
        Restaurant_Outside,
        Restaurant_Kitchen,
        Restaurant_Toilet,
        Restaurant_Office,
        Housing_01,
        Housing_02,
        ChumBucket
    }

    private void Awake()
    {
        avialableSpawnPoints = normalSpawnPoints.ToList();
    }

    public Transform GetAvailableSpawnpoint()
    {
        if (avialableSpawnPoints.Count == 0)
            return normalSpawnPoints[Random.Range(0, normalSpawnPoints.Length)];

        Transform spawnPoint = avialableSpawnPoints[Random.Range(0,avialableSpawnPoints.Count)];
        avialableSpawnPoints.Remove(spawnPoint);
        return spawnPoint;
    }

    private void OnDrawGizmos()
    {
        //Draw Point of interest area box
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, areaSize);

        Handles.color = Color.white;
        foreach(Transform t in normalSpawnPoints)
        {
            Handles.Label(t.position, $"{pointName} 's spawnpoint - {t.name}");
        }

        if (specialSpawnpoints.squidwardPoint != null)
            Handles.Label(specialSpawnpoints.squidwardPoint.position, $"{pointName}'s squidward spawnpoint"); 
        if(specialSpawnpoints.mrKrabsPoint != null)
            Handles.Label(specialSpawnpoints.mrKrabsPoint.position, $"{pointName}'s Mr Krabs spawnpoint");
        if(specialSpawnpoints.locationSpecialPoint != null)
            Handles.Label(specialSpawnpoints.locationSpecialPoint.position, $"{pointName}'s randomEvent spawnpoint");
    }
}
