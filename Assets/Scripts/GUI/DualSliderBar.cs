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
            float nextValue = Mathf.Lerp(easeHealthBarSlider.value, normalHealthBarSlider.value, lerpSpeed);

            // Bolt: Optimized - epsilon snapping to stop near-zero lerps and reduce per-frame UI writes
            if (Mathf.Abs(nextValue - normalHealthBarSlider.value) < 0.001f)
                nextValue = normalHealthBarSlider.value;

            easeHealthBarSlider.value = nextValue;
        }
    }

    // Instantly snap both bars to the same value (e.g. on target switch or death).
    protected void ForceSetBars(float normalizedValue)
    {
        if (normalHealthBarSlider != null) normalHealthBarSlider.value = normalizedValue;
        if (easeHealthBarSlider != null) easeHealthBarSlider.value = normalizedValue;
    }
}
