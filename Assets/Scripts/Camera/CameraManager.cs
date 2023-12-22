using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static CameraManager instance;
    public static CameraManager Instance 
    { 
        get { return instance; } 
    }

    private CinemachineVirtualCamera vCam;
    public CinemachineVirtualCamera VirtualCamera 
    { 
        get { return vCam; } 
    }

    private Transform cameraPointofInterestTransform;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);

        vCam = GetComponent<CinemachineVirtualCamera>();
    }

    private void Start()
    {
        LocationManager.OnNewPointOfInterest += InitCamera;
    }
    public void FocusOn(Transform t)
    {
        vCam.LookAt = t;
        vCam.Follow = t;
    }
    private void InitCamera(PointOfInterest poe)
    {
        cameraPointofInterestTransform = poe.cameraLocation;
        vCam.transform.position = 
            cameraPointofInterestTransform.position;
        vCam.transform.rotation = 
            cameraPointofInterestTransform.rotation;

        var transposer = 
            vCam.GetCinemachineComponent<CinemachineTransposer>();
        // transposer.m_BindingMode = CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
        transposer.m_FollowOffset = poe.cameraSettings.followOffset;
        vCam.m_Lens.NearClipPlane = poe.cameraSettings.nearClipPlane;
        transposer.m_XDamping = poe.cameraSettings.xDamping;
    }

    private void OnDestroy()
    {
        LocationManager.OnNewPointOfInterest -= InitCamera;
    }
}
