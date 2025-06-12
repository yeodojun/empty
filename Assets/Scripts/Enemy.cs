using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private Animator animator;
    private bool isDying = false;
    private bool isHitProcessing = false;

    public int health = 3;
    public float hitDelay = 0.5f;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            if (player.IsInvincible || player.WasParryBlocked()) return; // ★ 패리 성공 시 무시

            player.ApplyKnockback(transform.position, 5f);
            player.TakeDamage(1);
        }
    }



}
