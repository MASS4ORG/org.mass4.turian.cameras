namespace Turian.Cameras;

/// <summary>
/// Follows a node at a world-space offset, with optional position and orientation damping.
/// </summary>
[RequireComponent(typeof(CameraComponent))]
[DisallowMultipleComponent]
[ComponentContextMenu("Rendering/Camera/Follow")]
public class FollowCameraComponent : Component
{
    /// <summary>The node the camera follows. When null, the component is a no-op.</summary>
    [Required]
    public Node? Target { get; set; }

    /// <summary>World-space offset from the target's position.</summary>
    public Vector3 Offset { get; set; } = new(0f, 2f, -5f);

    /// <summary>Position response time in seconds; zero snaps to the target without smoothing.</summary>
    public float PositionDamping { get; set; }

    /// <summary>Whether the camera also follows the target's world orientation.</summary>
    public bool FollowRotation { get; set; }

    /// <summary>Orientation response time in seconds; zero snaps when rotation following is enabled.</summary>
    public float RotationDamping { get; set; }

    /// <inheritdoc/>
    public override void OnLateUpdate(float deltaTime)
    {
        if (Target is not { IsDestroyed: false } target || !float.IsFinite(deltaTime) || deltaTime < 0f)
        {
            return;
        }

        var camera = Node?.GetComponent<CameraComponent>();
        if (camera is null)
        {
            return;
        }

        var desired = target.GlobalTransform;
        camera.Position = Vector3.Lerp(camera.Position, desired.Position + Offset,
            Response(PositionDamping, deltaTime));
        if (FollowRotation)
        {
            var node = camera.Node!;
            var transform = node.GlobalTransform;
            node.GlobalTransform = transform with
            {
                Orientation = Quaternion.Slerp(transform.Orientation, desired.Orientation,
                    Response(RotationDamping, deltaTime))
            };
        }
    }

    static float Response(float damping, float deltaTime) =>
        float.IsFinite(damping) && damping > 0f ? 1f - MathF.Exp(-deltaTime / damping) : 1f;
}
