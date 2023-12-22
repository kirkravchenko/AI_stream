using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseCharacter : MonoBehaviour
{

    #region properties
    [Header("References")]
    [SerializeField] Rigidbody rb;
    public Rigidbody Rb { get { return rb; } }
    [SerializeField] private NavMeshAgent agent;
    public NavMeshAgent Agent { get { return agent; } }
    [SerializeField] Animator anim;
    public Animator Anim { get { return anim; } }

    [Header("NavMesh agent settings")]
    [SerializeField, Range(0, 4)] float moveSpeed = 1.5f;
    public float MoveSpeed { get { return moveSpeed; } }
    [SerializeField, Range(0, 4)] float baseOffset = 1.303f;
    public float BaseOffset { get { return baseOffset; } }
    [SerializeField, Range(0, 8)] float acceleration = 3;
    public float Acceleration { get { return acceleration; } }
    [SerializeField, Range(0, 400)] float rotateSpeed = 250f;
    public float RotationSpeed { get { return rotateSpeed; } }
    [SerializeField, Range(0, 10)] float lookAtRotationSpeed = 3;
    public float LookAtRotationSpeed { get { return lookAtRotationSpeed; } }
    [HideInInspector] public bool isSpeaking = false;

    [Header("Movement behaviour")]
    public bool moveRandomly = true;
    [SerializeField] float minWaitTime = 45f;
    [SerializeField] float maxWaitTime = 150f;
    [SerializeField] float maxWanderDistance = 3f;

    [Space(10), Header("Predetermined positions")]
    [SerializeField] bool enabledPredeterminedPos = false;
    [SerializeField, Range(0, 100)] float chanceRate_P = 50;
    [SerializeField] Transform[] predeterminedTransforms;

    private Transform lookAtTarget;
    private Transform selectedPredeterminedTransform;
    private bool randomMovement;
    private Vector3 startingPos;
    private Transform speakingCharacter;
    private bool waitTimerFinish = false;
    private float stuckThreshold = -1f;
    public bool disableAllMovement = false;

    [SerializeField] string name;

    #endregion

    #region Abstract functions
    public abstract void Init();
    public abstract void Tick();
    public abstract void LookAt(Transform target);
    #endregion

    private void Awake()
    {
        Init();
        if (Agent != null)
        {
            SetupMovement();
        }
    }

    public void SetPredeterminedTransform(Transform[] transforms)
    {
        predeterminedTransforms = transforms;
        enabledPredeterminedPos = true;
    }

    public void StartSpeaking()
    {
        disableAllMovement = true;
        isSpeaking = true;
        if (Anim) Anim.SetBool("isSpeaking", isSpeaking);
    }

    public void StopSpeaking()
    {
        disableAllMovement = false;
        isSpeaking = false;
        if (Anim) Anim.SetBool("isSpeaking", isSpeaking);
    }

    private void Update()
    {
        if (Agent && Agent.isOnNavMesh)
        {
            if (
                enabledPredeterminedPos && 
                predeterminedTransforms.Length > 0 && 
                stuckThreshold == -1f
            )
            {
                MoveTo(
                    predeterminedTransforms[
                        Random.Range(
                            0, predeterminedTransforms.Length
                        )
                    ], 0.01f
                );
            }

            if (isSpeaking) {
                // if (name == "Doomer_wrapper") Debug.Log(">> isSpeaking true");
                Agent.isStopped = true;
                if (Anim != null) Anim.SetBool("isStopped", Agent.isStopped);
            } 
            else
            {
                // if (name == "Doomer_wrapper") Debug.Log(">> isSpeaking false");
                Agent.isStopped = false;
                if (Anim != null) Anim.SetBool("isStopped", Agent.isStopped);
                ControlMovement();
            }
        }

        if (DestinationReached) {
            Agent.isStopped = true;
            if (Anim != null) Anim.SetBool("isStopped", Agent.isStopped);
        } else {
            Agent.isStopped = false;
            if (Anim != null) Anim.SetBool("isStopped", Agent.isStopped);
        }

        if (DestinationReached && speakingCharacter)
        {
            LookAt(speakingCharacter);
        }
        else if (lookAtTarget != null)
        {
            LookAt(lookAtTarget);
        }
        Tick();
    }

    public void NewLookAt(Transform target)
    {
        if (target != transform)
            StartCoroutine(LookAtTargetCoroutine(target));
    }

    private IEnumerator LookAtTargetCoroutine(Transform target)
    {
        yield return new WaitForSeconds(Random.Range(0, 0.3f));
        lookAtTarget = target;
    }

    public bool DestinationReached => 
        Distance(
            transform.position, Agent.destination
        ) < 0.1f || stuckThreshold == -1f;

    
    private float Distance(Vector3 a, Vector3 b)
    {
        float num = a.x - b.x;
        float num2 = a.z - b.z;
        return (float)System.Math.Sqrt(num * num + num2 * num2);
    }
    
    public bool HasPath => Agent.hasPath;

    public void MoveToRandomPoint()
    {
        if (!Agent || disableAllMovement) return;
        Vector3 newDesiredPosition = GetRandomPoint();
        Agent.SetDestination(newDesiredPosition);
        stuckThreshold = 5f;
    }

    public void MoveTo(Transform transform, float radius)
    {
        if (!Agent || disableAllMovement) return;
        stuckThreshold = 15f;
        Vector3 newDesiredPosition = 
            GetRandomPoint(transform, radius);
        Agent.SetDestination(newDesiredPosition);
    }
    
    public void RecalculatePathAndSetDestination(Vector3 position)
    {
        if (!Agent || disableAllMovement) return;
        stuckThreshold = 15f;
        Agent.SetDestination(position);
        Debug.Log("Moving...");
    }
    
    public void NewStartPosition(Vector3 position) => 
        startingPos = position;

    public bool TimerFinished => waitTimerFinish;

    public void WaitAndMove()
    {
        StopCoroutine(WaitAndMoveCoroutine());
        StartCoroutine(WaitAndMoveCoroutine());
    }

    public bool RandomChance(float chance = 50)
    {
        chance /= 100;
        if (Random.value < chance) return true;
        return false;
    }

    public void ForceNextDestinationTry() => stuckThreshold = -1f;

    public bool HasReached(Vector3 position)
    {
        Vector2 characterPos = 
            new(transform.position.x, transform.position.z);
        Vector2 targetPos = new(position.x, position.z);
        return Vector2.Distance(characterPos, targetPos) < 0.1f;
    }

    private void ControlMovement()
    {
        if (disableAllMovement) return;
        if (stuckThreshold > 0)
        {
            stuckThreshold -= Time.deltaTime;
            if (stuckThreshold <= 0) stuckThreshold = -1f;
        }

        if (enabledPredeterminedPos)
        {
            if (selectedPredeterminedTransform != null)
            {
                randomMovement = HasReached(
                    selectedPredeterminedTransform.position
                ) && moveRandomly;
                if (
                    !Agent.hasPath && 
                        !HasReached(
                            selectedPredeterminedTransform.position
                        )
                )
                {
                    RecalculatePathAndSetDestination(
                        selectedPredeterminedTransform.position
                    );
                }
                if (randomMovement) enabledPredeterminedPos = false;
            }
        }

        if (randomMovement)
        {
            if (DestinationReached && TimerFinished)
            {
                Agent.ResetPath();
                WaitAndMove();
            }
        }
    }

    private bool RandomPoint(
        Vector3 center, float range, out Vector3 result
    )
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = 
                center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (
                NavMesh.SamplePosition(
                    randomPoint, out hit, 0.3f, NavMesh.AllAreas
                )
            )
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    private Vector3 GetRandomPoint(
        Transform point = null, float radius = 0
    )
    {
        Vector3 newPoint;
        if (
            RandomPoint(point == null ? startingPos : point.position, 
            radius == 0 ? maxWanderDistance : radius, out newPoint)
        )
        {
            Debug.DrawRay(newPoint, Vector3.up, Color.green, 4);
            return newPoint;
        }
        return point == null ? startingPos : point.position;
    }

    private IEnumerator WaitAndMoveCoroutine()
    {
        waitTimerFinish = false;
        float randomWaitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(randomWaitTime);
        waitTimerFinish = true;
        MoveToRandomPoint();
    }

    public void GoToPredeterminedPosition()
    {
        if (!enabledPredeterminedPos || disableAllMovement) return;
        if (RandomChance(chanceRate_P))
        {
            if (predeterminedTransforms.Length > 0)
            {
                selectedPredeterminedTransform = 
                    predeterminedTransforms[
                        Random.Range(0, 
                            predeterminedTransforms.Length)
                    ];
                if (selectedPredeterminedTransform == null) 
                { 
                    enabledPredeterminedPos = false; 
                    return; 
                }
                MoveTo(selectedPredeterminedTransform, 0.01f);
                NewStartPosition(
                    selectedPredeterminedTransform.position
                );
            }
        }
    }

    private void SetupMovement()
    {
        if (!Agent || disableAllMovement) return;
        startingPos = transform.position;
        Agent.speed = MoveSpeed;
        Agent.baseOffset = BaseOffset;
        Agent.acceleration = Acceleration;
        Agent.angularSpeed = RotationSpeed;
        randomMovement = moveRandomly;
        stuckThreshold = -1f;
        waitTimerFinish = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(startingPos, maxWanderDistance);

        if (selectedPredeterminedTransform != null && enabledPredeterminedPos) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(selectedPredeterminedTransform.position, Vector3.one * 0.4f);
        }
    }
#endif

    private void OnValidate()
    {
        if (rb == null)
            TryGetComponent(out rb);
        if (anim == null)
            TryGetComponent(out anim);
        if (agent == null)
            TryGetComponent(out agent);
    }

    public void SetSpeakingCharacter(Transform speakingChar)
    {
        speakingCharacter = speakingChar;
    }
}