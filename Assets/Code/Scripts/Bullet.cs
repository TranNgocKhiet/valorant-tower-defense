using System.ComponentModel.Design.Serialization;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Attributes")]
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int bulletDamage = 1;

    public Transform target;

    public void SetTarget(Transform _target)
    {
        target = _target;
    }

    private void FixedUpdate()
    {
        if (!target)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * bulletSpeed;
    }

    // OnCollisionEnter2D: auto called this event when the bullet collides with another object
    // other: the collision information include collide position, collide force, and the game object that it collides with
    private void OnCollisionEnter2D(Collision2D other)
    {
        other.gameObject.GetComponent<Health>()?.TakeDamage(bulletDamage);
        Destroy(gameObject);
    }
}
