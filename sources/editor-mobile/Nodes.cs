using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace StrideStudio.Mobile.Nodes
{
    public class NodeGraphContext
    {
        public Entity Target { get; set; } = default!;
        public float DeltaTime { get; set; }
        public Dictionary<string, object> Variables { get; } = new();
    }

    public abstract class StudioNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Node";
        public Vector2 CanvasPosition { get; set; } // Position sa UI Canvas
        public List<StudioNode> Outputs { get; } = new();

        public abstract void Execute(NodeGraphContext ctx);

        protected void ExecuteOutputs(NodeGraphContext ctx)
        {
            foreach (var node in Outputs) node.Execute(ctx);
        }
    }

    // 1. Tick Event Node
    public class UpdateTickNode : StudioNode
    {
        public UpdateTickNode() => Title = "On Update";
        public override void Execute(NodeGraphContext ctx) => ExecuteOutputs(ctx);
    }

    // 2. Physics Jump Node
    public class PhysicsImpulseNode : StudioNode
    {
        public Vector3 Impulse { get; set; } = new Vector3(0, 7f, 0);
        public PhysicsImpulseNode() => Title = "Physics: Jump/Impulse";

        public override void Execute(NodeGraphContext ctx)
        {
            var rb = ctx.Target.Get<RigidbodyComponent>();
            if (rb != null)
            {
                rb.Activate();
                rb.ApplyImpulse(Impulse);
            }
            ExecuteOutputs(ctx);
        }
    }

    // 3. Continuous Rotation Node
    public class RotateNode : StudioNode
    {
        public Vector3 Axis { get; set; } = Vector3.UnitY;
        public float Speed { get; set; } = 3.0f;
        public RotateNode() => Title = "Transform: Rotate";

        public override void Execute(NodeGraphContext ctx)
        {
            ctx.Target.Transform.Rotation *= Quaternion.RotationAxis(Axis, Speed * ctx.DeltaTime);
            ExecuteOutputs(ctx);
        }
    }
}
