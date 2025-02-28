using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float damage = 10f;
    public SpriteRenderer spriteRenderer;

    private Vector3 moveDirection;

public void Launch(Vector3 direction)
{
    moveDirection = direction;

    if (spriteRenderer != null)
    {
        spriteRenderer.flipX = moveDirection.x > 0;
    }

    Debug.Log($"Projectile launched with direction: {moveDirection}");
}

void Update()
{
    transform.position += moveDirection * speed * Time.deltaTime;

    transform.rotation = Quaternion.identity;
}


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log("Projectile hit the player!");
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
