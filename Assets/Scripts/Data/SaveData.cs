using System;

[Serializable]
public class SaveData
{
    public int level = 1;
    public float currentExp = 0f;
    public float expToNextLevel = 1000f;
    public int maxHP = 0;        // 0 = use prefab default (no override)
    public int attackPoint = 0;  // 0 = use prefab default (no override)
}
