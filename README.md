# AiStarEnemyGame

AiStarEnemyGame é um protótipo de labirinto e suspense para PC, feito em Unity, com visão 2D de cima. Você precisa encontrar a saída enquanto foge de um inimigo que usa A* para calcular caminhos. Conforme você vence partidas, ele guarda suas rotas e tenta antecipar suas escolhas.

O jogo é para um jogador, sem monetização, e foi criado para estudo e portfólio. Ele reúne experiências com navegação em grade, estados de jogo, saves locais e adaptação do inimigo ao jogador.

## Como jogar

Clique em Iniciar no menu e aguarde a contagem `3, 2, 1, GO!`. A partir daí, procure a saída antes que o inimigo alcance você.

| Controle | Ação |
| --- | --- |
| W | Mover para cima |
| S | Mover para baixo |
| A | Mover para a esquerda |
| D | Mover para a direita |
| Mouse | Interagir com os botões da interface |

Você anda de célula em célula, em quatro direções e sem diagonais. Segurar uma tecla mantém o movimento. Os personagens deslizam entre os centros das células e viram na direção em que estão andando.

O botão Excluir Save apaga o progresso e a memória da IA. Sair fecha o executável; no Editor, apenas registra uma mensagem no Console.

## Fluxo de uma partida

1. A cena carrega a memória salva da IA e mostra o número da fase.
2. A contagem inicial mantém os personagens parados. Cada número dura 1 segundo; `GO!` permanece por 0,5 segundo.
3. Durante a partida, o jogador se move pelo labirinto e sua rota é registrada. O inimigo escolhe um alvo e busca um caminho até ele.
4. Se o inimigo tocar o gatilho de captura do jogador, os personagens param, a tentativa é descartada e a tela de derrota aparece após 0,75 segundo.
5. Se o jogador alcançar a saída, os personagens param e a tela de vitória aparece. A rota fica pendente de confirmação.
6. Ao clicar no botão de próxima fase, você confirma a vitória. A IA aprende com a rota, salva o progresso e recarrega a cena de jogo.

Se você vencer e voltar ao menu, essa rota não será aprendida e o número da fase não aumentará. A confirmação acontece em `StartNextLevel()`, no botão de próxima fase. Tentativas que terminam em derrota também não entram na memória da IA.

O `GameFlowManager` controla os estados `Countdown`, `Playing`, `DeathSequence`, `Dead`, `Victory` e `Transition`. Os personagens só se movem e o inimigo só toma decisões em `Playing`. As cenas são carregadas de forma assíncrona.

## Como funciona a IA

### Grade de navegação

`GridNavigation2D` consulta os Tilemaps de chão e parede para saber onde é possível andar. A regra é simples: a célula precisa ter chão e estar livre de parede. Jogador, inimigo e busca de caminho seguem essa mesma regra.

As posições do mundo são convertidas em coordenadas de grade. A navegação considera somente os vizinhos norte, leste, sul e oeste.

### Busca de caminho com A*

`AStarPathfinder2D` calcula uma rota entre a célula do inimigo e a célula escolhida como alvo. Cada passo custa 1. A prioridade de busca usa:

```text
f = g + h

g = custo acumulado desde a origem
h = distância Manhattan até o destino
h = |origem.x - destino.x| + |origem.y - destino.y|
```

A busca mantém uma lista de candidatos e um conjunto de células já examinadas. Escolhe o menor `f`, usando o menor `h` como desempate. Ao chegar ao alvo, reconstrói o caminho pelos nós anteriores. Destinos inválidos ou sem caminho produzem uma lista vazia.

O inimigo anda um passo da rota e calcula o próximo movimento ao terminar. Assim, acompanha a posição do jogador ao longo da partida.

### Memória das rotas vencedoras

`PlayerPathRecorder2D` registra as células percorridas, evitando repetições consecutivas da mesma célula. Ao confirmar uma vitória, `AIMemoryManager` conta as transições feitas a partir de cada posição: quantas vezes o jogador saiu para norte, sul, leste ou oeste.

Se você passou por uma célula e saiu três vezes pela direita e uma vez por cima, a IA passa a considerar a direita como a escolha mais provável. Nesse caso, a confiança é de `3 / 4 = 0,75`.

