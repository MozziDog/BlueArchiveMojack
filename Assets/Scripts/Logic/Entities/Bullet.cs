using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Character Attacker;
    public Character Target;
    public AttackType AttackType;
    public int AttackPower;
    [SerializeField] float _projectileSpeed;
    [SerializeField] Vector3 _destPosition;  // 대상이 사라진 경우에도 총알 진행 가능하도록 대상 위치 보관
    [SerializeField] int _disappearTimeTick;

    BattleSceneManager _battleSceneManager;

    public void Init(BattleSceneManager battleInstacne)
    {
        _battleSceneManager = battleInstacne;
    }

    public void Tick()
    {
        if(Target != null)
        {
            _destPosition = Target.transform.position;
        }
        transform.position = Vector3.MoveTowards(transform.position, _destPosition, _projectileSpeed / _battleSceneManager.BaseLogicTickrate);
        if(transform.position == _destPosition)
        {
            if(Target != null)
            {
                Target.TakeDamage(AttackType, AttackPower);
            }
            _battleSceneManager.RemoveBullet(this);
            gameObject.SetActive(false);
        }
    }
}
