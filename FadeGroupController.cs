using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeGroupController : MonoBehaviour
{
    [Tooltip("Objects that will fade together.")]
    public List<GameObject> objectsToFade = new List<GameObject>();

    [Tooltip("Fade duration in seconds.")]
    public float fadeDuration = 2f;

    [Tooltip("If true, all renderers start at alpha=0 on Play (no initial flash).")]
    public bool startTransparentOnAwake = true;

    [Tooltip("If true, disable objects after fading out.")]
    public bool disableOnFadeOut = false;

    private bool isFading = false;

    void Awake()
    {
        if (startTransparentOnAwake)
        {
            // Ensure everything is active so Timeline can drive them,
            // but make them invisible (alpha=0) before the first frame.
            var renderers = CollectRenderers();
            SetAlpha(renderers, 0f, forceTransparent: true);
        }
    }

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
        var renderers = CollectRenderers();

        // Cache start colors
        var startColors = CacheColors(renderers);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            ApplyAlpha(renderers, startColors, a);
            yield return null;
        }

        // Snap to end & optionally disable
        ApplyAlpha(renderers, startColors, 0f);
        if (disableOnFadeOut)
            foreach (var r in renderers) if (r) r.gameObject.SetActive(false);

        isFading = false;
    }

    // --- Fade-In ---
    private IEnumerator FadeInRoutine()
    {
        isFading = true;

        // Ensure objects are active and start at alpha=0 (in case Awake toggle was off)
        var renderers = CollectRenderers(ensureActive: true);
        SetAlpha(renderers, 0f, forceTransparent: true);

        var startColors = CacheColors(renderers); // cache post-zeroed colors (hues retained)

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            ApplyAlpha(renderers, startColors, a);
            yield return null;
        }

        // Snap to fully opaque at the end
        ApplyAlpha(renderers, startColors, 1f);

        isFading = false;
    }

    // --- Helpers ---
    private List<Renderer> CollectRenderers(bool ensureActive = false)
    {
        var renderers = new List<Renderer>();
        foreach (var obj in objectsToFade)
        {
            if (!obj) continue;
            if (ensureActive && !obj.activeSelf) obj.SetActive(true);
            renderers.AddRange(obj.GetComponentsInChildren<Renderer>(true));
        }
        return renderers;
    }

    private Dictionary<Renderer, Color> CacheColors(List<Renderer> renderers)
    {
        var dict = new Dictionary<Renderer, Color>();
        foreach (var r in renderers)
            if (r && r.material.HasProperty("_Color"))
                dict[r] = r.material.color;
        return dict;
    }

    private void ApplyAlpha(List<Renderer> renderers, Dictionary<Renderer, Color> baseColors, float alpha)
    {
        foreach (var r in renderers)
        {
            if (!r || !r.material.HasProperty("_Color")) continue;
            var c = baseColors[r];
            c.a = Mathf.Clamp01(alpha);
            r.material.color = c;
        }
    }

    private void SetAlpha(List<Renderer> renderers, float alpha, bool forceTransparent)
    {
        foreach (var r in renderers)
        {
            if (!r) continue;
            // Make sure material supports alpha blending
            if (forceTransparent) TryForceTransparent(r.material);
            if (r.material.HasProperty("_Color"))
            {
                var c = r.material.color;
                c.a = Mathf.Clamp01(alpha);
                r.material.color = c;
            }
        }
    }

    // Minimal "make it transparent-capable" for common shaders
    private void TryForceTransparent(Material m)
    {
        if (!m) return;

        // Unity Standard shader
        if (m.shader && m.shader.name.Contains("Standard"))
        {
            m.SetFloat("_Mode", 3);                       // Transparent
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
            // 0=Opaque, 1=Transparent
            m.SetFloat("_Surface", 1f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
