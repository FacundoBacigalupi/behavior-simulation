using System;

namespace BehaviorSimulation.DecisionAI
{
    public enum BTStatus { Success, Failure, Running }

    public abstract class BTNode
    {
        public abstract BTStatus Tick();
    }

    // Returns Success on the first child that doesn't fail; Failure if all fail.
    public sealed class Selector : BTNode
    {
        readonly BTNode[] _children;
        public Selector(params BTNode[] children) => _children = children;

        public override BTStatus Tick()
        {
            foreach (var c in _children)
            {
                var s = c.Tick();
                if (s != BTStatus.Failure) return s;
            }
            return BTStatus.Failure;
        }
    }

    // Returns Failure on the first child that doesn't succeed; Success when all succeed.
    public sealed class Sequence : BTNode
    {
        readonly BTNode[] _children;
        public Sequence(params BTNode[] children) => _children = children;

        public override BTStatus Tick()
        {
            foreach (var c in _children)
            {
                var s = c.Tick();
                if (s != BTStatus.Success) return s;
            }
            return BTStatus.Success;
        }
    }

    // Leaf delegates to a lambda — captures the agent's local fields.
    public sealed class Leaf : BTNode
    {
        readonly Func<BTStatus> _fn;
        public Leaf(Func<BTStatus> fn) => _fn = fn;
        public override BTStatus Tick() => _fn();
    }
}
