using UnityEngine;

[ExecuteAlways]
public class ParallaxOptimized : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layer;                     // 움직일 배경 레이어
        [Range(0f,1f)] public float parallaxFactor; // 0: 고정, 1: 카메라와 동일
        [HideInInspector] public Vector3 startPos;  // 시작 위치 (캐싱)
    }

    [Tooltip("위치와 ParallaxFactor만 설정하세요.")]
    public ParallaxLayer[] layers;

    private Transform cam;

    void Awake()
    {
        cam = Camera.main.transform; // 한 번만 캐싱
    }

    void Start()
    {
        // 레이어별 원래 위치 저장
        foreach (var pl in layers)
            if (pl.layer != null)
                pl.startPos = pl.layer.position;
    }

    void LateUpdate()
    {
        Vector3 camPos = cam.position;

        // 레이어마다 직접 계산해서 위치 대입
        for (int i = 0; i < layers.Length; i++)
        {
            var pl = layers[i];
            if (pl.layer == null) continue;

            pl.layer.position = new Vector3(
                pl.startPos.x + camPos.x * pl.parallaxFactor,
                pl.startPos.y + camPos.y * pl.parallaxFactor,
                pl.startPos.z
            );
        }
    }
}
