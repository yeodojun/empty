using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    public void AttemptAttack()
    {
        Vector2 attackCenter = attackTrigger.bounds.center;
        Vector2 attackSize = attackTrigger.bounds.size;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackSize, 0f, LayerMask.GetMask("Player"));
        foreach (var hit in hits)
        {
            Player player = hit.GetComponentInParent<Player>();
            if (player == null) continue;

            player.OnIncomingAttack(transform.position);

            if (!player.WasParryBlocked() && !player.IsInvincible)
            {
                player.ApplyKnockback(transform.position, 5f);
                player.TakeDamage(1);
            }
        }
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        Player player = other.collider.GetComponent<Player>();
        if (player != null)
        {
            player.ApplyKnockback(transform.position, 5f);
            player.TakeDamage(1);
        }
    }
}
