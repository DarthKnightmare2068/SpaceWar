using UnityEngine;
using UnityEngine.UI;

public abstract class DualSliderBar : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider normalHealthBarSlider;
    public Slider easeHealthBarSlider;

    [Header("Animation Settings")]
    public float lerpSpeed = 0.05f;

    // Animate the ease bar toward normalSlider's current value.
    protected void UpdateBars(float normalizedValue)
    {
        if (normalHealthBarSlider != null)
            normalHealthBarSlider.value = normalizedValue;
        if (easeHealthBarSlider != null && normalHealthBarSlider != null &&
            easeHealthBarSlider.value != normalHealthBarSlider.value)
        {
            easeHealthBarSlider.value = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);
        }
    }

    // Instantly snap both bars to the same value (e.g. on target switch or death).
    protected void ForceSetBars(float normalizedValue)
    {
        if (normalHealthBarSlider != null) normalHealthBarSlider.value = normalizedValue;
        if (easeHealthBarSlider != null) easeHealthBarSlider.value = normalizedValue;
    }
}
