# Migração: States → BattlerState + Perk System

## Objetivo
Substituir o sistema rígido de `BattlerState` (hardcoded) por uma arquitetura orientada a dados análoga a **Drawbacks**: um `BattlerStateSO` que agrupa uma lista de `PerkIds` atômicos, cada perk responsável por uma única modificação. Os perks de estado são ativados no Perk System normalmente via `PerkService.ApplyPerk`.

---

## Arquitetura Final (espelha Drawbacks)

```
BattlerStateSO        ←→   DrawbackSO
BattlerStateDatabase  ←→   DrawbackDatabase
BattlerStateCsvParser ←→   DrawbackCsvParser
BattlerStateRuntime   ←→   DrawbackRuntimeInstance
BattlerStatesTable.csv ←→  DrawbackTable.csv
```

**Perks são atômicos:** cada linha do PerkTable representa UMA modificação. O BattlerStateSO agrupa os IDs dos perks que compõem o estado.

**Exemplo — state_exposed:**
- Perk `state_exposed_max_accuracy` → `MaxRollPercent`, OwnerAsTarget, Accuracy, Add, -0.15  
- Perk `state_exposed_defense_power` → `DefensePower`, OwnerAsDefender, Multiply, 0.90  
- Perk `state_exposed_defender_accuracy` → `MaxRollPercent`, OwnerAsDefender, Accuracy, Add, 0.10  
- `BattlerStateSO(id="exposed")` → `PerkIds = ["state_exposed_max_accuracy", "state_exposed_defense_power", "state_exposed_defender_accuracy"]`

---

## Proposed Changes

---

### FASE 1 — Novos enums em PerkEnums.cs

#### [MODIFY] [PerkEnums.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Model/Perks/PerkEnums.cs)
Adicionar em `PerkModifierTarget`:
- `AttackPower` — multiplica o poder base da ação de ataque (contextual, não altera stat base)
- `DefensePower` — multiplica o poder base da ação de defesa (contextual)

Adicionar em `PerkCsvParser.InferTrigger`:
- `AttackPower` → `BeforeRoll`
- `DefensePower` → `BeforeRoll`

---

### FASE 2 — Novos arquivos: BattlerState entity (espelhando Drawbacks)

#### [NEW] `Assets/Scripts/CombatV2/Model/States/BattlerStateSO.cs`
```csharp
[CreateAssetMenu(menuName = "Combat/Battler State")]
public class BattlerStateSO : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public int DefaultDurationTurns = 1;
    public int MaxStacks = 1;
    public BattlerStateStackMode StackMode = BattlerStateStackMode.RefreshDuration;
    public List<string> PerkIds = new();  // Perks atômicos que compõem o estado
    public string FlavorText;
    public bool IsValid() => !string.IsNullOrEmpty(Id) && PerkIds.Count > 0;
}
```

#### [NEW] `Assets/Scripts/CombatV2/Model/States/BattlerStateDatabase.cs`
- Padrão idêntico ao `DrawbackDatabase`
- Pasta de resources: `"Data/BattlerStates"`
- `GetOrCreateRuntimeDatabase()`, `EnsureLoaded()`, `LoadAll()`, `GetById()`

#### [NEW] `Assets/Scripts/CombatV2/Model/States/BattlerStateCsvParser.cs`
- Padrão idêntico ao `DrawbackCsvParser`
- Colunas: `Id, DisplayName, Description, IconName, DurationTurns, MaxStacks, StackMode, PerkIds, FlavorText`
- `PerkIds` parseado como lista separada por `;`

#### [NEW] `Assets/Scripts/CombatV2/Model/States/BattlerStateRuntimeInstance.cs`
- Análogo a `DrawbackRuntimeInstance`
- Guarda `BattlerStateSO Definition`, `Battler Owner`, `Battler Source`, `int RemainingTurns`, `List<PerkRuntimeInstance> ActivePerks`
- Métodos: `IsActive()`, `DecreaseDuration()`

---

### FASE 3 — Novos arquivos de dados

#### [NEW] `Assets/Resources/Data/BattlerStates/` (pasta)
SOs carregados via `Resources.LoadAll<BattlerStateSO>("Data/BattlerStates")`.

