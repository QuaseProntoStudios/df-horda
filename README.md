# Dead Frequency: Horda

Spin-off 2D em pixel art de [Dead Frequency](https://github.com/dieguesmosken/dead-frequency).
A van contra a horda, ao som da rádio 96.6: sobreviva 15 minutos, junte sinais, escolha os anúncios (upgrades) e derrube o **Zumbão da 96.6**.

**Engine:** Godot 4.7.2 .NET (C# / .NET 10), renderizador Compatibility, tela base 320x180 com escala inteira.

## Como jogar

| Ação | Teclado | Controle |
|---|---|---|
| Acelerar / ré | W / S ou setas | gatilho direito / esquerdo |
| Virar | A / D ou setas | analógico esquerdo |
| Escolher upgrade | 1, 2, 3, setas + Enter ou clique | |
| Pausa | Esc ou P | |
| Rodar de novo / menu (fim de jogo) | Enter / Esc | |

Atropelar em velocidade machuca os inimigos e protege a van dos fracos. **Parado ou devagar, cada inimigo encostado morde junto**: a van não aguenta ficar parada.

## Launcher e canais

Baixe o `DeadFrequencyHordaLauncher` (Windows `.exe` ou Linux `.x86_64`) na [página de releases](https://github.com/dieguesmosken/df-horda/releases). Ele instala o jogo, atualiza sozinho e atualiza a si mesmo. Na engrenagem dá para trocar a pasta, ligar/desligar as atualizações automáticas e escolher o canal: **Estável** (releases) ou **Beta** (builds do branch `beta`, com mais chance de bug). O código está em `launcher/app/` (GDScript, Godot padrão); ao mudar `launcher.gd`, suba `LAUNCHER_VERSION` no script e `config/version` no `project.godot`.

## Fluxo de branches

Trabalho novo sai em um branch próprio (ex.: `feat/cenario`) e abre pull request para o `beta`: o CI roda, o merge é automático (squash) quando passa, o branch é apagado e sai um pré-lançamento `vX.Y.Z-beta.N` para testar. O CodeRabbit só revisa pull requests para a `main`. A `main` é protegida: só recebe pull request vindo do `beta`, com o CI passando e aprovação manual, e publica o release estável quando a versão em `Scripts/GameVersion.cs` muda.

## Menus e configurações

- Menu inicial: **Jogar** (escolhe a dificuldade), **Continuar** (retoma a partida salva), **Oficina** (upgrades permanentes com fichas), **Conquistas**, **Configurações**, **Sair**.
- Pausa: Continuar, Configurações, **Salvar e sair**, Sair sem salvar.
- Configurações: volume **geral, música, efeitos, inimigos e interface** (com som de teste), tremor de tela e tela cheia. Ficam salvas em `user://settings.cfg`.
- Conquistas (29) com aviso na tela, salvas em `user://achievements.cfg`; fichas e Oficina em `user://meta.cfg`.
- Fonte: Pixelify Sans (OFL), em `Fonts/`.
- Dificuldades: **Fácil, Normal, Difícil, Pesadelo** (vida, dano, velocidade, quantidade de inimigos, reparos e XP).

## Upgrades e raridades

5 armas (Antena 96.6, Carro de Som, Chinelada, Carro do Ovo, Escapamento Tóxico) e 10 passivos. Cada oferta sai com uma raridade, que multiplica o efeito:

| Raridade | Cor | Efeito |
|---|---|---|
| Comum | cinza | x1 |
| Incomum | verde | x1,25 |
| Raro | azul | x1,6 |
| Épico | roxo | x2,2 |
| Lendário | dourado | x3,2 |

Quando tudo estiver no máximo, entram anúncios sem fim (Sucata Aproveitada, Reparo Completo, Sinal Forte): o nível nunca fica sem escolha.

## Inimigos

Zumbi, Corredor (rápido), Cuspidor (atira de longe), Brutamontes (pesado, segura a van), Bomba Ambulante (acende o pavio e explode), Brutamontes Alfa (elite com hora marcada, larga kit de reparo), hordas surpresa e o chefe final aos 15:00. A vida deles cresce com o tempo.

## Estrutura

- `Scenes/main_menu.tscn` (cena inicial) e `Scenes/main.tscn` (partida); todo o resto é montado em código.
- `Scripts/Game.cs`: a partida (horda, armas, XP, níveis, chefe, pausa, fim de jogo). Os números de balanceamento ficam no topo.
- `Scripts/Upgrades.cs`, `Enemies.cs`, `Difficulty.cs`: dados de upgrades/raridades, inimigos e dificuldades.
- `Scripts/MainMenu.cs`, `MenuPanel.cs`, `SettingsPages.cs`, `GameSettings.cs`, `RunSave.cs`, `Session.cs`: menus, configurações e save.
- `Scripts/Van.cs`: direção com inércia e derrapagem.
- `Scripts/Hud.cs`, `UpgradeScreen.cs`, `UiStyle.cs`: interface verde fósforo.
- `Scripts/GameAudio.cs`: rádio, motor e efeitos (cada categoria no seu barramento de áudio).
- `Art/`: pixel art provisória, gerada por `Tools/make_art.py` (Python + Pillow).
- `Audio/`: sons e músicas do Dead Frequency Sound Pack (CC0, do mesmo autor).

## Estado

v0.2: menu, configurações de áudio, dificuldades, raridades, 15 upgrades, 5 novos tipos de inimigo, chefe final e dano bem mais pesado. O balanceamento ainda não foi muito testado: feedback é bem-vindo.

## Licença

Código sob [GPL-3.0](LICENSE), como o Dead Frequency. Os sons em `Audio/` são CC0 (ver `Audio/LICENSE.txt`).