O aprendizado usa a frequência dos movimentos em cada célula. A memória ajuda a escolher o destino do inimigo, e o A* encontra o caminho até lá. Todo esse comportamento é local, sem rede neural, treinamento de modelo externo ou serviço de IA online.

### Previsão e interceptação

`EnemyAStarController2D` considera a célula atual do jogador ou, caso ele esteja se movendo, sua célula de destino. Com memória suficiente e confiança mínima, tenta prever células seguintes e usa a posição prevista como alvo do A*.

A previsão segue a direção historicamente mais frequente e para quando encontra uma célula sem histórico, um caminho bloqueado, uma repetição de célula ou o limite de passos. A confiança é verificada na célula inicial da previsão. Empates entre direções usam a ordem norte, leste, sul e oeste.

Quando a confiança é baixa, o inimigo persegue a célula onde o jogador está ou aquela para a qual está andando. Se a previsão não produzir uma rota com um próximo passo, ele também volta a buscar um caminho até o jogador.

### Dificuldade progressiva

Os valores abaixo estão configurados em `Level 1` e podem ser ajustados pelo Inspector:

| Parâmetro | Valor atual |
| --- | --- |
| Velocidade do jogador | 4 unidades de mundo/s |
| Velocidade inicial do inimigo | 3 unidades de mundo/s |
| Aumento por vitória confirmada | 0,05 unidade de mundo/s |
| Velocidade máxima do inimigo | 6 unidades de mundo/s |
| Vitórias para ganhar um passo de previsão | 1 |
| Limite de previsão | 12 células |
| Confiança mínima para prever | 0,5 |

```text
velocidade do inimigo = min(3 + vitórias confirmadas × 0,05; 6)
profundidade de previsão = min(vitórias confirmadas; 12)
```

A velocidade é definida no início da cena. Já a previsão depende do histórico disponível: mesmo com o limite de 12 células liberado, o inimigo pode parar a previsão antes disso se não conhecer o caminho.

## Como as fases funcionam hoje

Existem duas cenas habilitadas na lista de build:

| Índice | Cena | Função |
| --- | --- | --- |
| 0 | `Assets/_Project/Scenes/Menu.unity` | Iniciar, excluir save e sair |
| 1 | `Assets/_Project/Scenes/Level 1.unity` | Labirinto, perseguição, vitória e derrota |

Por enquanto, a próxima fase usa o mesmo labirinto. Cada vitória confirmada aumenta o número da fase e a dificuldade do inimigo. A geração procedural de labirintos ainda não foi implementada.

A memória identifica as células pelas coordenadas, sem separar os dados por mapa. Ao adicionar outros labirintos, será preciso distinguir os layouts no save para não misturar as escolhas feitas em cada um.

## Save local

O arquivo `maze_ai_memory.json` fica em `Application.persistentDataPath`, fora da pasta de assets. Ele armazena:

- Versão do formato de dados.
- Fase atual e quantidade de vitórias confirmadas.
- Contagem de direções escolhidas em cada célula.
- Histórico de até 100 rotas vencedoras, na configuração atual.

O limite de 100 vale para as rotas guardadas. Quando uma rota antiga sai da lista, suas contagens de direção continuam na memória acumulada.

O save é carregado ao iniciar o gerenciador e gravado ao confirmar a próxima fase. Sem arquivo, a partida começa na fase 1, sem conhecimento prévio. Erros de leitura são registrados no Console e fazem o gerenciador usar dados iniciais.

Para descobrir onde o arquivo foi salvo, abra o menu de contexto do componente `AIMemoryManager` e escolha DEBUG - Mostrar caminho do save. Use Excluir Save no menu principal quando quiser testar desde o começo. Isso apaga a memória da IA e o progresso de fases.

## Visual, áudio e suspense

O visual usa arte e iluminação 2D com Universal Render Pipeline e Renderer 2D. Em `Level 1`, o pós-processamento está ligado na `Main Camera`. Os Volumes globais usam o perfil `Assets/_Project/Settings/Level1Horror.asset`.

| Efeito | Configuração atual | Resultado pretendido |
| --- | --- | --- |
| Color Adjustments | Exposição −0,35; contraste 18; saturação −28; filtro azulado | Ambiente frio e menos saturado |
| Vignette | Intensidade 0,38; suavidade 0,45 | Escurecimento das bordas |
| Film Grain | Intensidade 0,16 | Textura discreta de granulação |
| Bloom | Intensidade 0,2; threshold 0,9; scatter 1 | Brilho suave nas áreas claras |

