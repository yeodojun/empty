using UnityEngine;

public class ParallaxTrigger : MonoBehaviour
{
    [SerializeField] private ParallaxGroupController[] targetGroups;
    [SerializeField] private bool enableParallax = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var group in targetGroups)
        {
            group?.EnableParallax(enableParallax);
        }
    }
}
