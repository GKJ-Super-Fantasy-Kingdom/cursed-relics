"""Build or repackage a verified Thunderstore archive. Requires Pillow."""
import argparse
import io
import json
import subprocess
from pathlib import Path
from tempfile import TemporaryDirectory
from xml.etree import ElementTree
from zipfile import ZIP_DEFLATED, ZipFile

from PIL import Image

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--verified-zip", type=Path, help="Reuse the tested DLL from this same-version ZIP.")
parser.add_argument("--output-dir", type=Path, help="Write the archive here instead of dist.")
args = parser.parse_args()

root = Path(__file__).resolve().parent
listing = root / "thunderstore"
project = root / "CursedRelics.csproj"
manifest = json.loads((listing / "manifest.json").read_text("utf-8"))
version = manifest["version_number"]
xml = ElementTree.parse(project)
assert manifest["name"] == "CursedRelics" and version == xml.findtext(".//Version")
dll_entry = "plugins/CursedRelics.dll"
if args.verified_zip:
    with ZipFile(args.verified_zip) as verified:
        assert verified.testzip() is None, "The tested archive failed ZIP integrity checks."
        verified_manifest = json.loads(verified.read("manifest.json"))
        assert (verified_manifest["name"], verified_manifest["version_number"]) == (manifest["name"], version)
        dll = verified.read(dll_entry)
else:
    subprocess.run(["dotnet", "build", str(project), "-c", "Release"], cwd=root, check=True)
    build_dll = root / "bin/Release" / xml.findtext(".//TargetFramework") / "CursedRelics.dll"
    dll = build_dll.read_bytes()

files = {
    "LICENSE": (root / "LICENSE").read_bytes(),
    "CHANGELOG.md": (root / "CHANGELOG.md").read_bytes(),
    dll_entry: dll,
    "manifest.json": (listing / "manifest.json").read_bytes(),
    "README.md": (listing / "DETAILS.md").read_bytes(),
    "icon.png": (listing / "icon.png").read_bytes(),
}
dist = args.output_dir or root / "dist"
dist.mkdir(parents=True, exist_ok=True)
output = dist / f"CursedRelics-{version}.zip"
with TemporaryDirectory(dir=dist) as temp:
    staged = Path(temp) / output.name
    with ZipFile(staged, "w", ZIP_DEFLATED) as archive:
        for entry, contents in files.items():
            archive.writestr(entry, contents)
    with ZipFile(staged) as archive:
        assert archive.testzip() is None and sorted(archive.namelist()) == sorted(files)
        for entry, contents in files.items():
            assert archive.read(entry) == contents, entry
        assert json.loads(archive.read("manifest.json")) == manifest
        with Image.open(io.BytesIO(archive.read("icon.png"))) as icon:
            assert icon.format == "PNG" and icon.size == (256, 256)
            icon.load()
    staged.replace(output)
print(output)
