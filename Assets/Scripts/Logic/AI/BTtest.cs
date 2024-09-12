using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class BTtest : MonoBehaviour
{
    BehaviorTree bt;

    public float distToEnemy = 10f;
    public float attackRange = 7f;
    public bool doCover = false;
    public float distToCover = 10f;
    public int enemyHP = 10;

    // Start is called before the first frame update
    void Start()
    {
        BehaviorAction findEnemy = new BehaviorAction(FindNextEnemy);
        BehaviorAction move = new BehaviorAction(Move);
        BehaviorAction attack = new BehaviorAction(Attack);

        Sequence seq = new Sequence(findEnemy, move, attack);

        bt = new BehaviorTree();
        bt.Root = seq;
    }

    // Update is called once per frame
    void Update()
    {
        bt.Behave();
    }

    BehaviorResult Move()
    {
        if (distToEnemy < attackRange) return BehaviorResult.Success;
        else
        {
            distToEnemy -= 0.1f;
            Debug.Log("Move");
            return BehaviorResult.Running;
        }
    }

    BehaviorResult Attack()
    {
        if (distToEnemy > attackRange ) return BehaviorResult.Failure;
        if(enemyHP <= 0) return BehaviorResult.Success;

        enemyHP -= 1;
        Debug.Log("Attack");
        return BehaviorResult.Running;
    }

    BehaviorResult FindNextEnemy()
    {
        distToEnemy = 15f;
        enemyHP = 10;
        Debug.Log("Find Next Enemy");

        return BehaviorResult.Success;
    }
}
