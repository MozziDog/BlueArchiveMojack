using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Sirenix.OdinInspector;
using UnityEditor.Search;
using UnityEngine.AI;
using System;

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
    public bool isDoingCover = false;
    public float distToCover = 10f;

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
        Sequence moveToNextWave = new Sequence(waitSkillDone, moveToEnemyWave);

        // 교전
            // 기본 스킬
                Conditional canUseNormalSkill = new Conditional(() => { return normalSkillReady; });
                BehaviorAction useNormalSkill = new BehaviorAction(UseNormalSkill);
            BehaviorNode checkAndUseNormalSkill = new Sequence(canUseNormalSkill, useNormalSkill);
            BehaviorNode subTree_NormalSkill = new DecoratorInverter(checkAndUseNormalSkill);

            // 이동
                BehaviorAction move = new BehaviorAction(Move);

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
            Sequence basicAttack = new Sequence(isEnemyCloseEnough, isNotHitEnough, isHaveEnoughBulletInMagazine, attack);
        StatefulSequence combat = new StatefulSequence(subTree_NormalSkill, move, subTree_Reload, basicAttack);

        // 루트
        BehaviorTree tree = new BehaviorTree();
        tree.Root = new Sequence(waitUntilEnemySpawn, moveToNextWave, combat);
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

    BehaviorResult Move()
    {
        distToEnemy = (transform.position - currentTarget.transform.position).magnitude;
        if (distToEnemy < attackRange && !isObstacleJumping) return BehaviorResult.Success;
        else
        {
            NavMeshAgent pathFinder = GetComponent<NavMeshAgent>();
            pathFinder.SetDestination(currentTarget.transform.position);
            if(pathFinder.isOnOffMeshLink)      // 장애물 뛰어넘기
            {
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
            Debug.Log("Move");
            return BehaviorResult.Running;
        }
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
