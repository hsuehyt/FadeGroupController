using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeGroupController : MonoBehaviour
{
    [Tooltip("Objects that will fade together.")]
    public List<GameObject> objectsToFade = new List<GameObject>();

    [Tooltip("Fade duration in seconds.")]
    public float fadeDuration = 2f;

    [Tooltip("Start fully transparent on Play so there is no initial flash.")]
    public bool startTransparentOnAwake = true;

    [Tooltip("Disable objects after fading out.")]
    public bool disableOnFadeOut = false;

    bool isFading = false;

    void Awake()
    {
        if (startTransparentOnAwake)
        {
            var renderers = CollectRenderers(ensureActive: true);
            ForceTransparentForCommonShaders(renderers);
            SetAlpha(renderers, 0f);
        }
    }

    // Call from Timeline Signal Receiver
    public void StartFadeIn()
    {
        if (!isFading) StartCoroutine(FadeRoutine(0f, 1f));
    }

    public void StartFadeOut()
    {
        if (!isFading) StartCoroutine(FadeRoutine(1f, 0f));
    }

    IEnumerator FadeRoutine(float from, float to)
    {
        isFading = true;

        var renderers = CollectRenderers(ensureActive: true);

        // cache base colors per renderer
        var baseColors = new Dictionary<Renderer, Color>(renderers.Count);
        foreach (var r in renderers)
            if (r && r.material.HasProperty("_Color"))
                baseColors[r] = r.material.color;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            foreach (var r in renderers)
            {
                if (!r || !r.material.HasProperty("_Color")) continue;
                var c = baseColors[r]; c.a = a; r.material.color = c;
            }
            yield return null;
        }

        // snap to end
        foreach (var r in renderers)
        {
            if (!r || !r.material.HasProperty("_Color")) continue;
            var c = baseColors[r]; c.a = to; r.material.color = c;
            if (Mathf.Approximately(to, 0f) && disableOnFadeOut) r.gameObject.SetActive(false);
        }

        isFading = false;
    }

    List<Renderer> CollectRenderers(bool ensureActive = false)
    {
        var list = new List<Renderer>();
        foreach (var go in objectsToFade)
        {
            if (!go) continue;
            if (ensureActive && !go.activeSelf) go.SetActive(true);
            list.AddRange(go.GetComponentsInChildren<Renderer>(true));
        }
        return list;
    }

    void SetAlpha(List<Renderer> renderers, float a)
    {
        foreach (var r in renderers)
        {
            if (!r || !r.material.HasProperty("_Color")) continue;
            var c = r.material.color; c.a = Mathf.Clamp01(a); r.material.color = c;
        }
    }

    void ForceTransparentForCommonShaders(List<Renderer> renderers)
    {
        foreach (var r in renderers)
        {
            if (!r) continue;
            var m = r.material;
            if (!m) continue;

            // Standard
            if (m.shader && m.shader.name.Contains("Standard"))
            {
                m.SetFloat("_Mode", 3); // Transparent
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            // URP Lit
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f); // Transparent
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }
    }
}
