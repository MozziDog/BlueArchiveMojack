using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using AI;
using Sirenix.OdinInspector;

namespace Logic
{
    [Serializable]
    public class CharacterLogic
    {
        // 캐릭터 기본 정보
        public string Name;
        public bool isAlive = true;
        public bool isAiActive = true;  // 적군 허수아비 AI 꺼두는 용도
        public AttackType AttackType;
        public ArmorType ArmorType;
        public PathFinder pathFinder;

        // 기본 스탯 정보
        int _maxHP;
        int _currentHP;
        int _attackPower;
        int _defensePower;
        int _healPower;
        int _costRegen;

        // 엄폐 관련
        ObstacleLogic _coveringObstacle;       // 현재 엄폐를 수행중인 엄폐물
        private ObstacleLogic occupyinngObstacle;           // 현재 점유 중인 장애물

        // 이동 & 위치 선정 관련
        float _moveSpeed;
        Position2 _position;
        Position2 _moveDest;
        ObstacleLogic _destObstacle;
        float _attackRange;
        float _distToEnemy = 10f;
        bool _isObstacleJumping;

        // 전투 관련
        CharacterLogic _currentTarget;
        int _maxAmmo = 15;
        int _curAmmo;
        int _exSkillCost;
        bool _exSkillTrigger;
        IAutoSkillCheck _normalSkillCondition;
        SkillData exSkill;
        SkillData normalSkill;

        // 프레임 카운트
        int _curActionFrame = 0;
        int _exSkillFrame = 0;

        // 그 외 참조
        BattleLogic _battleLogic;
        BehaviorTree _bt;

        // 상수값
        static readonly float SightRange = 13f;
        static readonly float PositioningAttackRangeRatio = 0.88f;      // 이동 위치 선정할 때 최대 사거리 대신 사거리에 이 값을 곱해서 사용
        static readonly int MoveStartFrame = 0;
        static readonly int MoveEndFrame = 13;
        static readonly int AttackDurationFrame = 17;
        static readonly int ReloadDurationFrame = 40;
        static readonly float ObstacleJumpSpeedMultiplier = 0.8f;       // 장애물 뛰어넘을 때 이동속도 배율

        // 프로퍼티
        public Position2 Position { get { return _position; } }
        public bool IsMoving { get; private set; }
        public bool IsDoingSomeAction { get; private set; }
        public CharacterLogic CurrentTarget { get { return _currentTarget; } }
        public int CostRegen { get { return _costRegen; } }
        public int ExSkillCost { get { return _exSkillCost; } }
        public bool CanUseExSkill { get { return !_isObstacleJumping; } }

        // 이벤트
        public Action OnAttack;
        public Action OnUseExSkill;
        public Action OnUseNormalSkill;
        public Action OnReload;
        public delegate void CharacterDamageEvent(int damage, bool isCritical, AttackType attackType, ArmorType armorType);
        public CharacterDamageEvent OnCharacterTakeDamage;
        public Action OnDie;

        public void Init(BattleLogic battle, CharacterData charData, CharacterStatData statData)
        {
            _battleLogic = battle;
            _bt = BuildBehaviorTree();

            // 필드 초기화
            Name = charData.Name;
            AttackType = charData.AttackType;
            ArmorType = charData.ArmorType;

            _attackPower = statData.AttackPowerLevel1;
            _defensePower = statData.DefensePowerLevel1;
            _healPower = statData.HealPowerLevel1;
            _costRegen = statData.CostRegen;
            _moveSpeed = statData.MoveSpeed;
            _attackRange = statData.NormalAttackRange;

            _curAmmo = _maxAmmo;
            _currentHP = _maxHP;
            _exSkillCost = charData.skills[0].Cost;

            // 스킬 등록
            exSkill = charData.skills[0];
            normalSkill = charData.skills[1];


            // 일반 스킬 조건 등록
            AutoSkillCondition normalSkillConditionData = charData.skills[1].NormalSkillCondition;
            switch (normalSkillConditionData.ConditionType)
            {
                case AutoSkillConditionType.Cooltime:
                    _normalSkillCondition = new AutoSkillCheckCooltime(normalSkillConditionData.Argument);
                    break;
                default:
                    LogicDebug.LogError("해당 스킬 조건은 아직 미구현됨");
                    break;
            }
        }

