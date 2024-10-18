using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;
using Logic;

public class CharacterVisual : MonoBehaviour
{
    public Character CharacterLogic;
    [SerializeField] DamageNumber damageNumberPrefab;

    Vector3 _positionBeforeFrame;

    // Start is called before the first frame update
    void Start()
    {
        if(CharacterLogic == null)
        {
            CharacterLogic = GetComponent<Character>();
        }
        _positionBeforeFrame = transform.position;
        CharacterLogic.OnCharacterTakeDamage += DisplayDamageNumber;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(CharacterLogic.Position.x, 0, CharacterLogic.Position.y);

        if(CharacterLogic.isMoving)
        {
            Vector3 positionCurrentFrame = transform.position;
            // (현재 위치 + 이동방향) 바라보기
            // 이동방향은 지난 프레임과의 변위로 계산
            transform.LookAt(2 * positionCurrentFrame - _positionBeforeFrame);
            _positionBeforeFrame= positionCurrentFrame;
        }
    }

    void DisplayDamageNumber(int damage, bool isCritical, AttackType attackType, ArmorType armorType)
    {
        if(damageNumberPrefab != null)
        {
            damageNumberPrefab.Spawn(transform.position, damage);
        }
    }
}
