using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Lights;

namespace StrideStudio.Mobile
{
    public class EditorGame : Game
    {
        public bool IsPlaying { get; set; } = false;
        private Entity? _selectedEntity;
        private Entity? _cameraEntity;
        private CameraComponent? _camera;
        private readonly List<EditorNode> _nodeGraph = new();

        protected override void BeginRun()
        {
            base.BeginRun();
            SetupEditorScene();
            SetupNodeGraphDemo();
        }

        private void SetupEditorScene()
        {
            var scene = new Scene();

            // 1. Setup ng 3D Camera para sa Viewport
            _cameraEntity = new Entity("EditorCamera")
            {
                Transform = { Position = new Vector3(0, 3, 7), Rotation = Quaternion.RotationX(-0.3f) }
            };
            _camera = new CameraComponent { UseViewMatrix = false };
            _cameraEntity.Add(_camera);
            scene.Entities.Add(_cameraEntity);

            // 2. Setup ng Directional Light
            var lightEntity = new Entity("MainLight")
            {
                Transform = { Position = new Vector3(2, 10, 5), Rotation = Quaternion.RotationYawPitchRoll(0.4f, -0.8f, 0) }
            };
            var light = new LightComponent { Type = new LightDirectional() };
            lightEntity.Add(light);
            scene.Entities.Add(lightEntity);

            // 3. Setup ng Default 3D Cube (Preview Object)
            _selectedEntity = new Entity("PreviewCube")
            {
                Transform = { Position = new Vector3(0, 0.5f, 0) }
            };
            
            var modelComponent = new ModelComponent
            {
                Model = GeometricPrimitive.Cube.New(GraphicsDevice).ToModel()
            };
            _selectedEntity.Add(modelComponent);
            scene.Entities.Add(_selectedEntity);

            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
        }

        private void SetupNodeGraphDemo()
        {
            // Halimbawa ng Visual Node Script:
            // Kapag nag-tap sa screen habang naka-PLAY -> Paikutin ang Napiling Entity
            var touchEventNode = new OnTouchNode();
            var rotateActionNode = new RotateEntityActionNode();

            touchEventNode.ConnectedActions.Add(rotateActionNode);
            _nodeGraph.Add(touchEventNode);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            float dt = (float)gameTime.Elapsed.TotalSeconds;

            // --- TOUCH CONTROLS PARA SA ANDROID ---
            if (Input.PointerEvents.Count > 0)
            {
                foreach (var pointer in Input.PointerEvents)
                {
                    if (pointer.EventType == PointerEventType.Moved && !IsPlaying)
                    {
                        // Orbit Camera sa Edit Mode kapag nag-drag ang daliri
                        if (_cameraEntity != null)
                        {
                            _cameraEntity.Transform.Rotation *= Quaternion.RotationYawPitchRoll(-pointer.DeltaPosition.X * 2.5f, -pointer.DeltaPosition.Y * 2.5f, 0);
                        }
                    }
                    else if (pointer.EventType == PointerEventType.Pressed)
                    {
                        // 2-Finger Tap: Toggle Play / Edit Mode
                        if (Input.PointerEvents.Count >= 2)
                        {
                            IsPlaying = !IsPlaying;
                        }

                        // Sa Play Mode: I-trigger ang Node Scripts
                        if (IsPlaying)
                        {
                            ExecuteNodes(dt);
                        }
                    }
                }
            }

            // Kapag naka-Play mode, paandarin ang simulation
            if (IsPlaying && _selectedEntity != null)
            {
                _selectedEntity.Transform.Rotation *= Quaternion.RotationY(1.0f * dt);
            }
        }

        private void ExecuteNodes(float dt)
        {
            if (_selectedEntity == null) return;

            foreach (var node in _nodeGraph)
            {
                node.Execute(_selectedEntity, dt);
            }
        }
    }

    // ==========================================
    // VISUAL NODE SCRIPTING SYSTEM ARCHITECTURE
    // ==========================================
    public abstract class EditorNode
    {
        public string Title { get; set; } = "Node";
        public List<EditorNode> ConnectedActions { get; } = new();
        public abstract void Execute(Entity target, float dt);
    }

    public class OnTouchNode : EditorNode
    {
        public OnTouchNode() => Title = "On Screen Touch";

        public override void Execute(Entity target, float dt)
        {
            foreach (var action in ConnectedActions)
            {
                action.Execute(target, dt);
            }
        }
    }

    public class RotateEntityActionNode : EditorNode
    {
        public RotateEntityActionNode() => Title = "Rotate Entity";

        public override void Execute(Entity target, float dt)
        {
            target.Transform.Rotation *= Quaternion.RotationZ(0.5f);
        }
    }
}