        protected BehaviorTree BuildBehaviorTree()
        {
            // EX 스킬 (최우선 순위)
            Conditional isExSkillTriggerd = new Conditional(() => { return _exSkillTrigger; });
            BehaviorAction useExSkill = new BehaviorAction(UseExSkill);
            BehaviorNode checkAndUseExSkill = new StatefulSequence(isExSkillTriggerd, useExSkill);
            BehaviorNode subTree_ExSkill = new DecoratorInverter(checkAndUseExSkill);

            // 다음 웨이브 스폰까지 기다리기
            Conditional isNoEnemy = new Conditional(() => { return _battleLogic.EnemiesLogic.Count <= 0; });
            BehaviorAction waitEnemySpawn = new BehaviorAction(WaitEnemySpawn);
            BehaviorNode waitUntilEnemySpawn = new DecoratorInverter(new Sequence(isNoEnemy, waitEnemySpawn));

            // 다음 웨이브까지 이동
            BehaviorAction waitSkillDone = new BehaviorAction(WaitSkillDone);
            BehaviorAction moveToEnemyWave = new BehaviorAction(MoveToNextWave);
            BehaviorNode moveToNextWave = new StatefulSequence(waitSkillDone, moveToEnemyWave);

            // 교전
            // 기본 스킬
            Conditional canUseNormalSkill = new Conditional(CheckCanUseNormalSkill);
            BehaviorAction useNormalSkill = new BehaviorAction(UseNormalSkill);
            BehaviorNode checkAndUseNormalSkill = new StatefulSequence(canUseNormalSkill, useNormalSkill);
            BehaviorNode subTree_NormalSkill = new DecoratorInverter(checkAndUseNormalSkill);

            // 이동
            BehaviorAction getNextDest = new BehaviorAction(GetNextDest);
            BehaviorAction moveStart = new BehaviorAction(MoveStart);
            BehaviorAction moveDoing = new BehaviorAction(MoveDoing);
            BehaviorAction moveEnd = new BehaviorAction(MoveEnd);
            BehaviorNode subTree_Move = new StatefulSequence(getNextDest, moveStart, moveDoing, moveEnd);

            // 재장전
            Conditional needToReload = new Conditional(() => { return _curAmmo <= 0; });
            BehaviorAction doReload = new BehaviorAction(Reload);
            BehaviorNode reload = new Sequence(needToReload, doReload);
            BehaviorNode subTree_Reload = new DecoratorInverter(reload);

            // 교전 개시
            Conditional isEnemyCloseEnough = new Conditional(() => { return _distToEnemy < _attackRange; });
            Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return _curAmmo > 0; });
            BehaviorNode cannotUseNormalSkill = new DecoratorInverter(canUseNormalSkill);
            BehaviorAction attack = new BehaviorAction(Attack);
            BehaviorNode subTree_basicAttack = new Sequence(isEnemyCloseEnough, isHaveEnoughBulletInMagazine, cannotUseNormalSkill, attack);
            StatefulSequence combat = new StatefulSequence(subTree_NormalSkill, subTree_Move, subTree_Reload, subTree_basicAttack);

            // EX 스킬을 제외한 나머지
            BehaviorNode baseCharacterAI = new StatefulSequence(waitUntilEnemySpawn, moveToNextWave, combat);

