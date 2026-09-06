using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace StrideStudio.Mobile
{
    // Impormasyon tungkol sa script para sa UI
    public class ScriptMetadata
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public Func<ScriptComponent> Factory { get; set; } = null!;
    }

    public static class ScriptRegistry
    {
        public static List<ScriptMetadata> AvailableScripts { get; } = new()
        {
            new ScriptMetadata
            {
                Name = "Auto-Rotate Script",
                Description = "Pina-iikot ang entity bawat frame.",
                Factory = () => new AutoRotateScript()
            },
            new ScriptMetadata
            {
                Name = "Touch Jump Script",
                Description = "Tumatalon gamit ang 3D Physics kapag may tap.",
                Factory = () => new TouchJumpPhysicsScript()
            },
            new ScriptMetadata
            {
                Name = "Hover Float Script",
                Description = "Palutang-lutang na movement gamit ang Sin wave.",
                Factory = () => new HoverFloatScript()
            },
            new ScriptMetadata
            {
                Name = "Respawn If Fell",
                Description = "Ibabalik sa taas kapag nahulog sa bangin.",
                Factory = () => new FallRespawnScript()
            }
        };

        // Ikinakabit ang script sa napiling Entity
        public static void AttachScript(Entity target, ScriptMetadata metadata)
        {
            var scriptInstance = metadata.Factory();
            target.Add(scriptInstance);
        }
    }

    // =========================================================================
    // BUILT-IN STRIDE SCRIPTS NA PUWENG I-ADD NG USER
    // =========================================================================

    public class AutoRotateScript : SyncScript
    {
        public Vector3 Speed { get; set; } = new(0, 1.5f, 0);

        public override void Update()
        {
            float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Rotation *= Quaternion.RotationYawPitchRoll(Speed.Y * dt, Speed.X * dt, Speed.Z * dt);
        }
    }

    public class TouchJumpPhysicsScript : SyncScript
    {
        public override void Update()
        {
            if (Input.PointerEvents.Count > 0)
            {
                foreach (var p in Input.PointerEvents)
                {
                    if (p.EventType == Stride.Input.PointerEventType.Pressed)
                    {
                        var rb = Entity.Get<RigidbodyComponent>();
                        if (rb != null)
                        {
                            rb.Activate();
                            rb.ApplyImpulse(new Vector3(0, 6.0f, 0));
                        }
                    }
                }
            }
        }
    }

    public class HoverFloatScript : SyncScript
    {
        private float _timeAccumulator = 0;
        private float _startY;

        public override void Start()
        {
            _startY = Entity.Transform.Position.Y;
        }

        public override void Update()
        {
            _timeAccumulator += (float)Game.UpdateTime.Elapsed.TotalSeconds * 3.0f;
            var pos = Entity.Transform.Position;
            pos.Y = _startY + (float)Math.Sin(_timeAccumulator) * 0.4f;
            Entity.Transform.Position = pos;
        }
    }

    public class FallRespawnScript : SyncScript
    {
        public override void Update()
        {
            if (Entity.Transform.Position.Y < -8.0f)
            {
                Entity.Transform.Position = new Vector3(0, 4.0f, 0);
                var rb = Entity.Get<RigidbodyComponent>();
                if (rb != null)
                {
                    rb.LinearVelocity = Vector3.Zero;
                    rb.AngularVelocity = Vector3.Zero;
                }
            }
        }
    }
}
