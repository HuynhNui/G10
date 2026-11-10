"""Snapshot and compare current G10 files; never restores or deletes anything.

This is the correction-entry baseline, independent of the previous integration.
Commands: snapshot, verify, compare. All persistent files are protected. Changes
to project files are reported separately because production changes are intended.
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
ROOT = next(p for p in QA.parents if (p / "Assets").is_dir())
LOCAL = QA / "preserved-local" / "entry"
MANIFEST = QA / "preparation" / "entry-preservation-manifest.json"
PERSISTENT = Path("C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10")


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
    """Protect present first-party source, gameplay config, existing cutout art.

    New full-painting correction outputs are intentionally not entry assets.
    Includes untracked earlier integration files, unlike git ls-files alone.
    """
    roots = ["Assets/_Project/Scripts", "Assets/_Project/Content",
             "Assets/_Project/Data", "Assets/_Project/Art/Creatures",
             "ProjectSettings", "Packages"]
    paths = set()
    for relative in roots:
        directory = ROOT / relative
        if directory.is_dir():
            paths.update(p.relative_to(ROOT).as_posix() for p in directory.rglob("*") if p.is_file())
        meta = ROOT / (relative + ".meta")
        if meta.is_file():
            paths.add(meta.relative_to(ROOT).as_posix())
    return sorted(paths)


def copy_verified(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    for _ in range(3):
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
    if MANIFEST.exists() or LOCAL.exists():
        raise RuntimeError("Correction-entry snapshot exists; refusing to replace it.")
    if not PERSISTENT.is_dir():
        raise RuntimeError(f"Persistent directory missing: {PERSISTENT}")
    entry = {"schema": 2, "startedUtc": utc(), "projectRoot": str(ROOT),
             "persistentRoot": str(PERSISTENT), "backupRoot": str(LOCAL),
             "branch": git("branch", "--show-current").strip(),
             "head": git("rev-parse", "HEAD").strip(),
             "gitStatusBeforeSnapshot": git("status", "--short"),
             "scope": "All current real persistent files; current first-party Scripts/Content/Data/cutout Creatures plus ProjectSettings and Packages. Newly exported SurveyPaintings intentionally excluded.",
             "persistent": [], "protectedProject": []}
    for group, source_root, values in (
        ("persistent", PERSISTENT, sorted(p.relative_to(PERSISTENT).as_posix() for p in PERSISTENT.rglob("*") if p.is_file())),
        ("protectedProject", ROOT, project_paths()),
    ):
        for relative in values:
            target = LOCAL / group / relative
            entry[group].append({"path": relative,
                                 "backup": target.relative_to(QA).as_posix(),
                                 **copy_verified(source_root / relative, target)})
    entry["finishedUtc"] = utc()
    entry["verifiedCopies"] = sum(len(entry[k]) for k in ("persistent", "protectedProject"))
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(entry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary(entry), ensure_ascii=False, indent=2))


def summary(entry):
    gameplay = [r for r in entry["persistent"] if r["path"].startswith(("Expedition/", "Zone01Photos/"))]
    return {"manifest": str(MANIFEST), "manifestSha256": sha(MANIFEST),
            "persistentFiles": len(entry["persistent"]),
            "persistentBytes": sum(r["bytes"] for r in entry["persistent"]),
            "gameplayFiles": len(gameplay), "gameplayBytes": sum(r["bytes"] for r in gameplay),
            "protectedProjectFiles": len(entry["protectedProject"]),
            "verifiedCopies": entry["verifiedCopies"]}


def check(compare):
    entry = json.loads(MANIFEST.read_text(encoding="utf-8"))
    report = {"utc": utc(), "mode": "compare-current" if compare else "verify-backups",
              "entryManifestSha256": sha(MANIFEST), "counts": summary(entry),
              "persistent": {"checked": 0, "missing": [], "different": [], "new": []},
              "protectedProject": {"checked": 0, "missing": [], "different": []}}
    for group, source_root in (("persistent", PERSISTENT), ("protectedProject", ROOT)):
        for row in entry[group]:
            path = source_root / row["path"] if compare else QA / row["backup"]
            report[group]["checked"] += 1
            if not path.is_file():
                report[group]["missing"].append(row["path"])
            else:
                current = sha(path)
                if current != row["sha256"]:
                    report[group]["different"].append({"path": row["path"], "currentSha256": current, "entrySha256": row["sha256"]})
    supplement = QA / "preparation" / "photo-profile-entry.json"
    if supplement.is_file():
        report["photoProfile"] = {"checked": 0, "missing": [], "different": []}
        for row in json.loads(supplement.read_text(encoding="utf-8"))["files"]:
            path = ROOT / row["path"] if compare else QA / row["backup"]
            report["photoProfile"]["checked"] += 1
            if not path.is_file():
                report["photoProfile"]["missing"].append(row["path"])
            elif sha(path) != row["sha256"]:
                report["photoProfile"]["different"].append(row["path"])
    if compare:
        known = {r["path"] for r in entry["persistent"]}
        report["persistent"]["new"] = sorted(p.relative_to(PERSISTENT).as_posix() for p in PERSISTENT.rglob("*") if p.is_file() and p.relative_to(PERSISTENT).as_posix() not in known)
    gameplay = {r["path"] for r in entry["persistent"] if r["path"].startswith(("Expedition/", "Zone01Photos/"))}
    report["gameplayMissing"] = [p for p in report["persistent"]["missing"] if p in gameplay]
    report["gameplayDifferent"] = [r for r in report["persistent"]["different"] if r["path"] in gameplay]
    report["allGameplayMatched"] = not report["gameplayMissing"] and not report["gameplayDifferent"]
    protected_groups = ["persistent", "protectedProject"] + (["photoProfile"] if "photoProfile" in report else [])
    report["allBackupsMatched"] = not any(report[g][k] for g in protected_groups for k in ("missing", "different")) if not compare else None
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if (report["allGameplayMatched"] if compare else report["allBackupsMatched"]) else 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("snapshot", "verify", "compare"))
    args = parser.parse_args()
    if args.action == "snapshot":
        snapshot()
    else:
        sys.exit(check(args.action == "compare"))
