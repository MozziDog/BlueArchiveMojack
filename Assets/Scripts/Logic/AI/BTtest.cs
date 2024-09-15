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

        // 기본 스킬
        Conditional canUseNormalSkill = new Conditional(() => { return normalSkillReady; });
        BehaviorAction useNormalSkill = new BehaviorAction(UseNormalSkill);
        BehaviorNode checkAndUseNormalSkill = new Sequence(canUseNormalSkill, useNormalSkill);

        // 교전 개시
        BehaviorAction findEnemy = new BehaviorAction(FindNextEnemy);
        BehaviorAction move = new BehaviorAction(Move);
        BehaviorAction coverUp = new BehaviorAction(TryCoverUp);
        BehaviorAction pullBack = new BehaviorAction(PullBack);
        BehaviorAction reload = new BehaviorAction(Reload);
        BehaviorAction attack = new BehaviorAction(Attack);
        Sequence basicCombat = new Sequence(findEnemy, move, coverUp, pullBack, reload, attack);

        // 기본 스킬 + 기본 교전
        Selector combat = new Selector(checkAndUseNormalSkill, basicCombat);

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

    BehaviorResult TryCoverUp()
    {
        if (isDoingCover)
            return BehaviorResult.Success;

        if (distToCover > 5.0f)
            return BehaviorResult.Success;
        else
        {
            if(distToCover > 0.1f)
            {
                Debug.Log("엄폐물로 이동");
                return BehaviorResult.Running;
            }
            else
            {
                Debug.Log("엄폐 성공");
                return BehaviorResult.Success;
            }
        }
    }

    BehaviorResult PullBack()
    {
        if (recentHit > 10)
        {
            Debug.Log("후퇴");
            return BehaviorResult.Success;
        }
        else
            return BehaviorResult.Success;
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

    BehaviorResult FindNextEnemy()
    {
        if(currentEnemy == null || !currentEnemy.activeSelf)
        {
            Debug.Log("Find Next Enemy");
            currentEnemy = enemy;
        }
        return BehaviorResult.Success;
    }
}
