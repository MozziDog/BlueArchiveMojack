using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class Selector : BehaviorNode
    {
        private List<BehaviorNode> _behaviors = new List<BehaviorNode>();

        public Selector(params BehaviorNode[] behaviors)
        {
            for(int i=0; i<behaviors.Length; i++)
            {
                _behaviors.Add(behaviors[i]);
            }
        }

        public void Add(BehaviorNode behavior)
        {
            _behaviors.Add(behavior);
        }

        public override BehaviorResult Behave()
        {
            if(_behaviors == null || _behaviors.Count == 0) { return BehaviorResult.Failure; }

            for(int i=0; i<_behaviors.Count; i++)
            {
                BehaviorResult childResult = _behaviors[i].Behave();
                if (childResult == BehaviorResult.Failure)
                {
                    continue;
                }
                else
                {
                    return childResult;
                }
            }
            return BehaviorResult.Failure;  // ±âº»°ª Failure
        }
    }
}