using System;
using System.Collections;
using System.Collections.Generic;

namespace Logic
{
    [Serializable]
    public class BulletLogic
    {
        public Position2 Position;
        Position2 _destPosition;  // 대상이 사라진 경우에도 총알 진행 가능하도록 대상 위치 보관

        public CharacterLogic Attacker;
        public CharacterLogic Target;
        public AttackType AttackType;
        public int AttackPower;
        public float ProjectileSpeed;

        BattleLogic _battleSceneManager;

        public Action OnExpired;

        public void Init(BattleLogic battleInstacne)
        {
            _battleSceneManager = battleInstacne;
        }

        public void Tick()
        {
            if (Target != null)
            {
                _destPosition = Target.Position;
            }
            Position = Position2.MoveTowards(Position, _destPosition, ProjectileSpeed / _battleSceneManager.BaseLogicTickrate);
            if (Position == _destPosition)
            {
                if (Target != null)
                {
                    Target.TakeDamage(AttackType, AttackPower);
                }
                Expire();
            }
        }

        void Expire()
        {
            _battleSceneManager.RemoveExpiredBullet(this);
            if(OnExpired != null)
            {
                OnExpired();
            }
        }
    }
}
