# Contributing

Use Unity 6000.4.9f1 and keep changes focused. Include the matching `.meta` file
when adding or moving an asset; use the Unity Editor to move serialized assets
so their GUIDs remain stable. Do not commit `Library`, exported builds, personal
editor settings, cloud account configuration, or generated solution files.

Before submitting a change:

1. Run `python scripts/validate_project.py`.
2. Import the project in Unity and resolve compilation errors.
3. Run `RepositoryValidation.Validate` in batch mode, as shown in the README.
4. Open `Obstacles.unity` in Play Mode. Confirm fish spawn, sliders respond,
   presets apply, metrics update, and restart works without duplicate panels.
5. For browser changes, build WebGL and verify the uploaded build on desktop
   and a touch device where available. Do not infer browser compatibility from
   an Editor-only run.

Explain what changed, why it changed, and what you tested in the pull request.
For changes to stochastic behavior or metrics, state the measurement definition
and parameter settings. Avoid claiming biological accuracy without evidence.
