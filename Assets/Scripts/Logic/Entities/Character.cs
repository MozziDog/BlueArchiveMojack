using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Sirenix.OdinInspector;
using System;

[Serializable]
public class Character : MonoBehaviour
{
    [Title("기본 정보")]
    public string Name;
    public bool isAlive = true;
    public AttackType AttackType;
    public ArmorType ArmorType;
    public AutoSkillCondition normalSkillConditionData;

    [Title("기본 스탯 정보")]
    [SerializeField] int _maxHP;
    [SerializeField] int currentHP;
    [SerializeField] int attackPower;
    [SerializeField] int defensePower;
    [SerializeField] int healPower;
    [SerializeField] float moveSpeed;
    public int CostRegen;

    [Title("엄폐 관련")]
    public Obstacle coveringObstacle;       // 현재 엄폐를 수행중인 엄폐물
    public bool isDoingCover = false;
    private Obstacle occupyinngObstacle;           // 현재 점유 중인 장애물

    [Title("이동 관련")]
    public int moveStartFrame = 0;
    public int moveEndFrame = 13;
    [SerializeField] float obstacleJumpSpeed;
    public Vector3 moveDest;
    public Obstacle destObstacle;
    public float sightRange = 13f;
    public float attackRange = 7f;
    public float distToEnemy = 10f;
    public float positioningAttackRangeRatio = 0.88f;       // 이동 위치 선정할 때 최대 사거리 대신 사거리에 이 값을 곱해서 사용
    public int recentHit = 0;
    [SerializeField] bool _isObstacleJumping;

    [Title("전투 관련")]
    public Character currentTarget;
    [SerializeField] int _maxAmmo = 15;
    [SerializeField] int _curAmmo;
    public int ExSkillCost;
    public bool exSkillTrigger;
    public IAutoSkillCheck normalSkillCondition;
    public GameObject BulletPrefab;

    [Title("행동 관련 프레임 정보")]
    [SerializeField, ReadOnly] int curActionFrame;
    public int attackDurationFrame;
    public int reloadDurationFrame;
    public int normalSkillDurationFrame;
    public int exSkillDurationFrame;

    [Title("컴포넌트 레퍼런스")]
    public PathFinder pathFinder;
    [SerializeField] BattleSceneManager battleSceneManager;

    BehaviorTree _bt;

    // 프로퍼티
    public bool isMoving { get; private set; }
    public bool isDoingSomeAction { get; private set; }
    public bool CanUseExSkill { get{ return !_isObstacleJumping; } }


