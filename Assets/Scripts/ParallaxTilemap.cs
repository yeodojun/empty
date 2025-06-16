using UnityEngine;

[ExecuteAlways]
public class ParallaxTilemap : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)] public float parallaxMultiplierX = 0.3f;
    [Range(0f, 1f)] public float parallaxMultiplierY = 0.3f;

    private Vector3 lastCameraPosition;
    private Transform _transform;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogWarning("[ParallaxTilemap] 카메라를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        _transform = transform;
        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || cameraTransform == null)
            return;

        Vector3 delta = cameraTransform.position - lastCameraPosition;

        _transform.position += new Vector3(
            delta.x * parallaxMultiplierX,
            delta.y * parallaxMultiplierY,
            0f
        );

        lastCameraPosition = cameraTransform.position;
    }
}
