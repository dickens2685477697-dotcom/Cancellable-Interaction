# Speed sampling controls

Open this worktree as a Unity project. Select the experiment controller with the
`BallSpawner` component. At the top of its Inspector, under **Fixed Sampling /
采样频率**, choose **Fixed Sampling Mode** before entering Play Mode:

- **Project Default** leaves the project timestep unchanged (currently 0.02 s).
- **30 Hz (33.33 ms)** sets `Time.fixedDeltaTime` to `1 / 30` seconds.
- **50 Hz (20 ms)** sets it to `1 / 50` seconds.

The override applies to all `FixedUpdate` methods in the scene, including speed
estimation, collider entry/exit evaluation, and trajectory logging. It does not
increase or decrease the hardware hand-tracking or display refresh rate. Update
methods still follow the rendered frame cadence. This is a comparison of Unity
fixed-update frequencies, not a native 30 Hz tracking-device experiment.

Set an override on one active Spawner only. Leave other Spawners on Project
Default. Conflicting overrides produce a Console warning. The timestep is
restored when the owning controller is disabled or Play Mode ends. Controls are
locked during Play Mode so a trial cannot mix frequencies through Inspector edits.

`trajectory.csv` and `speed-boundary-debug.csv` now append `fixedTime`,
`fixedDeltaTime`, `fixedSamplingHz`, and `samplingMode`. Trajectories also append
`frame` and `time`. Existing columns retain their original ordering. Use
`fixedTime` to identify fixed steps: one rendered frame can contain more than
one fixed update. `fixedSamplingHz` reports the effective configured frequency;
`samplingMode` records that Spawner's requested mode.

For a 30 Hz run, verify `fixedSamplingHz = 30.000` and
`fixedDeltaTime` approximately `0.033333335` in the output. Compare runs with the
same Speed technique, target size, thresholds, and cancellation settings.
