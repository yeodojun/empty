using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private Animator animator;
    private bool isDying = false;
    private bool isHitProcessing = false;

    public int health = 3;
    public float hitDelay = 0.5f;
    [SerializeField] private Collider2D attackTrigger;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void EnableAttackTrigger() => attackTrigger.enabled = true;
    public void DisableAttackTrigger() => attackTrigger.enabled = false;

    public void TakeDamage(int amount, Player sourceplayer)
    {
        if (isDying || isHitProcessing) return; // 중복 방지

        health -= amount;
        sourceplayer.AttackHit();

        if (health <= 0)
        {
            StartCoroutine(HandleDeath());
        }
        else
        {
            StartCoroutine(HandleHit());
        }
    }

    private IEnumerator HandleHit()
    {
        isHitProcessing = true;
        animator.SetTrigger("Hit");
        yield return new WaitForSeconds(hitDelay);
        isHitProcessing = false;
    }

    private IEnumerator HandleDeath()
    {
        isDying = true;
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(0.8f); // 죽음 애니메이션 시간
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!attackTrigger.enabled) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        player.OnIncomingAttack(transform.position);

        if (!player.IsInvincible)
        {
            player.ApplyKnockback(transform.position, 5f);
            player.TakeDamage(1);
        }

        attackTrigger.enabled = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.collider.GetComponent<Player>();
        if (player == null) return;

        player.OnIncomingAttack(transform.position);

        if (!player.IsInvincible)
        {
            player.ApplyKnockback(transform.position, 5f);
            player.TakeDamage(1);
        }
    }
}
