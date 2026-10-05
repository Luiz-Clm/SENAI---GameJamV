using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Player : Character
{
    [SerializeField] float speed = 5f;

    Rigidbody2D rb;
    Vector2 input;
    Vector2 halfSize;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        halfSize = GetComponent<SpriteRenderer>().bounds.extents;
    }

    void Update()
    {
        if (IsDead) return;

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        anim.SetBool("isMoving", input != Vector2.zero);

        if (Input.GetButton("Fire1")) TryShoot(Vector2.right);
    }

    void FixedUpdate()
    {
        if (IsDead) return;
        rb.MovePosition(ClampToCamera(rb.position + input * speed * Time.fixedDeltaTime));
    }

    Vector2 ClampToCamera(Vector2 pos)
    {
        Camera cam = Camera.main;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector2 c = cam.transform.position;

        pos.x = Mathf.Clamp(pos.x, c.x - w + halfSize.x, c.x + w - halfSize.x);
        pos.y = Mathf.Clamp(pos.y, c.y - h + halfSize.y, c.y + h - halfSize.y);
        return pos;
    }

    protected override void OnDeath()
    {
        // Sem isso o Animator ficaria preso em "andando" porque o Update para de rodar
        anim.SetBool("isMoving", false);
    }
}