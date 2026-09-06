using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Lights;

namespace StrideStudio.Mobile
{
    public class EditorGame : Game
    {
        public bool IsPlaying { get; set; } = false;

        private Entity? _cameraEntity;
        private CameraComponent? _camera;
        public Entity? SelectedEntity { get; private set; }
        private RigidbodyComponent? _selectedRb;

        private ScriptEditorUI? _editorUI;
        private readonly List<VisualNode> _graphNodes = new();

        protected override void BeginRun()
        {
            base.BeginRun();

            SetupEditorWorld();
            SetupEditorUI();
        }

        private void SetupEditorUI()
        {
            _editorUI = new ScriptEditorUI(this);

            var uiEntity = new Entity("ScriptingUIOverlay")
            {
                _editorUI.UIComponent
            };
            SceneSystem.SceneInstance.RootScene.Entities.Add(uiEntity);
        }

        private void SetupEditorWorld()
        {
            var scene = new Scene();

            // Camera
            _cameraEntity = new Entity("EditorCamera")
            {
                Transform = { Position = new Vector3(0, 4, 8), Rotation = Quaternion.RotationX(-0.35f) }
            };
            _camera = new CameraComponent { UseViewMatrix = false };
            _cameraEntity.Add(_camera);
            scene.Entities.Add(_cameraEntity);

            // Light
            var lightEntity = new Entity("MainLight")
            {
                Transform = { Position = new Vector3(3, 10, 5), Rotation = Quaternion.RotationYawPitchRoll(0.5f, -0.7f, 0) }
            };
            lightEntity.Add(new LightComponent { Type = new LightDirectional() });
            scene.Entities.Add(lightEntity);

            // Static Ground
            var groundEntity = new Entity("GroundPlane")
            {
                Transform = { Position = new Vector3(0, -0.5f, 0) }
            };
            groundEntity.Add(new ModelComponent
            {
                Model = GeometricPrimitive.Cube.New(GraphicsDevice, new Vector3(14, 0.2f, 14)).ToModel()
            });
            var groundCollider = new StaticColliderComponent();
            groundCollider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = new Vector3(14, 0.2f, 14), IsStatic = true });
            groundEntity.Add(groundCollider);
            scene.Entities.Add(groundEntity);

            // Target Interactive Object
            SelectedEntity = new Entity("InteractiveCube")
            {
                Transform = { Position = new Vector3(0, 2.5f, 0) }
            };
            SelectedEntity.Add(new ModelComponent
            {
                Model = GeometricPrimitive.Cube.New(GraphicsDevice, Vector3.One).ToModel()
            });

            _selectedRb = new RigidbodyComponent
            {
                Mass = 1.0f,
                Restitution = 0.5f,
                Friction = 0.5f
            };
            _selectedRb.ColliderShapes.Add(new BoxColliderShapeDesc { Size = Vector3.One });
            SelectedEntity.Add(_selectedRb);
            scene.Entities.Add(SelectedEntity);

            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
        }

        public void AddDynamicNode(VisualNode node)
        {
            _graphNodes.Add(node);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            float dt = (float)gameTime.Elapsed.TotalSeconds;

            // Touch Camera Navigation sa Edit Mode
            if (!IsPlaying && Input.PointerEvents.Count > 0)
            {
                foreach (var pointer in Input.PointerEvents)
                {
                    // Tiyakin na hindi nasa ibabaw ng top bar bago mag-orbit
                    if (pointer.EventType == PointerEventType.Moved && pointer.Position.Y > 0.15f && _cameraEntity != null)
                    {
                        _cameraEntity.Transform.Rotation *= Quaternion.RotationYawPitchRoll(
                            -pointer.DeltaPosition.X * 3.0f,
                            -pointer.DeltaPosition.Y * 3.0f,
                            0);
                    }
                }
            }

            // Pagpapatakbo ng mga Visual Node scripts kapag naka-Play
            if (IsPlaying && SelectedEntity != null)
            {
                var context = new NodeExecutionContext(SceneSystem.SceneInstance.RootScene, SelectedEntity, Input, dt);
                foreach (var node in _graphNodes)
                {
                    node.Execute(context);
                }
            }
        }
    }
}
