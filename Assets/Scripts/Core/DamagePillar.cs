using UnityEngine;
using System.Collections;

public class DamagePillar : MonoBehaviour
{
    [Header("Pillar Settings")]
    public int maxStages = 3;                    
    private int currentStage = 1;                

    public int CurrentStage => currentStage;     

    [Header("Health Settings")]
    public float stageHealth = 25f;              
    private float currentHealth;

    [Header("Damage Settings")]
    public float damageRadius = 5f;              
    public float damageAmount = 20f;            
    public LayerMask enemyLayer;          

    [Header("Visual & Effects")]
    public SpriteRenderer spriteRenderer;           
    public Sprite[] stageSprites;                    
    public ParticleSystem hitParticle;            

    [Header("Shake Effect")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    private bool canBeHit = true;

    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        currentHealth = stageHealth;
        UpdateSprite();
    }

    public void TakeDamage(float damage)
    {
        if (!canBeHit) return;

        currentHealth -= damage;

        if (hitParticle != null)
        {
            hitParticle.Play();
        }

        StartCoroutine(Shake());

        if (currentStage < maxStages)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                //player.TriggerVibration(0.2f, 0.6f);
            }
        }

        if (currentHealth <= 0)
        {
            AdvanceToNextStage();
        }
    }

    private void AdvanceToNextStage()
    {
        if (currentStage < maxStages)
        {
            currentStage++;
            currentHealth = stageHealth;
            UpdateSprite();
            Debug.Log($"Pillar advanced to stage {currentStage}.");
        }
        else
        {
            canBeHit = false;
            Debug.Log("Pillar cannot be hit anymore.");
        }

        DealDamageToEnemies();
    }

    private void UpdateSprite()
    {
        if (stageSprites != null && stageSprites.Length >= currentStage)
        {
            spriteRenderer.sprite = stageSprites[currentStage - 1];
        }
    }

    private void DealDamageToEnemies()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, damageRadius, enemyLayer);

        /*foreach (Collider enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            EnemyRange enemyRangeScript = enemy.GetComponent<EnemyRange>();

            if (enemyScript != null)
            {
                enemyScript.TakeDamage(damageAmount);
                Debug.Log("Damaged enemy: " + enemy.name);
            }
             else if (enemyRangeScript != null)
            {
                enemyRangeScript.TakeDamage(damageAmount);
                Debug.Log("Damaged enemy: " + enemy.name);
            }
        }*/
    }

    private IEnumerator Shake()
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = new Vector3(originalPosition.x + offsetX, originalPosition.y + offsetY, originalPosition.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
