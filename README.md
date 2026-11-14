# Dead Frequency: Horda

Spin-off 2D em pixel art de [Dead Frequency](https://github.com/dieguesmosken/dead-frequency).
A van contra a horda, ao som da rádio 96.6: sobreviva 15 minutos, junte sinais e escolha os anúncios (upgrades).

**Engine:** Godot 4.7.2 .NET (C# / .NET 10), renderizador Compatibility, tela base 320x180 com escala inteira.

## Como jogar

| Ação | Teclado | Controle |
|---|---|---|
| Acelerar / ré | W / S ou setas | gatilho direito / esquerdo |
| Virar | A / D ou setas | analógico esquerdo |
| Escolher upgrade | 1, 2, 3 ou clique | |
| Pausa | Esc ou P | |
| Rodar de novo | Enter | |

Atropelar em velocidade causa dano e não machuca a van. Parado ou devagar, a horda bate em você.

## Estrutura

- `Scenes/main.tscn`: cena única; todo o resto é montado em código.
- `Scripts/Game.cs`: a partida (horda, armas, sinais, níveis, fim de jogo). Os números de balanceamento ficam no topo.
- `Scripts/Van.cs`: direção com inércia e derrapagem.
- `Scripts/Upgrades.cs`: catálogo dos anúncios; os efeitos são aplicados em `Game.cs`.
- `Scripts/Hud.cs`, `UpgradeScreen.cs`, `UiStyle.cs`: interface verde fósforo.
- `Scripts/GameAudio.cs`: rádio, motor e efeitos.
- `Art/`: pixel art provisória, gerada por `Tools/make_art.py` (Python + Pillow).
- `Audio/`: sons e músicas do Dead Frequency Sound Pack (CC0, do mesmo autor).

## Estado

v0.1: primeira versão jogável. Um tipo de inimigo, Antena 96.6, Carro de Som e quatro upgrades passivos.
