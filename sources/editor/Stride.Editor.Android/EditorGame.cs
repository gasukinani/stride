using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives; // Idinagdag para sa GeometricPrimitive
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace Stride.Editor.Android;

public class EditorGame : Game
{
    public Entity? SelectedEntity { get; private set; }
    public event Action<Entity>? OnEntitySelected;
    public event Action<List<Entity>>? OnHierarchyChanged;

    private readonly List<Entity> _editorEntities = new();
    private Entity? _cameraEntity;

    protected override async System.Threading.Tasks.Task LoadContent()
    {
        await base.LoadContent();

        // 1. Setup Editor Camera
        _cameraEntity = new Entity("EditorCamera")
        {
            new CameraComponent
            {
                UseCustomProjectionMatrix = false,
                UseCustomAspectRatio = false,
                NearClipPlane = 0.1f,
                FarClipPlane = 1000.0f
            }
        };
        _cameraEntity.Transform.Position = new Vector3(0, 4, 8);
        _cameraEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(0, -0.35f, 0);
        SceneSystem.SceneInstance.RootScene.Entities.Add(_cameraEntity);

        // 2. Setup Directional Light
        var lightEntity = new Entity("DirectionalLight")
        {
            new LightComponent
            {
                Type = new LightDirectional
                {
                    Color = new ColorRgbProvider(new Color3(1f, 1f, 1f)),
                    Shadow = { Enabled = true }
                },
                Intensity = 1.5f
            }
        };
        lightEntity.Transform.Rotation = Quaternion.RotationYawPitchRoll(0.5f, -0.8f, 0);
        SceneSystem.SceneInstance.RootScene.Entities.Add(lightEntity);

        // 3. Default Ground Plane & Cube
        CreatePrimitive(PrimitiveType.Plane, "GroundPlane", new Vector3(0, 0, 0), new Vector3(10, 1, 10));
        CreatePrimitive(PrimitiveType.Cube, "DefaultCube", new Vector3(0, 0.5f, 0), Vector3.One);
    }

    public enum PrimitiveType { Cube, Sphere, Plane }

    public Entity CreatePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 scale)
    {
        var entity = new Entity(name);
        entity.Transform.Position = position;
        entity.Transform.Scale = scale;

        var meshDraw = type switch
        {
            PrimitiveType.Cube => GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw(),
            PrimitiveType.Sphere => GeometricPrimitive.Sphere.New(GraphicsDevice).ToMeshDraw(),
            PrimitiveType.Plane => GeometricPrimitive.Plane.New(GraphicsDevice).ToMeshDraw(),
            _ => GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw()
        };

        var model = new Model
        {
            new Mesh { Draw = meshDraw }
        };

        entity.Add(new ModelComponent { Model = model });

        SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
        _editorEntities.Add(entity);

        NotifyHierarchyChanged();
        SelectEntity(entity);

        return entity;
    }

    public void SelectEntity(Entity? entity)
    {
        SelectedEntity = entity;
        if (entity != null)
        {
            OnEntitySelected?.Invoke(entity);
        }
    }

    public void UpdateEntityPosition(Vector3 newPosition)
    {
        if (SelectedEntity != null)
        {
            SelectedEntity.Transform.Position = newPosition;
        }
    }

    private void NotifyHierarchyChanged()
    {
        OnHierarchyChanged?.Invoke(new List<Entity>(_editorEntities));
    }
}        entity.Transform.Scale = scale;

        // Gumawa ng procedural mesh gamit ang Stride Graphics Device
        var meshDraw = type switch
        {
            PrimitiveType.Cube => GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw(),
            PrimitiveType.Sphere => GeometricPrimitive.Sphere.New(GraphicsDevice).ToMeshDraw(),
            PrimitiveType.Plane => GeometricPrimitive.Plane.New(GraphicsDevice).ToMeshDraw(),
            _ => GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw()
        };

        var model = new Model
        {
            new Mesh { Draw = meshDraw }
        };

        entity.Add(new ModelComponent { Model = model });

        SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
        _editorEntities.Add(entity);

        NotifyHierarchyChanged();
        SelectEntity(entity);

        return entity;
    }

    public void SelectEntity(Entity? entity)
    {
        SelectedEntity = entity;
        OnEntitySelected?.Invoke(entity!);
    }

    public void UpdateEntityPosition(Vector3 newPosition)
    {
        if (SelectedEntity != null)
        {
            SelectedEntity.Transform.Position = newPosition;
        }
    }

    private void NotifyHierarchyChanged()
    {
        OnHierarchyChanged?.Invoke(new List<Entity>(_editorEntities));
    }
}
