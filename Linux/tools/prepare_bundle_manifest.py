"""Record original Addressables bundle paths for the Linux rebuild."""
import json
from pathlib import Path
import UnityPy

root = Path(__file__).resolve().parents[1]
bundles = []
for path in sorted((root / "steam-source/MateEngineX_Data/StreamingAssets/aa/StandaloneWindows64").glob("*.bundle")):
    env = UnityPy.load(str(path))
    names = list(env.container)
    if not names:
        continue  # BuildPipeline includes each bundle's MonoScripts itself.
    for name in names:
        if not (root / "project" / name).is_file():
            raise RuntimeError(f"Missing restored bundle asset: {name}")
    bundles.append({"name": path.name, "assets": names})
(root / "inspection/linux-bundle-inputs.json").write_text(json.dumps({"bundles": bundles}, indent=2))
print(f"Prepared {len(bundles)} bundles / {sum(len(row['assets']) for row in bundles)} assets")
