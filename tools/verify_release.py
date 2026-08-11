#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
import struct
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "docs" / "project-state.json"

def fail(message: str) -> None:
    raise SystemExit(message)

def png_info(path: Path) -> tuple[int, int]:
    data = path.read_bytes()
    if len(data) < 24 or data[:8] != b"\x89PNG\r\n\x1a\n":
        fail(f"Not a valid PNG: {path.relative_to(ROOT)}")
    return struct.unpack(">II", data[16:24])

def main() -> None:
    if not STATE.is_file():
        fail("docs/project-state.json is missing.")

    state = json.loads(STATE.read_text(encoding="utf-8-sig"))

    if state.get("project") != "DebugForge Studio":
        fail("project-state project name mismatch.")

    if state.get("version") != "0.1.0":
        fail("Expected generated baseline version 0.1.0.")

    groups = sorted((ROOT / "docs" / "screenshot-groups").glob("screenshot-group-*"))
    if len(groups) != 3:
        fail(f"Expected 3 screenshot groups, found {len(groups)}.")

    count = 0
    hashes: dict[str, str] = {}

    for group in groups:
        images = sorted(group.glob("*.png"))
        if len(images) != 4:
            fail(f"{group.name} must contain exactly four PNG files.")

        if not (group / "README.md").is_file():
            fail(f"Missing screenshot README: {group.name}")

        for image in images:
            width, height = png_info(image)
            expected = (390, 844) if image.name.startswith("04-mobile") else (1440, 900)
            if (width, height) != expected:
                fail(f"Wrong screenshot dimensions: {image.relative_to(ROOT)} -> {width}x{height}")

            digest = hashlib.sha256(image.read_bytes()).hexdigest()
            if digest in hashes:
                fail(f"Duplicate screenshot bytes: {image.relative_to(ROOT)} and {hashes[digest]}")
            hashes[digest] = image.relative_to(ROOT).as_posix()
            count += 1

    if count != 12:
        fail(f"Expected 12 screenshots, found {count}.")

    if state.get("officialScreenshots") != count:
        fail("project-state screenshot count mismatch.")

    text_ext = {".md", ".txt", ".json", ".yml", ".yaml", ".html", ".css", ".js", ".mjs", ".ts", ".vue", ".py", ".cs", ".csproj", ".sln", ".xml", ".toml", ".ps1"}
    excluded_dirs = {".git", "bin", "obj", "node_modules", "dist", "artifacts", "__pycache__", ".venv"}
    patterns = [
        re.compile(r"[A-Za-z]:[\\/]+Users[\\/]+[^\\/]+[\\/]+", re.I),
        re.compile(r"/home/[^/]+/", re.I),
        re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
        re.compile(r"gh[opsu]_[A-Za-z0-9]{20,}"),
        re.compile(r"(?:client[_-]?secret|api[_-]?key|password)\s*[:=]\s*['\"]?[A-Za-z0-9_\-]{16,}", re.I),
    ]

    findings: list[str] = []
    for path in ROOT.rglob("*"):
        if not path.is_file() or path.suffix.lower() not in text_ext:
            continue
        rel = path.relative_to(ROOT)
        if rel.as_posix() == "tools/verify_release.py":
            continue
        if any(part in excluded_dirs for part in rel.parts[:-1]):
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for pattern in patterns:
            if pattern.search(text):
                findings.append(f"{rel.as_posix()}: {pattern.pattern}")

    if findings:
        fail("Machine path or secret-shaped finding:\n" + "\n".join(findings[:30]))

    messages = subprocess.run(
        ["git", "log", "--reverse", "--format=%s"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        check=True,
    ).stdout.splitlines()

    if len(messages) != 8:
        fail(f"Expected 8 commits, found {len(messages)}.")

    expected = ["chore: establish DebugForge Studio repository foundation", "feat: add DebugForge Studio core and synthetic fixtures", "feat(sg01): deliver Log Intake, Streaming Scan, and Incident Triage", "docs(sg01): add Log Intake, Streaming Scan, and Incident Triage evidence", "feat(sg02): deliver Reproduction, Hypotheses, and File Comparison", "docs(sg02): add Reproduction, Hypotheses, and File Comparison evidence", "feat(sg03): deliver Reports, Evidence Export, and Product Boundaries", "docs(sg03): close evidence and v0.1.0 generated baseline"]
    if messages != expected:
        fail("Commit history does not match the planned meaningful history.")

    status = subprocess.run(
        ["git", "status", "--porcelain"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        check=True,
    ).stdout.strip()

    if status:
        fail("Working tree is not clean.")

    print("DebugForge Studio repository verification passed.")
    print("Version:           0.1.0")
    print("Screenshot groups: 3")
    print("Screenshots:       12")
    print("Commits:           8")
    print("Public boundary:   passed")

if __name__ == "__main__":
    main()
