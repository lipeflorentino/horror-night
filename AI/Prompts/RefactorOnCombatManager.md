# Plano de Reorganização — CombatManager.cs

## Objetivo
Reduzir o escopo do `CombatManager` para Presenter fino, distribuindo responsabilidades em novas classes C# puras (exceto coroutines).

## Novos Arquivos

| Arquivo | Tipo | Responsabilidades migradas |
|---|---|---|
| `CombatInitializer.cs` | C# pura | `InitializeBattlers`, busca de referências (`FindObjectOfType`), `ClampCoreStat`, instanciação dos services |
| `CombatInventoryInitializer.cs` | C# pura | `BuildCombatInventory`, `BuildPlayerTrickInventory`, `BuildEnemyTrickInventory`, `ActivatePlayerIdentityTricks`, `ActivateEnemyIdentityTricks` |
| `TurnManager.cs` | C# pura* | `ReceivePlayerInput`, `ReceivePlayerSkipTurn`, `DefineStartingTurnByInitiative`, `GenerateEnemyAction`, `EndTurn`, `UpdateTurnRoleUI` |
| `CombatDiceRollManager.cs` | C# pura | `RollActions`, `RollExtraPowerDiceWithoutPool`, `BuildPlayerRollContext`, `GetPlayerTierBoundaries` |
| `CombatResolutionManager.cs` | C# pura | `Resolve`, `ResolveAttackAccuracy`, `ResolveDefenseAccuracy`, `BuildDefinitionFromBattler` |
| `CombatEndService.cs` | C# pura | `TryHandleCombatEnd`, `ProceedToGameplayScene`, `RestartCombat`, `QuitCombat`, `BuildResultPlayerSnapshot` |

\* `TurnManager` fornece a lógica de `ResolveTurnFlow`/`SkipTurnRoutine`, mas as coroutines (`StartCoroutine`, `yield return`) permanecem no `CombatManager` (MonoBehaviour), que delega os passos internos ao `TurnManager`.

## Estado Compartilhado
- Criar `CombatTurnState`: consolida `CurrentTurn`, `PendingPlayerPowerRolls`, `PendingPlayerAccuracyRolls`, `PendingEnemyPowerRolls`, `PendingEnemyAccuracyRolls`, `PendingEnemyPowerDiceTypes`, `PendingEnemyAccuracyDiceTypes`.
- Injetado por referência nos managers/coordinators que precisam ler/alterar o turno.

## CombatManager (resultante)
Mantém:
- Referências: `Player`, `Enemy`, `View`, `Input`, `SessionData`, `CombatEnded`, `CombatTurnState`
- `Start()`: chama `CombatInitializer` e `CombatInventoryInitializer`, depois `RefreshCombatUI`/`UpdateTurnRoleUI`
- Coroutines `ResolveTurnFlow`/`SkipTurnRoutine` (com `yield return`), delegando lógica interna ao `TurnManager`
- Fachadas públicas usadas por View/Input (mesma assinatura atual, delegando ao manager/coordinator correspondente): `ReceivePlayerInput`, `ReceivePlayerSkipTurn`, `TryCastPlayerTrick`, `ExecuteManualTrickActivation`, `RefreshCombatUI`, `GetDiceService`, `GetBattlerStateService`, `GetEffectivePlayerActionPower`, `GetPlayerTierBoundaries`

## Composição
- Todos os managers/coordinators são instanciados no `Start()` do `CombatManager`, recebendo services (`DiceService`, `PerkService`, etc.) e `CombatTurnState` via construtor.
- Nenhum deles referencia MonoBehaviour ou UI diretamente (exceto o necessário para `View`/coroutine, restrito ao `CombatManager`).

---

## Melhorias fora do escopo (sinalizadas, não implementadas)
- Comentários de debug já mortos (`// Logger.Log(...)`) em `ActivateEnemyIdentityTricks`/`BuildEnemyTrickInventory` podem ser removidos.
- `Debug.Log` espalhados poderiam seguir padrão único de logger com prefixo `[CombatManager]`/`[TurnManager]` conforme checklist do projeto.
- `RestartCombat`/`QuitCombat` fazem chamadas de infraestrutura (Scene/Application) que poderiam ir para um `SceneFlowService` genérico reutilizável fora do combate.