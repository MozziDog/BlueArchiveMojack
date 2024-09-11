using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class BehaviorAction : BehaviorNode
    {
        private Func<BehaviorResult> action;

        public override BehaviorResult Behave()
        {
            return action.Invoke();
        }

        public BehaviorAction() { }

        public BehaviorAction(Func<BehaviorResult> action)
        {
            this.action = action;
        }
    }

    public class BehaviorAction<T> : BehaviorNode
    {
        private Func<T, BehaviorResult> action;
        private T arg;

        public override BehaviorResult Behave()
        {
            return action.Invoke(arg);
        }

        public BehaviorAction(Func<T, BehaviorResult> action, T arg)
        {
            this.action = action;
            this.arg = arg;
        }
    }

    public class BehaviorAction<T1, T2> : BehaviorNode
    {
        private Func<T1, T2, BehaviorResult> action;
        private T1 arg1;
        private T2 arg2;

        public override BehaviorResult Behave()
        {
            return action.Invoke(arg1, arg2);
        }

        public BehaviorAction(Func<T1, T2, BehaviorResult> action, T1 arg1, T2 arg2)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class BehaviorAction<T1, T2, T3> : BehaviorNode
    {
        private Func<T1, T2, T3, BehaviorResult> action;
        private T1 arg1;
        private T2 arg2;
        private T3 arg3;

        public override BehaviorResult Behave()
        {
            return action.Invoke(arg1 , arg2 , arg3 );
        }

        public BehaviorAction(Func<T1, T2, T3, BehaviorResult> action, T1 arg1, T2 arg2, T3 arg3)
        {
            this.action = action;
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }
    }
}