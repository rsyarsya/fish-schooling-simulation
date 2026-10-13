# Fish Schooling Simulation

A Unity simulation of schooling fish using the Boids algorithm, with stochastic
water currents and live measurements of group coherence.

**[Try the browser demo](https://rsyarsya.itch.io/boidsfish-simulation)** ·
**[Project notes on Notion](https://app.notion.com/p/Teknik-Permodelan-Simulasi-364bb9582b77803f9565fc23f2fa624f)**

Developed for the Modeling and Stochastic Simulation course at Universitas
Gadjah Mada. This repository contains the Unity source project. Presentation
slides, papers, exported builds, editor caches, and internal working notes are
not included. The hosted demo is a separately published build and may differ
from the current source.

## Features

- Separation, alignment, cohesion, obstacle avoidance, and bounded movement.
- Adjustable fish count, steering weights, current strength, and current interval.
- Shared stochastic currents and independent perturbations for individual fish.
- Live polarization, spread, nearest-neighbor distance, speed, and force metrics.
- Visual Demo and Paper Based presets, with desktop camera and mobile controls.
- Compute-shader flock updates on supported desktop hardware; CPU fallback for WebGL.

## Run locally

1. Install **Unity 6000.4.9f1** through Unity Hub. Add Web Build Support if you plan
   to export the browser version.
2. Clone this repository:

   ```bash
   git clone https://github.com/rsyarsya/fish-schooling-simulation.git
   ```

3. In Unity Hub, select **Add project from disk** and choose the cloned folder.
4. Let Unity restore the pinned packages and import assets.
5. Open `Assets/Scenes/Obstacles.unity` and enter Play Mode.

| Control | Action |
|---|---|
| W / A / S / D | Move the camera |
| Q / E | Move down / up |
| Shift | Move faster |
| Hold right mouse button | Look around |
| On-screen sliders and preset controls | Adjust the simulation |
| Mobile joystick and swipe area | Move and look on touch devices |

`Assets/Scenes/BoidsAdventure.unity` is an additional desktop exploration scene
with a submarine and procedural terrain. It is excluded from the default build
because its terrain generation requires compute shaders. Use `Obstacles.unity`
for the browser simulation.

## Build for itch.io

1. Open **File > Build Profiles**, select **Web**, and switch platform.
2. Confirm that `Assets/Scenes/Obstacles.unity` is the enabled scene.
3. Use a fish count around **50–150** as a starting point; measure performance on
   the target device. The maximum slider value is a stress-test setting.
4. Build into an ignored `Builds/Web/` directory. For straightforward static
   hosting, use disabled compression or enable Decompression Fallback.
5. Zip the build contents with `index.html` at the ZIP root and upload it to
   itch.io as an HTML project. Test the uploaded build in a browser.

WebGL uses the CPU flock-update path. Its pairwise neighbor search is O(N²), so
larger schools can reduce frame rate. Hardware and browser differences matter;
no cross-device performance guarantee is claimed.

## Source layout

| Path | Responsibility |
|---|---|
| `Assets/Scripts/Boid.cs` | Per-agent steering and movement |
| `Assets/Scripts/BoidManager.cs` | Neighbor aggregation and CPU/GPU execution |
| `Assets/Scripts/StochasticCurrent.cs` | Shared current sampling |
| `Assets/Scripts/BoidMetrics.cs` | Measurements and runtime metrics panel |
| `Assets/Scripts/UIManager.cs` | Sliders, presets, restart, and fish count |
| `Assets/Scripts/FreeCamera.cs` | Desktop and touch camera movement |
| `Assets/MarchingCubes/` | Terrain code for the exploration scene |
| `Assets/Settings/` | Serialized simulation presets |
| `Assets/Scenes/` | Simulation and exploration scenes |
| `Packages/` | Dependency manifest and lockfile |
| `ProjectSettings/` | Unity version and shared project configuration |

## Model and interpretation

The project extends [Sebastian Lague’s Boids implementation](https://github.com/SebLague/Boids).
Steering combines local flocking rules with additional targets, bounds, obstacle
avoidance, and stochastic forces. This is an educational simulation, not a
validated predictor of biological fish behavior.

Polarization measures directional alignment; spread measures distance from the
school center. The current sampler uses Box–Muller-style Gaussian sampling,
reduces the vertical component, and resamples at a configurable interval.
Additional per-fish perturbations are scaled by a tuning multiplier. These
parameters should not be interpreted directly as calibrated ocean measurements.
Randomness currently follows Unity’s random state; the UI does not provide a
repeatable experiment seed.

The Paper Based preset is a parameter configuration, not a claim of validation
against experimental data. See the linked Notion notes for the academic context.

## Checks and contributions

Run repository checks with Python 3.11 or later:

```bash
python scripts/validate_project.py
```

The GitHub Actions workflow checks project structure, asset metadata, scene
configuration, and excluded artifacts. It does not run Unity or test gameplay.
Unity import and scene checks can be run with the installed editor:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe" `
  -batchmode -nographics -quit -projectPath "$PWD" `
  -executeMethod RepositoryValidation.Validate -logFile "Logs/validation.log"
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the validation steps expected before a change.

## Attribution and licensing

The original Boids and Marching Cubes code is by **Sebastian Lague** under the
MIT license. The original copyright notice is retained in [LICENSE](LICENSE)
and [licenses/Sebastian-Lague-MIT.txt](licenses/Sebastian-Lague-MIT.txt).
Unity UI resources and the Liberation Sans font retain their separate notices
under [licenses/](licenses/). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
