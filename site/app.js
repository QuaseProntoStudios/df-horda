// Preenche o site com os dados gerados (site/data/site.json) e os links do último release.
const REPO = "QuaseProntoStudios/df-horda";

function el(tag, attrs = {}, text = "") {
  const node = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs)) node.setAttribute(k, v);
  if (text) node.textContent = text;
  return node;
}

async function loadSite() {
  try {
    const data = await (await fetch("data/site.json", { cache: "no-cache" })).json();
    document.getElementById("hero-version").textContent = "v" + data.version;
    document.getElementById("dl-version").textContent = "v" + data.version;

    const box = document.getElementById("changelog");
    box.textContent = "";
    data.changelog.forEach((release, index) => {
      const details = el("details");
      if (index === 0) details.setAttribute("open", "");
      const summary = el("summary");
      summary.append(el("span", { class: "v" }, "v" + release.version), el("span", {}, release.title));
      if (index === 0) summary.append(el("span", { class: "new" }, "ATUAL"));
      summary.append(el("span", { class: "d" }, release.date));
      const list = el("ul");
      release.changes.forEach((change) => list.append(el("li", {}, change)));
      details.append(summary, list);
      box.append(details);
    });
  } catch (e) {
    document.getElementById("changelog").textContent = "Não foi possível carregar o histórico. Veja o CHANGELOG.md no GitHub.";
  }
}

async function loadRelease() {
  try {
    const res = await fetch(`https://api.github.com/repos/${REPO}/releases/latest`);
    if (!res.ok) return;
    const release = await res.json();
    const find = (suffix) => (release.assets || []).find((a) => a.name.endsWith(suffix));
    const set = (id, asset) => { if (asset) document.getElementById(id).href = asset.browser_download_url; };
    const win = find("-windows.zip");
    set("dl-win", win);
    set("hero-download", win);
    set("dl-linux", find("-linux.zip"));
    set("dl-sums", find("SHA256SUMS.txt"));
    if (release.published_at) {
      const date = new Date(release.published_at).toLocaleDateString("pt-BR");
      document.getElementById("dl-date").textContent = "(publicada em " + date + ")";
    }
  } catch (e) { /* sem release ainda: os botões levam à página de releases */ }
}

loadSite();
loadRelease();
