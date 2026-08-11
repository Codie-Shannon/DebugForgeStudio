from __future__ import annotations
import argparse, hashlib, json, re, struct, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]

def fail(msg:str)->None:
    print(msg,file=sys.stderr)
    raise SystemExit(1)

def png_info(path:Path)->tuple[int,int]:
    data=path.read_bytes()
    if len(data)<24 or data[:8]!=b"\x89PNG\r\n\x1a\n":
        fail(f"Not a valid PNG: {path.relative_to(ROOT)}")
    return struct.unpack(">II",data[16:24])

def main()->None:
    p=argparse.ArgumentParser()
    p.add_argument("--allow-pending-review",action="store_true")
    a=p.parse_args()

    state=json.loads((ROOT/"docs/project-state.json").read_text(encoding="utf-8-sig"))

    required=[
        "README.md","BUILD_STATUS.md","CHANGELOG.md","SECURITY.md",
        "RIGHTS_AND_LICENSING.md","GITHUB_METADATA.md",
        "src/DebugForgeStudio.Core/DebugForgeStudio.Core.csproj",
        "src/DebugForgeStudio.Core/Models.cs",
        "src/DebugForgeStudio.Core/ScanEngine.cs",
        "src/DebugForgeStudio.Core/InvestigationEngine.cs",
        "src/DebugForgeStudio.Core/ReportEngine.cs",
        "src/DebugForgeStudio.Web/DebugForgeStudio.Web.csproj",
        "src/DebugForgeStudio.Web/Program.cs",
        "tests/DebugForgeStudio.Tests/DebugForgeStudio.Tests.csproj",
        "tests/DebugForgeStudio.Tests/Program.cs",
        "docs/MASTER.md","docs/ARCHITECTURE.md","docs/LIMITATIONS.md",
    ]
    if not a.allow_pending_review:
        required += [
            "docs/manual-validation/v1.0.0-native-review.json",
            "docs/release-notes/v1.0.0.md",
        ]

    for rel in required:
        if not (ROOT/rel).is_file():
            fail(f"Missing required file: {rel}")

    program=(ROOT/"src/DebugForgeStudio.Web/Program.cs").read_text(encoding="utf-8")
    for route in [
        '"/health"','"/api/status"','"/api/scan"','"/api/scan/stream"',
        '"/api/triage"','"/api/reproduction"','"/api/hypothesis"',
        '"/api/compare"','"/api/report/markdown"','"/api/report/json"'
    ]:
        if route not in program:
            fail(f"Missing expected DebugForge route: {route}")

    groups=sorted((ROOT/"docs/screenshot-groups").glob("screenshot-group-*"))
    if len(groups)!=3:
        fail(f"Expected 3 screenshot groups, found {len(groups)}")

    hashes={}
    count=0
    for group in groups:
        imgs=sorted(group.glob("*.png"))
        if len(imgs)!=4:
            fail(f"{group.name}: expected 4 screenshots, found {len(imgs)}")
        for image in imgs:
            w,h=png_info(image)
            expected=(390,844) if image.name.startswith("04-mobile") else (1440,900)
            if (w,h)!=expected:
                fail(f"Wrong dimensions: {image.relative_to(ROOT)} -> {w}x{h}")
            digest=hashlib.sha256(image.read_bytes()).hexdigest()
            if digest in hashes:
                fail(f"Duplicate screenshot bytes: {image.relative_to(ROOT)} and {hashes[digest]}")
            hashes[digest]=image.relative_to(ROOT).as_posix()
            count+=1

    if count!=12 or state.get("officialScreenshots")!=12:
        fail("Expected exactly 12 official screenshots.")

    forbidden=[
        re.compile(r"(?i)client[_-]?secret\s*[:=]"),
        re.compile(r"(?i)password\s*[:=]\s*['\"][^'\"]{12,}"),
        re.compile(r"https://api\.",re.I),
    ]
    for rel in [
        "src/DebugForgeStudio.Core/ScanEngine.cs",
        "src/DebugForgeStudio.Core/InvestigationEngine.cs",
        "src/DebugForgeStudio.Core/ReportEngine.cs",
        "src/DebugForgeStudio.Web/Program.cs",
    ]:
        text=(ROOT/rel).read_text(encoding="utf-8",errors="ignore")
        if any(rx.search(text) for rx in forbidden):
            fail(f"External/secret-shaped content found: {rel}")

    approved=False
    if not a.allow_pending_review:
        review=json.loads(
            (ROOT/"docs/manual-validation/v1.0.0-native-review.json")
            .read_text(encoding="utf-8-sig")
        )
        approved=review.get("approved") is True
        if not approved:
            fail("Native review is not approved.")

    print("DebugForge Studio repository verification passed.")
    print("Version:            1.0.0")
    print("Screenshot groups:  3")
    print("Screenshots:        12")
    print(f"Evidence approved:  {approved}")
    print("External writes:    0")
    print("Public boundary:    passed")

if __name__=="__main__":
    main()
