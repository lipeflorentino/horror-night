# Plano de evolução: Tricks, Perks e TrickInventory no CombatV2

## Objetivo funcional

Implementar o fluxo de Tricks como habilidades adquiríveis, guardadas em um `TrickInventory`, com suporte a:

- 4 slots fixos de **Identity Tricks** do Battler, vindos da classe/identidade do personagem e ativos por padrão.
- 4 slots de **Casted Tricks**, preenchidos quando o jogador faz cast de Tricks adquiridas.
- Cast com consumo dos recursos do Trick (`MindCost`, `BodyCost`, `HeartCost`).
- Trick castado permanece no slot de castados, entra em cooldown após o cast, e seus Perks ficam disponíveis para modificar Battler/fluxo de combate conforme as regras.
- UI de inventário em formato de livro/lista de habilidades, com ícones clicáveis e painel de detalhes com ações `cast`, `dischard` e `close`.
- UI runtime `CastedTricks` mostrando somente Tricks castados, com feedback visual de `triggered` e `cooldown`.

## Estado atual do sistema

### Battler e runtime de Tricks

- `Battler` já possui `List<PerkRuntimeInstance> Perks` e `List<TrickRuntimeInstance> Tricks`, mas não há separação entre Tricks de identidade, Tricks aprendidas em inventário e Tricks castadas em slots.
- `Battler.GetEffectivePerks()` agrega Perks diretos com `ActivePerks` de cada `TrickRuntimeInstance`, mas o `PerkService` usa um agregador próprio e não consome esse método, o que cria risco de divergência.
- `TrickRuntimeInstance` guarda `Definition`, `Owner`, `RemainingTurns`, `CastTime` e `ActivePerks`, mas não possui estado de slot, cooldown, origem (`Identity`/`Casted`) nem eventos de trigger por Trick.

### Definição e dados de Tricks

- `TrickSO` já modela metadados, custo, nível, timing, duração e `PerkIds`.
- O CSV `Resources/Data/TrickTable.csv` já contém colunas para ID, nome, descrição, custos, timing, duração, lista de Perks, raridade, tags, flavor e ícone.
- `TrickDatabase` carrega todos os `TrickSO` de `Resources/Data/Tricks` e permite busca por ID/tag/level/raridade.

### Serviço de Tricks

- `TrickService.TryCastTrick` valida recursos, consome custos e chama `ApplyTrick`.
- `ApplyTrick` cria `TrickRuntimeInstance`, aplica cada Perk no `PerkService` e adiciona o Trick em `Battler.Tricks`.
- Hoje não existe limite de 4 slots castados, cooldown, inventário, aquisição, descarte, recast, origem do Trick ou validação de “já adquirido”.
- Há uma inconsistência importante: os Perks aplicados pelo `PerkService` vão para `Battler.Perks`, mas não são adicionados ao `TrickRuntimeInstance.ActivePerks`. Assim, o vínculo runtime entre Trick e Perk fica incompleto.

### Perks e triggers

- `PerkService` aplica/remover Perks e expira duração no fim do turno.
- `PerkTriggerEvaluator` dispara `OnPerkTriggered` para regras de `BeforeRoll`, `PowerMultiplier` e `AfterResolve`.
- O avaliador atual percorre `owner.Perks` diretamente e retorna cedo se essa lista estiver vazia, então Perks vindos apenas de uma futura lista de Tricks não seriam avaliados sem ajustar esse ponto.
- `PerkTriggeredEvent` não carrega `TrickId`/origem do Perk, então a UI `CastedTricks` não consegue saber qual Trick deve piscar como triggered.

### UI de Tricks atual

- O clique em `TrickIconUI` só loga seleção/click e não abre um painel de ações equivalente ao inventário de combate.
- `TrickTooltip` é apenas informativo, sem ações `cast`, `dischard` e `close`.
- A cena `Combat.unity` já tem objetos relacionados a Tricks (`TricksPanel`, `TricksContainer`), mas o wiring atual ainda não cumpre o fluxo de inventário + slots castados.

### Inventário de combate como base

