using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;
using Logic;

public class CharacterVisual : MonoBehaviour
{
    public Character CharacterLogic;
    BattleSceneManager _battleManager;
    [SerializeField] DamageNumber damageNumberPrefab;

    Vector3 _positionBeforeFrame;

    // Start is called before the first frame update
    void Start()
    {
        _positionBeforeFrame = transform.position;
        CharacterLogic.OnCharacterTakeDamage += DisplayDamageNumber;
        CharacterLogic.OnReload += DisplayReloadMessage;
        CharacterLogic.OnDie += DestroyVisual;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(CharacterLogic.Position.x, 0, CharacterLogic.Position.y);

        if (CharacterLogic.isMoving)
        {
            Vector3 positionCurrentFrame = transform.position;
            // (현재 위치 + 이동방향) 바라보기
            // 이동방향은 지난 프레임과의 변위로 계산
            transform.LookAt(2 * positionCurrentFrame - _positionBeforeFrame);
            _positionBeforeFrame = positionCurrentFrame;
        }
    }

    void DisplayDamageNumber(int damage, bool isCritical, AttackType attackType, ArmorType armorType)
    {
        if (damageNumberPrefab != null)
        {
            damageNumberPrefab.Spawn(transform.position, damage);
        }
    }

    void DisplayReloadMessage() 
    {
        damageNumberPrefab.Spawn(transform.position, "Reloaded");
    }

    void DestroyVisual()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // 사거리 원
        Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, attackRange);

        // 이동 목표 위치
        Gizmos.color = Color.green;
        // Gizmos.DrawSphere(CharacterLogic.moveDest, 0.1f);

        // 공격 대상
        if (CharacterLogic.currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(new Vector3(CharacterLogic.Position.x, 0, CharacterLogic.Position.y), 0.1f);
        }
    }
}