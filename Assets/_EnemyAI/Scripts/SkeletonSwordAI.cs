using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

public class SkeletonSwordAI : MonoBehaviour, IHasHealth, IHasRigidbody
{
    public enum State
    {
        Idle,
        Patrolling,
        Chasing,
        Dead
    }

    public enum Action
    {
        Idle,
        Attacking,
    }

    public State currentState;
    public Action currentAction;

    [SerializeField] private int maxHealth;
    [SerializeField] private int damage;
    [SerializeField] private float stunDuration;
    [SerializeField] private Collider weaponCollider;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private SplineContainer splineContainer;
    private Animator anim;
    private Transform targetTransform;

    [Header("Patrol Parameters:")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float patrolSpeed;
    [SerializeField] private float lookDistance;
    [SerializeField] private float lookHeight;
    [SerializeField][Range(20, 180)] private int lookAngle;
    [SerializeField] private int minPatrolWaitTime;
    [SerializeField] private int maxPatrolWaitTime;
    [SerializeField] private float patrolDistance;
    [SerializeField] private float detectionRadius;

    [Header("Chase Parameters:")]
    [SerializeField] private float chaseRadius;
    [SerializeField] private float chaseSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float angularSpeed;

    [Header("Attack Parameters:")]
    [SerializeField] private float attackRadius;
    [SerializeField] private float minAttackInterval;
    [SerializeField] private float maxAttackInterval;   
    [SerializeField] private float roatateSpeed;

    private int currentHealth;
    private int noOfRays;
    private Vector3 splinePos;
    private Quaternion splineRot;
    private float distanceTravelled = 0f;
    private float currentEndRange = 0f;
    private float waitTime;
    private Vector3 targetDir;
    private float lastAttackTime;
    private float currentAttackInterval = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        splineContainer = GetComponentInChildren<SplineContainer>();

        currentHealth = maxHealth;

        currentEndRange += patrolDistance / splineContainer.CalculateLength();

        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRadius;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;

        splinePos = splineContainer.transform.position;
        splineRot = splineContainer.transform.rotation;

        lookAngle -= lookAngle % 5;
        noOfRays = lookAngle / 5 + 1;
    }

    private void Update()
    {
        if (currentState == State.Dead) return;

        if (targetTransform == null)
        {
            agent.enabled = false;
        }
        else
        {
            agent.enabled = true;
        }

        HandleAction();
        HandleState();
        RotateToTarget();


        splineContainer.transform.SetPositionAndRotation(splinePos, splineRot);
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnStateChanged();
    }

    private void ChangeAction(Action newAction)
    {
        if (currentAction == newAction) return;

        currentAction = newAction;
        OnActionChanged();
    }

    private void OnStateChanged()
    {
        if (currentState == State.Idle)
        {
            anim.CrossFadeInFixedTime("Idle", 0.2f);
            if (agent.enabled) agent.isStopped = true;
        }
        else if (currentState == State.Patrolling)
        {
            anim.CrossFadeInFixedTime("Walk", 0.2f);
        }
        else if (currentState == State.Chasing)
        {
            anim.CrossFadeInFixedTime("Run", 0.2f);
            if (agent.enabled) agent.isStopped = false;
        }
    }

    private void OnActionChanged()
    {
        if (currentAction == Action.Attacking)
        {
            anim.CrossFadeInFixedTime("Attack", 0.05f);
        }
    }

