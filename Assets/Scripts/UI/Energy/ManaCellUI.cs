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
}
