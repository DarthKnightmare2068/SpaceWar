using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DmgPopUpAnimation : MonoBehaviour
{
    public AnimationCurve opacityCurve;
    public AnimationCurve scaleCurve;
    public AnimationCurve heightCurve; // Y movement only
    public Color baseColor = Color.white;

    private TextMeshProUGUI tmp;
    private RectTransform rectTransform;
    private float time = 0;
    private Vector2 originAnchoredPosition;

    private void Awake()
    {
        tmp = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        originAnchoredPosition = rectTransform.anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        float scaleValue = scaleCurve.Evaluate(time);
        tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, opacityCurve.Evaluate(time));
        tmp.rectTransform.localScale = Vector3.one * scaleValue;
        rectTransform.anchoredPosition = originAnchoredPosition + new Vector2(0, heightCurve.Evaluate(time));
        time += Time.deltaTime;
    }
}
