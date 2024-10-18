using System;
using System.Collections;
using System.Collections.Generic;

namespace Logic
{
    public class Bullet
    {
        public Position2 Position;
        Position2 _destPosition;  // 대상이 사라진 경우에도 총알 진행 가능하도록 대상 위치 보관
        
        public Character Attacker;
        public Character Target;
        public AttackType AttackType;
        public int AttackPower;
        float _projectileSpeed;

        BattleSceneManager _battleSceneManager;

        public Action OnDestroyed;

        public void Init(BattleSceneManager battleInstacne)
        {
            _battleSceneManager = battleInstacne;
        }

        public void Tick()
        {
            if (Target != null)
            {
                _destPosition = Target.Position;
            }
            Position = Position2.MoveTowards(Position, _destPosition, _projectileSpeed / _battleSceneManager.BaseLogicTickrate);
            if (Position == _destPosition)
            {
                if (Target != null)
                {
                    Target.TakeDamage(AttackType, AttackPower);
                }
                _battleSceneManager.RemoveBullet(this);
            }
        }

        ~Bullet()
        {
            LogicDebug.Log("bullet 삭제 테스트");
            if(OnDestroyed != null)
            {
                OnDestroyed();
            }
        }
    }
}
