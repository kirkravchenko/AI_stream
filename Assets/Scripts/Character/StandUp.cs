using UnityEngine;

public class StandUp : MonoBehaviour
{
    public float standUpSpeed = 0.5f; // Speed at which the character stands up
    private bool isStandingUp = false;

    void Update()
    {
        // Check if the character has fallen over (e.g., rotation around the X axis greater than 45 degrees)
        if (transform.eulerAngles.x > 45f && !isStandingUp) 
            isStandingUp = true; // Flag to indicate the character is in the process of standing up

        // If the character is standing up, gradually rotate back to the upright position
        if (isStandingUp)
        {
            Quaternion targetRotation = 
                Quaternion.Euler(
                    0f, transform.eulerAngles.y, 
                    transform.eulerAngles.z
                );
            transform.rotation = 
                Quaternion.Lerp(
                    transform.rotation, targetRotation, 
                    standUpSpeed * Time.deltaTime
                );

            // Check if the character is close enough to the upright position to stop standing up
            if (Quaternion
                .Angle(transform.rotation, targetRotation) < 1f) 
                    isStandingUp = false;
        }
    }
}