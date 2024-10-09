using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleData
{
    /// <summary>
    /// 아군 캐릭터 정보
    /// </summary>
    public List<CharacterData> characters;
    public List<CharacterStatData> characterStats;

    /// <summary>
    /// 적군 캐릭터 정보
    /// </summary>
    public List<CharacterData> enemies;
    public List<CharacterStatData> enemyStats;
}
