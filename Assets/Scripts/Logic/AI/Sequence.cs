using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AI
{
    public class Sequence : BehaviorNode
    {
        private List<BehaviorNode> _behaviors = new List<BehaviorNode>();
        private int curChildIdx = 0;        // ���������� ������ child ��ȣ ���

        public Sequence(params BehaviorNode[] behaviors)
        {
            for (int i = 0; i < behaviors.Length; i++)
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
            if (_behaviors == null || _behaviors.Count == 0) { return BehaviorResult.Failure; }
            if(curChildIdx >= _behaviors.Count)
            {
                curChildIdx = 0;
            }

            for (; curChildIdx < _behaviors.Count; curChildIdx++)
            {
                BehaviorResult childResult = _behaviors[curChildIdx].Behave();
                if (childResult == BehaviorResult.Success)
                    continue;
                else
                    return childResult;
            }
            return BehaviorResult.Success;  // �⺻�� Success
        }
    }
}