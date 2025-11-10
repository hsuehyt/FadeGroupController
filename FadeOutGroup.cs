using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeOutGroup : MonoBehaviour
{
    public List<GameObject> objectsToFade;
    public float fadeDuration = 2f;

    private bool isFading = false;

    public void StartFadeOut()
    {
        if (!isFading)
            StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        isFading = true;
        float elapsed = 0f;

        // Get all renderers from the selected objects
        List<Renderer> renderers = new List<Renderer>();
        foreach (GameObject obj in objectsToFade)
        {
            if (obj != null)
                renderers.AddRange(obj.GetComponentsInChildren<Renderer>());
        }

        // Store original colors
        Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
                originalColors[r] = r.material.color;
        }

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

        // Optionally disable the objects after fade
        foreach (GameObject obj in objectsToFade)
            if (obj) obj.SetActive(false);

        isFading = false;
    }
}
