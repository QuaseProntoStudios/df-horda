"""Gera os dados do site: site/data/site.json (versão e histórico, vindos do CHANGELOG.md)
e copia os sprites de Art/ para site/assets/sprites/.
Rode da raiz do repositório:  python3 site/build.py   (o GitHub Actions roda antes de publicar)."""
import json
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "Tools"))
import changelog  # noqa: E402

SITE = os.path.join(ROOT, "site")
SPRITES = ["van", "zombie", "runner", "spitter", "brute", "bomber", "boss", "chinelo", "egg", "gas", "spit", "repair", "gem", "wave"]

os.makedirs(os.path.join(SITE, "data"), exist_ok=True)
releases = changelog.parse()
if not releases:
    sys.exit("CHANGELOG.md sem versões")
with open(os.path.join(SITE, "data", "site.json"), "w", encoding="utf-8") as f:
    json.dump({"version": changelog.current_version(), "changelog": releases}, f, ensure_ascii=False, indent=1)

dest = os.path.join(SITE, "assets", "sprites")
os.makedirs(dest, exist_ok=True)
for name in SPRITES:
    shutil.copy(os.path.join(ROOT, "Art", name + ".png"), os.path.join(dest, name + ".png"))
shutil.copy(os.path.join(ROOT, "icon.png"), os.path.join(SITE, "assets", "icon.png"))

print(f"site.json: v{changelog.current_version()}, {len(releases)} versões, {len(SPRITES)} sprites")
