            using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using Stride.UI;
using StrideStudio.Mobile.Nodes;
using StrideStudio.Mobile.Scripting;
using StrideStudio.Mobile.UI;

namespace StrideStudio.Mobile
{
    public class EditorGame : Game
    {
        public bool IsPlaying { get; private set; }

        private Scene _scene = null!;
        private Entity _cameraEntity = null!;
        private CameraComponent _camera = null!;
        private Entity? _selectedEntity;

        // Viewport Control Values
        private float _cameraDistance = 12.0f;
        private Vector2 _cameraAngles = new(0.4f, 0.6f);

        // Snapshot System para sa Edit/Play Mode Restore
        private Vector3 _origPos;
        private Quaternion _origRot;

        // Visual Scripting Data
        private readonly List<StudioNode> _nodeGraph = new();
        private readonly NodeGraphContext _graphContext = new();

        // UI Manager
        private EditorUIManager _uiManager = null!;

        protected override void BeginRun()
        {
            base.BeginRun();

            SetupWorldScene();
            SetupEditorUI();
            SetupDefaultGraph();
        }

        private void SetupWorldScene()
        {
            _scene = new Scene();

            // 1. Viewport Camera
            _cameraEntity = new Entity("ViewportCamera");
            _camera = new CameraComponent { VerticalFieldOfView = 55f };
            _cameraEntity.Add(_camera);
            UpdateCameraTransform();
            _scene.Entities.Add(_cameraEntity);

            // 2. Infinite Ground Grid Floor
            var floor = new Entity("FloorGrid") { Transform = { Position = new Vector3(0, -0.5f, 0) } };
            var floorMeshDraw = GeometricPrimitive.Cube.New(GraphicsDevice, new Vector3(50, 1, 50)).ToMeshDraw();
            floor.Add(new ModelComponent { Model = new Model { new Mesh { Draw = floorMeshDraw } } });
            
            var floorCollider = new StaticColliderComponent();
            floorCollider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = new Vector3(50, 1, 50) });
            floor.Add(floorCollider);
            _scene.Entities.Add(floor);

            // 3. Directional Sunlight
            var sun = new Entity("Sun") { Transform = { Position = new Vector3(10, 20, 10), Rotation = Quaternion.RotationYawPitchRoll(0.5f, -0.8f, 0) } };
            sun.Add(new LightComponent { Type = new LightDirectional { Color = new ColorRgbProvider(new Color(1f, 1f, 1f)), Intensity = 2.5f } });
            _scene.Entities.Add(sun);

            // 4. Default Interactive Cube (Target Object)
            _selectedEntity = new Entity("InteractiveCube") { Transform = { Position = new Vector3(0, 3, 0) } };
            var cubeMeshDraw = GeometricPrimitive.Cube.New(GraphicsDevice, Vector3.One).ToMeshDraw();
            _selectedEntity.Add(new ModelComponent { Model = new Model { new Mesh { Draw = cubeMeshDraw } } });

            var rb = new RigidbodyComponent
            {
                Mass = 1.0f,
                Restitution = 0.5f,
                IsKinematic = true,
                ColliderShapes = { new BoxColliderShapeDesc { Size = Vector3.One } }
            };
            _selectedEntity.Add(rb);
            _scene.Entities.Add(_selectedEntity);

            _origPos = _selectedEntity.Transform.Position;
            _origRot = _selectedEntity.Transform.Rotation;

