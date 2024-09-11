using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AI;

public class BTtest : MonoBehaviour
{
    BehaviorTree bt;

    public bool hiRunning = true;
    public bool byeRunning = true;

    // Start is called before the first frame update
    void Start()
    {
        BehaviorAction action1 = new BehaviorAction(sayHi);
        BehaviorAction action2 = new BehaviorAction(sayBye);

        Sequence seq = new Sequence(action1, action2);

        bt = new BehaviorTree();
        bt.Root = seq;
    }

    // Update is called once per frame
    void Update()
    {
        bt.Behave();
    }

    BehaviorResult sayHi()
    {
        if (!hiRunning) return BehaviorResult.Success;

        Debug.Log("Hi");
        return BehaviorResult.Running;
    }

    BehaviorResult sayBye()
    {
        if (!byeRunning) return BehaviorResult.Success;

        Debug.Log("Bye");
        return BehaviorResult.Running;
    }
}