#### [NEW] `Assets/Resources/Data/BattlerStatesTable.csv`
```csv
Id,DisplayName,Description,DurationTurns,MaxStacks,StackMode,PerkIds,FlavorText
exposed,Exposto,Reduz eficácia defensiva,1,1,RefreshDuration,state_exposed_max_accuracy;state_exposed_defense_power;state_exposed_defender_accuracy,Vulnerável a ataques
angry,Raivoso,Aumenta ataque mas prejudica defesa,1,1,RefreshDuration,state_angry_attack_power;state_angry_power_high_threshold;state_angry_power_low_threshold;state_angry_defense_power;state_angry_defender_accuracy,Fúria às cegas
cautious,Cauteloso,Postura defensiva extrema prejudica precisão,1,1,RefreshDuration,state_cautious_max_accuracy;state_cautious_min_accuracy,Prudência excessiva
```

#### [MODIFY] `Assets/Resources/Data/PerkTable.csv`
Adicionar perks atômicos dos estados (sem agrupar no parser, cada linha = 1 perk = 1 regra):

**state_exposed:**
```csv
state_exposed_max_accuracy,OwnerAsTarget,Attack,Accuracy,Any,Any,Always,,MaxRollPercent,Add,-0.15,1,RefreshDuration
state_exposed_defense_power,OwnerAsDefender,Defense,Any,Any,Any,Always,,DefensePower,Multiply,0.90,1,RefreshDuration
state_exposed_defender_accuracy,OwnerAsDefender,Defense,Accuracy,Any,Any,Always,,MaxRollPercent,Add,0.10,1,RefreshDuration
```

**state_angry:**
```csv
state_angry_attack_power,OwnerAsAttacker,Attack,Any,Any,Any,Always,,AttackPower,Multiply,1.10,1,RefreshDuration
state_angry_power_high_threshold,OwnerAsAttacker,Attack,Power,Any,Any,Always,,MaxRollPercent,Add,-0.15,1,RefreshDuration
state_angry_power_low_threshold,OwnerAsAttacker,Attack,Power,Any,Any,Always,,MinRollPercent,Add,-0.15,1,RefreshDuration
state_angry_defense_power,OwnerAsDefender,Defense,Any,Any,Any,Always,,DefensePower,Multiply,0.90,1,RefreshDuration
state_angry_defender_accuracy,OwnerAsDefender,Defense,Accuracy,Any,Any,Always,,MaxRollPercent,Add,0.15,1,RefreshDuration
```

**state_cautious:**
```csv
state_cautious_max_accuracy,OwnerAsDefender,Defense,Accuracy,Any,Any,Always,,MaxRollPercent,Add,-0.15,1,RefreshDuration
state_cautious_min_accuracy,OwnerAsDefender,Defense,Accuracy,Any,Any,Always,,MinRollPercent,Add,-0.15,1,RefreshDuration
```

---

### FASE 4 — PerkService: novos métodos para AttackPower/DefensePower e stats

#### [MODIFY] [PerkService.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/Service/PerkService.cs)

Adicionar:
```csharp
// Substitui BattlerStateService.GetEffectiveActionPower
public int GetEffectiveActionPower(Battler actor, Battler opponent, ActionType actionType)

// Substitui BattlerStateService.GetEffectiveFocus
public int GetEffectiveFocus(Battler actor, Battler opponent, ActionType actionType)

// Substitui BattlerStateService.GetEffectiveStrength
public int GetEffectiveStrength(Battler actor, Battler opponent, ActionType actionType)
```

Lógica interna: percorre `GetEffectivePerks(actor)` e `GetEffectivePerks(opponent)`, filtra regras pelo `ModifierTarget` adequado (`AttackPower`/`DefensePower` para power, `Focus`/`Strength` para stats), aplica `IsRoleMatch` e acumula o valor.

Adicionar suporte interno em `ApplyRollModifiersFromOwner` e `ApplyDiceModifiersFromOwner` para os novos targets `AttackPower` e `DefensePower`.

#### [MODIFY] [PerkCsvParser.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Model/Perks/PerkCsvParser.cs)
- `InferTrigger`: adicionar `AttackPower` → `BeforeRoll` e `DefensePower` → `BeforeRoll`.
- **Sem** agrupamento por ID — cada linha continua gerando 1 PerkSO com 1 PerkRule (comportamento atômico mantido).

---

### FASE 5 — Remoção da dependência do BattlerStateService

#### [MODIFY] [DiceService.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/Service/DiceService.cs)
- `stateService.GetEffectiveFocus(...)` → `perkService.GetEffectiveFocus(...)`
- `stateService.GetEffectiveStrength(...)` → `perkService.GetEffectiveStrength(...)`
- `stateService.ApplyThresholdModifiers(...)` → **removido** (coberto automaticamente pelos perks de estado com `MaxRollPercent`/`MinRollPercent` via `ApplyRollModifiers`)
- Remover campo `stateService` e parâmetro do construtor.

