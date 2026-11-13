namespace Turian.Cameras.Tests;

/// <summary>Device-free camera targeting, damping and movement contracts.</summary>
public sealed class CameraRigTests
{
    /// <summary>An orbit follows a moving target in world space, retaining its authored centre offset.</summary>
    [Fact]
    public void OrbitTracksNodeAcrossMovesAndZoom()
    {
        var parent = new Node { Position = new Vector3(20, 10, -5) };
        var target = new Node { Position = new Vector3(2, 3, 4), Parent = parent };
        var (camera, rig) = Camera(new OrbitCameraComponent
        {
            TargetNode = target,
            Target = Vector3.UnitY,
            Distance = 8
        });
        rig.OnLateUpdate(1f / 60f);
        Near(target.GlobalTransform.Position + Vector3.UnitY - camera.Front * 8, camera.Position);
        target.Position += Vector3.UnitX * 7;
        rig.Orbit(30, -20);
        Near(target.GlobalTransform.Position + Vector3.UnitY - camera.Front * 8, camera.Position);
        rig.Zoom(2);
        Near(target.GlobalTransform.Position + Vector3.UnitY - camera.Front * 7, camera.Position);
    }

    /// <summary>Point targets keep their original behavior, and zoom cannot cross the orbit centre.</summary>
    [Fact]
    public void OrbitPointAndMinimumDistanceArePreserved()
    {
        var (camera, rig) = Camera(new OrbitCameraComponent { Target = new Vector3(3, 4, 5) });
        rig.OnLateUpdate(0);
        Near(rig.Target - camera.Front * rig.Distance, camera.Position);
        rig.Zoom(100);
        Assert.Equal(0.01f, rig.Distance);
        Near(rig.Target - camera.Front * 0.01f, camera.Position);
    }

    /// <summary>Destroyed targets freeze their camera until cleared or replaced.</summary>
    [Fact]
    public void DestroyedTargetsStopFollowingAndCanBeReplaced()
    {
        var target = new Node { Position = new Vector3(3, 4, 5) };
        var (camera, orbit) = Camera(new OrbitCameraComponent { TargetNode = target });
        orbit.OnLateUpdate(0.1f);
        var previous = camera.Position;
        target.DestroyImmediate();
        orbit.OnLateUpdate(0.1f);
        Near(previous, camera.Position);
        orbit.TargetNode = new Node { Position = Vector3.UnitX };
        orbit.OnLateUpdate(0.1f);
        Near(Vector3.UnitX - camera.Front * orbit.Distance, camera.Position);

        var (_, follow) = Camera(new FollowCameraComponent { Target = target });
        follow.OnLateUpdate(0.1f);
        Near(Vector3.Zero, follow.Node!.Position);
    }

    /// <summary>Undamped following preserves the camera orientation and uses a world-space offset.</summary>
    [Fact]
    public void FollowDefaultsPreserveFixedOffsetAndOrientation()
    {
        var target = new Node { Position = new Vector3(7, 8, 9), Rotation = new Vector3(10, 45, 0) };
        var (camera, rig) = Camera(new FollowCameraComponent { Target = target });
        camera.Yaw = 0.2f;
        rig.OnLateUpdate(0.1f);
        Near(target.Position + rig.Offset, camera.Position);
        Assert.Equal(0.2f, camera.Yaw, 5);
        rig.Target = null;
        rig.OnLateUpdate(0.1f);
        Near(target.Position + rig.Offset, camera.Position);
    }

    /// <summary>Damping converges in position and rotation and depends on elapsed time rather than frame count.</summary>
    [Fact]
    public void DampingConvergesAndMatchesPartitionedTime()
    {
        var target = new Node { Position = new Vector3(10, 20, 30), Rotation = new Vector3(0, 90, 0) };
        var (a, first) = Camera(Follower(target));
        var (b, second) = Camera(Follower(target));
        first.OnLateUpdate(1);
        for (var i = 0; i < 60; i++)
        {
            second.OnLateUpdate(1f / 60f);
        }
        Near(a.Position, b.Position);
        Assert.True(MathF.Abs(Quaternion.Dot(first.Node!.Orientation, second.Node!.Orientation)) > 0.99999f);
        Assert.InRange(a.Position.X, 0.01f, 9.99f);
        for (var i = 0; i < 600; i++)
        {
            first.OnLateUpdate(1f / 60f);
        }
        Near(target.Position, a.Position);
        Assert.True(MathF.Abs(Quaternion.Dot(target.Orientation, first.Node.Orientation)) > 0.99999f);
    }

