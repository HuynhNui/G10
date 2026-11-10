"""Create/verify a local, checksummed snapshot. Never restores or deletes files.

Run from anywhere: python preservation_snapshot.py snapshot
                  python preservation_snapshot.py verify
                  python preservation_snapshot.py compare
The source PSD cache remains under the repository's already ignored .tools/.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time

QA = Path(__file__).resolve().parents[1]
ROOT = QA.parents[2]
LOCAL = QA / "preserved-local"
MANIFEST = QA / "preparation" / "entry-preservation-manifest.json"
PERSISTENT = Path.home() / "AppData/LocalLow/DefaultCompany/G10"


def utc():
    return datetime.now(timezone.utc).isoformat()


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT).decode("utf-8")


def project_paths():
    for value in git("ls-files", "-z").split("\0"):
        if not value:
            continue
        keep = value.startswith(("Assets/_Project/", "ProjectSettings/", "Packages/"))
        keep |= value.startswith("Assets/TextMesh Pro/") and value.endswith((".asset", ".meta"))
        if keep and (ROOT / value).is_file():
            yield value


def copy_verified(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    for attempt in range(3):
        before = sha(source)
        shutil.copy2(source, destination)
        copied = sha(destination)
        after = sha(source)
        if before == copied == after:
            return {"bytes": source.stat().st_size, "sha256": copied,
                    "sourceMatchedBeforeAndAfterCopy": True}
        time.sleep(.1)
    raise RuntimeError(f"Source changed while backing up: {source}")


def snapshot():
    if MANIFEST.exists() or (LOCAL / "entry").exists():
        raise RuntimeError("Entry snapshot already exists; refusing to replace it.")
    if not PERSISTENT.is_dir():
        raise RuntimeError(f"Persistent directory not found: {PERSISTENT}")
    result = {"schema": 1, "startedUtc": utc(), "projectRoot": str(ROOT),
              "persistentRoot": str(PERSISTENT), "branch": git("branch", "--show-current").strip(),
              "head": git("rev-parse", "HEAD").strip(), "gitStatusBeforeSnapshot": git("status", "--short"),
              "scope": "All tracked first-party Assets/_Project files, ProjectSettings, Packages, TMP font assets/metas; all existing real G10 persistent files.",
              "backupRoot": str(LOCAL / "entry"), "persistent": [], "protectedProject": []}
    for group, source_root, values in (
        ("persistent", PERSISTENT, sorted(str(x.relative_to(PERSISTENT)) for x in PERSISTENT.rglob("*") if x.is_file())),
        ("protectedProject", ROOT, sorted(project_paths())),
    ):
        for relative in values:
            source = source_root / relative
            target = LOCAL / "entry" / group / relative
            result[group].append({"path": relative.replace("\\", "/"),
                                  "backup": str(target.relative_to(QA)).replace("\\", "/"),
                                  **copy_verified(source, target)})
    result["finishedUtc"] = utc()
    result["verifiedCopies"] = sum(len(result[key]) for key in ("persistent", "protectedProject"))
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"manifest": str(MANIFEST), "persistent": len(result["persistent"]),
                      "protectedProject": len(result["protectedProject"]), "verifiedCopies": result["verifiedCopies"]}))


def check(compare):
    entry = json.loads(MANIFEST.read_text(encoding="utf-8"))
    result = {"utc": utc(), "mode": "compare-live-entry" if compare else "verify-backups",
              "checked": 0, "missing": [], "different": [], "newPersistentFiles": []}
    for group, source_root in (("persistent", Path(entry["persistentRoot"])), ("protectedProject", ROOT)):
        for row in entry[group]:
            path = source_root / row["path"] if compare else QA / row["backup"]
            result["checked"] += 1
            if not path.is_file():
                result["missing"].append({"group": group, "path": row["path"]})
            elif sha(path) != row["sha256"]:
                result["different"].append({"group": group, "path": row["path"], "currentSha256": sha(path), "entrySha256": row["sha256"]})
    if compare:
        known = {row["path"] for row in entry["persistent"]}
        result["newPersistentFiles"] = sorted(str(x.relative_to(PERSISTENT)).replace("\\", "/") for x in PERSISTENT.rglob("*") if x.is_file() and str(x.relative_to(PERSISTENT)).replace("\\", "/") not in known)
    result["allMatched"] = not result["missing"] and not result["different"]
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result["allMatched"] else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("snapshot", "verify", "compare"))
    args = parser.parse_args()
    if args.action == "snapshot":
        snapshot()
    else:
        sys.exit(check(args.action == "compare"))
