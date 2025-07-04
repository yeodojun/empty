using UnityEngine;
using UnityEngine.UI;

public class ManaCellUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite filledSprite;

    public void SetFilled()
    {
        image.sprite = filledSprite;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Glow()
    {
        // 예: Animator 트리거나 이미지 색상 조절
        GetComponent<Animator>().SetTrigger("Glow");
    }

    public void ResetState()
    {
        // Animator를 초기 상태로 리셋하거나, 이미지 스프라이트 재설정
        GetComponent<Animator>().SetTrigger("Reset");
    }
}