- `CombatInventory` encapsula estado runtime de itens, slots equipados e snapshot de persistência.
- `ICombatInventory` expõe operações simples (`UseItem`, `UnEquipItem`, `DeschardItem`, `GetSnapshot`) e listas read-only para a UI.
- `InventoryView`, `InventoryItemUI` e `ItemInfoPanelUI` já formam um bom padrão para o TrickInventory: view vinculada a uma interface, ícones/slots instanciados, painel de detalhes desacoplado, eventos de interação e refresh após ações.

## Lacunas principais

1. **Falta o modelo de inventário de Tricks**: não existe `ITrickInventory`, `TrickInventory`, slots de identidade/castados, nem snapshot/persistência de Tricks adquiridos/castados.
2. **Trick castado não tem estado suficiente**: precisa armazenar slot, cooldown, disponibilidade, origem e estado visual transitório de trigger.
3. **Perk não conhece o Trick de origem**: necessário para remover corretamente por cast, exibir feedback e mapear trigger para a UI.
4. **Avaliação de Perks não está centralizada**: `Battler.GetEffectivePerks()`, `PerkService.GetEffectivePerks()` e `PerkTriggerEvaluator` podem divergir.
5. **Identity Tricks estão misturados conceitualmente com Identity Perks**: hoje o database de Perks possui lógica de identity global, mas o requisito fala em 4 Tricks de identidade por Battler/classe.
6. **UI atual lista tudo do database**: deve listar Tricks adquiridas no inventário e separar identity/castable/casted.
7. **Não há contrato claro para cooldown**: falta definir se cooldown é por turnos, por round, por ação ou tempo real; recomenda-se turno de combate para manter consistência com `TickTurnEnd`.

## Arquitetura proposta

### 1. Modelo de slots e inventário

Criar os modelos:

- `TrickSlotKind`: `Identity`, `Castable`, `Casted`.
- `TrickSlot`: índice, tipo, `TrickSO`, `TrickRuntimeInstance`, estado bloqueado/vazio.
- `ITrickInventory`: contrato semelhante ao `ICombatInventory`.
- `TrickInventory`: dono do estado de Tricks adquiridas, identity slots e casted slots.

Contrato sugerido de `ITrickInventory`:

```csharp
public interface ITrickInventory
{
    IReadOnlyList<TrickSlot> IdentitySlots { get; }
    IReadOnlyList<TrickSO> LearnedTricks { get; }
    IReadOnlyList<TrickSlot> CastedSlots { get; }

    bool LearnTrick(TrickSO trick);
    bool CastTrick(TrickSO trick, out TrickRuntimeInstance instance);
    bool DischardTrick(TrickSO trick);
    bool RemoveCastedTrick(int slotIndex);
    TrickInventorySnapshot GetSnapshot();
}
```

Regras iniciais:

- Identity slots: máximo 4, pré-carregados por classe/personagem, permanentes e não descartáveis.
- Casted slots: máximo 4, recebem Tricks após cast.
- Learned/castable: lista de Tricks adquiridas e ainda disponíveis para cast.
- Ao castar:
  - validar nível, recurso, cooldown e disponibilidade de slot;
  - consumir custo;
  - criar/atualizar `TrickRuntimeInstance`;
  - ativar Perks vinculados ao Trick;
  - alocar no primeiro slot castado livre ou substituir conforme regra futura;
  - iniciar cooldown do Trick.

### 2. Estado runtime de Trick

Expandir `TrickRuntimeInstance` com:

- `string InstanceId` ou `Guid` para distinguir múltiplas instâncias.
- `TrickSlotKind SlotKind`.
- `int SlotIndex`.
- `int CooldownTurnsRemaining`.
- `bool IsCoolingDown`.
- `bool WasTriggeredThisFrameOrTurn` ou timestamp de último trigger.
- `List<PerkRuntimeInstance> ActivePerks` preenchida de verdade pelo cast.

Também adicionar campos de definição em `TrickSO` ou tabela de dados:

- `CooldownTurns`.
- `MaxCastedCopies` ou política de duplicidade.
- Opcional: `IsIdentityEligible`/tags de classe.

### 3. Perks com origem rastreável

Expandir `PerkRuntimeInstance` com:

