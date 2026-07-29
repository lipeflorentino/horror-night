using DG.Tweening;
using TMPro;
using UnityEngine;

public class ResourceCostPopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private float riseDistance = 60f;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float startScale = 0.7f;
    [SerializeField] private float bounceScale = 1.2f;

    public void Show(string text, Color color)
    {
        popupText.text = text;
        popupText.color = color;
        transform.localScale = Vector3.one * startScale;

        Vector3 endPos = transform.position + Vector3.up * riseDistance;
        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(bounceScale, duration * 0.4f).SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo));
        seq.Join(popupText.DOFade(0f, duration).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(gameObject));
    }
}