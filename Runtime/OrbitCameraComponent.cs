namespace Turian.Cameras;

/// <summary>
/// Orbits a node or world-space point at a fixed distance along the camera's Front axis.
/// Mouse deltas update yaw and pitch; a node target is tracked every late update.
/// </summary>
[RequireComponent(typeof(CameraComponent))]
[DisallowMultipleComponent]
[ComponentContextMenu("Rendering/Camera/Orbit")]
public class OrbitCameraComponent : Component
{
    /// <summary>World-space point the camera orbits around.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Optional node to orbit; its world position plus <see cref="Target"/> is the orbit centre.</summary>
    public Node? TargetNode { get; set; }

    /// <summary>Distance from <see cref="Target"/> along the camera's Front axis.</summary>
    public float Distance
    {
        get => field;
        set => field = MathF.Max(value, 0.01f);
    } = 5f;

    /// <summary>Mouse look sensitivity in radians per pixel.</summary>
    public float LookSensitivity { get; set; } = 0.005f;

    /// <summary>Distance change per scroll unit (negative = zoom in).</summary>
    public float ZoomStep { get; set; } = 0.5f;

    /// <summary>Updates yaw/pitch by mouse-pixel deltas and re-snaps the camera to its orbit position.</summary>
    public void Orbit(float dxPixels, float dyPixels)
    {
        var camera = Node?.GetComponent<CameraComponent>();
        if (camera is null)
        {
            return;
        }

        camera.Yaw += dxPixels * LookSensitivity;
        camera.Pitch -= dyPixels * LookSensitivity;
        SnapToOrbit(camera);
    }

    /// <summary>Adjusts <see cref="Distance"/> by a scroll amount and re-snaps.</summary>
    public void Zoom(float scrollDelta)
    {
        Distance -= scrollDelta * ZoomStep;
        var camera = Node?.GetComponent<CameraComponent>();
        if (camera is not null)
        {
            SnapToOrbit(camera);
        }
    }

    /// <inheritdoc/>
    public override void OnLateUpdate(float deltaTime)
    {
        var camera = Node?.GetComponent<CameraComponent>();
        if (camera is not null)
        {
            SnapToOrbit(camera);
        }
    }

    void SnapToOrbit(CameraComponent camera)
    {
        if (TargetNode is { IsDestroyed: true })
        {
            return;
        }

        var centre = Target + (TargetNode?.GlobalTransform.Position ?? Vector3.Zero);
        camera.Position = centre - camera.Front * Distance;
    }
}
