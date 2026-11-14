# Launcher do Dead Frequency: Horda.
# 1ª vez: pergunta a pasta de instalação, as atualizações automáticas e o canal (Estável ou Beta).
# Depois: confere o release do canal escolhido no GitHub; instala ou atualiza o jogo e também atualiza o próprio launcher.
# Configurações em user://launcher.cfg; a versão instalada do jogo fica em <pasta>/version.txt.
extends Control

const LAUNCHER_VERSION := "1.0.1"
const REPO := "dieguesmosken/df-horda"
const API := "https://api.github.com/repos/%s/releases?per_page=30" % REPO
const RELEASES := "https://github.com/%s/releases" % REPO
const CFG := "user://launcher.cfg"

const GREEN := Color(0.55, 1.0, 0.62)
const GREEN_DIM := Color(0.28, 0.55, 0.33)
const AMBER := Color(1.0, 0.72, 0.25)
const CYAN := Color(0.35, 0.9, 1.0)
const RED := Color(1.0, 0.4, 0.35)
const BG := Color(0.03, 0.06, 0.05)

var cfg := ConfigFile.new()
var install_dir := ""
var auto_update := true
var installed := ""
var latest := ""
var asset_url := ""
var asset_name := ""
var launcher_name := ""
var release_body := ""
var asset_size := 0
var notes := ""
var channel := "stable"
var launcher_auto := true
var launcher_remote := ""
var launcher_url := ""

var http_api: HTTPRequest
var http_dl: HTTPRequest
var http_self: HTTPRequest
var _self_resolving := false
var _self_busy := false
var downloading := false
var dl_started := 0.0
var installing := false
var _resolving := false
var zip: ZIPReader
var zip_files: PackedStringArray
var zip_index := 0
var zip_root := ""

var version_label: Label
var status: Label
var bar: ProgressBar
var play: Button
var gear: Button
var dialog: PanelContainer
var dialog_box: VBoxContainer
var settings_open := false
var channel_badge: Label
var launcher_label: Label


func _ready() -> void:
	get_window().title = "Dead Frequency: Horda - Launcher"
	_cleanup_old_launcher()
	_build_ui()
	_load_cfg()
	_use_system_proxy()
	if install_dir == "":
		_ask_folder(true)
	elif not cfg.has_section_key("launcher", "auto_update"):
		_ask_auto_update()
	elif not cfg.has_section_key("launcher", "channel"):
		_ask_channel()
	else:
		_check()


# ================================================================ interface