    public void Init(BattleSceneManager battle, CharacterData charData, CharacterStatData statData)
    {
        battleSceneManager = battle;
        pathFinder = GetComponent<PathFinder>();
        _bt = BuildBehaviorTree();

        // 필드 초기화
        Name = charData.Name;
        attackPower = statData.AttackPowerLevel1;
        defensePower = statData.DefensePowerLevel1;
        healPower = statData.HealPowerLevel1;
        CostRegen = statData.CostRegen;
        ExSkillCost = charData.ExCost;

        _curAmmo = _maxAmmo;
        currentHP = _maxHP;

        // 일반 스킬 조건 등록
        switch(normalSkillConditionData.conditionType)
        {
            case AutoSkillConditionType.Cooltime:
                normalSkillCondition = new AutoSkillCheckCooltime(normalSkillConditionData.argument);
                break;
            default:
                Debug.LogError("해당 스킬 조건은 아직 미구현됨");
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
            Conditional isNoEnemy = new Conditional(() => { return battleSceneManager.EnemiesActive.Count <= 0; });
            BehaviorAction waitEnemySpawn = new BehaviorAction(WaitEnemySpawn);
        BehaviorNode waitUntilEnemySpawn = new DecoratorInverter(new Sequence(isNoEnemy, waitEnemySpawn));

        // 다음 웨이브까지 이동
            BehaviorAction waitSkillDone = new BehaviorAction(WaitSkillDone);
            BehaviorAction moveToEnemyWave = new BehaviorAction(MoveToNextWave);
        BehaviorNode moveToNextWave = new StatefulSequence(waitSkillDone, moveToEnemyWave);

        // 교전
            // 기본 스킬
                Conditional canUseNormalSkill = new Conditional(() => { return normalSkillCondition.CanUseSkill(); });
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
                Conditional isNotHitEnough = new Conditional(()=> { return recentHit < 20; });
                Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return _curAmmo > 0; });
                BehaviorAction attack = new BehaviorAction(Attack);
            BehaviorNode subTree_basicAttack = new Sequence(isEnemyCloseEnough, isNotHitEnough, isHaveEnoughBulletInMagazine, attack);
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
        UpdateValues();
        _bt.Behave();
    }

    void UpdateValues()
    {
        if(currentTarget == null || !currentTarget.isAlive)
        {
            FindNextEnemy();
        }
        if(currentTarget != null)
        {
            distToEnemy = Vector3.Distance(transform.position, currentTarget.transform.position);
        }
        normalSkillCondition.CheckSkillCondition();
    }

    void FindNextEnemy()
    {
        float minDist = float.MaxValue;
        foreach(var enemy in battleSceneManager.EnemiesActive)
        {
            if(currentTarget == null || !currentTarget.isAlive)
            {
                currentTarget = enemy;
            }
            float dist = (enemy.transform.position - transform.position).magnitude;
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
        if(currentTarget == null || currentTarget.isAlive)
        {
            Debug.Log("적 스폰 대기중");
            return BehaviorResult.Running;
        }
        else
            return BehaviorResult.Success;
    }

    BehaviorResult GetNextDest()
    {
        Debug.Log("GetNextDest 수행");
        Vector3 destination = Vector3.zero;

        // 기존에 엄폐중인 엄폐물이 있다면 '점유' 해제
        if(coveringObstacle != null)
        {
            coveringObstacle.isOccupied = false;
            coveringObstacle = null;
        }

        // 적 위치 파악
        Vector3 enemyPosition = currentTarget.transform.position;

        // BattleSceneManager에 보관된 Obstacle들 중에
        // 1. 사거리 * 0.88 이내이면서
        // 2. 그 중에 가장 나와 가까운 것을 선정
        Obstacle targetObstacle = null;
        float targetObstacleDistance = float.MaxValue;
        foreach(var ob in battleSceneManager.Obstacles)
        {
            // 엄폐물이 이미 점유중인 경우 더 고려할 필요 없음
            if(ob.isOccupied) continue;

            // 엄폐물 앞뒤로 있는 CoveringPoint 중에 나와 가까운 쪽을 선택
            Vector3 coveringPoint;
            if(Vector3.Distance(transform.position, ob.CoveringPoint[0].position)
                < Vector3.Distance(transform.position, ob.CoveringPoint[1].position))
            {
                coveringPoint = ob.CoveringPoint[0].position;
            }
            else
            {
                coveringPoint = ob.CoveringPoint[1].position;
            }

            float obstacleToEnemy = (enemyPosition - coveringPoint).magnitude;
            if(obstacleToEnemy > attackRange * positioningAttackRangeRatio)
            {
                continue;
            }

            float characterToObstacle = (coveringPoint - transform.position).magnitude;
            if(characterToObstacle < targetObstacleDistance)
            {
                targetObstacle = ob;
                destination = coveringPoint;
                targetObstacleDistance = characterToObstacle;
            }
        }

        // 적당한 obstacle이 있을 경우 그곳을 목적지로 설정
        destObstacle = targetObstacle;
        if(destObstacle != null)
        {
            Debug.Log("엄폐물로 위치 설정");
            moveDest = destination;
        }
        // 없을 경우, 적 위치를 목적지로 설정
        else
        {
            moveDest = currentTarget.transform.position;
        }

        return BehaviorResult.Success;
    }

    BehaviorResult MoveStart()
    {
        curActionFrame++;
        if(curActionFrame >= moveStartFrame)
        {
            curActionFrame = 0;
            isMoving = true;
            // 점유중인 엄폐물이 있었다면 점유 해제
            if(coveringObstacle != null)
            {
                coveringObstacle.isOccupied = false;
                coveringObstacle = null;
            }
            Debug.Log("MoveStart");
            return BehaviorResult.Success;
        }
        return BehaviorResult.Running;
    }

    BehaviorResult MoveDoing()
    {
        // MoveIng 종료 조건 판단
        if(!_isObstacleJumping)
        {
            // 엄폐물로 이동중인 경우, 해당 엄폐물이 다른 캐릭터에 의해 '점유'되었는지 체크
            if(destObstacle != null)
            {
                if(destObstacle.isOccupied)
                {
                    Debug.Log("엄폐물 선점당함. 경로 재탐색");
                    return BehaviorResult.Failure;
                }
                if(Vector3.Distance(transform.position, moveDest) < 0.1f)
                {
                    Debug.Log("목표 엄폐물에 도달, 엄폐 수행. 이동 종료");
                    destObstacle.isOccupied = true;
                    coveringObstacle = destObstacle;
                    return BehaviorResult.Success;
                }
            }
            // 엄폐물이 아닌 바로 적을 향해 이동중인 경우 사거리 체크 수행
            else if(distToEnemy < (attackRange * positioningAttackRangeRatio))
            {
                Debug.Log("공격 대상과 사거리 이내로 가까워짐. 이동 종료");
                return BehaviorResult.Success;
            }
        }

        // 엄폐물 뛰어넘기 조건
        if(!_isObstacleJumping && pathFinder.isOnOffMeshLink)
        {
            _isObstacleJumping = true;
            // 뛰어넘는 중에는 다른 캐릭터가 엄폐물 뒤에서 기다리는 상황을 방지하기 위해 OffMeshLink 비활성화
            occupyinngObstacle = pathFinder.GetOccupyingObstacle();
            occupyinngObstacle.isOccupied = true;
        }

        // 이동 속도 조절을 위해 장애물 뛰어넘기는 수동으로 진행
        if(_isObstacleJumping)
        {
            Debug.Log("장애물 극복 중");
            Vector3 jumpEndPos = pathFinder.GetObstacleJumpEndPos();
            transform.position = Vector3.MoveTowards(transform.position, jumpEndPos, obstacleJumpSpeed / battleSceneManager.BaseLogicTickrate);
            if ((transform.position - jumpEndPos).magnitude < 0.1f)
            {
                Debug.Log("장애물 극복 완료");
                _isObstacleJumping = false;
                occupyinngObstacle.isOccupied = false;
                pathFinder.CompleteObstacleJump();
            }
        }
        else
        {
            // 이동 수행
            Debug.Log("그냥 걷기");

            float stepLength = moveSpeed / battleSceneManager.BaseLogicTickrate;
            pathFinder.CalculatePath(moveDest);
            pathFinder.FollowPath(stepLength);
        }
        return BehaviorResult.Running;
    }

    BehaviorResult MoveEnd()
    {
        curActionFrame++;
        if(curActionFrame >= moveEndFrame)
        {
            curActionFrame = 0;
            isMoving = false;
            Debug.Log("MoveEnd");
            return BehaviorResult.Success;
        }
        return BehaviorResult.Running;
    }

    BehaviorResult WaitSkillDone()
    {
        if (isDoingSomeAction)
        {
            Debug.Log("스킬 종료 대기중");
            return BehaviorResult.Running;
        }
        else return BehaviorResult.Success;
    }

    BehaviorResult MoveToNextWave()
    {
        if (currentTarget != null) return BehaviorResult.Success;
        else
        {
            pathFinder.CalculatePath(transform.position + Vector3.forward * 3);
            pathFinder.FollowPath(moveSpeed / battleSceneManager.BaseLogicTickrate);
            Debug.Log("Move to next wave");
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
        if(curActionFrame >= attackDurationFrame)
        {
            Debug.Log("기본 공격 투사체 생성");
            GameObject bulletInstance = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
            Bullet bulletComponent = bulletInstance.GetComponent<Bullet>();
            bulletComponent.Attacker = this;
            bulletComponent.Target = currentTarget;
            bulletComponent.AttackType = AttackType;
            bulletComponent.AttackPower = attackPower;

            battleSceneManager.AddBullet(bulletComponent);

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
        if(curActionFrame >= reloadDurationFrame)
        {
            curActionFrame = 0;
            isDoingSomeAction = false;
            _curAmmo = 15;
            Debug.Log("재장전");
            return BehaviorResult.Success;
        }
        return BehaviorResult.Running;
    }

    BehaviorResult UseExSkill()
    {
        curActionFrame++;
        isDoingSomeAction = true;
        if(curActionFrame >= exSkillDurationFrame)
        {
            curActionFrame = 0;
            isDoingSomeAction = false;
            exSkillTrigger = false;
            Debug.Log("Ex 스킬 사용 종료");
            return BehaviorResult.Success;
        }
        Debug.Log("Ex 스킬 사용 중");
        return BehaviorResult.Running;
    }

    BehaviorResult UseNormalSkill()
    {
        curActionFrame++;
        isDoingSomeAction = true;
        if(curActionFrame >= normalSkillDurationFrame)
        {
            curActionFrame = 0;
            isDoingSomeAction = false;
            normalSkillCondition.ResetSkillCondition();
            Debug.Log("기본 스킬 사용 종료");
            return BehaviorResult.Success;
        }
        Debug.Log("기본 스킬 사용 중");
        return BehaviorResult.Running;
    }


    public void TakeDamage(AttackType attackType, int dmg)
    {
        // TODO: 공격 계산식 수정하기
        float damageMultiplier = AttackEffectiveness.GetEffectiveness(attackType, ArmorType);
        Debug.Log($"특효 배율: {damageMultiplier}");
        currentHP -= Mathf.RoundToInt(dmg * damageMultiplier);
        if(currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // TODO: 후퇴 연출 필요
        isAlive = false;
        battleSceneManager.OnCharacterDie(this);
    }

    public void TakeHeal(int heal)
    {
        currentHP += heal;
        if(currentHP > _maxHP) currentHP = _maxHP;
    }

    private void OnDrawGizmosSelected()
    {
        // 사거리 원
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 이동 목표 위치
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(moveDest, 0.1f);

        // 공격 대상
        if(currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTarget.transform.position, 0.1f);
        }
    }
}