            SceneSystem.SceneInstance = new SceneInstance(Services, _scene);
        }

        private void SetupEditorUI()
        {
            SpriteFont? font = null;
            try
            {
                font = Content.Load<SpriteFont>("StrideDefaultFont");
            }
            catch
            {
                font = null;
            }

            _uiManager = new EditorUIManager(font);

            _uiManager.OnPlayClicked += StartSimulation;
            _uiManager.OnStopClicked += StopSimulation;
            _uiManager.OnCompileCodeClicked += HandleCodeCompilation;

            var uiEntity = new Entity("EditorUI");
            var uiComp = new UIComponent { Page = _uiManager.Page, IsFullScreen = true };
            uiEntity.Add(uiComp);
            _scene.Entities.Add(uiEntity);
        }

        private void SetupDefaultGraph()
        {
            var tick = new UpdateTickNode();
            var rotate = new RotateNode { Speed = 2.5f };
            tick.Outputs.Add(rotate);
            _nodeGraph.Add(tick);
        }

        public void StartSimulation()
        {
            if (IsPlaying) return;
            IsPlaying = true;

            _uiManager.StatusText.Text = "Mode: SIMULATION (PLAYING)";

            if (_selectedEntity != null)
            {
                _origPos = _selectedEntity.Transform.Position;
                _origRot = _selectedEntity.Transform.Rotation;

                var rb = _selectedEntity.Get<RigidbodyComponent>();
                if (rb != null)
                {
                    rb.IsKinematic = false;
                    rb.Activate();
                }
            }
        }

        public void StopSimulation()
        {
            if (!IsPlaying) return;
            IsPlaying = false;

            _uiManager.StatusText.Text = "Mode: EDITING";

            if (_selectedEntity != null)
            {
                var rb = _selectedEntity.Get<RigidbodyComponent>();
                if (rb != null)
                {
                    rb.LinearVelocity = Vector3.Zero;
                    rb.AngularVelocity = Vector3.Zero;
                    rb.IsKinematic = true;
                }

                _selectedEntity.Transform.Position = _origPos;
                _selectedEntity.Transform.Rotation = _origRot;
            }
        }

        private void HandleCodeCompilation(string code)
        {
            if (_selectedEntity == null) return;

            var (success, scriptType, errors) = RuntimeScriptCompiler.CompileCSharpScript(code, "RotatorScript");
            if (success && scriptType != null)
            {
                var scriptInstance = (ScriptComponent)Activator.CreateInstance(scriptType)!;
                _selectedEntity.Add(scriptInstance);
                _uiManager.StatusText.Text = "Build Success: Script Attached!";
                _uiManager.CodeEditorPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                _uiManager.StatusText.Text = $"Build Error:\n{errors}";
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            float dt = (float)gameTime.Elapsed.TotalSeconds;

            HandleViewportGestures();

            if (IsPlaying && _selectedEntity != null)
            {
                _graphContext.Target = _selectedEntity;
                _graphContext.DeltaTime = dt;

                foreach (var rootNode in _nodeGraph)
                {
                    rootNode.Execute(_graphContext);
                }
            }
        }

        private void HandleViewportGestures()
        {
            if (_uiManager.CodeEditorPanel.Visibility == Visibility.Visible) return;

            // Pinch Zoom
            if (Input.PointerEvents.Count >= 2)
            {
                var p1 = Input.PointerEvents[0];
                var p2 = Input.PointerEvents[1];

                if (p1.EventType == PointerEventType.Moved && p2.EventType == PointerEventType.Moved)
                {
                    float prevDist = Vector2.Distance(p1.AbsolutePosition - p1.DeltaPosition, p2.AbsolutePosition - p2.DeltaPosition);
                    float currentDist = Vector2.Distance(p1.AbsolutePosition, p2.AbsolutePosition);
                    float deltaDist = currentDist - prevDist;

                    _cameraDistance = MathUtil.Clamp(_cameraDistance - (deltaDist * 15.0f), 2.0f, 40.0f);
                    UpdateCameraTransform();
                }
                return;
            }

            // Orbit Drag
            if (Input.PointerEvents.Count == 1 && !IsPlaying)
            {
                var p = Input.PointerEvents[0];
                if (p.EventType == PointerEventType.Moved)
                {
                    _cameraAngles.X += -p.DeltaPosition.X * 4.0f;
                    _cameraAngles.Y = MathUtil.Clamp(_cameraAngles.Y + (-p.DeltaPosition.Y * 4.0f), -1.4f, 1.4f);
                    UpdateCameraTransform();
                }
                else if (p.EventType == PointerEventType.Pressed)
                {
                    PerformObjectPicking(p.AbsolutePosition);
                }
            }
        }

        private void UpdateCameraTransform()
        {
            var rotation = Quaternion.RotationYawPitchRoll(_cameraAngles.X, _cameraAngles.Y, 0);
            var offset = Vector3.Transform(new Vector3(0, 0, _cameraDistance), rotation);
            _cameraEntity.Transform.Position = offset;
            _cameraEntity.Transform.Rotation = rotation;
        }

        private void PerformObjectPicking(Vector2 screenPos)
        {
            var sim = SceneSystem.SceneInstance?.GetProcessor<PhysicsProcessor>()?.Simulation;
            if (sim == null) return;

            // Stride Camera Raycast Unprojection
            Matrix invViewProj = Matrix.Invert(_camera.ViewProjectionMatrix);
            Vector3 sPos = new Vector3(screenPos.X * 2f - 1f, 1f - screenPos.Y * 2f, 0f);
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            var hit = sim.Raycast(vectorNear.XYZ(), vectorFar.XYZ());
            if (hit.Succeeded && hit.Collider.Entity != null && hit.Collider.Entity.Name != "FloorGrid")
            {
                _selectedEntity = hit.Collider.Entity;
                _uiManager.StatusText.Text = $"Selected: {_selectedEntity.Name}";
            }
        }
    }
}
