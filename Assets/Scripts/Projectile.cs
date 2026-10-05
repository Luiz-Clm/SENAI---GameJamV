using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] int damage = 1;
    [SerializeField] float lifetime = 3f;

    Vector2 direction = Vector2.right;
    string targetTag;

    // Quem atira configura a bala, então o prefab é o mesmo para player e inimigo
    public void Init(Vector2 dir, string target)
    {
        direction = dir.normalized;
        targetTag = target;
    }

    void Start() => Destroy(gameObject, lifetime);

    void Update() => transform.Translate(direction * speed * Time.deltaTime);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}