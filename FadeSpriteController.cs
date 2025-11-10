using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeSpriteController : MonoBehaviour
{
    [Tooltip("Assign your SpriteRenderer here. You can add more later.")]
    public List<SpriteRenderer> sprites = new List<SpriteRenderer>();

    [Tooltip("Seconds for the fade.")]
    public float duration = 1.5f;

    [Tooltip("If true, forces all sprites to alpha=0 at start.")]
    public bool startInvisible = true;

    [Tooltip("If true, disables the GameObject when fully faded out.")]
    public bool disableOnFadeOut = false;

    bool isFading;

    void Awake()
    {
        if (startInvisible)
        {
            SetAlphaAll(0f);
            // Ensure objects are enabled so Timeline can fade them in
            foreach (var s in sprites) if (s) s.gameObject.SetActive(true);
        }
    }

    // --- Public methods (hook these from Timeline Signal Receiver) ---
    public void StartFadeIn()
    {
        if (!isFading) StartCoroutine(FadeRoutine(toAlpha: 1f));
    }

    public void StartFadeOut()
    {
        if (!isFading) StartCoroutine(FadeRoutine(toAlpha: 0f));
    }

    // --- Core fade ---
    IEnumerator FadeRoutine(float toAlpha)
    {
        isFading = true;

        // Cache starting alphas
        var startAlphas = new float[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i])
            {
                startAlphas[i] = sprites[i].color.a;
                // Make sure sprite is active when fading in
                if (toAlpha > startAlphas[i]) sprites[i].gameObject.SetActive(true);
            }
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];
                if (!s) continue;
                float a = Mathf.Lerp(startAlphas[i], toAlpha, k);
                var c = s.color; c.a = a; s.color = c;
            }
            yield return null;
        }

        // Snap to final alpha & optionally disable on fade-out
        for (int i = 0; i < sprites.Count; i++)
        {
            var s = sprites[i];
            if (!s) continue;
            var c = s.color; c.a = toAlpha; s.color = c;
            if (disableOnFadeOut && Mathf.Approximately(toAlpha, 0f))
                s.gameObject.SetActive(false);
        }

        isFading = false;
    }

    void SetAlphaAll(float a)
    {
        foreach (var s in sprites)
        {
            if (!s) continue;
            var c = s.color; c.a = Mathf.Clamp01(a); s.color = c;
        }
    }
}