func _build_ui() -> void:
	var bg := ColorRect.new()
	bg.color = BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	var glow := ColorRect.new()
	glow.set_anchors_preset(Control.PRESET_FULL_RECT)
	var sh := Shader.new()
	sh.code = """
shader_type canvas_item;
void fragment() {
	vec2 uv = UV;
	float g = smoothstep(0.9, 0.0, distance(uv, vec2(0.85, 0.15)));
	float line = step(0.5, fract(FRAGCOORD.y * 0.5)) * 0.06;
	float vig = smoothstep(0.45, 0.95, distance(uv, vec2(0.5)));
	COLOR = vec4(vec3(0.05, 0.22, 0.14) * g, g * 0.6) + vec4(0.0, 0.0, 0.0, line + vig * 0.5);
}"""
	var mat := ShaderMaterial.new()
	mat.shader = sh
	glow.material = mat
	glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(glow)

	var title := _label("DEAD", 84, Color.WHITE)
	title.position = Vector2(44, 52)
	title.add_theme_constant_override("outline_size", 14)
	title.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	add_child(title)
	var title2 := _label("FREQUENCY", 46, GREEN)
	title2.position = Vector2(48, 148)
	title2.add_theme_constant_override("outline_size", 10)
	title2.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	add_child(title2)
	var tag := _label("H O R D A   ·   sobreviva na van da rádio 96.6", 15, AMBER)
	tag.position = Vector2(50, 206)
	add_child(tag)

	version_label = _label("", 15, GREEN_DIM)
	version_label.position = Vector2(440, 16)
	version_label.size = Vector2(190, 24)
	version_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	add_child(version_label)
	channel_badge = _channel_badge(Vector2(660, 14))
	add_child(channel_badge)

	gear = _button("⚙", 22)
	gear.tooltip_text = "Configurações do launcher"
	gear.position = Vector2(16, 12)
	gear.size = Vector2(44, 40)
	gear.pressed.connect(_toggle_settings)
	add_child(gear)
	gear.text = ""
	gear.icon = _gear_icon()

	status = _label("", 16, Color(0.75, 0.82, 0.8))
	status.position = Vector2(44, 312)
	status.size = Vector2(440, 40)
	status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(status)

	bar = ProgressBar.new()
	bar.position = Vector2(44, 368)
	bar.size = Vector2(440, 16)
	bar.show_percentage = false
	var fill := StyleBoxFlat.new()
	fill.bg_color = CYAN
	fill.set_corner_radius_all(8)
	var back := StyleBoxFlat.new()
	back.bg_color = Color(0.1, 0.14, 0.15)
	back.set_corner_radius_all(8)
	bar.add_theme_stylebox_override("fill", fill)
	bar.add_theme_stylebox_override("background", back)
	add_child(bar)

	launcher_label = _label("Launcher v%s" % LAUNCHER_VERSION, 12, GREEN_DIM)
	launcher_label.position = Vector2(44, 396)
	add_child(launcher_label)

	play = _button("▶  JOGAR", 26)
	play.position = Vector2(516, 344)
	play.size = Vector2(200, 64)
	play.disabled = true
	play.pressed.connect(_launch)
	add_child(play)

	# Caixa central para perguntas e configurações.
	dialog = PanelContainer.new()
	var st := StyleBoxFlat.new()
	st.bg_color = Color(0.04, 0.08, 0.07, 0.97)
	st.border_color = GREEN_DIM
	st.set_border_width_all(2)
	st.set_corner_radius_all(10)
	st.set_content_margin_all(18)
	dialog.add_theme_stylebox_override("panel", st)
	dialog.position = Vector2(110, 60)
	dialog.size = Vector2(540, 300)
	dialog.custom_minimum_size = Vector2(540, 0)
	dialog.visible = false
	add_child(dialog)
	dialog_box = VBoxContainer.new()
	dialog_box.add_theme_constant_override("separation", 12)
	dialog.add_child(dialog_box)

	http_api = HTTPRequest.new()
	http_api.timeout = 15
	http_api.request_completed.connect(_on_api)
	add_child(http_api)
	http_dl = HTTPRequest.new()
	http_dl.use_threads = true
	http_dl.max_redirects = 10
	http_dl.request_completed.connect(_on_download)
	add_child(http_dl)
	http_self = HTTPRequest.new()
	http_self.use_threads = true
	http_self.max_redirects = 0
	http_self.request_completed.connect(_on_self_download)
	add_child(http_self)


## Etiqueta ao lado da versão: ESTÁVEL (verde) ou BETA (amarela).
func _channel_badge(pos: Vector2) -> Label:
	var l := _label("", 13, Color(0.1, 0.08, 0.02))
	l.position = pos
	return l


func _refresh_badge() -> void:
	var beta := channel == "beta"
	channel_badge.text = "BETA" if beta else "ESTÁVEL"
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(1.0, 0.8, 0.16) if beta else GREEN
	sb.set_corner_radius_all(3)
	sb.content_margin_left = 6
	sb.content_margin_right = 6
	sb.content_margin_top = 1
	sb.content_margin_bottom = 1
	channel_badge.add_theme_stylebox_override("normal", sb)
	channel_badge.position.x = 746 - channel_badge.get_minimum_size().x


func _label(text: String, size: int, color: Color) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	return l


func _button(text: String, size := 18, primary := true) -> Button:
	var b := Button.new()
	b.text = text
	b.add_theme_font_size_override("font_size", size)
	for state in ["normal", "hover", "pressed", "disabled", "focus"]:
		var sb := StyleBoxFlat.new()
		sb.set_corner_radius_all(10)
		sb.set_content_margin_all(10)
		match state:
			"normal": sb.bg_color = Color(0.12, 0.35, 0.22) if primary else Color(0.1, 0.13, 0.14)
			"hover": sb.bg_color = Color(0.18, 0.5, 0.3) if primary else Color(0.16, 0.2, 0.21)
			"pressed": sb.bg_color = Color(0.1, 0.28, 0.18)
			"disabled": sb.bg_color = Color(0.1, 0.12, 0.13)
			"focus":
				sb.bg_color = Color(0, 0, 0, 0)
				sb.border_color = GREEN
				sb.set_border_width_all(2)
		b.add_theme_stylebox_override(state, sb)
	b.add_theme_color_override("font_color", Color.WHITE)
	b.add_theme_color_override("font_disabled_color", Color(0.45, 0.5, 0.5))
	return b


