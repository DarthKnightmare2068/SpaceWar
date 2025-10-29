using UnityEngine;
using TMPro;

public class ReviveCD : MonoBehaviour
{
    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = "";
    }

    // Call this from GameManager to update the countdown
    public void SetCountdown(float seconds)
    {
        if (text != null)
            text.text = $"Revive in: {Mathf.CeilToInt(seconds)}";
    }

    public void ShowRevived()
    {
        if (text != null)
            text.text = "Revived!";
    }

    public void Clear()
    {
        if (text != null)
            text.text = "";
    }
}