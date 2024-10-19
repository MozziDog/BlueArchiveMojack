using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using AI;
using Sirenix.OdinInspector;

namespace Logic
{
    [Serializable]
    public class Character
    {
        [Title("기본 정보")]
        public string Name;
        public bool isAlive = true;
        public bool isAiActive = true;  // 적군 허수아비 AI 꺼두는 용도
        public AttackType AttackType;
        public ArmorType ArmorType;

        [Title("기본 스탯 정보")]
        public int _maxHP;
        public int currentHP;
        public int attackPower;
        public int defensePower;
        public int healPower;
        public float moveSpeed;
        public int CostRegen;

        [Title("엄폐 관련")]
        public Obstacle coveringObstacle;       // 현재 엄폐를 수행중인 엄폐물
        public bool isDoingCover = false;
        private Obstacle occupyinngObstacle;           // 현재 점유 중인 장애물

        [Title("이동 관련")]
        public Position2 Position;
        public Position2 moveDest;
        public int moveStartFrame = 0;
        public int moveEndFrame = 13;
        public float obstacleJumpSpeed;
        public Obstacle destObstacle;
        public float sightRange = 13f;
        public float attackRange;
        public float distToEnemy = 10f;
        public float positioningAttackRangeRatio = 0.88f;       // 이동 위치 선정할 때 최대 사거리 대신 사거리에 이 값을 곱해서 사용
        public int recentHit = 0;
        public bool _isObstacleJumping;

        [Title("전투 관련")]
        public Character currentTarget;
        public int _maxAmmo = 15;
        public int _curAmmo;
        public int ExSkillCost;
        public bool exSkillTrigger;
        // public IAutoSkillCheck normalSkillCondition;
        public AutoSkillCheckCooltime normalSkillCondition;

        [Title("행동 관련 프레임 정보")]
        [ReadOnly] public int curActionFrame = 0;
        [ReadOnly] public int exSkillFrame = 0;
        public int attackDurationFrame = 17;
        public int reloadDurationFrame = 40;

        [Title("컴포넌트 레퍼런스")]
        public PathFinder pathFinder;
        public BattleLogic battleLogic;

        BehaviorTree _bt;
        SkillData exSkill;
        SkillData normalSkill;

        // 프로퍼티
        public bool isMoving { get; private set; }
        public bool isDoingSomeAction { get; private set; }
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
            battleLogic = battle;
            _bt = BuildBehaviorTree();

            // 필드 초기화
            Name = charData.Name;
            this.AttackType = charData.AttackType;
            this.ArmorType = charData.ArmorType;

            attackPower = statData.AttackPowerLevel1;
            defensePower = statData.DefensePowerLevel1;
            healPower = statData.HealPowerLevel1;
            CostRegen = statData.CostRegen;
            moveSpeed = statData.MoveSpeed;
            obstacleJumpSpeed = statData.ObstacleJumpSpeed;
            attackRange = statData.NormalAttackRange;

            _curAmmo = _maxAmmo;
            currentHP = _maxHP;
            ExSkillCost = charData.skills[0].Cost;

            // 스킬 등록
            exSkill = charData.skills[0];
            normalSkill = charData.skills[1];


            // 일반 스킬 조건 등록
            AutoSkillCondition normalSkillConditionData = charData.skills[1].NormalSkillCondition;
            switch (normalSkillConditionData.ConditionType)
            {
                case AutoSkillConditionType.Cooltime:
                    normalSkillCondition = new AutoSkillCheckCooltime(normalSkillConditionData.Argument);
                    break;
                default:
                    LogicDebug.LogError("해당 스킬 조건은 아직 미구현됨");
                    break;
            }
        }