    private void HandleAction()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack"))
        {
            ChangeAction(Action.Attacking);
        }
        else
        {
            ChangeAction(Action.Idle);
        }
    }

    private void HandleState()
    {
        if (currentAction == Action.Attacking) return;

        if (targetTransform == null)
        {
            Patrol();
        }
        else
        {
            targetDir = new Vector3(targetTransform.position.x, 0f, targetTransform.position.z) - 
                new Vector3(transform.position.x, 0f, transform.position.z);
            var targetDistance = targetDir.magnitude;
            targetDir = targetDir.normalized;

            if (targetDistance > chaseRadius)
            {
                //When target is not within chase radius
                ChangeState(State.Chasing);
                agent.destination = targetTransform.position;
            }
            else if (Time.time - lastAttackTime < currentAttackInterval)
            {
                //When not ready to attack
                ChangeState(State.Idle);
            }
            else
            {
                //Ready to attack
                if (targetDistance > attackRadius)
                {
                    //Chase the target till it gets within attack radius
                    ChangeState(State.Chasing);
                    agent.destination = targetTransform.position;
                }
                else
                {
                    //Attack when target is within attack radius
                    weaponCollider.enabled = true;
                    ChangeState(State.Idle);
                    ChangeAction(Action.Attacking);
                    Attack();
                }
            }
        }
    }

    private void RotateToTarget()
    {
        if (currentState == State.Idle && targetTransform != null)
        {
            var targetForward = targetDir == Vector3.zero ? transform.forward : targetDir;
            transform.forward = Vector3.Lerp(transform.forward, targetForward, roatateSpeed * Time.deltaTime);
        }
    }

    private void Patrol()
    {
        agent.enabled = false;
        DetectPlayer();
        LookForPlayer();
        if (waitTime > 0f)
        {
            waitTime -= Time.deltaTime;
            ChangeState(State.Idle);
        }
        else
        {
            MoveAlongSpline();
            ChangeState(State.Patrolling);
        }
    }

    private void DetectPlayer()
    {
        Collider[] collidersHit = Physics.OverlapSphere(transform.position + Vector3.up * lookHeight, detectionRadius, playerLayer);
        foreach (Collider collider in collidersHit)
        {
            if (collider != null && collider.GetComponent<Player>())
            {
                targetTransform = collider.transform;
            }
        }
    }

    private void LookForPlayer()
    {
        for (int i = 0; i < noOfRays; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(lookAngle * 0.5f - i * 5, Vector3.up) * transform.forward;
            Vector3 from = transform.position + Vector3.up * lookHeight;
            if (Physics.Raycast(from, dir, out RaycastHit hit, lookDistance, playerLayer))
            {
                if (hit.transform.GetComponent<Player>())
                {
                    targetTransform = hit.transform;
                }
            }
        }
    }

    private void MoveAlongSpline()
    {
        distanceTravelled += patrolSpeed * Time.deltaTime;
        if (distanceTravelled >= splineContainer.CalculateLength())
        {
            distanceTravelled %= splineContainer.CalculateLength();
        }
        float normalizedTime = distanceTravelled / splineContainer.CalculateLength();
        if (Mathf.Abs(normalizedTime - currentEndRange) < 0.1f)
        {
            waitTime = UnityEngine.Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
            currentEndRange += patrolDistance / splineContainer.CalculateLength();
            currentEndRange %= 1;
        }

        Vector3 position = splineContainer.EvaluatePosition(normalizedTime);
        Vector3 rotation = splineContainer.EvaluateTangent(normalizedTime);

        transform.position = position;
        transform.forward = rotation;
    }

    private void Attack()
    {
        lastAttackTime = Time.time;
        currentAttackInterval = UnityEngine.Random.Range(minAttackInterval, maxAttackInterval);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IHasHealth hasHealth) && other.GetComponent<Player>())
        {
            hasHealth.TakeDamage(damage, gameObject);
            weaponCollider.enabled = false;
        }
    }

    public void Heal(int healAmount)
    {
        if (currentState == State.Dead) return;

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int damageAmount, GameObject attacker)
    {
        if (currentState == State.Dead) return;

        targetTransform = attacker.transform;
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnZeroHealth();
            return;
        }
        OnTakeDamage();
    }

    private void OnTakeDamage()
    {
        if (stunDuration > currentAttackInterval - (Time.time - lastAttackTime))
        {
            lastAttackTime = Time.time;
            currentAttackInterval = stunDuration;
        }

        int index = Random.Range(0, 3);
        switch (index)
        {
            case 0:
                anim.Play("Hit01");
                break;
            case 1:
                anim.Play("Hit02");
                break;
            case 2:
                anim.Play("Hit03");
                break;
        }
    }

    private void OnZeroHealth()
    {
        ChangeState(State.Dead);
        agent.enabled = false;
        int index = Random.Range(0, 3);
        switch (index)
        {
            case 0:
                anim.Play("Death01");
                break;
            case 1:
                anim.Play("Death02");
                break;
            case 2:
                anim.Play("Death03");
                break;
        }
    }

    public void Push(Vector3 puchDir, float force)
    {
        rb.AddForce(force * puchDir, ForceMode.Force);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        lookAngle -= lookAngle % 5;
        noOfRays = lookAngle / 5 + 1;
        for (int i = 0; i < noOfRays; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(lookAngle * 0.5f - i * 5, Vector3.up) * transform.forward;
            Vector3 from = transform.position + Vector3.up * lookHeight;
            Vector3 to = transform.position + Vector3.up * lookHeight + dir.normalized * lookDistance;
            Gizmos.DrawLine(from, to);
        }
        Gizmos.DrawWireSphere(transform.position + lookHeight * Vector3.up, detectionRadius);
    }
}