            // 루트
            BehaviorTree tree = new BehaviorTree();
            tree.Root = new Sequence(subTree_ExSkill, baseCharacterAI);
            return tree;
        }

        // Update is called once per frame
        public void Tick()
        {
            if (!isAiActive)
            {
                return;
            }

            UpdateValues();
            _bt.Behave();
        }

        void UpdateValues()
        {
            if (_currentTarget == null || !_currentTarget.isAlive)
            {
                FindNextEnemy();
            }
            if (_currentTarget != null)
            {
                _distToEnemy = Position2.Distance(this.Position, _currentTarget.Position);
            }
            _normalSkillCondition.CheckSkillCondition();
        }

        void FindNextEnemy()
        {
            _currentTarget = null;
            float minDist = float.MaxValue;
            foreach (var enemy in _battleLogic.EnemiesLogic)
            {
                float dist = (enemy.Position - this.Position).magnitude;
                if (dist > SightRange)
                {
                    continue;
                }
                if (minDist > dist)
                {
                    minDist = dist;
                    _currentTarget = enemy;
                }
            }
        }

        BehaviorResult WaitEnemySpawn()
        {
            // 적이 스폰될 때까지 최대 n초 대기하기
            if (_currentTarget == null || _currentTarget.isAlive)
            {
                LogicDebug.Log("적 스폰 대기중");
                return BehaviorResult.Running;
            }
            else
                return BehaviorResult.Success;
        }

        BehaviorResult GetNextDest()
        {
            LogicDebug.Log("GetNextDest 수행");
            Position2 destination = Position2.zero;

            // 기존에 엄폐중인 엄폐물이 있다면 '점유' 해제
            if (_coveringObstacle != null)
            {
                _coveringObstacle.isOccupied = false;
                _coveringObstacle = null;
            }

            // 적 위치 파악
            Position2 enemyPosition = _currentTarget.Position;

            // BattleSceneManager에 보관된 Obstacle들 중에
            // 1. 사거리 * 0.88 이내이면서
            // 2. 그 중에 가장 나와 가까운 것을 선정
            ObstacleLogic targetObstacle = null;
            float targetObstacleDistance = float.MaxValue;
            foreach (var ob in _battleLogic.Obstacles)
            {
                // 엄폐물이 이미 점유중인 경우 더 고려할 필요 없음
                if (ob.isOccupied) continue;

                // 엄폐물 앞뒤로 있는 CoveringPoint 중에 나와 가까운 쪽을 선택
                Position2 coveringPoint;
                if (Position2.Distance(this.Position, ob.CoveringPoint[0])
                    < Position2.Distance(this.Position, ob.CoveringPoint[1]))
                {
                    coveringPoint = ob.CoveringPoint[0];
                }
                else
                {
                    coveringPoint = ob.CoveringPoint[1];
                }

                float obstacleToEnemy = (enemyPosition - coveringPoint).magnitude;
                if (obstacleToEnemy > _attackRange * PositioningAttackRangeRatio)
                {
                    continue;
                }

                float characterToObstacle = Position2.Distance(coveringPoint, Position);
                if (characterToObstacle < targetObstacleDistance)
                {
                    targetObstacle = ob;
                    destination = coveringPoint;
                    targetObstacleDistance = characterToObstacle;
                }
            }

            // 적당한 obstacle이 있을 경우 그곳을 목적지로 설정
            _destObstacle = targetObstacle;
            if (_destObstacle != null)
            {
                LogicDebug.Log("엄폐물로 위치 설정");
                _moveDest = destination;
            }
            // 없을 경우, 적 위치를 목적지로 설정
            else
            {
                _moveDest = _currentTarget.Position;
            }

            return BehaviorResult.Success;
        }

        BehaviorResult MoveStart()
        {
            if(_distToEnemy < _attackRange)
            {
                _curActionFrame = 0;
                IsMoving = false;
                return BehaviorResult.Success;
            }

            _curActionFrame++;
            if (_curActionFrame >= MoveStartFrame)
            {
                _curActionFrame = 0;
                IsMoving = true;
                // 점유중인 엄폐물이 있었다면 점유 해제
                if (_coveringObstacle != null)
                {
                    _coveringObstacle.isOccupied = false;
                    _coveringObstacle = null;
                }
                LogicDebug.Log("MoveStart");
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult MoveDoing()
        {
            // MoveIng 종료 조건 판단
            if (!_isObstacleJumping)
            {
                // 엄폐물로 이동중인 경우, 해당 엄폐물이 다른 캐릭터에 의해 '점유'되었는지 체크
                if (_destObstacle != null)
                {
                    if (_destObstacle.isOccupied)
                    {
                        LogicDebug.Log("엄폐물 선점당함. 경로 재탐색");
                        _destObstacle = null;
                        return BehaviorResult.Failure;
                    }
                    if (Position2.Distance(Position, _moveDest) < 0.1f)
                    {
                        LogicDebug.Log("목표 엄폐물에 도달, 엄폐 수행. 이동 종료");
                        _destObstacle.isOccupied = true;
                        _coveringObstacle = _destObstacle;
                        _destObstacle = null;
                        return BehaviorResult.Success;
                    }
                }
                // 엄폐물이 아닌 바로 적을 향해 이동중인 경우 사거리 체크 수행
                else if (_distToEnemy < (_attackRange * PositioningAttackRangeRatio))
                {
                    LogicDebug.Log("공격 대상과 사거리 이내로 가까워짐. 이동 종료");
                    return BehaviorResult.Success;
                }
            }

            // 엄폐물 뛰어넘기 조건
            if (!_isObstacleJumping && pathFinder.isOnOffMeshLink)
            {
                _isObstacleJumping = true;
                // 뛰어넘는 중에는 다른 캐릭터가 엄폐물 뒤에서 기다리는 상황을 방지하기 위해 OffMeshLink 비활성화
                occupyinngObstacle = pathFinder.GetOccupyingObstacle();
                occupyinngObstacle.isOccupied = true;
            }

            // 이동 속도 조절을 위해 장애물 뛰어넘기는 수동으로 진행
            if (_isObstacleJumping)
            {
                Position2 jumpEndPos = pathFinder.GetObstacleJumpEndPos();
                _position = Position2.MoveTowards(Position, jumpEndPos, ObstacleJumpSpeedMultiplier * _moveSpeed / _battleLogic.BaseLogicTickrate);
                if ((Position - jumpEndPos).magnitude < 0.1f)
                {
                    LogicDebug.Log("장애물 극복 완료");
                    _isObstacleJumping = false;
                    occupyinngObstacle.isOccupied = false;
                    pathFinder.CompleteObstacleJump();
                }
            }
            else
            {
                // 이동 수행
                float stepLength = _moveSpeed / _battleLogic.BaseLogicTickrate;
                pathFinder.CalculatePath(_moveDest);
                _position = pathFinder.FollowPath(stepLength);
            }
            return BehaviorResult.Running;
        }

        BehaviorResult MoveEnd()
        {
            _curActionFrame++;
            if (_curActionFrame >= MoveEndFrame)
            {
                _curActionFrame = 0;
                IsMoving = false;
                LogicDebug.Log("MoveEnd");
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult WaitSkillDone()
        {
            if (_battleLogic.GetIfSomeAllyDoingAction())
            {
                LogicDebug.Log("스킬 종료 대기중");
                return BehaviorResult.Running;
            }
            else return BehaviorResult.Success;
        }

        BehaviorResult MoveToNextWave()
        {
            if (_currentTarget != null) return BehaviorResult.Success;
            else
            {
                // 엄폐물 뛰어넘기 조건
                if (!_isObstacleJumping && pathFinder.isOnOffMeshLink)
                {
                    _isObstacleJumping = true;
                    // 뛰어넘는 중에는 다른 캐릭터가 엄폐물 뒤에서 기다리는 상황을 방지하기 위해 OffMeshLink 비활성화
                    occupyinngObstacle = pathFinder.GetOccupyingObstacle();
                    occupyinngObstacle.isOccupied = true;
                }

                // 이동 속도 조절을 위해 장애물 뛰어넘기는 수동으로 진행
                if (_isObstacleJumping)
                {
                    LogicDebug.Log("장애물 극복 중");
                    Position2 jumpEndPos = pathFinder.GetObstacleJumpEndPos();
                    _position = Position2.MoveTowards(Position, jumpEndPos, ObstacleJumpSpeedMultiplier * _moveSpeed / _battleLogic.BaseLogicTickrate);
                    if ((Position - jumpEndPos).magnitude < 0.1f)
                    {
                        LogicDebug.Log("장애물 극복 완료");
                        _isObstacleJumping = false;
                        occupyinngObstacle.isOccupied = false;
                        pathFinder.CompleteObstacleJump();
                    }
                }
                // 앞에 뛰어넘을 장애물이 없다면, 단순 전방으로 이동
                else
                {
                    bool isPathFounded = pathFinder.CalculatePath(Position + Position2.forward * 3);
                    if (isPathFounded)
                    {
                        _position = pathFinder.FollowPath(_moveSpeed / _battleLogic.BaseLogicTickrate);
                    }
                    else
                    {
                        LogicDebug.Log("길찾기 실패, 임의로 앞으로 이동");
                        _position += new Position2(0, _moveSpeed / _battleLogic.BaseLogicTickrate);
                    }
                }

                return BehaviorResult.Running;
            }
        }

        BehaviorResult Attack()
        {
            if (_currentTarget == null || !_currentTarget.isAlive)
            {
                _curActionFrame = 0;
                return BehaviorResult.Success;
            }
            if (_distToEnemy > _attackRange || _curAmmo <= 0)
            {
                _curActionFrame = 0;
                return BehaviorResult.Failure;
            }

            _curActionFrame++;
            if (_curActionFrame >= AttackDurationFrame)
            {
                LogicDebug.Log("기본 공격 투사체 생성");
                BulletLogic bulletComponent = new BulletLogic();
                bulletComponent.Position = this.Position;
                bulletComponent.Attacker = this;
                bulletComponent.Target = _currentTarget;
                bulletComponent.AttackType = AttackType;
                bulletComponent.AttackPower = _attackPower;
                bulletComponent.ProjectileSpeed = 15f;

                _battleLogic.AddBullet(bulletComponent);

                if(OnAttack != null)
                {
                    OnAttack();
                }

                // currentTarget.TakeDamage(AttackType, attackPower);
                _curAmmo -= 1;
                _curActionFrame = 0;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult Reload()
        {
            _curActionFrame++;
            IsDoingSomeAction = true;
            if (_curActionFrame >= ReloadDurationFrame)
            {
                _curActionFrame = 0;
                IsDoingSomeAction = false;
                _curAmmo = 15;
                OnReload();
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult UseExSkill()
        {
            // Action의 첫 프레임
            if (_exSkillFrame == 0)
            {
                switch (exSkill.SkillRange.ConditionType)
                {
                    case SkillTargetType.Enemy:
                        // currentTarget을 그대로 사용
                        break;
                    default:
                        LogicDebug.LogError("해당 스킬 옵션은 구현되지 않음!");
                        break;
                }
            }

            _exSkillFrame++;
            IsDoingSomeAction = true;

            // 선딜레이 끝난 타이밍: 스킬 시전
            if (_exSkillFrame == exSkill.StartupFrame)
            {
                // TODO: 힐/버프 시에는 Bullet 대신 별도의 클래스로 구현하기
                BulletLogic skillProjectile = new BulletLogic();
                skillProjectile.Position = this.Position;
                skillProjectile.Attacker = this;
                skillProjectile.Target = _currentTarget;
                // TODO: 투사체 공격력 설정에 EX 스킬 데이터 반영하기
                skillProjectile.AttackPower = _attackPower * 10;
                skillProjectile.AttackType = AttackType;
                skillProjectile.ProjectileSpeed = 20f;
                _battleLogic.AddBullet(skillProjectile);

                if(OnUseExSkill != null)
                {
                    OnUseExSkill();
                }
            }

            // Action의 마지막 프레임
            if (_exSkillFrame >= exSkill.StartupFrame + exSkill.RecoveryFrame)
            {
                _exSkillFrame = 0;
                _curActionFrame = 0;
                IsDoingSomeAction = false;
                _exSkillTrigger = false;
                _currentTarget = null;       // 아군을 타겟팅한 경우 등을 고려, 스킬 종료 시 대상 재선정 필요

                LogicDebug.Log("Ex 스킬 사용 종료");
                return BehaviorResult.Success;
            }
            LogicDebug.Log("Ex 스킬 사용 중");
            return BehaviorResult.Running;
        }

        bool CheckCanUseNormalSkill()
        {
            if (_normalSkillCondition.CanUseSkill())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        BehaviorResult UseNormalSkill()
        {
            // 일반스킬 첫 프레임
            if (_curActionFrame == 0)
            {
                switch (normalSkill.SkillRange.ConditionType)
                {
                    case SkillTargetType.Enemy:
                        // currentTarget을 그대로 사용
                        break;
                    default:
                        LogicDebug.LogError("해당 스킬 옵션은 구현되지 않음!");
                        break;
                }
            }

            _curActionFrame++;
            IsDoingSomeAction = true;

            // 선딜 끝났을 때
            if (_curActionFrame == normalSkill.StartupFrame)
            {
                // TODO: 힐/버프 시에는 Bullet 대신 별도의 클래스로 구현하기
                BulletLogic skillProjectile = new BulletLogic();
                skillProjectile.Position = this.Position;
                skillProjectile.Attacker = this;
                skillProjectile.Target = _currentTarget;
                // TODO: 투사체 공격력 설정에 EX 스킬 데이터 반영하기
                skillProjectile.AttackPower = _attackPower * 2;
                skillProjectile.AttackType = AttackType;
                skillProjectile.ProjectileSpeed = 20f;
                _battleLogic.AddBullet(skillProjectile);

                if(OnUseNormalSkill != null)
                {
                    OnUseNormalSkill();
                }

                _normalSkillCondition.ResetSkillCondition();
            }

            if (_curActionFrame >= normalSkill.StartupFrame + normalSkill.RecoveryFrame)
            {
                _curActionFrame = 0;
                IsDoingSomeAction = false;
                LogicDebug.Log("기본 스킬 사용 종료");
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        public void SetPosition(Position2 newPosition)
        {
            _position = newPosition;
        }

        public void TriggerExSkill()
        {
            _exSkillTrigger = true;
        }

        public void TakeDamage(AttackType attackType, int attackPower)
        {
            // TODO: 공격 계산식 수정하기
            float damageMultiplier = AttackEffectiveness.GetEffectiveness(attackType, ArmorType);
            int damage = (int)Math.Round(attackPower * damageMultiplier);
            _currentHP -= damage;
            if (OnCharacterTakeDamage != null)
            {
                OnCharacterTakeDamage(damage, false, attackType, ArmorType); // 현재 치명타 구현 안되어있음.
            }
            if (_currentHP <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            // TODO: 후퇴 연출 필요
            isAlive = false;
            _battleLogic.RemoveDeadCharacter(this);
            if(OnDie != null)
            {
                OnDie();
            }
        }

        public void TakeHeal(int heal)
        {
            _currentHP += heal;
            if (_currentHP > _maxHP) _currentHP = _maxHP;
        }

        #region 디버깅용
        public void SetHP(int newHP)
        {
            _currentHP = newHP;
        }
        #endregion
    }
}