    /// <summary>Rotation following uses world transforms under a rotated parent.</summary>
    [Fact]
    public void FollowRotationAndPositionAreWorldSpace()
    {
        var parent = new Node { Rotation = new Vector3(0, 30, 0), Position = Vector3.One };
        var target = new Node { Parent = parent, Position = Vector3.UnitZ, Rotation = new Vector3(0, 60, 0) };
        var (camera, rig) = Camera(new FollowCameraComponent { Target = target, FollowRotation = true });
        rig.Node!.Parent = new Node { Rotation = new Vector3(0, -25, 0) };
        rig.OnLateUpdate(0.1f);
        Near(target.GlobalTransform.Position + rig.Offset, camera.Position);
        Assert.True(MathF.Abs(Quaternion.Dot(target.GlobalTransform.Orientation,
            rig.Node.GlobalTransform.Orientation)) > 0.99999f);
    }

    /// <summary>Paused damped updates and invalid deltas leave the camera transform unchanged.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidOrPausedDampedUpdatesDoNotMove(float delta)
    {
        var (camera, rig) = Camera(Follower(new Node { Position = Vector3.One }));
        rig.OnLateUpdate(delta);
        Near(Vector3.Zero, camera.Position);
        Assert.Equal(Quaternion.Identity, rig.Node!.Orientation);
    }

    /// <summary>Free-fly uses all three local axes while FPS movement stays on the ground plane.</summary>
    [Fact]
    public void FlyAndFpsUseTheirDocumentedAxes()
    {
        var (flyCamera, fly) = Camera(new FreeFlyCameraComponent { Speed = 4 });
        fly.Look(20, -30);
        var expected = (flyCamera.Right + flyCamera.Up + flyCamera.Front) * 2;
        fly.Move(Vector3.One, 0.5f);
        Near(expected, flyCamera.Position);

        var (fpsCamera, fps) = Camera(new FpsCameraComponent { Speed = 4, CaptureInput = false });
        fps.Look(20, -30);
        fps.Move(new Vector3(0, 0, 1), 0.5f);
        Assert.Equal(0, fpsCamera.Position.Y);
        Assert.Equal(2, fpsCamera.Position.Length(), 5);
        fps.Move(Vector3.UnitY, 0.5f);
        Assert.Equal(2, fpsCamera.Position.Y);
    }

    /// <summary>The FPS rig reads its injected source and stops capturing when disabled.</summary>
    [Fact]
    public void FpsCaptureReadsItsInjectedInputSource()
    {
        var source = new BufferedInputSource();
        var (camera, rig) = Camera(new FpsCameraComponent { Speed = 2 });
        rig.Node!.Awake(null, new Services(source));
        source.PushKeyDown(Silk.NET.Input.Key.W);
        source.PushMouseDown(Silk.NET.Input.MouseButton.Right);
        source.PushMouseMove(new Vector2(20, 10));
        rig.OnUpdate(0.5f);
        Assert.Equal(1f, camera.Position.Length(), 4);
        Assert.NotEqual(0f, camera.Yaw);
        var position = camera.Position;
        rig.CaptureInput = false;
        rig.OnUpdate(1);
        Near(position, camera.Position);
        rig.CaptureInput = true;
        source.PushKeyUp(Silk.NET.Input.Key.W);
        source.PushMouseUp(Silk.NET.Input.MouseButton.Right);
        source.NewFrame();
        rig.OnUpdate(1);
        Near(position, camera.Position);
    }

    /// <summary>Detached rigs and rigs whose camera was removed are safe to call.</summary>
    [Fact]
    public void DetachedRigsHaveNoSideEffects()
    {
        var orbit = new OrbitCameraComponent();
        orbit.Orbit(1, 2);
        orbit.Zoom(1);
        orbit.OnLateUpdate(0.1f);
        var follow = new FollowCameraComponent { Target = new Node() };
        follow.OnLateUpdate(0.1f);
        new FreeFlyCameraComponent().Move(Vector3.One, 1);
        new FreeFlyCameraComponent().Look(1, 2);
        new FpsCameraComponent().Move(Vector3.One, 1);
        new FpsCameraComponent().Look(1, 2);
        new FpsCameraComponent().OnUpdate(1);
        new Node().AddComponent(follow);
        follow.OnLateUpdate(0.1f);
    }

    static FollowCameraComponent Follower(Node target) => new()
    {
        Target = target,
        Offset = Vector3.Zero,
        PositionDamping = 0.5f,
        FollowRotation = true,
        RotationDamping = 0.5f
    };

    static (CameraComponent Camera, T Rig) Camera<T>(T rig) where T : Component
    {
        var node = new Node();
        var camera = new CameraComponent();
        node.AddComponent(camera);
        node.AddComponent(rig);
        return (camera, rig);
    }

    static void Near(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < 0.0002f, $"Expected {expected}, got {actual}.");

    sealed class Services(IInputSource source) : IServiceProvider
    {
        /// <inheritdoc/>
        public object? GetService(Type serviceType) => serviceType == typeof(IInputSource) ? source : null;
    }
}
