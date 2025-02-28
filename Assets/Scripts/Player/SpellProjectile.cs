using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 20f;
    public int damage = 50;
    public float lifetime = 5f;
    public SpriteRenderer spriteRenderer;

    private Vector3 moveDirection; 
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector3 direction)
    {
        moveDirection = direction.normalized;

        spriteRenderer.flipX = moveDirection.x > 0;

        Debug.Log($"Projectile launched with direction: {moveDirection}");
    }

    void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
        
        transform.rotation = Quaternion.identity;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ถ้าโดนศัตรู
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Projectile hit the enemy!");
            }

            Destroy(gameObject);
        }
        
        else if (other.CompareTag("Wall"))
        {
            Debug.Log("Projectile hit the wall.");
            Destroy(gameObject);
        }
    }
}
