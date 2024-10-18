using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class CharacterStatData : ScriptableObject       // 임시로 ScriptableObject로 구현. 추후 엑셀 읽어오는 것으로 바꾸기.
{
    public int Id;

    // 레벨에 따른 성장 수치. 성장 곡선은 선형
    public int HPLevel1;                          // 체력
    public int HPLevel100;
    public int AttackPowerLevel1;                 // 공격력
    public int AttackPowerLevel100;
    public int DefensePowerLevel1;                // 방어력
    public int DefensePowerLevel100;
    public int HealPowerLevel1;                   // 치유력
    public int HealPowerLevel100;

    // 레벨 이외의 성장 수치
    public int DodgePower;                      // 회피율
    public int AccuracyPower;                   // 명중률
    public int CriticalPower;                   // 치명타율
    public int CriticalResistPower;             // 치명확률저항
    public int CriticalDamageRate;              // 치명피해
    public int CriticalDamageResistRate;        // 치명피해저항
    public int BlockRate;                       // 기본 엄폐 성공률
    public int HealEffectiveness;               // 받는 회복 효과 강화율
    public int EnhanceExplosiveRate;            // 폭발 특효
    public int EnhancePiercingRate;             // 관통 특효
    public int EnhanceMysticRate;               // 신비 특효
    public int EnhanceSonicRate;                // 진동 특효
    public int BuffExtendRate;                  // 이로운 효과 지속시간
    public int CrowdControlExtendRate;          // 군중제어 강화력
    public int CrowdControlResistRate;          // 군중제어 저항력

    // 비성장 수치
    public int AmmoCount;                       // 장탄수
    public int NormalAttackSpeed;               // 평타 속도
    public int NormalAttackRange;               // 평타 사거리
    public int InitialRangeRate;                // 위치 선정 시의 평타 거리 감소율
    public float MoveSpeed;                       // 기본 이동 속도
    public float ObstacleJumpSpeed; 
    public int SightRange;                      // 다음 웨이브의 적 인식 거리
    public int CostRegen;                       // 코스트 회복량
    public int Stability;                       // 안정 수치

    // 지형 적성
    public TerrainAdaptation StreetAdaptation;  // 시가전 적성
    public TerrainAdaptation OutdoorAdaptation; // 야전 적성
    public TerrainAdaptation IndoorAdaptation;  // 실내전 적성

    // 기타
    public long DefensePenetration;             // 방어 관통
}