- `string SourceTrickId`.
- `string SourceTrickInstanceId`.
- `TrickRuntimeInstance SourceTrick` opcional.

Expandir `PerkTriggeredEvent` com:

- `string SourceTrickId`.
- `string SourceTrickInstanceId`.

Ajustar `PerkService.ApplyPerk` para retornar a instância criada/atualizada, ou adicionar método específico:

```csharp
public PerkRuntimeInstance ApplyPerkFromTrick(
    Battler target,
    string perkId,
    TrickRuntimeInstance sourceTrick,
    Battler source,
    int durationTurns,
    int stacks = 1)
```

Assim, `TrickService` consegue popular `trickInstance.ActivePerks` e a UI consegue mapear `OnPerkTriggered` para o ícone do Trick correto.

### 4. Unificar avaliação de Perks efetivos

Refatorar para uma única fonte de verdade:

- `Battler.GetEffectivePerks()` deve retornar Perks diretos, Perks de identity Tricks e Perks de casted Tricks ativos.
- `PerkService` e `PerkTriggerEvaluator` devem usar essa coleção efetiva em vez de iterar `owner.Perks` diretamente.
- Evitar duplicidade entre “Identity Perks globais do database” e “Identity Tricks do Battler”. Se os Identity Perks ainda forem necessários, tratá-los como legado ou migrá-los para Identity Tricks.

### 5. Cooldown e ciclo de turnos

Adicionar ao `TrickService`/`TrickInventory`:

- `TickTrickCooldowns(Battler battler)` chamado em `CombatManager.EndTurn`.
- Diminuição de `RemainingTurns` separada da diminuição de `CooldownTurnsRemaining`.
- Evento `OnTrickCooldownChanged` para atualizar UI.
- Evento `OnTrickTriggered` derivado de `PerkService.OnPerkTriggered` quando o evento carrega origem de Trick.

Regra recomendada:

- `RemainingTurns` controla duração do efeito ativo.
- `CooldownTurnsRemaining` controla quando o Trick pode ser castado novamente.
- Trick castado permanece visível no slot castado mesmo em cooldown; se a duração expirar, o slot pode mostrar “efeito expirado” ou liberar slot, conforme design. Pelo requisito, após cast ele permanece no slot de castados, então a primeira implementação deve manter o slot ocupado e atualizar estado visual.

### 6. UI TrickInventory

Criar componentes paralelos ao inventário de itens:

- `TrickInventoryView`: painel/livro de Tricks.
- `TrickSlotUI`: ícone de slot com estado vazio/identity/learned/casted.
- `TrickInfoPanelUI`: painel com nome, descrição, custos, duração, cooldown, Perks, tags e botões.
- `TrickInventoryInputHandler`: ponte entre UI e `ITrickInventory`/`TrickService`.

Comportamento:

- Mostrar seção “Identity Tricks” com 4 slots fixos.
- Mostrar seção “Tricks aprendidas/castáveis” com ícones clicáveis.
- Mostrar seção “Casted Slots” com 4 slots.
- Ao clicar em `TrickIconUI`/`TrickSlotUI`, abrir `TrickInfoPanelUI` com ações contextuais:
  - `Cast`: visível se Trick aprendido/castável, recursos suficientes, slot disponível e não em cooldown.
  - `Dischard`: visível para Tricks aprendidas não-identity; definir se pode descartar Trick castado.
  - `Close`: sempre.

### 7. UI CastedTricks runtime

Criar ou separar `CastedTricksView`:

- Vincular aos `CastedSlots` do `ITrickInventory`.
- Mostrar os 4 slots em runtime.
- Estados visuais mínimos:
  - normal/ativo;
  - `triggered` com flash/animação curta quando um Perk do Trick dispara;
  - `cooldown` com overlay, contador de turnos e bloqueio visual;
  - vazio.
- Escutar eventos:
  - `OnTrickCasted` para preencher slot;
  - `OnTrickCooldownChanged` para atualizar overlay;
  - `OnTrickTriggered` ou `PerkService.OnPerkTriggered` com `SourceTrickId` para feedback de trigger;
  - `OnTrickRemoved`/`OnTrickExpired` para limpar/atualizar estado.

