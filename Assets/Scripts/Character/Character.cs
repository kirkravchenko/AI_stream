using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Character : BaseCharacter
{

    [Space(10)]
    public Data characterData;
    [System.Serializable]
    public struct Data
    {
         public string name;
         public string[] prefixes;
         public string voicemodelUuid;
    }

    public CharacterType type = CharacterType.None;
    [SerializeField] AudioSource audioSource;
    [Header("Interactions")]
    public AudioClip[] kissAudio;
    [SerializeField, Range(0, 100)] float kissChance = 20;
    private float interactionCooldown = 5f;
    private float interactionTimer;
    private bool CanInteract => Time.time > interactionTimer + interactionCooldown;

    // Basically it's an Awake() function
    public override void Init()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        interactionTimer = 0;
    }

    //How this character should behave when he's supposed to LookAt target?
    public override void LookAt(Transform target)
    {
        if (target == null) return;
        Vector3 lookDir = Vector3.Normalize(target.position - transform.position);
        lookDir.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, LookAtRotationSpeed * Time.deltaTime);
    }

    //Tick is basically an Update() function
    public override void Tick()
    {

    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        Agent.ResetPath();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 6) //Layer: AIActor
        {
            if (other.TryGetComponent(out Character character))
            {
                // Character is very close to another character

                //TODO: possible interactions with one another

                if (CanInteract)
                {
                    if (isSpeaking) return;

                    //Kiss sound
                    if (RandomChance(kissChance))
                    {
                        if (audioSource != null)
                        {
                            interactionTimer = Time.time;
                            audioSource.PlayOneShot(kissAudio[Random.Range(0, kissAudio.Length)]);
                        }
                    }
                }
            }
        }
    }
}