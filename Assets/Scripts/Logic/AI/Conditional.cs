using AI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class Conditional : BehaviorNode
    {
        private Func<bool> condition;

        public Conditional(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override BehaviorResult Behave()
        {
            return condition() ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }

    public class Conditional<T> : BehaviorNode
    {
        private Func<T, bool> condition;
        T arg;

        public Conditional(Func<T, bool> condition, T arg)
        {
            this.condition = condition;
            this.arg = arg;
        }

        public override BehaviorResult Behave()
        {
            return condition(arg) ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }

    public class Conditional<T1, T2> : BehaviorNode
    {
        private Func<T1, T2, bool> condition;
        T1 arg1;
        T2 arg2;

        public Conditional(Func<T1, T2, bool> condition, T1 arg1, T2 arg2)
        {
            this.condition = condition;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }

        public override BehaviorResult Behave()
        {
            return condition(arg1, arg2) ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }
}
