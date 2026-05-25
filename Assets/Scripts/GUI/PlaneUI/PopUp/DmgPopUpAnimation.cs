using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DmgPopUpAnimation : MonoBehaviour
{
    public AnimationCurve opacityCurve;
    public AnimationCurve scaleCurve;
    public AnimationCurve heightCurve;
    public Color baseColor = Color.white;

    private TextMeshProUGUI tmp;
    private RectTransform rectTransform;
    private float time = 0;
    private Vector2 originAnchoredPosition;
    private bool registered;

    private static readonly List<DmgPopUpAnimation> active = new List<DmgPopUpAnimation>();
    private static DmgPopUpAnimatorRunner runner;

    private void Awake()
    {
        tmp = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        originAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (!registered)
        {
            EnsureRunner();
            active.Add(this);
            registered = true;
        }
    }

    private void OnDisable()
    {
        if (registered)
        {
            active.Remove(this);
            registered = false;
        }
    }

    public void ResetAnimation()
    {
        time = 0f;
        if (rectTransform != null)
            originAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void Setup(string text, Color color)
    {
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }
        baseColor = color;
        ResetAnimation();
    }

    public void Setup(int damage, Color color)
    {
        if (tmp != null)
        {
            // Bolt: Optimized - use SetText to avoid string allocation
            tmp.SetText("{0}", damage);
            tmp.color = color;
        }
        baseColor = color;
        ResetAnimation();
    }

    // Per-popup advance, driven by the single coordinator below — replaces N MonoBehaviour Updates
    // with one batched loop.
    internal void Tick(float dt)
    {
        if (tmp == null || rectTransform == null) return;
        float scaleValue = scaleCurve.Evaluate(time);
        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, opacityCurve.Evaluate(time));
        tmp.rectTransform.localScale = Vector3.one * scaleValue;
        rectTransform.anchoredPosition = originAnchoredPosition + new Vector2(0, heightCurve.Evaluate(time));
        time += dt;
    }

    private static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("DmgPopUpAnimatorRunner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<DmgPopUpAnimatorRunner>();
    }

    private class DmgPopUpAnimatorRunner : MonoBehaviour
    {
        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < active.Count; i++)
            {
                var anim = active[i];
                if (anim != null && anim.isActiveAndEnabled) anim.Tick(dt);
            }
        }
    }
}
