using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class CharacterData : ScriptableObject       // 임시로 ScriptableObject로 구현. 추후 엑셀 읽어오는 것으로 바꾸기.
{
    public int Id;
    public Rarity Rarity;

    public string Name;
    public School School;
    public Club Club;

    public CombatClass CombatClass;     // 스트라이커 or 스페셜 
    public TacticRole TacticRole;       // 딜러, 탱커, 힐러, 서포터 ...
    public WeaponType WeaponType;
    public TacticPosition TacticPosition;   // front, middle or back
    public AttackType AttackType;
    public ArmorType ArmorType;

    public List<SkillData> skills;
    public List<EquipmentCategory> EquipmentSlot;
}
