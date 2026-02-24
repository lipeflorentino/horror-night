# Horror Night

Projeto em Unity com foco em exploração por "clareiras", progressão por níveis e encontros que emergem de sistemas de tensão/presença.

## Visão geral

- **Gênero/loop base**: exploração + decisões de rota + combate.
- **Fluxo principal**:
  1. Inicializa uma fase (`LevelSO`).
  2. Gera nós (`LevelNode`) com conteúdo possível (`SpawnEntry`).
  3. Jogador se move em grade horizontal.
  4. Sistemas globais (Tensão, Presença e Encounter) determinam risco/combate.
  5. UI e efeitos visuais refletem estado da run.

## Arquitetura (Model-View-Presenter)

### Model (`Scripts/Model`)
Responsável por **dados, definições e regras de domínio**.

- Define tipos e estruturas (`Enums`, `Structs`).
- Define dados de spawn (`SpawnEntry`).
- Define dados de nível e nó via ScriptableObject (`LevelSO`, `LevelNodeSO`).
- Materializa instâncias de nó durante execução (`LevelNode`).

### View (`Scripts/View`)
Responsável por **renderização, feedback visual e UI**.

- UI de progressão (`LevelUpUI`, `UILevelIndicator`).
- Efeitos de transição e ambientação (`Fade`, `VisualSystem`).

### Presenter (`Scripts/Presenter`)
Responsável por **orquestrar fluxo, entrada do jogador e integração entre Model e View**.

- Fluxo de run e carregamento de nível (`RunController`, `LevelController`, `LevelInstance`).
- Movimento e status do jogador (`PlayerGridMovement`, `PlayerStatusManager`).
- Sistemas de gameplay acoplados ao ciclo da run (`TensionSystem`, `PresenceSystem`, `EncounterSystem`, `CombatManager`).

---

## Organização de pastas (alto nível)

- `Art/` → sprites, concept art e materiais visuais.
- `Prefabs/` → prefabs de personagem, níveis, estruturas e coletáveis.
- `Scenes/` → cenas jogáveis e de criação.
- `ScriptableObjects/` → dados configuráveis de nível, nós e construções.
- `Scripts/` → código-fonte principal organizado em MVP.
  - `Scripts/Model/`
  - `Scripts/View/`
  - `Scripts/Presenter/`
- `Shaders/` → shaders e materiais de efeitos.
- `Settings/` e assets `.asset` de pipeline/render → configuração do projeto.

---

## Arquivos e funções (documentação breve)

## `Scripts/Model`

### `Core/Enums.cs`
- `NodeType` → classifica tipo de nó (portal, história, tesouro etc).
- `Rarity` → raridade de recompensa.
- `NodeFlags` → conjunto de flags para comportamento do nó (spawn de loot/inimigo/evento etc).

### `Core/Structs.cs`
- `StatModifier` → estrutura com modificadores numéricos (vida, físico, sanidade, poder).

### `Collectables/SpawnEntry.cs`
- Classe serializável que define um item possível de spawn:
  - `itemName`, `rarity`, `weight`, `effects`.

### `Level/LevelNodeSO.cs`
- ScriptableObject com dados do tipo de nó.
- `GetRandomSpawn()` → sorteia uma entrada da `spawnTable` com base em peso (`weight`).

### `Level/LevelNode.cs`
- Representa um nó em runtime.
- `LevelNode(int index, LevelNodeSO definition)` → construtor do nó.
- `GenerateContent()` → gera conteúdo de spawn quando o nó permite loot.

### `Level/LevelSO.cs`
- ScriptableObject com configuração completa de uma clareira/fase.
- Contém identidade, tamanho, layout, regras de tier, modificadores de tensão/presença/encounter e tags.

## `Scripts/Presenter`

