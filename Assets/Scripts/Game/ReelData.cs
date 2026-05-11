using System;

[Serializable]
public class ReelStats
{
    public int maxHP = 5;
    public int currentHP = 5;
    public int atk = 3;
    public int def = 1;

    public ReelStats Clone() => new ReelStats
    {
        maxHP = maxHP,
        currentHP = currentHP,
        atk = atk,
        def = def
    };
}