func _gear_icon() -> Texture2D:
	var s := 26
	var img := Image.create_empty(s, s, false, Image.FORMAT_RGBA8)
	for y in s:
		for x in s:
			var d := Vector2(x - s * 0.5 + 0.5, y - s * 0.5 + 0.5)
			var r := d.length()
			var outer := 12.0 if cos(atan2(d.y, d.x) * 8.0) > 0.2 else 9.0
			img.set_pixel(x, y, GREEN if r <= outer and r >= 4.0 else Color(0, 0, 0, 0))
	return ImageTexture.create_from_image(img)


func _set_status(text: String, color := Color(0.75, 0.82, 0.8)) -> void:
	status.text = text
	status.add_theme_color_override("font_color", color)


func _refresh_version() -> void:
	installed = _read_installed()
	version_label.text = installed if installed != "" else "não instalado"
	_refresh_badge()
	play.disabled = not _game_exists() or downloading or installing


func _open_dialog(title: String, text: String) -> void:
	for c in dialog_box.get_children():
		c.queue_free()
	dialog_box.add_child(_label(title, 22, GREEN))
	if text != "":
		var l := _label(text, 15, Color(0.8, 0.86, 0.84))
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		l.custom_minimum_size = Vector2(500, 0)
		dialog_box.add_child(l)
	dialog.visible = true
	dialog.size = Vector2(540, 0)
	dialog.position = Vector2((760 - 540) * 0.5, 40)
	dialog.call_deferred("reset_size")


func _dialog_buttons(buttons: Array) -> void:
	var row := HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_END
	row.add_theme_constant_override("separation", 10)
	for b in buttons:
		row.add_child(b)
	dialog_box.add_child(row)
	dialog.call_deferred("reset_size")


func _close_dialog() -> void:
	dialog.visible = false
	settings_open = false


# ================================================================ configuração

func _default_dir() -> String:
	var home := OS.get_environment("USERPROFILE") if OS.get_name() == "Windows" else OS.get_environment("HOME")
	if home == "":
		home = OS.get_user_data_dir()
	return home.path_join("Games").path_join("Dead Frequency Horda")


func _load_cfg() -> void:
	cfg.load(CFG)
	install_dir = cfg.get_value("launcher", "install_dir", "")
	auto_update = cfg.get_value("launcher", "auto_update", true)
	launcher_auto = cfg.get_value("launcher", "launcher_auto", true)
	channel = str(cfg.get_value("launcher", "channel", "stable"))
	if channel != "beta":
		channel = "stable"
	_refresh_version()


func _save_cfg(save_auto := false, save_channel := false) -> void:
	cfg.set_value("launcher", "install_dir", install_dir)
	if save_channel or cfg.has_section_key("launcher", "channel"):
		cfg.set_value("launcher", "channel", channel)
	cfg.set_value("launcher", "launcher_auto", launcher_auto)
	if save_auto or cfg.has_section_key("launcher", "auto_update"):
		cfg.set_value("launcher", "auto_update", auto_update)
	cfg.save(CFG)


func _use_system_proxy() -> void:
	# Respeita HTTPS_PROXY/HTTP_PROXY (redes de empresa/escola).
	var p := OS.get_environment("HTTPS_PROXY")
	if p == "":
		p = OS.get_environment("https_proxy")
	if p == "":
		return
	p = p.replace("http://", "").replace("https://", "").trim_suffix("/")
	var host := p.get_slice(":", 0)
	var port := int(p.get_slice(":", 1)) if p.contains(":") else 8080
	for h in [http_api, http_dl, http_self]:
		h.set_https_proxy(host, port)
		h.set_http_proxy(host, port)


func _exe_name() -> String:
	return "DeadFrequencyHorda.exe" if OS.get_name() == "Windows" else "DeadFrequencyHorda.x86_64"


func _game_exists() -> bool:
	return install_dir != "" and FileAccess.file_exists(install_dir.path_join(_exe_name()))


