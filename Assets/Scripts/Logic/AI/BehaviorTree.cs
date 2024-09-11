using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class BehaviorTree
    {
        public BehaviorNode Root;
        public BehaviorResult Behave()
        {
            return Root.Behave();
        }
    }
}
