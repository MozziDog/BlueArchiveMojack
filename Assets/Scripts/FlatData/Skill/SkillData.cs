using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
[Serializable]
public class SkillData : ScriptableObject
{
    public SkillType SkillType;
    public int Cost;                                    // EX 스킬 코스트
    public AutoSkillCondition NormalSkillCondition;     // 일반 스킬 사용 조건
    public SkillTargetAndArea SkillRange;               // 스킬 대상 및 범위
    public float Range;                                 // 사거리
    public int StartupFrame;                            // 선딜레이
    public int RecoveryFrame;                           // 후딜레이
}