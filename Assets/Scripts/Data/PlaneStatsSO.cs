using UnityEngine;

[CreateAssetMenu(fileName = "PlaneStats", menuName = "SpaceWar/Plane Stats")]
public class PlaneStatsSO : ScriptableObject
{
    [Header("Health")]
    public int maxHP = 100;

    [Header("Regeneration")]
    public float regenerationDelay = 3f;
    public float regenerationRate = 0.2f;

    [Header("Attack")]
    public int attackPoint = 10;
}