func _read_installed() -> String:
	if install_dir == "":
		return ""
	var f := FileAccess.open(install_dir.path_join("version.txt"), FileAccess.READ)
	return f.get_as_text().strip_edges() if f else ""


# ================================================================ perguntas

func _ask_folder(first_run: bool) -> void:
	_open_dialog("Pasta de instalação", "Onde o Dead Frequency: Horda vai ficar? A pasta é criada se não existir. Seus saves ficam guardados à parte e não se perdem nas atualizações.")
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)
	var edit := LineEdit.new()
	edit.text = install_dir if install_dir != "" else _default_dir()
	edit.custom_minimum_size = Vector2(380, 38)
	edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(edit)
	var browse := _button("Procurar...", 15, false)
	browse.pressed.connect(func():
		var fd := FileDialog.new()
		fd.file_mode = FileDialog.FILE_MODE_OPEN_DIR
		fd.access = FileDialog.ACCESS_FILESYSTEM
		fd.use_native_dialog = true
		fd.title = "Escolha a pasta de instalação"
		fd.current_dir = edit.text.get_base_dir() if not DirAccess.dir_exists_absolute(edit.text) else edit.text
		fd.dir_selected.connect(func(d): edit.text = d.path_join("Dead Frequency Horda") if not d.ends_with("Dead Frequency Horda") else d)
		add_child(fd)
		fd.popup_centered(Vector2i(640, 420)))
	row.add_child(browse)
	dialog_box.add_child(row)
	var err := _label("", 14, RED)
	dialog_box.add_child(err)
	var ok := _button("Criar pasta e continuar", 17)
	ok.pressed.connect(func():
		var path := edit.text.strip_edges()
		if path == "":
			err.text = "Escolha uma pasta."
			return
		var e := DirAccess.make_dir_recursive_absolute(path)
		if e != OK and not DirAccess.dir_exists_absolute(path):
			err.text = "Não deu para criar a pasta (%s). Escolha outra." % error_string(e)
			return
		install_dir = path
		_save_cfg()
		_refresh_version()
		_close_dialog()
		if first_run and not cfg.has_section_key("launcher", "auto_update"):
			_ask_auto_update()
		else:
			_check())
	var buttons := [ok]
	if not first_run:
		var cancel := _button("Cancelar", 17, false)
		cancel.pressed.connect(_close_dialog)
		buttons.push_front(cancel)
	_dialog_buttons(buttons)


func _ask_auto_update() -> void:
	_open_dialog("Atualizações automáticas", "Quer que o launcher baixe e instale as versões novas sozinho sempre que abrir? Dá para mudar depois na engrenagem.")
	var yes := _button("Sim, atualizar sozinho", 17)
	var no := _button("Não, perguntar antes", 17, false)
	yes.pressed.connect(func(): _set_auto(true))
	no.pressed.connect(func(): _set_auto(false))
	_dialog_buttons([no, yes])


func _set_auto(value: bool) -> void:
	auto_update = value
	_save_cfg(true)
	_close_dialog()
	if not cfg.has_section_key("launcher", "channel"):
		_ask_channel()
	else:
		_check()


func _ask_channel() -> void:
	_open_dialog("Qual versão você quer?", "Estável: as versões lançadas, mais seguras.\nBeta: as novidades antes de todo mundo, com mais chance de bug. Dá para trocar depois na engrenagem.")
	var stable := _button("Estável (recomendado)", 17)
	var beta := _button("Beta", 17, false)
	stable.pressed.connect(func(): _set_channel("stable"))
	beta.pressed.connect(func(): _set_channel("beta"))
	_dialog_buttons([beta, stable])


func _set_channel(value: String, recheck := true) -> void:
	channel = value
	_save_cfg(true, true)
	_refresh_badge()
	_close_dialog()
	if recheck:
		_check()


func _ask_download(update: bool) -> void:
	var mb := "%.0f MB" % (asset_size / 1048576.0)
	if update:
		_open_dialog("Versão disponível (%s)" % _channel_name(), "A versão %s está disponível (você tem a %s). Baixar agora? (%s)\n\n%s" % [latest, installed, mb, notes])
	else:
		_open_dialog("Instalar o Dead Frequency: Horda", "O jogo ainda não está instalado nesta pasta:\n%s\n\nBaixar a versão %s agora? (%s)" % [install_dir, latest, mb])
	var yes := _button("Atualizar" if update else "Baixar e instalar", 17)
	var no := _button("Jogar assim mesmo" if update else "Agora não", 17, false)
	yes.pressed.connect(func():
		_close_dialog()
		_start_download())
	no.pressed.connect(func():
		_close_dialog()
		_set_status("Atualização adiada. Você está na %s." % installed if update else "Clique na engrenagem quando quiser instalar.", AMBER))
	_dialog_buttons([no, yes])


