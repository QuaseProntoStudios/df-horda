# Changelog

Histórico de todas as versões do Dead Frequency: Horda. O site mostra a mesma lista.

Para lançar uma versão nova: mude `Current` em `Scripts/GameVersion.cs`, escreva a entrada no topo deste arquivo (`## vX.Y.Z — Título  (DD/MM/AAAA)`) e dê push na `main`.

## v0.3.1 — Launcher  (14/11/2026)

- **Launcher** próprio (Windows e Linux): instala o jogo, atualiza sozinho e abre com um clique. Vem junto de cada release.
- O launcher também se atualiza sozinho (dá para desligar na engrenagem).
- Dois canais de versão: **Estável** (releases) e **Beta** (builds novos do branch `beta`, com mais chance de bug). Dá para trocar a qualquer hora.
- Fonte trocada para a Silkscreen (nítida no tamanho do jogo) e menu sem o painel escuro, como antes: dá para ver o fundo de novo. Textos longos encurtados.
- Launcher 1.0.1: confere o checksum do jogo e do launcher baixados, recusa caminhos suspeitos dentro do zip, usa o proxy também na atualização do launcher e pergunta o canal na primeira vez. Checksums do release agora têm os nomes certos.
- Fluxo de trabalho novo: mudanças entram primeiro no branch `beta` e só chegam na `main` por pull request.

## v0.3.0 — Oficina da 96.6  (14/11/2026)

- Fonte pixel art nova (Pixelify Sans, licença OFL) em todo o jogo, com contorno nos textos e painel escuro atrás dos menus para ler melhor.
- Controle novo "Motor da van" nas configurações, e o motor agora fica mais alto que a música por padrão.
- 29 conquistas com aviso na tela ao desbloquear e aba **Conquistas** no menu. Algumas são secretas.
- **Fichas da 96.6**: ganhas ao fim de cada partida (e com conquistas), gastas na **Oficina** do menu em 9 upgrades permanentes: vida, velocidade, dano, ímã, XP, sorte, kits de reparo, largada com níveis e Segunda Chance.
- "Sair sem salvar" agora também paga as fichas da partida.

## v0.2.0 — Rádio Pirata  (14/11/2026)

- Menu inicial com Jogar, Continuar, Configurações e Sair. Pausa com Salvar e sair e Sair sem salvar.
- Configurações de áudio: volume geral, música, efeitos, inimigos e interface, com som de teste. Música padrão bem mais baixa. Tremor de tela e tela cheia.
- Seletor de dificuldade: Fácil, Normal, Difícil e Pesadelo.
- 15 upgrades (5 armas e 10 passivos) com 5 raridades coloridas: comum, incomum, raro, épico e lendário. Anúncios sem fim quando tudo estiver no máximo, então as escolhas nunca acabam.
- Armas novas: Chinelada, Carro do Ovo e Escapamento Tóxico.
- Inimigos novos: Corredor, Cuspidor, Brutamontes, Bomba Ambulante e Brutamontes Alfa. Hordas surpresa e o chefe final Zumbão da 96.6 aos 15:00.
- Dano de contato empilha por inimigo: ficar parado agora é fatal. Atropelar em velocidade continua protegendo a van dos fracos.
- Kits de reparo caem dos inimigos (sempre dos elites).
- Arte nova para os inimigos, projéteis e itens.
- Projeto no GitHub com Actions: verificação e build a cada push, release automático e site.

## v0.1.0 — Primeira volta  (14/11/2026)

- Primeira versão jogável: a van com inércia e derrapagem contra a horda ao som da Rádio 96.6.
- Antena 96.6 e Carro de Som como armas, quatro upgrades passivos e um tipo de inimigo.
- Sinais (XP), subida de nível com três anúncios para escolher, pausa e fim de jogo.
- Pixel art provisória gerada por código e sons do Dead Frequency Sound Pack (CC0).
