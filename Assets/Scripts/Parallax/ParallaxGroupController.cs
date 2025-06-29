using UnityEngine;

public class ParallaxGroupController : MonoBehaviour
{
    [SerializeField] private ParallaxTilemap[] childTilemaps;

    public void EnableParallax(bool enable)
    {
        foreach (var tilemap in childTilemaps)
        {
            if (tilemap != null)
                tilemap.EnableParallax(enable);
        }
    }
}