func _toggle_settings() -> void:
	if settings_open:
		_close_dialog()
		return
	if downloading or installing:
		return
	settings_open = true
	_open_dialog("Configurações", "")
	dialog_box.add_child(_label("Pasta de instalação:", 15, GREEN_DIM))
	dialog_box.add_theme_constant_override("separation", 8)
	var path := _label(install_dir if install_dir != "" else "(nenhuma)", 15, Color.WHITE)
	path.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
	path.custom_minimum_size = Vector2(500, 0)
	dialog_box.add_child(path)
	var auto := CheckButton.new()
	auto.text = "Atualizar automaticamente ao abrir"
	auto.button_pressed = auto_update
	auto.add_theme_font_size_override("font_size", 16)
	auto.toggled.connect(func(v):
		auto_update = v
		_save_cfg(true))
	dialog_box.add_child(auto)
	var lauto := CheckButton.new()
	lauto.text = "Atualizar o próprio launcher (v%s)" % LAUNCHER_VERSION
	lauto.button_pressed = launcher_auto
	lauto.add_theme_font_size_override("font_size", 16)
	lauto.toggled.connect(func(v):
		launcher_auto = v
		_save_cfg(true))
	dialog_box.add_child(lauto)
	var crow := HBoxContainer.new()
	crow.add_theme_constant_override("separation", 10)
	crow.add_child(_label("Versão do jogo:", 16, Color.WHITE))
	var opt := OptionButton.new()
	opt.add_theme_font_size_override("font_size", 16)
	opt.add_item("Estável", 0)
	opt.add_item("Beta (novidades, pode ter bugs)", 1)
	opt.selected = 1 if channel == "beta" else 0
	opt.item_selected.connect(func(i):
		_set_channel("beta" if i == 1 else "stable"))
	crow.add_child(opt)
	dialog_box.add_child(crow)
	var row1 := HBoxContainer.new()
	row1.add_theme_constant_override("separation", 8)
	var change := _button("Trocar pasta", 15, false)
	change.pressed.connect(func():
		_close_dialog()
		_ask_folder(false))
	var open := _button("Abrir pasta", 15, false)
	open.pressed.connect(func():
		if install_dir != "":
			DirAccess.make_dir_recursive_absolute(install_dir)
			OS.shell_open(install_dir))
	var check := _button("Verificar agora", 15, false)
	check.pressed.connect(func():
		_close_dialog()
		_check())
	var reinstall := _button("Reinstalar", 15, false)
	reinstall.pressed.connect(func():
		_close_dialog()
		if asset_url != "":
			_start_download()
		else:
			_check(true))
	for b in [change, open, check, reinstall]:
		row1.add_child(b)
	dialog_box.add_child(row1)
	var site := _button("Página de releases", 15, false)
	site.tooltip_text = "Abre github.com/%s/releases" % REPO
	site.pressed.connect(func(): OS.shell_open(RELEASES))
	var close := _button("Fechar", 17)
	close.pressed.connect(_close_dialog)
	_dialog_buttons([site, close])


# ================================================================ GitHub

func _check(force_download := false) -> void:
	_refresh_version()
	_set_status("Procurando atualizações...")
	bar.value = 0
	var err := http_api.request(API, ["User-Agent: DeadFrequencyHordaLauncher", "Accept: application/vnd.github+json"])
	if err != OK:
		_offline("Não deu para procurar atualizações (%s)." % error_string(err))
	set_meta("force", force_download)


func _offline(msg: String) -> void:
	if _game_exists():
		_set_status(msg + " Jogando a versão instalada.", AMBER)
	else:
		_set_status(msg + " Confira a internet e clique na engrenagem > Verificar agora.", RED)
	_refresh_version()


func _channel_name() -> String:
	return "Beta" if channel == "beta" else "Estável"


