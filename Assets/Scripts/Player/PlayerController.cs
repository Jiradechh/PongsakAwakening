using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float dashForce = 15f;
    public float dashCooldown = 0.5f;

    [Header("Attack Settings")]
    public float lightAttackDamage = 10f;
    public float heavyAttackDamage = 25f;
    public float attackRange = 1.5f;
    public float lightAttackCooldown = 0.3f;
    public float heavyAttackCooldown = 0.7f;
    public float attackMoveLockDuration = 1.0f;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    public float projectileDamage = 50f;
    public float reloadTime = 3f;
    private bool canShoot = true;

    [Header("Firepoint Settings")]
    public Transform firepoint;
    public float firepointRotationSpeed = 10f;

    [Header("Sprite & Animation")]
    public SpriteRenderer spriteRenderer;
    public Animator anim;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Components")]
    public Rigidbody rb;
    private Gamepad gamepad;

    [Header("Direction Indicator")]
    public GameObject arrowIndicator;
    public float arrowShowThreshold = 0.1f;

    private Vector2 moveInput;
    private bool canDash = true;
    private bool canLightAttack = true;
    private bool canHeavyAttack = true;
    private bool canMove = true;
    private bool isPlayingAnimation = false;
    private bool isAttackAnimation = false;
    private bool isGrounded;
    private Vector3 lastShootDirection;

    private bool isInvulnerable = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        gamepad = Gamepad.current;
        currentHealth = maxHealth;
        lastShootDirection = Vector3.forward;
    }

    private void Update()
    {
        HandleGroundCheck();

        if (currentHealth > 0)
        {
            if (!isPlayingAnimation || !isAttackAnimation)
            {
                HandleMovement();
                HandleFirepointRotation();
            }
            HandleInput();
        }
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void HandleMovement()
    {
        if (!canMove || gamepad == null || isAttackAnimation) return;

        moveInput = gamepad.leftStick.ReadValue();
        float speed = Mathf.Lerp(walkSpeed, runSpeed, moveInput.magnitude);
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * speed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        UpdateArrowIndicator();

        if (moveInput.x != 0)
            spriteRenderer.flipX = moveInput.x < 0;

        PlayAnimation(moveInput.magnitude > arrowShowThreshold ? "P_Walk" : "P_Idle");
    }

    void HandleFirepointRotation()
    {
        if (gamepad == null || firepoint == null) return;

        Vector2 input = gamepad.leftStick.ReadValue();
        if (input.magnitude > 0.1f)
        {
            Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            firepoint.rotation = Quaternion.Slerp(firepoint.rotation, Quaternion.Euler(0f, targetAngle, 0f), Time.deltaTime * firepointRotationSpeed);
            lastShootDirection = direction;
            firepoint.position = transform.position + direction * 1.0f;
        }
    }

    void HandleInput()
    {
        if (gamepad == null) return;

        if (gamepad.buttonSouth.wasPressedThisFrame && canDash && canMove && isGrounded)
            StartCoroutine(DashCoroutine());

        if (gamepad.buttonWest.wasPressedThisFrame && canLightAttack)
        {
            Attack(lightAttackDamage, "P_LAttack");
            canLightAttack = false;
            Invoke(nameof(ResetLightAttack), lightAttackCooldown);
        }

        if (gamepad.buttonNorth.wasPressedThisFrame && canHeavyAttack)
        {
            Attack(heavyAttackDamage, "P_HAttack");
            canHeavyAttack = false;
            Invoke(nameof(ResetHeavyAttack), heavyAttackCooldown);
        }

        if (gamepad.buttonEast.wasPressedThisFrame)
            ShootProjectile();
    }

    void ShootProjectile()
    {
        if (!canShoot || projectilePrefab == null || isPlayingAnimation) return;

        GameObject projectile = Instantiate(projectilePrefab, firepoint.position, Quaternion.identity);
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        Vector3 direction = lastShootDirection.magnitude > 0.1f ? lastShootDirection : Vector3.forward;

        if (projectileRb != null)
            projectileRb.linearVelocity = direction * projectileSpeed;

        PlayAnimation("P_CastSpell", true);
        canShoot = false;
        Invoke(nameof(ReloadProjectile), reloadTime);
    }

    private void ReloadProjectile()
    {
        canShoot = true;
        Debug.Log("Projectile reloaded!");
    }

    private void Attack(float damage, string animationName)
    {
        if (isPlayingAnimation) return;

        isAttackAnimation = true;
        canMove = false;
        PlayAnimation(animationName, true);
        Invoke(nameof(ResetMove), attackMoveLockDuration);

        Collider[] hitObjects = Physics.OverlapSphere(firepoint.position, attackRange, LayerMask.GetMask("Enemy"));
        foreach (var obj in hitObjects)
        {
            EnemyController enemy = obj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
                Debug.Log($"Hit enemy: {obj.name}, Damage: {(int)damage}");
            }
        }
    }

    IEnumerator DashCoroutine()
    {
        if (isPlayingAnimation) yield break;

        PlayAnimation("P_Dash", true);
        Vector3 dashDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        float dashDuration = 0.3f;
        float elapsed = 0f;

        canMove = false;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * dashForce;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector3.zero;
        canMove = true;
        canDash = false;
        Invoke(nameof(ResetDash), dashCooldown);
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0 || isInvulnerable) return;

        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}");

        if (currentHealth > 0)
        {
            PlayAnimation("P_Hurt", true);
            StartCoroutine(HandlePlayerHurt());
        }
        else
        {
            Die();
        }
    }

    private IEnumerator HandlePlayerHurt()
    {
        isInvulnerable = true;
        canMove = false;

        float blinkDuration = 0.5f;
        float blinkInterval = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < blinkDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            elapsedTime += blinkInterval;
            yield return new WaitForSeconds(blinkInterval);
        }

        spriteRenderer.enabled = true;
        canMove = true;

        yield return new WaitForSeconds(1.0f - blinkDuration);
        isInvulnerable = false;
    }

    void Die()
    {
        Debug.Log("Player has died.");
        anim.Play("P_Die");
        canMove = false;
        rb.linearVelocity = Vector3.zero;
        GameManager.Instance?.PlayerDied();
    }

    void PlayAnimation(string animationName, bool lockDuringAnimation = false)
    {
        if (!anim) return;

        anim.Play(animationName);
        if (lockDuringAnimation)
            StartCoroutine(WaitForAnimationToEnd());
    }

    IEnumerator WaitForAnimationToEnd()
    {
        isPlayingAnimation = true;
        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        isPlayingAnimation = false;
        isAttackAnimation = false;
    }

    private void ResetDash() => canDash = true;
    private void ResetLightAttack() => canLightAttack = true;
    private void ResetHeavyAttack() => canHeavyAttack = true;
    private void ResetMove() => canMove = true;

    void UpdateArrowIndicator()
    {
        if (!arrowIndicator) return;

        if (moveInput.magnitude > arrowShowThreshold)
        {
            arrowIndicator.SetActive(true);
            float angle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            arrowIndicator.transform.rotation = Quaternion.Euler(90f, angle, 0f);
            arrowIndicator.transform.position = transform.position + new Vector3(moveInput.x, -0.7f, moveInput.y).normalized * 0.5f;
        }
        else
        {
            arrowIndicator.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ReloadItem"))
        {
            ReloadProjectile();
            Destroy(other.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firepoint.position, attackRange);
    }
}
