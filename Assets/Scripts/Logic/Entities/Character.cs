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
    public bool isAlive = true;

    [Title("기본 스탯 정보")]
    public int maxHP;
    public int currentHP;
    public int attackPower;
    public int defensePower;
    public int healPower;
    public float moveSpeed;
    public float obstacleJumpSpeed;

    [Title("엄폐 관련")]
    public Obstacle coveringObstacle;       // 현재 엄폐를 수행중인 엄폐물
    public bool isDoingCover = false;
    private Obstacle occupyinngObstacle;           // 현재 점유 중인 장애물

    [Title("이동 관련")]
    public int moveStartFrame = 0;
    public int moveEndFrame = 13;
    public Vector3 moveDest;
    public Obstacle destObstacle;
    public float sightRange = 13f;
    public float attackRange = 7f;
    public float distToEnemy = 10f;
    public float positioningAttackRangeRatio = 0.88f;       // 이동 위치 선정할 때 최대 사거리 대신 사거리에 이 값을 곱해서 사용
    public int recentHit = 0;
    public bool isObstacleJumping;

    [Title("전투 관련")]
    public Character currentTarget;
    public int maxAmmo = 15;
    public int curAmmo;
    public bool normalSkillReady;
    public bool usingSomeSkill;     // 스킬 사용 중 이동 중지 테스트용

    [Title("행동 관련 프레임 정보")]
    [SerializeField, ReadOnly] int curActionFrame;
    public int attackDurationFrame;
    public int reloadDurationFrame;

    [Title("컴포넌트 레퍼런스")]
    public PathFinder pathFinder;



    BattleSceneManager battleSceneManager;
    BehaviorTree bt;

    // 플래그
    public bool isMoving { get; private set;}


    public void Init(BattleSceneManager battle, CharacterData charData, CharacterStatData statData)
    {
        battleSceneManager = battle;
        pathFinder = GetComponent<PathFinder>();
        bt = BuildBehaviorTree();

        // 필드 초기화
        attackPower = statData.AttackPowerLevel1;
        defensePower = statData.DefensePowerLevel1;
        healPower = statData.HealPowerLevel1;

        curAmmo = maxAmmo;
        currentHP = maxHP;
    }

    protected BehaviorTree BuildBehaviorTree()
    {
        // 다음 웨이브 스폰까지 기다리기
            Conditional isNoEnemy = new Conditional(() => { return battleSceneManager.activeEnemies.Count <= 0; });
            BehaviorAction waitEnemySpawn = new BehaviorAction(WaitEnemySpawn);
        BehaviorNode waitUntilEnemySpawn = new DecoratorInverter(new Sequence(isNoEnemy, waitEnemySpawn));

        // 다음 웨이브까지 이동
            BehaviorAction waitSkillDone = new BehaviorAction(WaitSkillDone);
            BehaviorAction moveToEnemyWave = new BehaviorAction(MoveToNextWave);
        BehaviorNode moveToNextWave = new StatefulSequence(waitSkillDone, moveToEnemyWave);

        // 교전
            // 기본 스킬
                Conditional canUseNormalSkill = new Conditional(() => { return normalSkillReady; });
                BehaviorAction useNormalSkill = new BehaviorAction(UseNormalSkill);
            BehaviorNode checkAndUseNormalSkill = new StatefulSequence(canUseNormalSkill, useNormalSkill);
            BehaviorNode subTree_NormalSkill = new DecoratorInverter(checkAndUseNormalSkill);

            // 이동
                BehaviorAction getNextDest = new BehaviorAction(GetNextDest);
                BehaviorAction moveStart = new BehaviorAction(MoveStart);
                BehaviorAction moveIng = new BehaviorAction(MoveIng);
                BehaviorAction moveEnd = new BehaviorAction(MoveEnd);
            BehaviorNode subTree_Move = new StatefulSequence(getNextDest, moveStart, moveIng, moveEnd);

            // 재장전
                Conditional needToReload = new Conditional(() => { return curAmmo <= 0; });
                BehaviorAction doReload = new BehaviorAction(Reload);
            BehaviorNode reload = new Sequence(needToReload, doReload);
            BehaviorNode subTree_Reload = new DecoratorInverter(reload);

            // 교전 개시
                Conditional isEnemyCloseEnough = new Conditional(() => { return distToEnemy < attackRange; });
                Conditional isNotHitEnough = new Conditional(()=> { return recentHit < 20; });
                Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return curAmmo > 0; });
                BehaviorAction attack = new BehaviorAction(Attack);
            BehaviorNode basicAttack = new Sequence(isEnemyCloseEnough, isNotHitEnough, isHaveEnoughBulletInMagazine, attack);
        StatefulSequence combat = new StatefulSequence(subTree_NormalSkill, subTree_Move, subTree_Reload, basicAttack);

        // 루트
        BehaviorTree tree = new BehaviorTree();
        tree.Root = new StatefulSequence(waitUntilEnemySpawn, moveToNextWave, combat);
        return tree;
    }

    // Update is called once per frame
    public void Tick()
    {
        if(currentTarget == null || !currentTarget.isAlive)
        {
            FindNextEnemy();
        }
        bt.Behave();
    }

    public void FindNextEnemy()
    {
        float minDist = float.MaxValue;
        foreach(var enemy in battleSceneManager.activeEnemies)
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
        foreach(var ob in battleSceneManager.obstacles)
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

    BehaviorResult MoveIng()
    {
        // MoveIng 종료 조건 판단
        if(!isObstacleJumping)
        {
            distToEnemy = (transform.position - currentTarget.transform.position).magnitude;
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
        if(!isObstacleJumping && pathFinder.isOnOffMeshLink)
        {
            isObstacleJumping = true;
            // 뛰어넘는 중에는 다른 캐릭터가 엄폐물 뒤에서 기다리는 상황을 방지하기 위해 OffMeshLink 비활성화
            occupyinngObstacle = pathFinder.GetOccupyingObstacle();
            occupyinngObstacle.isOccupied = true;
        }

        // 이동 속도 조절을 위해 장애물 뛰어넘기는 수동으로 진행
        if(isObstacleJumping)
        {
            Debug.Log("장애물 극복 중");
            Vector3 jumpEndPos = pathFinder.GetObstacleJumpEndPos();
            transform.position = Vector3.MoveTowards(transform.position, jumpEndPos, obstacleJumpSpeed / battleSceneManager.logicTickPerSecond);
            if ((transform.position - jumpEndPos).magnitude < 0.1f)
            {
                Debug.Log("장애물 극복 완료");
                isObstacleJumping = false;
                occupyinngObstacle.isOccupied = false;
                pathFinder.CompleteObstacleJump();
            }
        }
        else
        {
            // 이동 수행
            Debug.Log("그냥 걷기");

            float stepLength = moveSpeed / 30;
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
        if (usingSomeSkill)
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
            pathFinder.FollowPath(moveSpeed / 30);
            Debug.Log("Move to next wave");
            return BehaviorResult.Running;
        }
    }

    BehaviorResult Reload()
    {
        curActionFrame++;
        if(curActionFrame >= reloadDurationFrame)
        {
            curActionFrame = 0;
            curAmmo = 15;
            Debug.Log("재장전");
            return BehaviorResult.Success;
        }
        return BehaviorResult.Running;
    }

    BehaviorResult UseNormalSkill()
    {
        Debug.Log("기본 스킬 사용");
        normalSkillReady = false;
        return BehaviorResult.Success;
    }

    BehaviorResult Attack()
    {
        if (currentTarget == null || !currentTarget.isAlive)
        {
            curActionFrame = 0;
            return BehaviorResult.Success;
        }
        if (distToEnemy > attackRange || curAmmo <= 0)
        {
            curActionFrame = 0;
            return BehaviorResult.Failure;
        }
        curActionFrame++;
        if(curActionFrame >= attackDurationFrame)
        {
            Debug.Log("Attack");
            currentTarget.TakeDamage(attackPower);
            curAmmo -= 1;
            curActionFrame = 0;
        }
        return BehaviorResult.Running;
    }

    public void TakeDamage(int dmg)
    {
        // TODO: 공격 계산식 수정하기
        currentHP -= (dmg - defensePower);
        if(currentHP <= 0)
        {
            isAlive = false;
            // TODO: 후퇴 연출 필요
        }
    }

    public void TakeHeal(int heal)
    {
        currentHP += heal;
        if(currentHP > maxHP) currentHP = maxHP;
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
