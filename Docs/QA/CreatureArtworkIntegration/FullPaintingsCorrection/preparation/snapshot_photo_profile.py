"""Protect legacy photo profile (stored outside Content/Data); no restoration."""
import json
from preservation_snapshot import QA, ROOT, LOCAL, copy_verified, utc, sha

manifest = QA / "preparation" / "photo-profile-entry.json"
if manifest.exists():
    raise RuntimeError("Refusing to replace profile entry snapshot")
rows = []
for relative in (
    "Assets/_Project/Art/PhotoCaptureRuntime/Zone01CaptureProfile.asset",
    "Assets/_Project/Art/PhotoCaptureRuntime/Zone01CaptureProfile.asset.meta",
):
    target = LOCAL / "protectedProject" / relative
    rows.append({"path": relative, "backup": target.relative_to(QA).as_posix(),
                 **copy_verified(ROOT / relative, target)})
manifest.write_text(json.dumps({"utc": utc(), "files": rows}, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"manifest": str(manifest), "sha256": sha(manifest), "files": len(rows)}))