        protected BehaviorTree BuildBehaviorTree()
        {
            // EX 스킬 (최우선 순위)
            Conditional isExSkillTriggerd = new Conditional(() => { return exSkillTrigger; });
            BehaviorAction useExSkill = new BehaviorAction(UseExSkill);
            BehaviorNode checkAndUseExSkill = new StatefulSequence(isExSkillTriggerd, useExSkill);
            BehaviorNode subTree_ExSkill = new DecoratorInverter(checkAndUseExSkill);

            // 다음 웨이브 스폰까지 기다리기
            Conditional isNoEnemy = new Conditional(() => { return battleLogic.EnemiesLogic.Count <= 0; });
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
            Conditional isEnemyCloseEnough = new Conditional(() => { return distToEnemy < attackRange; });
            Conditional isNotHitEnough = new Conditional(() => { return recentHit < 20; });
            Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return _curAmmo > 0; });
            BehaviorNode cannotUseNormalSkill = new DecoratorInverter(canUseNormalSkill);
            BehaviorAction attack = new BehaviorAction(Attack);
            BehaviorNode subTree_basicAttack = new Sequence(isEnemyCloseEnough, isNotHitEnough, isHaveEnoughBulletInMagazine, cannotUseNormalSkill, attack);
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
            if (currentTarget == null || !currentTarget.isAlive)
            {
                FindNextEnemy();
            }
            if (currentTarget != null)
            {
                distToEnemy = Position2.Distance(this.Position, currentTarget.Position);
            }
            normalSkillCondition.CheckSkillCondition();
        }

        void FindNextEnemy()
        {
            currentTarget = null;
            float minDist = float.MaxValue;
            foreach (var enemy in battleLogic.EnemiesLogic)
            {
                float dist = (enemy.Position - Position).magnitude;
                if (dist > sightRange)
                {
                    continue;
                }
                if (minDist > dist)
                {
                    minDist = dist;
                    currentTarget = enemy;
                }
            }
        }

        BehaviorResult WaitEnemySpawn()
        {
            // 적이 스폰될 때까지 최대 n초 대기하기
            if (currentTarget == null || currentTarget.isAlive)
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
            if (coveringObstacle != null)
            {
                coveringObstacle.isOccupied = false;
                coveringObstacle = null;
            }

            // 적 위치 파악
            Position2 enemyPosition = currentTarget.Position;

            // BattleSceneManager에 보관된 Obstacle들 중에
            // 1. 사거리 * 0.88 이내이면서
            // 2. 그 중에 가장 나와 가까운 것을 선정
            Obstacle targetObstacle = null;
            float targetObstacleDistance = float.MaxValue;
            foreach (var ob in battleLogic.Obstacles)
            {
                // 엄폐물이 이미 점유중인 경우 더 고려할 필요 없음
                if (ob.isOccupied) continue;

                // 엄폐물 앞뒤로 있는 CoveringPoint 중에 나와 가까운 쪽을 선택
                Position2 coveringPoint;
                if (Position2.Distance(Position, ob.CoveringPoint[0])
                    < Position2.Distance(Position, ob.CoveringPoint[1]))
                {
                    coveringPoint = ob.CoveringPoint[0];
                }
                else
                {
                    coveringPoint = ob.CoveringPoint[1];
                }

                float obstacleToEnemy = (enemyPosition - coveringPoint).magnitude;
                if (obstacleToEnemy > attackRange * positioningAttackRangeRatio)
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
            destObstacle = targetObstacle;
            if (destObstacle != null)
            {
                LogicDebug.Log("엄폐물로 위치 설정");
                moveDest = destination;
            }
            // 없을 경우, 적 위치를 목적지로 설정
            else
            {
                moveDest = currentTarget.Position;
            }

            return BehaviorResult.Success;
        }

        BehaviorResult MoveStart()
        {
            if(distToEnemy < attackRange)
            {
                curActionFrame = 0;
                isMoving = false;
                return BehaviorResult.Success;
            }

            curActionFrame++;
            if (curActionFrame >= moveStartFrame)
            {
                curActionFrame = 0;
                isMoving = true;
                // 점유중인 엄폐물이 있었다면 점유 해제
                if (coveringObstacle != null)
                {
                    coveringObstacle.isOccupied = false;
                    coveringObstacle = null;
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
                if (destObstacle != null)
                {
                    if (destObstacle.isOccupied)
                    {
                        LogicDebug.Log("엄폐물 선점당함. 경로 재탐색");
                        destObstacle = null;
                        return BehaviorResult.Failure;
                    }
                    if (Position2.Distance(Position, moveDest) < 0.1f)
                    {
                        LogicDebug.Log("목표 엄폐물에 도달, 엄폐 수행. 이동 종료");
                        destObstacle.isOccupied = true;
                        coveringObstacle = destObstacle;
                        destObstacle = null;
                        return BehaviorResult.Success;
                    }
                }
                // 엄폐물이 아닌 바로 적을 향해 이동중인 경우 사거리 체크 수행
                else if (distToEnemy < (attackRange * positioningAttackRangeRatio))
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
                Position = Position2.MoveTowards(Position, jumpEndPos, obstacleJumpSpeed / battleLogic.BaseLogicTickrate);
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
                Position2 oldPosition = this.Position;
                float stepLength = moveSpeed / battleLogic.BaseLogicTickrate;
                pathFinder.CalculatePath(moveDest);
                Position = pathFinder.FollowPath(stepLength);
                Position2 newPosition = this.Position;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult MoveEnd()
        {
            curActionFrame++;
            if (curActionFrame >= moveEndFrame)
            {
                curActionFrame = 0;
                isMoving = false;
                LogicDebug.Log("MoveEnd");
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult WaitSkillDone()
        {
            if (isDoingSomeAction)
            {
                LogicDebug.Log("스킬 종료 대기중");
                return BehaviorResult.Running;
            }
            else return BehaviorResult.Success;
        }

        BehaviorResult MoveToNextWave()
        {
            if (currentTarget != null) return BehaviorResult.Success;
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
                    Position = Position2.MoveTowards(Position, jumpEndPos, obstacleJumpSpeed / battleLogic.BaseLogicTickrate);
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
                        Position = pathFinder.FollowPath(moveSpeed / battleLogic.BaseLogicTickrate);
                    }
                    else
                    {
                        LogicDebug.Log("길찾기 실패, 임의로 앞으로 이동");
                        Position += new Position2(0, moveSpeed / battleLogic.BaseLogicTickrate);
                    }
                }

                return BehaviorResult.Running;
            }
        }

        BehaviorResult Attack()
        {
            if (currentTarget == null || !currentTarget.isAlive)
            {
                curActionFrame = 0;
                return BehaviorResult.Success;
            }
            if (distToEnemy > attackRange || _curAmmo <= 0)
            {
                curActionFrame = 0;
                return BehaviorResult.Failure;
            }

            curActionFrame++;
            if (curActionFrame >= attackDurationFrame)
            {
                LogicDebug.Log("기본 공격 투사체 생성");
                Bullet bulletComponent = new Bullet();
                bulletComponent.Position = this.Position;
                bulletComponent.Attacker = this;
                bulletComponent.Target = currentTarget;
                bulletComponent.AttackType = AttackType;
                bulletComponent.AttackPower = attackPower;
                bulletComponent.ProjectileSpeed = 15f;

                battleLogic.AddBullet(bulletComponent);

                if(OnAttack != null)
                {
                    OnAttack();
                }

                // currentTarget.TakeDamage(AttackType, attackPower);
                _curAmmo -= 1;
                curActionFrame = 0;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult Reload()
        {
            curActionFrame++;
            isDoingSomeAction = true;
            if (curActionFrame >= reloadDurationFrame)
            {
                curActionFrame = 0;
                isDoingSomeAction = false;
                _curAmmo = 15;
                OnReload();
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }

        BehaviorResult UseExSkill()
        {
            // Action의 첫 프레임
            if (exSkillFrame == 0)
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

            exSkillFrame++;
            isDoingSomeAction = true;

            // 선딜레이 끝난 타이밍: 스킬 시전
            if (exSkillFrame == exSkill.StartupFrame)
            {
                // TODO: 힐/버프 시에는 Bullet 대신 별도의 클래스로 구현하기
                Bullet skillProjectile = new Bullet();
                skillProjectile.Position = this.Position;
                skillProjectile.Attacker = this;
                skillProjectile.Target = currentTarget;
                // TODO: 투사체 공격력 설정에 EX 스킬 데이터 반영하기
                skillProjectile.AttackPower = attackPower * 10;
                skillProjectile.AttackType = AttackType;
                skillProjectile.ProjectileSpeed = 20f;
                battleLogic.AddBullet(skillProjectile);

                if(OnUseExSkill != null)
                {
                    OnUseExSkill();
                }
            }

            // Action의 마지막 프레임
            if (exSkillFrame >= exSkill.StartupFrame + exSkill.RecoveryFrame)
            {
                exSkillFrame = 0;
                curActionFrame = 0;
                isDoingSomeAction = false;
                exSkillTrigger = false;
                currentTarget = null;       // 아군을 타겟팅한 경우 등을 고려, 스킬 종료 시 대상 재선정 필요

                LogicDebug.Log("Ex 스킬 사용 종료");
                return BehaviorResult.Success;
            }
            LogicDebug.Log("Ex 스킬 사용 중");
            return BehaviorResult.Running;
        }

        bool CheckCanUseNormalSkill()
        {
            if (normalSkillCondition.CanUseSkill())
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
            if (curActionFrame == 0)
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

            curActionFrame++;
            isDoingSomeAction = true;

            // 선딜 끝났을 때
            if (curActionFrame == normalSkill.StartupFrame)
            {
                // TODO: 힐/버프 시에는 Bullet 대신 별도의 클래스로 구현하기
                Bullet skillProjectile = new Bullet();
                skillProjectile.Position = this.Position;
                skillProjectile.Attacker = this;
                skillProjectile.Target = currentTarget;
                // TODO: 투사체 공격력 설정에 EX 스킬 데이터 반영하기
                skillProjectile.AttackPower = attackPower * 2;
                skillProjectile.AttackType = AttackType;
                skillProjectile.ProjectileSpeed = 20f;
                battleLogic.AddBullet(skillProjectile);

                if(OnUseNormalSkill != null)
                {
                    OnUseNormalSkill();
                }

                normalSkillCondition.ResetSkillCondition();
            }

            if (curActionFrame >= normalSkill.StartupFrame + normalSkill.RecoveryFrame)
            {
                curActionFrame = 0;
                isDoingSomeAction = false;
                LogicDebug.Log("기본 스킬 사용 종료");
                return BehaviorResult.Success;
            }
            return BehaviorResult.Running;
        }


        public void TakeDamage(AttackType attackType, int attackPower)
        {
            // TODO: 공격 계산식 수정하기
            float damageMultiplier = AttackEffectiveness.GetEffectiveness(attackType, ArmorType);
            int damage = (int)Math.Round(attackPower * damageMultiplier);
            currentHP -= damage;
            if (OnCharacterTakeDamage != null)
            {
                OnCharacterTakeDamage(damage, false, attackType, ArmorType); // 현재 치명타 구현 안되어있음.
            }
            if (currentHP <= 0)
            {
                Die();
            }
        }

        void Die()
        {
            // TODO: 후퇴 연출 필요
            isAlive = false;
            battleLogic.RemoveDeadCharacter(this);
            if(OnDie != null)
            {
                OnDie();
            }
        }

        public void TakeHeal(int heal)
        {
            currentHP += heal;
            if (currentHP > _maxHP) currentHP = _maxHP;
        }
    }
}