namespace AI
{
    public class DecoratorMute : BehaviorNode
    {
        BehaviorNode _behavior;

        public DecoratorMute(BehaviorNode behavior)
        {
            _behavior = behavior;
        }

        public override BehaviorResult Behave()
        {
            BehaviorResult result = _behavior.Behave();
            switch(result)
            {
                case BehaviorResult.Running:
                    return BehaviorResult.Running;
                default:
                    return BehaviorResult.Success;
            }
        }
    }
}