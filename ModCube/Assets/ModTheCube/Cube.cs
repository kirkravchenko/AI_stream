using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    public MeshRenderer Renderer;
    public Vector3 newPos;
    public float scaleRatio;
    public Vector3 rotateAngle;
    
    void Start()
    {
        transform.position = newPos;
        transform.localScale = Vector3.one * scaleRatio;
        Material material = Renderer.material;
        material.color = new Color(
                Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 
                Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)
            );    }
    
    void Update()
    {
        transform.Rotate(
                rotateAngle.x * Time.deltaTime, 
                rotateAngle.y, rotateAngle.z
            );
    }
}
