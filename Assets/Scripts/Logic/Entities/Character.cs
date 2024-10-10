using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Sirenix.OdinInspector;
using UnityEditor.Search;
using UnityEngine.AI;
using System;
using VHierarchy.Libs;

[Serializable]
public class Character : MonoBehaviour
{
    BattleSceneManager battleSceneManager;

    BehaviorTree bt;

    public bool isAlive = true;
    [Title("기본 정보")]
    public int maxHP;
    public int currentHP;
    public int attackPower;
    public int defensePower;
    public int healPower;
    public float moveSpeed;
    public float obstacleJumpSpeed;

    [Title("일반 스킬 관련")]
    public bool normalSkillReady;
    public bool usingSomeSkill;     // 스킬 사용 중 이동 중지 테스트용

    [Title("엄폐 관련")]
    public Obstacle coveringObstacle;       // 현재 엄폐를 수행중인 엄폐물
    public bool isDoingCover = false;
    public float distToCover = 10f;

    [Title("이동 관련")]
    public Vector3 moveDest;
    public Obstacle destObstacle;
    public float positioningAttackRangeRatio = 0.88f;       // 이동 위치 선정할 때 최대 사거리 대신 사거리에 이 값을 곱해서 사용

    [Title("교전 관련")]
    public int recentHit = 0;
    public float distToEnemy = 10f;
    public Character currentTarget;
    public float sightRange = 13f;
    public float attackRange = 7f;
    public int maxAmmo = 15;
    public int currentAmmo;

    [Title("프레임 관련 정보")]
    public int moveFrame;
    public int attackFrame;
    public int reloadFrame;

    [Title("플래그")]
    public bool isObstacleJumping;

    public void Init(BattleSceneManager battle, CharacterData charData, CharacterStatData statData)
    {
        battleSceneManager = battle;
        bt = BuildBehaviorTree();

        // 필드 초기화
        attackPower = statData.AttackPowerLevel1;
        defensePower = statData.DefensePowerLevel1;
        healPower = statData.HealPowerLevel1;

        currentAmmo = maxAmmo;
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
                BehaviorAction move = new BehaviorAction(Move);
            BehaviorNode subTree_Move = new StatefulSequence(getNextDest, move);

            // 재장전
                Conditional needToReload = new Conditional(() => { return currentAmmo <= 0; });
                BehaviorAction doReload = new BehaviorAction(Reload);
            BehaviorNode reload = new Sequence(needToReload, doReload);
            BehaviorNode subTree_Reload = new DecoratorInverter(reload);

            // 교전 개시
                Conditional isEnemyCloseEnough = new Conditional(() => { return distToEnemy < attackRange; });
                Conditional isNotHitEnough = new Conditional(()=> { return recentHit < 20; });
                Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return currentAmmo > 0; });
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

    BehaviorResult Move()
    {
        // 엄폐물로 이동중인 경우, 해당 엄폐물이 '점유'되었는지 체크
        if(destObstacle != null)
        {
            if(destObstacle.isOccupied)
            return BehaviorResult.Failure;
        }
        // 엄폐물이 아닌 적을 향해 이동중인 경우 사거리 체크 수행
        else
        {
            distToEnemy = (transform.position - currentTarget.transform.position).magnitude;
            // 장애물 극복 중에는 사거리체크 무시
            if (distToEnemy < attackRange * positioningAttackRangeRatio && !isObstacleJumping)
                return BehaviorResult.Success;
        }

        // NavMeshAgent로 이동 수행
        NavMeshAgent pathFinder = GetComponent<NavMeshAgent>();
        pathFinder.SetDestination(currentTarget.transform.position);
        // 이동 속도 조절을 위해 장애물 뛰어넘기는 수동으로 진행
        if(pathFinder.isOnOffMeshLink )
        {
            // offmeshLink startPoint가 목표 지점일 경우
            // == 마주한 장애물이 '엄폐물'일 경우, 뛰어넘기 스킵
            Vector3 characterPosition = transform.position;
            characterPosition.y = 0;
            if(Vector3.Distance(characterPosition, moveDest) < 0.1f)
            {
                // 엄폐물 '점유'
                coveringObstacle = destObstacle;
                coveringObstacle.isOccupied = true;
                return BehaviorResult.Success;
            }

            isObstacleJumping = true;
            OffMeshLinkData obstacleJump = pathFinder.currentOffMeshLinkData;
            Vector3 yFloat = Vector3.up * transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, obstacleJump.endPos + yFloat, obstacleJumpSpeed / battleSceneManager.logicTickPerSecond);
            if((transform.position - (obstacleJump.endPos + yFloat)).magnitude < 0.1f)
            {
                isObstacleJumping = false;
                pathFinder.CompleteOffMeshLink();
            }
        }
        Debug.Log($"Move, distToDest: {Vector3.Distance(transform.position, moveDest)}");
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
            GetComponent<NavMeshAgent>().SetDestination(transform.position + Vector3.forward * 3);
            Debug.Log("Move to next wave");
            return BehaviorResult.Running;
        }
    }

    BehaviorResult Reload()
    {
        moveFrame++;
        if(moveFrame >= reloadFrame)
        {
            moveFrame = 0;
            currentAmmo = 15;
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
            moveFrame = 0;
            return BehaviorResult.Success;
        }
        if (distToEnemy > attackRange || currentAmmo <= 0)
        {
            moveFrame = 0;
            return BehaviorResult.Failure;
        }
        moveFrame++;
        if(moveFrame >= attackFrame)
        {
            Debug.Log("Attack");
            currentTarget.TakeDamage(attackPower);
            currentAmmo -= 1;
            moveFrame = 0;
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
}
