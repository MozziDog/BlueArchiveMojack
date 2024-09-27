using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Sirenix.OdinInspector;
using UnityEditor.Search;
using UnityEngine.AI;

public class BTtest : MonoBehaviour
{
    BehaviorTree bt;
    public GameObject character;
    public GameObject enemy;

    [Title("남은 적 수")]
    public int leftEnemy = 10;

    [Title("일반 스킬 관련")]
    public bool normalSkillReady;
    public bool usingSomeSkill;     // 스킬 사용 중 이동 중지 테스트용

    [Title("엄폐 관련")]
    public bool isDoingCover = false;
    public float distToCover = 10f;

    [Title("후퇴 관련")]
    public int recentHit = 0;

    [Title("교전 관련")]
    public float distToEnemy = 10f;
    public GameObject currentEnemy;
    public float attackRange = 7f;
    public int bulletInMagazine = 15;

    // Start is called before the first frame update
    void Start()
    {
        // 다음 웨이브 스폰까지 기다리기
            Conditional isNoEnemy = new Conditional(() => { return leftEnemy <= 0; });
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
                Conditional needToReload = new Conditional(() => { return bulletInMagazine <= 0; });
                BehaviorAction doReload = new BehaviorAction(Reload);
            BehaviorNode reload = new Sequence(needToReload, doReload);
            BehaviorNode subTree_Reload = new DecoratorInverter(reload);


            // 교전 개시
                Conditional isEnemyCloseEnough = new Conditional(() => { return distToEnemy < attackRange; });
                Conditional isNotHitEnough = new Conditional(()=> { return recentHit < 20; });
                Conditional isHaveEnoughBulletInMagazine = new Conditional(() => { return bulletInMagazine > 0; });
                BehaviorAction attack = new BehaviorAction(Attack);
            Sequence basicAttack = new Sequence(isEnemyCloseEnough, isNotHitEnough, isHaveEnoughBulletInMagazine, attack);
        StatefulSequence combat = new StatefulSequence(subTree_NormalSkill, move, subTree_Reload, basicAttack);

        // 루트
        bt = new BehaviorTree();
        bt.Root = new Sequence(waitUntilEnemySpawn, moveToNextWave, combat);
    }

    // Update is called once per frame
    void Update()
    {
        bt.Behave();
        if (enemy.activeSelf)
            leftEnemy = 1;
        else
            leftEnemy = 0;
    }

    BehaviorResult WaitEnemySpawn()
    {
        // 적이 스폰될 때까지 최대 n초 대기하기
        if(enemy == null || !enemy.activeSelf)
        {
            Debug.Log("적 스폰 대기중");
            return BehaviorResult.Running;
        }
        else
            return BehaviorResult.Success;
    }

    BehaviorResult Move()
    {
        distToEnemy = (character.transform.position - enemy.transform.position).magnitude;
        if (distToEnemy < attackRange) return BehaviorResult.Success;
        else
        {
            character.GetComponent<NavMeshAgent>().SetDestination(enemy.transform.position);
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
        distToEnemy = (character.transform.position - enemy.transform.position).magnitude;
        if (distToEnemy < attackRange) return BehaviorResult.Success;
        else
        {
            character.GetComponent<NavMeshAgent>().SetDestination(enemy.transform.position);
            Debug.Log("Move to next wave");
            return BehaviorResult.Running;
        }
    }

    BehaviorResult Reload()
    {
        Debug.Log("재장전");
        bulletInMagazine = 15;
        return BehaviorResult.Success;
    }

    BehaviorResult UseNormalSkill()
    {
        Debug.Log("기본 스킬 사용");
        normalSkillReady = false;
        return BehaviorResult.Success;
    }

    BehaviorResult Attack()
    {
        if (!enemy.activeSelf) return BehaviorResult.Success;
        if (distToEnemy > attackRange ) return BehaviorResult.Failure;
        if (bulletInMagazine <= 0)
        {
            return BehaviorResult.Failure;
        }

        Debug.Log("Attack");
        bulletInMagazine -= 1;
        return BehaviorResult.Running;
    }
}
