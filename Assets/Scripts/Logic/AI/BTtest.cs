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

    [Title("���� �� ��")]
    public int leftEnemy = 10;

    [Title("�Ϲ� ��ų ����")]
    public bool normalSkillReady;
    public bool usingSomeSkill;     // ��ų ��� �� �̵� ���� �׽�Ʈ��

    [Title("���� ����")]
    public bool isDoingCover = false;
    public float distToCover = 10f;

    [Title("���� ����")]
    public int recentHit = 0;

    [Title("���� ����")]
    public float distToEnemy = 10f;
    public GameObject currentEnemy;
    public float attackRange = 7f;
    public int bulletInMagazine = 15;

    // Start is called before the first frame update
    void Start()
    {
        // ���� ���̺� �������� ��ٸ���
        Conditional isNoEnemy = new Conditional(() => { return leftEnemy <= 0; });
        BehaviorAction waitEnemySpawn = new BehaviorAction(WaitEnemySpawn);
        BehaviorNode waitUntilEnemySpawn = new DecoratorInverter(new Sequence(isNoEnemy, waitEnemySpawn));

        // ���� ���̺���� �̵�
        BehaviorAction waitSkillDone = new BehaviorAction(WaitSkillDone);
        BehaviorAction moveToEnemyWave = new BehaviorAction(MoveToNextWave);
        Sequence moveToNextWave = new Sequence(waitSkillDone, moveToEnemyWave);

        // �⺻ ��ų
        Conditional canUseNormalSkill = new Conditional(() => { return normalSkillReady; });
        BehaviorAction useNormalSkill = new BehaviorAction(UseNormalSkill);
        BehaviorNode checkAndUseNormalSkill = new Sequence(canUseNormalSkill, useNormalSkill);

        // ���� ����
        BehaviorAction findEnemy = new BehaviorAction(FindNextEnemy);
        BehaviorAction move = new BehaviorAction(Move);
        BehaviorAction coverUp = new BehaviorAction(TryCoverUp);
        BehaviorAction pullBack = new BehaviorAction(PullBack);
        BehaviorAction reload = new BehaviorAction(Reload);
        BehaviorAction attack = new BehaviorAction(Attack);
        Sequence basicCombat = new Sequence(findEnemy, move, coverUp, pullBack, reload, attack);

        // �⺻ ��ų + �⺻ ����
        Selector combat = new Selector(checkAndUseNormalSkill, basicCombat);

        // ��Ʈ
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
        // ���� ������ ������ �ִ� n�� ����ϱ�
        if(enemy == null || !enemy.activeSelf)
        {
            Debug.Log("�� ���� �����");
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
            Debug.Log("��ų ���� �����");
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
                Debug.Log("���󹰷� �̵�");
                return BehaviorResult.Running;
            }
            else
            {
                Debug.Log("���� ����");
                return BehaviorResult.Success;
            }
        }
    }

    BehaviorResult PullBack()
    {
        if (recentHit > 10)
        {
            Debug.Log("����");
            return BehaviorResult.Success;
        }
        else
            return BehaviorResult.Success;
    }

    BehaviorResult Reload()
    {
        Debug.Log("������");
        bulletInMagazine = 15;
        return BehaviorResult.Success;
    }

    BehaviorResult UseNormalSkill()
    {
        Debug.Log("�⺻ ��ų ���");
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