#### [MODIFY] [CombatResolutionManager.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/CombatResolutionManager.cs)
- `BuildDefinitionFromBattler` troca `BattlerStateService` por `PerkService`
- `stateService.GetEffectiveActionPower(...)` → `perkService.GetEffectiveActionPower(...)`

#### [MODIFY] [CombatDiceRollManager.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/CombatDiceRollManager.cs)
- Remover parâmetro `BattlerStateService battlerStateService` de todos os métodos
- `battlerStateService.GetEffectiveFocus/Strength` → `perkService.GetEffectiveFocus/Strength`

#### [MODIFY] [TurnManager.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/TurnManager.cs)
- Remover parâmetro `BattlerStateService stateService` de `EndTurn`
- Remover chamadas `stateService.TickTurnEnd(player/enemy)` (já coberto por `perkService.TickTurnEnd`)

#### [MODIFY] [CombatManager.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Presenter/CombatManager.cs)
- Remover campo `BattlerStateService`, instanciação no `Start()`, e todas as passagens como argumento
- `GetEffectivePlayerActionPower()` → usar `PerkService`
- Remover `GetBattlerStateService()`

#### [MODIFY] [Battler.cs](file:///c:/Users/lipef/Game%20Projects/Horror%20Night/Assets/Scripts/CombatV2/Model/Battler.cs)
- Remover `List<BattlerStateInstance> States`
- Remover métodos `ApplyState`, `RemoveState`, `HasState`

---

### FASE 6 — [DELETE] Arquivos removidos

| Arquivo | Motivo |
|---|---|
| `Presenter/Service/BattlerStateService.cs` | Absorvido pelo PerkService |
| `Model/States/BattlerStateCatalog.cs` | Substituído pelo BattlerStatesTable.csv |
| `Model/States/BattlerStateDefinition.cs` | Substituído pelo BattlerStateSO |
| `Model/States/BattlerStateInstance.cs` | Substituído pelo BattlerStateRuntimeInstance |
| `Model/States/BattlerStateModifier.cs` | Removido (abstrato, sem uso) |
| `Model/States/BattlerStatModifier.cs` | Removido (substituído por PerkRule) |
| `Model/States/ThresholdModifier.cs` | Removido (substituído por PerkRule) |
| `Model/States/ThresholdPair.cs` | Removido |
| `Enums/Enums.cs` | Enums `BattlerStateRole`, `BattlerStateStackMode`, `BattlerStateStatType`, `ModifierOperation`, `ThresholdBand` — os que ainda são usados por Perks (`BattlerStateRole`, `BattlerStateStackMode`) ficam em `PerkEnums.cs`; os exclusivos de States são deletados. |

> [!WARNING]
> `BattlerStateStackMode` é usado em `PerkSO` e `DrawbackSO`. Este enum deve **migrar** para `PerkEnums.cs` antes de deletar `Enums.cs`.
> `BattlerStateRole` idem — já está em `PerkEnums.cs` nesse projeto? Confirmar antes de deletar.

---

## Fluxo de Ativação de Estado (implementado nessa PR)

```
// Quando um estado for ativado (lógica completa será implementada depois):
BattlerStateSO stateDef = BattlerStateDatabase.GetOrCreateRuntimeDatabase().GetById("exposed");

BattlerStateRuntimeInstance stateInstance = new(stateDef, target, source, stateDef.DefaultDurationTurns);
target.ActiveStates.Add(stateInstance); // nova lista em Battler

// Aplicar os perks do estado:
foreach (string perkId in stateDef.PerkIds)
{
    PerkRuntimeInstance appliedPerk = perkService.ApplyPerk(target, perkId, source, stateDef.DefaultDurationTurns);
    if (appliedPerk != null)
        stateInstance.ActivePerks.Add(appliedPerk);
}
```

O tick de duração do estado e a remoção dos perks vinculados ficam em `PerkService.TickTurnEnd` (já existe para Drawbacks via `battler.Drawbacks`) — o `BattlerStateRuntimeInstance` será adicionado a uma nova lista `battler.ActiveStates`.

---

## Verification Plan

### Compilação
- Zero erros de compilação no Unity Console.

### Lógica
- `perkService.GetEffectiveFocus/Strength/ActionPower` retornam valores corretos com e sem perks de estado ativos.
- Perks `state_exposed_*` aplicados via `perkService.ApplyPerk` aparecem em `battler.Perks` e são respeitados durante o roll.
- `perkService.TickTurnEnd` expira perks de estado após 1 turno corretamente.
