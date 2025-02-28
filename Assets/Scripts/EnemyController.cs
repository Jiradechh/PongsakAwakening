using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    #region Variables
    NavMeshAgent navMash;

    [Header("Status")]
    public float health = 100f;

    [Header("Move")]
    public float moveSpeed = 2f;
    public bool canMove = true;

    [Header("Knock")]
    public float knockbackForce;
    public float durationKnockback;

    [Header("Attack")]
    public int attackDamage = 10;
    public float attackRange = 1f;
    public float delayAttack = 1f;
    public float detectionRange = 5f;

    [Header("Ranged Settings")]
    public bool isRangedEnemy;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;

    private bool onAttack = false;
    private float distanceToPlayer;
    private Rigidbody rigidbody3D;
    private Collider collider3D;
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    protected Transform player;
    private bool isDead = false;
    private bool hasDetectedPlayer = false;

    private PlayerController playerHealth;
    private float attackCooldown;
    private float nextAttackTime = 0;
    private float initialYPosition;

    public bool IsDead => isDead;
    #endregion

    #region Unity Methods
    protected virtual void Start()
    {
        navMash = GetComponent<NavMeshAgent>();
        navMash.updateRotation = false;
        navMash.speed = moveSpeed;

        player = GameObject.FindWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerController>();
        rigidbody3D = GetComponent<Rigidbody>();
        collider3D = GetComponent<Collider>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rigidbody3D.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        initialYPosition = transform.position.y;
        attackCooldown = Random.Range(0.5f, 1.5f);
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        Vector3 lockedPosition = transform.position;
        lockedPosition.y = initialYPosition;
        transform.position = lockedPosition;

        HandleGroupBehavior();
    }
    #endregion

    #region Damage Methods
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;

        if (health > 0)
        {
            PlayHurtAnimation();
        }
        else
        {
            health = 0;
            Die();
        }
    }

    private void PlayHurtAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        if (animator != null) animator.SetTrigger("Die");

        canMove = false;

        if (navMash != null) navMash.isStopped = true;
        if (collider3D != null) collider3D.enabled = false;

        Invoke("DestroyEnemy", 1.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration = 0.2f)
    {
        StartCoroutine(KnockbackCoroutine(direction * force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 knockbackDistance, float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + knockbackDistance;
        endPosition.y = startPosition.y;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
    }
    #endregion

    #region Behavior Methods
    private void HandleGroupBehavior()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!hasDetectedPlayer && distanceToPlayer <= detectionRange)
        {
            hasDetectedPlayer = true;
            Debug.Log("Player detected. Enemy will now follow.");
        }

        if (hasDetectedPlayer)
        {
            if (distanceToPlayer <= attackRange)
            {
                if (Time.time >= nextAttackTime && !onAttack)
                {
                    StartCoroutine(PerformAttack());
                }
                else
                {
                    navMash.isStopped = true;
                    SetIdleAnimation();
                }
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
    }

private IEnumerator PerformAttack()
{
    if (isDead || onAttack) yield break;

    onAttack = true;
    canMove = false;
    navMash.isStopped = true;

    if (isRangedEnemy)
    {
        yield return StartCoroutine(PerformRangedAttack()); 
    }
    else
    {
        if (animator != null) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(delayAttack);

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log("Player attacked.");
        }
    }

    nextAttackTime = Time.time + attackCooldown;

    navMash.isStopped = false;
    onAttack = false;
    canMove = true;
}

private IEnumerator PerformRangedAttack()
{
    if (firePoint == null || projectilePrefab == null || isDead) yield break;

    if (animator != null)
    {
        animator.SetTrigger("Attack");
    }

    yield return new WaitForSeconds(0.3f);  

    GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();

    if (enemyProjectile != null)
    {
        Vector3 direction = (player.position - firePoint.position).normalized;
        enemyProjectile.Launch(direction);
    }

    Debug.Log("Ranged attack performed.");
}




    protected virtual void MoveTowardsPlayer()
    {
        if (isDead || !canMove) return;

        navMash.destination = player.position;
        navMash.isStopped = false;

        FlipSprite(); 
        if (animator != null) animator.SetBool("isWalking", true);
    }

    private void FlipSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = player.position.x > transform.position.x;
        }
    }

    private void SetIdleAnimation()
    {
        if (animator != null) animator.SetBool("isWalking", false);
    }
    #endregion

    #region Debugging Methods
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}
