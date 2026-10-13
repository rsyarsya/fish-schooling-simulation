"""Validate the source package without importing Unity or installing dependencies."""
from pathlib import Path
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]


def main():
    errors = []
    required = ['Assets/Scenes/Obstacles.unity', 'Assets/Scripts/Boid.cs',
                'Assets/Scripts/BoidManager.cs', 'Assets/Scripts/StochasticCurrent.cs',
                'Packages/manifest.json', 'Packages/packages-lock.json',
                'ProjectSettings/ProjectVersion.txt', 'LICENSE']
    for name in required:
        if not (ROOT / name).is_file():
            errors.append(f'Missing required file: {name}')
    for name in ['Packages/manifest.json', 'Packages/packages-lock.json']:
        try:
            json.loads((ROOT / name).read_text(encoding='utf-8'))
        except (OSError, ValueError) as exc:
            errors.append(f'{name}: {exc}')

    guids = {}
    for file in (ROOT / 'Assets').rglob('*'):
        if not file.is_file():
            continue
        relative = file.relative_to(ROOT).as_posix()
        if file.suffix == '.meta':
            original = Path(str(file)[:-5])
            if not original.exists():
                errors.append(f'Orphan metadata: {relative}')
            match = re.search(r'^guid: ([0-9a-f]{32})$', file.read_text(), re.M)
            if not match:
                errors.append(f'Invalid GUID: {relative}')
            elif match[1] in guids:
                errors.append(f'Duplicate GUID: {relative} and {guids[match[1]]}')
            else:
                guids[match[1]] = relative
        elif not Path(str(file) + '.meta').exists():
            errors.append(f'Missing metadata: {relative}')
        if file.stat().st_size >= 50 * 1024 * 1024:
            errors.append(f'Oversized asset: {relative}')

    settings = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
    if 'enabled: 1\n    path: Assets/Scenes/Obstacles.unity' not in settings:
        errors.append('Obstacles.unity must be the enabled default build scene')
    tracked = subprocess.check_output(['git', 'ls-files', '-z'], cwd=ROOT).decode().split('\0')
    forbidden = {'Library', 'Temp', 'Build', 'Builds', 'Logs', 'UserSettings', '.vs'}
    for name in filter(None, tracked):
        if name.split('/')[0] in forbidden or Path(name).suffix.lower() in {'.pptx', '.pdf', '.zip', '.max'}:
            errors.append(f'Excluded artifact tracked: {name}')

    if errors:
        print('\n'.join(errors), file=sys.stderr)
        return 1
    print(f'PASS: project files, package JSON, {len(guids)} unique asset GUIDs, and tracked-file scope')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
