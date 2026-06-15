using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the BloodyTalons VFX prefab root.
/// Pre-position the 3 slash sprite children in the prefab editor in a fan layout.
/// The script handles scale-up and fade.
/// </summary>
public class BloodyTalonsEffect : MonoBehaviour
{
    [Header("Slash Renderers")]
    public SpriteRenderer slashLeft;
    public SpriteRenderer slashCenter;
    public SpriteRenderer slashRight;

    [Header("Timing")]
    public float expandTime = 0.1f;
    public float holdTime   = 0.12f;
    public float fadeTime   = 0.25f;

    void Start()
    {
        SpriteRenderer[] slashes = { slashLeft, slashCenter, slashRight };

        foreach (SpriteRenderer sr in slashes)
        {
            if (sr == null) continue;
            sr.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateSlash(sr));
        }

        Destroy(gameObject, expandTime + holdTime + fadeTime + 0.1f);
    }

    IEnumerator AnimateSlash(SpriteRenderer sr)
    {
        Vector3 fullScale = sr.transform.localScale == Vector3.zero
            ? Vector3.one
            : sr.transform.localScale;
        fullScale = Vector3.one;

        // Expand
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            sr.transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, t / expandTime);
            yield return null;
        }
        sr.transform.localScale = fullScale;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade alpha
        Color c = sr.color;
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeTime);
            sr.color = c;
            yield return null;
        }

        sr.gameObject.SetActive(false);
    }
}
