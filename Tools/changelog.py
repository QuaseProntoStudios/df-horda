"""Lê o CHANGELOG.md. Usado pelo site, pelo release e pela verificação do CI.

    python3 Tools/changelog.py check          confere se a versão do GameVersion.cs é a do topo do changelog
    python3 Tools/changelog.py notes 0.2.0    imprime o texto do release dessa versão
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HEADING = re.compile(r"^## v(\d+\.\d+\.\d+) — (.+?)\s+\((\d\d/\d\d/\d{4})\)\s*$")


def current_version():
    src = open(os.path.join(ROOT, "Scripts", "GameVersion.cs"), encoding="utf-8").read()
    return re.search(r'Current\s*=\s*"([^"]+)"', src).group(1)


def parse():
    """Devolve a lista de versões, da mais nova para a mais antiga."""
    releases = []
    for line in open(os.path.join(ROOT, "CHANGELOG.md"), encoding="utf-8").read().splitlines():
        m = HEADING.match(line)
        if m:
            releases.append({"version": m.group(1), "title": m.group(2), "date": m.group(3), "changes": []})
        elif releases and line.startswith("- "):
            releases[-1]["changes"].append(line[2:].strip())
    return releases


def check():
    releases = parse()
    problems = []
    if not releases:
        problems.append("CHANGELOG.md não tem nenhuma entrada no formato '## vX.Y.Z — Título  (DD/MM/AAAA)'")
    else:
        if releases[0]["version"] != current_version():
            problems.append(f"GameVersion.Current é {current_version()}, mas o topo do CHANGELOG.md é {releases[0]['version']}")
        seen = set()
        for r in releases:
            if r["version"] in seen:
                problems.append(f"versão {r['version']} repetida no CHANGELOG.md")
            seen.add(r["version"])
            if not r["changes"]:
                problems.append(f"versão {r['version']} sem nenhum item")
    if problems:
        for p in problems:
            print("ERRO:", p)
        sys.exit(1)
    print(f"Versão {current_version()} confere com o changelog ({len(releases)} versões).")


def notes(version):
    version = version.lstrip("v")
    for r in parse():
        if r["version"] == version:
            repo = "https://github.com/dieguesmosken/df-horda"
            dl = f"{repo}/releases/download/v{version}/"
            print(f"## v{version} — {r['title']}\n*{r['date']}*\n")
            for c in r["changes"]:
                print(f"- {c}")
            print("\n### Baixar\n")
            print("| Sistema | Arquivo |\n|---|---|")
            print(f"| Windows (extraia e abra `DeadFrequencyHorda.exe`) | [DeadFrequencyHorda-v{version}-windows.zip]({dl}DeadFrequencyHorda-v{version}-windows.zip) |")
            print(f"| Linux (extraia e rode `./DeadFrequencyHorda.x86_64`) | [DeadFrequencyHorda-v{version}-linux.zip]({dl}DeadFrequencyHorda-v{version}-linux.zip) |")
            print(f"\nConfira o SHA-256 com o [SHA256SUMS.txt]({dl}SHA256SUMS.txt) (no Windows: `certutil -hashfile ARQUIVO SHA256`).")
            print("\nJogo grátis e de código aberto (GPL-3.0). Histórico completo: [CHANGELOG.md](" + repo + "/blob/main/CHANGELOG.md).")
            return
    sys.exit(f"versão {version} não está no CHANGELOG.md")


if __name__ == "__main__":
    if len(sys.argv) >= 2 and sys.argv[1] == "check":
        check()
    elif len(sys.argv) >= 3 and sys.argv[1] == "notes":
        notes(sys.argv[2])
    else:
        sys.exit(__doc__)
