# Turian camera rigs

Follow, orbit, free-fly and first-person camera components for Turian scenes, distributed as source assemblies: the runtime assembly compiles with the project's scripts.

Licensed under MIT. See `package.json` for version and engine compatibility.

## Install

```sh
turian-cli brick add org.mass4.turian.cameras
```

Or enable `org.mass4.turian.cameras` from the Bricks panel.

## Rigs

`OrbitCameraComponent.Target` is a world-space centre. Assign `TargetNode` to orbit a moving node; `Target` then becomes a world-space offset from that node. `Orbit(dx, dy)` changes yaw/pitch and `Zoom(scroll)` changes distance, clamped to 0.01 units. Late updates track the node without additional code. A destroyed target freezes the camera until the target is cleared or replaced.

`FollowCameraComponent.Target` follows a node with a world-space `Offset`. `PositionDamping` sets the exponential response time in seconds; zero (the default) snaps immediately. Set `FollowRotation = true` to follow the target's world orientation and `RotationDamping` to smooth it. One response time removes about 63% of the remaining error. Damping depends on elapsed time, so fixed targets converge consistently across frame rates. Null or destroyed targets leave the camera unchanged.

`FreeFlyCameraComponent.Move(input, deltaTime)` moves along camera right/up/front; `FpsCameraComponent.Move` keeps forward and strafe on the ground plane. Both expose `Look(dx, dy)`. FPS can read its injected input source automatically through `CaptureInput`.

## Tests

Run `dotnet run --project Brick~/Tests/Turian.Cameras.Tests -p:TurianDir=/path/to/Turian/ -p:NoLocalPackages=true` against a Turian checkout. The device-free suite covers all four rigs, world-space targets, target loss, damping convergence and partitioned-time equivalence.

## Source and baked packages

The regular brick contains editable code under `Runtime/` and/or `Editor/`; its assembly definitions let any Turian client compile it without an engine checkout. Build and test projects live under the opaque `Brick~/` roles. Keep committed `.meta` identities when editing or publishing.

`turian-cli brick pack . --precast --twin --out .bricks` compiles the code, imports the assets and writes the regular brick plus its `.bake` twin. The source archive omits generated payloads; the twin includes the assemblies and verified import cache, with no C# source. `brick publish . --store <store> --key <key> --precast --twin` performs the same producer steps before upload; protected production CI signs and uploads the qualified archive bytes.

Consumers can enable `brick twins on --project <project>`, then `brick swap org.mass4.turian.cameras source --project <project>` to download and embed writable originals at the same version. Swapping back to `twin` preserves the edited fork in the project trash. GLSL edits in HZB require rebuilding its shader payload with the build project before packing.