Há sons previstos nos scripts para passos, contagem, vitória e derrota. Para ouvi-los, os clipes e suas referências precisam estar atribuídos no Inspector.

## Organização do código

Os scripts ficam em `Assets/_Project/Scripts`:

| Área | Scripts | Responsabilidade |
| --- | --- | --- |
| `AI` | `AIMemoryData`, `AIMemoryManager` | Dados, persistência, aprendizado e previsão |
| `Core` | `GameState`, `GameFlowManager`, `CountdownManager` | Estados, início e fim da partida, contagem e transições |
| `Grid` | `GridNavigation2D` | Conversão de coordenadas e validação das células |
| `Player` | `PlayerGridMovement2D`, `PlayerFacing2D`, `PlayerPathRecorder2D` | Teclado, movimento, orientação e registro da tentativa |
| `Enemy` | `AStarPathfinder2D`, `EnemyAStarController2D`, `EnemyGridMovement2D`, `EnemyFacing2D`, `EnemyCatchDetector2D` | Caminho, decisão, movimento, orientação e captura |
| `Level` | `ExitGoal2D`, `LevelNumberUI` | Detecção da saída e indicação da fase |
| `UI` | `MainMenuManager` | Botões do menu e exclusão de save |

`Assets/_Project` também reúne cenas, configurações, arte, áudio, prefabs e pastas de apoio. Os arquivos `.meta` fazem parte do projeto e devem acompanhar seus assets no controle de versão.

## Como abrir e executar

1. Instale o Unity Hub e o Editor 6000.5.7f1, versão registrada em `ProjectSettings/ProjectVersion.txt`.
2. Clone ou baixe o repositório.
3. Adicione ao Hub a pasta raiz que contém `Assets`, `Packages` e `ProjectSettings`.
4. Abra o projeto e aguarde a importação dos assets e a resolução dos pacotes.
5. Abra `Assets/_Project/Scenes/Menu.unity`.
6. Entre em Play Mode e clique em Iniciar.

Para gerar um executável Windows, selecione o perfil/plataforma Windows em Build Profiles, confira `Menu` antes de `Level 1` na lista de cenas e gere a build em uma pasta própria, como `Builds/Windows`.

O movimento lê WASD diretamente por `Keyboard.current`, do Input System. Alterar somente o asset de Input Actions não remapeia esses controles; é necessário adaptar `PlayerGridMovement2D`.

### Tecnologias

- Unity 6.5 (`6000.5.7f1`) e C#.
- URP 2D e framework de Volumes.
- Tilemap, Rigidbody2D e gatilhos de colisão 2D.
- Input System para entrada de teclado.
- uGUI e TextMeshPro para interfaces.
- JSON para memória e progresso locais.

As dependências e suas versões estão em `Packages/manifest.json`. O arquivo `Packages/packages-lock.json` registra a resolução dos pacotes. Também estão incluídas ferramentas do template e o pacote Pipeline, usado na integração com o Editor. O comportamento do inimigo funciona localmente.

## Para quem for mexer no projeto

- Mantenha as referências de Grid, Tilemaps, Rigidbody2D e gerenciadores atribuídas no Inspector.
- A saída identifica a tag `Player`; a captura usa `PlayerDeathTrigger`. Preserve essas tags e os colliders/gatilhos correspondentes.
- Mantenha os índices de cena compatíveis com `MainMenuManager` e `GameFlowManager`.
- Para comparar a IA sem memória com a IA adaptada, vença, confirme a próxima fase e repita trajetos. Use Excluir Save para reiniciar a comparação.
- Ajuste velocidade e previsão separadamente para entender o efeito de cada uma na dificuldade.
- O A* usa uma lista para procurar o próximo nó. Para mapas muito maiores, uma fila de prioridade é uma possível melhoria.

## Possíveis evoluções

Algumas ideias para continuar o protótipo, ainda sem implementação completa:

- Geração procedural de labirintos e memória separada por mapa.
- Mais fases com layouts próprios.
- Remapeamento de teclas e suporte a controle.
- Visualização de rotas A* e células previstas para depuração.
- Testes automatizados de navegação, aprendizado e progressão.
- Balanceamento da dificuldade com testes de jogadores.
