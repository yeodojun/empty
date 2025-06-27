using UnityEngine;

public class ParallaxTrigger : MonoBehaviour
{
    [SerializeField] private ParallaxTilemap[] targetTilemaps;
    [SerializeField] private bool enableParallax = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var tilemap in targetTilemaps)
        {
            tilemap?.EnableParallax(enableParallax);
        }
    }
}