## Melhorias de organização recomendadas

- Mover todo o domínio de Tricks para `Scripts/CombatV2/Model/Tricks` e os presenters para `Scripts/CombatV2/Presenter/Tricks` ou `Presenter/Service` conforme responsabilidade.
- Padronizar nomes: corrigir `Deschard` para `Discard` em APIs novas; manter adapters temporários para compatibilidade se necessário.
- Separar “definitions” (`TrickSO`, `PerkSO`) de “runtime state” (`TrickRuntimeInstance`, `TrickSlot`, `PerkRuntimeInstance`) e “persistence snapshot”.
- Escrever testes de domínio para `TrickInventory` e `TrickService`, já que a maior parte das regras pode ser testada sem UI.

## Fases e tarefas

### Fase 1 — Contratos e dados base

1. Criar `TrickSlotKind`, `TrickSlot`, `ITrickInventory`, `TrickInventorySnapshot` e `TrickInventory`.
2. Definir estrutura de snapshot para learned, identity e casted slots.
3. Adicionar suporte a `CooldownTurns` em `TrickSO`, `TrickCsvParser`, `TrickCsvImporter` e `TrickTable.csv`.
4. Definir fonte dos 4 Identity Tricks por classe/personagem (snapshot do player, classe SO ou config temporária no `CombatSessionData`).

### Fase 2 — Serviço e runtime

1. Expandir `TrickRuntimeInstance` com slot, cooldown, origem e estado de trigger.
2. Ajustar `PerkRuntimeInstance` e `PerkTriggeredEvent` para guardar origem do Trick.
3. Fazer `PerkService.ApplyPerk` retornar instância ou criar `ApplyPerkFromTrick`.
4. Corrigir `TrickService.ApplyTrick` para preencher `ActivePerks`.
5. Implementar cast via `ITrickInventory`, respeitando 4 slots castados, custos e cooldown.
6. Implementar ticking de duração/cooldown e eventos de mudança.

### Fase 3 — Unificação dos Perks efetivos

1. Refatorar `PerkTriggerEvaluator` para receber/usar a lista efetiva de Perks do Battler.
2. Fazer todos os modificadores e triggers do `PerkService` usarem a mesma fonte de Perks efetivos.
3. Decidir migração ou convivência entre Identity Perks globais e Identity Tricks por Battler.
4. Garantir que remoção/expiração de Trick remova apenas os Perks originados daquela instância.

### Fase 4 — UI de TrickInventory

1. Criar `TrickInventoryView` inspirado em `InventoryView`.
2. Criar `TrickSlotUI`/adaptar `TrickIconUI` para ser clicável e emitir eventos tipados.
3. Criar `TrickInfoPanelUI` com botões `cast`, `dischard` e `close`.
4. Criar `TrickInventoryInputHandler` inspirado em `InventoryInputHandler`.
5. Integrar `CombatManager.Start` para construir e bindar `TrickInventory` junto do inventário de itens.

### Fase 5 — UI CastedTricks runtime e feedback

1. Criar `CastedTricksView` com 4 slots runtime.
2. Adicionar overlay/contador de cooldown.
3. Adicionar animação/flash de `triggered` quando um Perk vindo do Trick dispara.
4. Atualizar `CombatView.Init`/`UpdateView` para bindar e refrescar a UI runtime.

### Fase 6 — Persistência, rewards e aquisição

1. Definir como Tricks são adquiridas fora do combate (loot, reward, loja, evento).
2. Adicionar snapshot de TrickInventory ao snapshot do jogador ou sessão.
3. Integrar RewardService/loot para oferecer Trick como recompensa futura.
4. Garantir que Tricks aprendidas persistem entre combates e casted/cooldown seguem a regra de design escolhida.

### Fase 7 — Testes e validação

1. Testes de cast com recursos suficientes/insuficientes.
2. Testes de limite de 4 casted slots.
3. Testes de cooldown e duração por turno.
4. Testes de vínculo Trick -> Perk -> trigger -> UI event.
5. Testes de descarte de Trick aprendido e bloqueio de descarte de identity.
6. Validação visual em cena Combat para painéis, botões e feedbacks.