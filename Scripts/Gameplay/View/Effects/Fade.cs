using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Fade : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeSpeed = 2f;

    private void SetAlpha(float alpha)
    {
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    public void InstantBlack()
    {
        SetAlpha(1f);
    }

    public IEnumerator FadeIn()
    {
        float t = 1f;

        while (t > 0f)
        {
            t -= Time.deltaTime * fadeSpeed;
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(0f);
    }

    public IEnumerator BlackThenFadeIn(float holdDuration)
    {
        InstantBlack();

        yield return new WaitForSeconds(holdDuration);

        yield return StartCoroutine(FadeIn());
    }
}