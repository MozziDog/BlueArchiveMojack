using System.Collections;
using System.Collections.Generic;

public class AttackEffectiveness
{
    public static readonly float Resist = 0.50f;
    public static readonly float Normal = 1.00f;
    public static readonly float Effective = 1.50f;
    public static readonly float Weak = 2.00f;

    static readonly Dictionary<(AttackType, ArmorType), float> effectivenessTable
    = new Dictionary<(AttackType, ArmorType), float>()
    {
        {(AttackType.Normal, ArmorType.Normal), Normal },
        {(AttackType.Normal, ArmorType.Light), Normal},
        {(AttackType.Normal, ArmorType.Heavy), Normal},
        {(AttackType.Normal, ArmorType.Special), Normal},
        {(AttackType.Normal, ArmorType.Elastic), Normal},

        {(AttackType.Explosive, ArmorType.Normal), Normal },
        {(AttackType.Explosive, ArmorType.Light), Weak},
        {(AttackType.Explosive, ArmorType.Heavy), Normal},
        {(AttackType.Explosive, ArmorType.Special), Resist},
        {(AttackType.Explosive, ArmorType.Elastic), Resist},

        {(AttackType.Piercing, ArmorType.Normal), Normal },
        {(AttackType.Piercing, ArmorType.Light), Resist},
        {(AttackType.Piercing, ArmorType.Heavy), Weak},
        {(AttackType.Piercing, ArmorType.Special), Normal},
        {(AttackType.Piercing, ArmorType.Elastic), Normal},

        {(AttackType.Mystic, ArmorType.Normal), Normal },
        {(AttackType.Mystic, ArmorType.Light), Normal},
        {(AttackType.Mystic, ArmorType.Heavy), Resist},
        {(AttackType.Mystic, ArmorType.Special), Weak},
        {(AttackType.Mystic, ArmorType.Elastic), Normal},

        {(AttackType.Sonic, ArmorType.Normal), Normal },
        {(AttackType.Sonic, ArmorType.Light), Normal},
        {(AttackType.Sonic, ArmorType.Heavy), Resist},
        {(AttackType.Sonic, ArmorType.Special), Effective},
        {(AttackType.Sonic, ArmorType.Elastic), Weak}
    };

    public static float GetEffectiveness(AttackType attackType, ArmorType armorType)
    {
        return effectivenessTable[(attackType, armorType)];
    }
}