func _on_api(result: int, code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if result != HTTPRequest.RESULT_SUCCESS or code != 200:
		_offline("Sem conexão com o GitHub.")
		return
	var data = JSON.parse_string(body.get_string_from_utf8())
	if typeof(data) != TYPE_ARRAY:
		_offline("Resposta estranha do GitHub.")
		return

	# Estável: o release mais novo que não é pré-lançamento. Beta: o mais novo de todos.
	var release = null
	for r in data:
		if typeof(r) != TYPE_DICTIONARY or r.get("draft", false):
			continue
		if channel == "stable" and r.get("prerelease", false):
			continue
		release = r
		break
	if release == null:
		_offline("Nenhum release no canal %s ainda." % _channel_name())
		return

	latest = str(release.get("tag_name", ""))
	release_body = str(release.get("body", ""))
	notes = release_body.split("\n---")[0].split("<!--")[0].strip_edges().left(420)
	asset_url = ""
	launcher_url = ""
	launcher_remote = ""
	var platform := "windows" if OS.get_name() == "Windows" else "linux"
	for a in release.get("assets", []):
		var n := str(a.get("name", ""))
		if n.begins_with("DeadFrequencyHorda-") and n.contains(platform) and n.ends_with(".zip"):
			asset_url = str(a.get("browser_download_url", ""))
			asset_name = n
			asset_size = int(a.get("size", 0))
		# DeadFrequencyHordaLauncher-1.0.0-windows.exe / ...-linux.x86_64
		elif n.begins_with("DeadFrequencyHordaLauncher-") and n.contains("-" + platform):
			launcher_url = str(a.get("browser_download_url", ""))
			launcher_name = n
			launcher_remote = n.trim_prefix("DeadFrequencyHordaLauncher-").get_slice("-", 0)
	if asset_url == "":
		_offline("O release %s não tem o arquivo para %s." % [latest, platform])
		return

	# Primeiro o launcher: se ele se atualizar, reinicia e o resto acontece na versão nova.
	if _launcher_outdated():
		if launcher_auto:
			_start_self_update()
			return
		_set_status("Launcher novo disponível (%s). Atualize na engrenagem." % launcher_remote, AMBER)

	var force: bool = get_meta("force", false)
	if not _game_exists():
		_set_status("Jogo não instalado.", AMBER)
		_ask_download(false)
	elif force:
		_start_download()
	elif latest != installed:
		if auto_update:
			_start_download()
		else:
			_set_status("Versão nova disponível: %s." % latest, AMBER)
			_ask_download(true)
	else:
		_set_status("Tudo atualizado (%s, canal %s). Bom turno!" % [installed, _channel_name()], GREEN)
		bar.value = 100
	_refresh_version()


func _compare(a: String, b: String) -> int:
	var x := a.trim_prefix("v").split(".")
	var y := b.trim_prefix("v").split(".")
	for i in 3:
		var xi := int(x[i]) if i < x.size() else 0
		var yi := int(y[i]) if i < y.size() else 0
		if xi != yi:
			return 1 if xi > yi else -1
	return 0


# ================================================================ download e instalação

func _zip_path() -> String:
	return install_dir.path_join(".download.zip")


func _start_download() -> void:
	DirAccess.make_dir_recursive_absolute(install_dir)
	downloading = true
	dl_started = Time.get_ticks_msec() / 1000.0
	# O GitHub responde com um redirecionamento para o servidor de arquivos: pegamos o endereço
	# final primeiro (sem baixar nada) e depois baixamos direto de lá.
	http_dl.download_file = ""
	http_dl.max_redirects = 0
	_resolving = true
	var err := http_dl.request(asset_url, ["User-Agent: DeadFrequencyHordaLauncher"])
	if err != OK:
		downloading = false
		_set_status("Falha ao iniciar o download (%s)." % error_string(err), RED)
	_refresh_version()


func _process(_delta: float) -> void:
	if downloading:
		var done := http_dl.get_downloaded_bytes()
		var total := http_dl.get_body_size()
		if total <= 0:
			total = asset_size
		var secs := maxf(0.1, Time.get_ticks_msec() / 1000.0 - dl_started)
		bar.value = 100.0 * done / maxf(1.0, total)
		_set_status("Baixando %s  ·  %.0f / %.0f MB  ·  %.1f MB/s" % [latest, done / 1048576.0, total / 1048576.0, done / 1048576.0 / secs])
	elif installing:
		_install_step()


func _on_download(result: int, code: int, headers: PackedStringArray, _b: PackedByteArray) -> void:
	if _resolving:
		_resolving = false
		var url := asset_url
		if code >= 300 and code < 400:
			for h in headers:
				if h.to_lower().begins_with("location:"):
					url = h.substr(9).strip_edges()
		http_dl.max_redirects = 10
		http_dl.download_file = _zip_path()
		dl_started = Time.get_ticks_msec() / 1000.0
		var err := http_dl.request(url, ["User-Agent: DeadFrequencyHordaLauncher"])
		if err != OK:
			downloading = false
			_set_status("Falha ao iniciar o download (%s)." % error_string(err), RED)
			_refresh_version()
		return
	downloading = false
	if result != HTTPRequest.RESULT_SUCCESS or code != 200:
		DirAccess.remove_absolute(_zip_path())
		_set_status("O download falhou (%d / %d). Tente de novo pela engrenagem." % [result, code], RED)
		_refresh_version()
		return
	if not _hash_ok(_zip_path(), asset_name):
		DirAccess.remove_absolute(_zip_path())
		_set_status("O arquivo baixado não confere com o checksum do release. Tente de novo.", RED)
		_refresh_version()
		return
	zip = ZIPReader.new()
	if zip.open(_zip_path()) != OK:
		_set_status("O arquivo baixado está corrompido. Tente de novo.", RED)
		_refresh_version()
		return
	zip_files = zip.get_files()
	zip_index = 0
	# Se o zip tiver uma pasta por fora, tira ela.
	zip_root = ""
	for f in zip_files:
		if f.get_file() == _exe_name() and f.contains("/"):
			zip_root = f.get_base_dir() + "/"
	installing = true
	_refresh_version()


func _install_step() -> void:
	# Um arquivo por quadro, para a barra andar e a janela não travar.
	if zip_index >= zip_files.size():
		zip.close()
		installing = false
		DirAccess.remove_absolute(_zip_path())
		var vf := FileAccess.open(install_dir.path_join("version.txt"), FileAccess.WRITE)
		if vf:
			vf.store_string(latest)
			vf.close()
		if OS.get_name() != "Windows":
			OS.execute("chmod", ["+x", install_dir.path_join(_exe_name())])
		bar.value = 100
		_set_status("Pronto! Dead Frequency %s instalado." % latest, GREEN)
		_refresh_version()
		return
	var name := zip_files[zip_index]
	zip_index += 1
	bar.value = 100.0 * zip_index / zip_files.size()
	_set_status("Instalando %s  ·  %d/%d" % [latest, zip_index, zip_files.size()])
	var rel := name.trim_prefix(zip_root)
	if not _safe_rel(rel):
		installing = false
		zip.close()
		DirAccess.remove_absolute(_zip_path())
		_set_status("O pacote tem um caminho inválido (%s). Instalação cancelada." % rel, RED)
		_refresh_version()
		return
	if rel == "" or rel.ends_with("/"):
		DirAccess.make_dir_recursive_absolute(install_dir.path_join(rel))
		return
	var target := install_dir.path_join(rel)
	DirAccess.make_dir_recursive_absolute(target.get_base_dir())
	var f := FileAccess.open(target, FileAccess.WRITE)
	if f == null:
		installing = false
		zip.close()
		_set_status("Não deu para gravar %s (o jogo está aberto?). Feche o jogo e tente de novo." % rel, RED)
		_refresh_version()
		return
	f.store_buffer(zip.read_file(name))
	f.close()


# ================================================================ jogar

func _launch() -> void:
	if not _game_exists():
		return
	var pid := OS.create_process(install_dir.path_join(_exe_name()), ["--from-launcher"])
	if pid <= 0:
		_set_status("Não deu para abrir o jogo.", RED)
		return
	_set_status("Abrindo o jogo... boa entrega!", GREEN)
	await get_tree().create_timer(1.0).timeout
	get_tree().quit()


# ================================================================ atualização do próprio launcher

func _launcher_outdated() -> bool:
	# No editor do Godot não existe executável do launcher para trocar.
	if OS.has_feature("editor"):
		return false
	return launcher_url != "" and launcher_remote != "" and _compare(launcher_remote, LAUNCHER_VERSION) > 0


func _cleanup_old_launcher() -> void:
	var old := OS.get_executable_path() + ".old"
	if FileAccess.file_exists(old):
		DirAccess.remove_absolute(old)


func _self_new_path() -> String:
	return OS.get_executable_path() + ".new"


func _start_self_update() -> void:
	if _self_busy:
		return
	_self_busy = true
	_set_status("Atualizando o launcher para a versão %s..." % launcher_remote, CYAN)
	bar.value = 0
	http_self.download_file = ""
	http_self.max_redirects = 0
	_self_resolving = true
	var err := http_self.request(launcher_url, ["User-Agent: DeadFrequencyHordaLauncher"])
	if err != OK:
		_self_failed("Não deu para baixar o launcher novo (%s)." % error_string(err))


func _on_self_download(result: int, code: int, headers: PackedStringArray, _b: PackedByteArray) -> void:
	if _self_resolving:
		_self_resolving = false
		var url := launcher_url
		if code >= 300 and code < 400:
			for h in headers:
				if h.to_lower().begins_with("location:"):
					url = h.substr(9).strip_edges()
		http_self.max_redirects = 10
		http_self.download_file = _self_new_path()
		var err := http_self.request(url, ["User-Agent: DeadFrequencyHordaLauncher"])
		if err != OK:
			_self_failed("Não deu para baixar o launcher novo (%s)." % error_string(err))
		return
	if result != HTTPRequest.RESULT_SUCCESS or code != 200:
		DirAccess.remove_absolute(_self_new_path())
		_self_failed("O download do launcher falhou (%d / %d)." % [result, code])
		return
	if not _hash_ok(_self_new_path(), launcher_name):
		DirAccess.remove_absolute(_self_new_path())
		_self_failed("O launcher novo não confere com o checksum do release.")
		return
	_swap_launcher()


func _swap_launcher() -> void:
	# O executável em uso não pode ser apagado, mas pode ser renomeado.
	var exe := OS.get_executable_path()
	var old := exe + ".old"
	if FileAccess.file_exists(old):
		DirAccess.remove_absolute(old)
	if DirAccess.rename_absolute(exe, old) != OK:
		DirAccess.remove_absolute(_self_new_path())
		_self_failed("Sem permissão para trocar o launcher nesta pasta. Baixe o novo na página de releases.")
		return
	if DirAccess.rename_absolute(_self_new_path(), exe) != OK:
		DirAccess.rename_absolute(old, exe)
		_self_failed("Não deu para instalar o launcher novo.")
		return
	if OS.get_name() != "Windows":
		OS.execute("chmod", ["+x", exe])
	_set_status("Launcher atualizado para %s! Reiniciando..." % launcher_remote, GREEN)
	bar.value = 100
	OS.create_process(exe, [])
	await get_tree().create_timer(0.6).timeout
	get_tree().quit()


func _self_failed(msg: String) -> void:
	_self_busy = false
	_set_status(msg + " Seguindo com o launcher atual.", AMBER)
	# Segue o fluxo normal do jogo mesmo sem atualizar o launcher.
	launcher_url = ""
	_check_after_self_failure()


func _check_after_self_failure() -> void:
	if not _game_exists():
		_ask_download(false)
	elif latest != installed:
		if auto_update:
			_start_download()
		else:
			_ask_download(true)
	_refresh_version()


# ================================================================ segurança

## Checksum esperado do arquivo, escrito pelo CI nas notas do release como <!-- sha256:NOME:HASH -->.
func _expected_hash(file_name: String) -> String:
	var re := RegEx.new()
	re.compile("sha256:([^:\\s]+):([0-9a-fA-F]{64})")
	for m in re.search_all(release_body):
		if m.get_string(1) == file_name:
			return m.get_string(2).to_lower()
	return ""


## Releases antigos não têm checksum nas notas: nesses, segue sem conferir.
func _hash_ok(path: String, file_name: String) -> bool:
	var expected := _expected_hash(file_name)
	if expected == "":
		return true
	return FileAccess.get_sha256(path) == expected


## Recusa caminhos absolutos ou com ".." dentro do zip (Zip Slip).
func _safe_rel(rel: String) -> bool:
	if rel.begins_with("/") or rel.begins_with("\\") or rel.contains(":"):
		return false
	for part in rel.replace("\\", "/").split("/"):
		if part == "..":
			return false
	return true
