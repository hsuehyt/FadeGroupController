using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeGroupController : MonoBehaviour
{
    [Tooltip("Objects that will fade together.")]
    public List<GameObject> objectsToFade = new List<GameObject>();

    [Tooltip("Fade duration in seconds.")]
    public float fadeDuration = 2f;

    private bool isFading = false;

    // --- Public methods to call from Timeline Signal Receiver or other scripts ---
    public void StartFadeOut()
    {
        if (!isFading)
            StartCoroutine(FadeOutRoutine());
    }

    public void StartFadeIn()
    {
        if (!isFading)
            StartCoroutine(FadeInRoutine());
    }

    // --- Fade-Out ---
    private IEnumerator FadeOutRoutine()
    {
        isFading = true;
        float elapsed = 0f;

        // Collect all renderers from assigned objects
        List<Renderer> renderers = new List<Renderer>();
        foreach (GameObject obj in objectsToFade)
        {
            if (obj != null)
                renderers.AddRange(obj.GetComponentsInChildren<Renderer>(true));
        }

        // Store original colors
        Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
                originalColors[r] = r.material.color;
        }

        // Fade loop
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (Renderer r in renderers)
            {
                if (r && r.material.HasProperty("_Color"))
                {
                    Color c = originalColors[r];
                    c.a = alpha;
                    r.material.color = c;
                }
            }

            yield return null;
        }

        // Optionally disable after fade-out
        foreach (GameObject obj in objectsToFade)
            if (obj) obj.SetActive(false);

        isFading = false;
    }

    // --- Fade-In ---
    private IEnumerator FadeInRoutine()
    {
        isFading = true;
        float elapsed = 0f;

        List<Renderer> renderers = new List<Renderer>();
        foreach (GameObject obj in objectsToFade)
        {
            if (obj != null)
            {
                obj.SetActive(true); // ensure visible before fading in
                renderers.AddRange(obj.GetComponentsInChildren<Renderer>(true));
            }
        }

        // Record colors, start alpha = 0
        Dictionary<Renderer, Color> targetColors = new Dictionary<Renderer, Color>();
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = 0f;
                r.material.color = c;      // start transparent
                targetColors[r] = c;
            }
        }

        // Fade loop
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);

            foreach (Renderer r in renderers)
            {
                if (r && r.material.HasProperty("_Color"))
                {
                    Color c = targetColors[r];
                    c.a = alpha;
                    r.material.color = c;
                }
            }

            yield return null;
        }

        isFading = false;
    }
}