### `Flow/LevelController.cs`
- `Initialize(LevelSO level)` → monta os nós do nível e posiciona índice inicial.
- `TryMove(int direction)` → tenta mover para esquerda/direita, gera conteúdo do nó se necessário e dispara evento.
- `GetWorldPositionFromIndex(int index)` → converte índice lógico para posição no mundo.
- `GetCurrentNode()` → retorna nó atual.
- `MarkLevelCompleted()` → marca conclusão do nível e notifica ouvintes.
- `ResetCompletion()` → limpa estado de conclusão.

### `Flow/LevelInstance.cs`
- `Initialize(LevelSO def)` → carrega definição de nível na instância.
- `ApplyDefinition()` → propaga modificadores do nível para sistemas (Tensão, Presença, Encounter e Visual).

### `Flow/RunController.cs`
- `StartRun()` → inicia o fluxo da run com nível inicial.
- `LoadLevel(LevelSO level)` → inicializa nível e conecta eventos.
- `HandleLevelCompleted()` → pausa movimento e exibe UI de progressão.
- `HandleContinuePressed()` → retoma movimento e avança nível.
- `HandleNodeChanged(int index)` → detecta entrada em portal.
- `HandlePortal()` → troca para próximo nível em contexto de portal.
- `GoToNextLevel()` → avança para um novo nível.
- `GetNextLevel()` → sorteia próximo nível dentre opções possíveis.

### `Player/PlayerGridMovement.cs`
- Captura input horizontal e solicita movimento ao `LevelController`.
- `TryMove(int direction)` → executa tentativa de movimento e ajusta direção do sprite.
- `MoveTo(Vector3 targetPosition)` → interpolação suave de deslocamento.

### `Player/PlayerStatusManager.cs`
- Mantém vida, força e sanidade do jogador.
- `IncreaseLife/DecreaseLife(float amount)` → altera vida.
- `IncreaseStrength/DecreaseStrength(float amount)` → altera força.
- `IncreaseSanity/DecreaseSanity(float amount)` → altera sanidade.
- `GetCurrentStrength()` → consulta força atual.
- `CanSpendStrength(float amount)` → valida custo de força.
- `TrySpendStrength(float amount)` → tenta gastar força com segurança.
- `RefreshAllBars()` / `UpdateBar(...)` → atualiza barras de UI.

### `Systems/TensionSystem.cs`
- `SetBaseModifier(float value)` → define ganho base de tensão e zera contador.
- `OnPlayerMove()` → incrementa tensão por movimento; ao alcançar limiar, dispara encounter.
- `GetEncounterThreshold()` → retorna limite de tensão para encontro.

### `Systems/PresenceSystem.cs`
- `SetBaseModifier(float value)` → define crescimento base da presença.
- `Tick()` → acumula presença com o tempo e força encounter ao atingir limite.

### `Systems/EncounterSystem.cs`
- `SetRiskModifier(float value)` → define modificador de risco global.
- `TriggerEncounter()` → inicia combate padrão.
- `TriggerForcedEncounter()` → inicia combate forçado com risco elevado.

### `Systems/CombatManager.cs`
- `StartCombat(float difficultyModifier)` → ponto central para iniciar combate com ajuste de dificuldade.

## `Scripts/View`

### `UI/LevelUpUI.cs`
- `Show()` / `Hide()` → controla visibilidade da UI de avanço.
- `OnContinuePressed` (evento) → notifica continuidade ao pressionar tecla configurada.

### `UI/UILevelIndicator.cs`
- `Refresh(int currentIndex)` → atualiza janela visual de nós com destaque para posição atual e portais.

### `Effects/Fade.cs`
- `FadeOut()` → transição para preto.
- `FadeIn()` → transição de volta da tela preta.

### `Effects/VisualSystem.cs`
- `ApplyTags(string tags)` → aplica efeitos visuais conforme tags do nível.
- `EnableFogEffect()` / `DarkenEnvironment()` → stubs de efeitos de névoa e escurecimento.

---

## Observações rápidas

- O projeto já está preparado para expansão orientada a dados com `ScriptableObjects`.
- A estrutura MVP está clara e separa bem dados, apresentação e fluxo.
- Alguns pontos estão em forma de placeholder (ex.: implementação real de combate e efeitos visuais).
