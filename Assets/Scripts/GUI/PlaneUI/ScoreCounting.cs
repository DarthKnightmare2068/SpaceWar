using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScoreCounting : MonoBehaviour
{
    public static ScoreCounting Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("Total damage dealt to enemies")]
    [SerializeField] private float totalDamageDealt = 0f;
    [Tooltip("Event triggered when damage is dealt")]
    public UnityEvent<float> onDamageDealt;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        totalDamageDealt = 0f;
    }

    /// <summary>Record damage dealt to enemies and update score.</summary>
    public void RecordDamageDealt(float damage)
    {
        if (damage <= 0)
            return;
        totalDamageDealt += damage;
        Debug.Log($"[ScoreCounting] Dealt {damage} damage. Total damage: {totalDamageDealt}");
        onDamageDealt?.Invoke(totalDamageDealt);
    }

    public float TotalDamageDealt => totalDamageDealt;
}
