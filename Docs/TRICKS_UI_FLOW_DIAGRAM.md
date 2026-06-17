# 🔄 Fluxo de Tricks UI - Diagrama Completo

## 📊 Fluxo de Dados: Gameplay → UI Renderizada

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        GAMEPLAY SCENE                                       │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  Player Entity                                                       │  │
│  │  ├─ Battler (stats, perks)                                          │  │
│  │  └─ CharacterSO                                                      │  │
│  │     └─ IdentityTricks: [Trick1, Trick2, Trick3, Trick4]            │  │
│  │        └─ CreateInitialTrickSnapshot()                             │  │
│  │           └─ snapshot.identityTrickIds = ["trick_id_1", ...]       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
                     [Transição para COMBAT SCENE]
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                      COMBAT MANAGER.START()                                 │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  1. BuildPlayerTrickInventory(Player)                               │  │
│  │     ├─ sessionData.PlayerSnapshot.trickInventory obtido             │  │
│  │     ├─ TrickInventory constructor                                   │  │
│  │     └─ InitializeSlots(4, 4)                                        │  │
│  │        ├─ identitySlots = [TrickSlot, TrickSlot, ...]              │  │
│  │        └─ castedSlots = [TrickSlot, TrickSlot, ...]                │  │
│  │                                                                      │  │
│  │  2. RestoreSnapshot(snapshot)                                       │  │
│  │     ├─ RestoreIdentitySlots(["trick_id_1", "trick_id_2", ...])    │  │
│  │     │  └─ TrickDatabase.GetById(id)                                │  │
│  │     │     └─ TrickRuntimeInstance instance = new(trick, owner...)  │  │
│  │     │        └─ identitySlots[0].BindRuntimeInstance(instance)     │  │
│  │     │           └─ Definition = trick, RuntimeInstance = instance  │  │
│  │     │                                                               │  │
│  │     └─ RestoreLearnedTricks([...])                                 │  │
│  │        └─ learnedTricks.Add(trick)                                 │  │
│  │                                                                      │  │
│  │  3. ActivatePlayerIdentityTricks()                                 │  │
│  │     ├─ Para cada IdentitySlot                                       │  │
│  │     └─ TrickService.ApplyTrick(player, instance, player)           │  │
│  │        └─ Perks aplicados ao Battler                              │  │
│  │                                                                      │  │
│  │  4. TrickInventoryInputHandler.Init(this, PlayerTrickInventory)   │  │
│  │     └─ trickInventoryView.BindInventory(playerTrickInventory)     │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│              TRICK INVENTORY VIEW.BIND INVENTORY()                          │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  boundInventory = trickInventory                                    │  │
│  │  boundInventory.OnChanged += Refresh  ← Listener instalado         │  │
│  │  Refresh()                                                           │  │
│  │     ├─ ClearSpawnedSlots()                                          │  │
│  │     ├─ SpawnSlots(IdentitySlots, identitySlotsRoot, IdentitySlot)  │  │
│  │     │  └─ Para cada TrickSlot:                                      │  │
│  │     │     └─ SpawnTrickView(trick, runtimeInstance, parent, ...)   │  │
│  │     │        └─ Instantiate(identityTrickSlotPrefab, parent)       │  │
│  │     │           └─ TrickSlotUI created & added to spawnedSlots     │  │
│  │     │                                                               │  │
│  │     ├─ SpawnLearnedTricks()                                         │  │
│  │     │  └─ Para cada TrickSO em LearnedTricks:                       │  │
│  │     │     └─ SpawnTrickView(trick, null, learnedTricksRoot, ...)   │  │
│  │     │        └─ Instantiate(learnedTrickSlotPrefab, parent)        │  │
│  │     │           └─ TrickSlotUI created                              │  │
│  │     │                                                               │  │
│  │     └─ SpawnSlots(CastedSlots, castedSlotsRoot, CastedSlot)        │  │
│  │        └─ [Slots vazios inicialmente]                              │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SCENE RENDERED                                           │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  [TrickInventoryView Panel]                                         │  │
│  │  ├─ IdentitySlotsGrid                                               │  │
│  │  │  ├─ [TrickSlotUI] Trick1 - LOCKED                               │  │
│  │  │  ├─ [TrickSlotUI] Trick2 - LOCKED                               │  │
│  │  │  ├─ [TrickSlotUI] Trick3 - LOCKED                               │  │
│  │  │  └─ [TrickSlotUI] Trick4 - LOCKED                               │  │
│  │  │                                                                   │  │
│  │  ├─ LearnedTricksGrid                                               │  │
│  │  │  ├─ [TrickSlotUI] TrickA - Available for cast                   │  │
│  │  │  ├─ [TrickSlotUI] TrickB - Available for cast                   │  │
│  │  │  └─ [TrickSlotUI] TrickC - Available for cast                   │  │
│  │  │                                                                   │  │
│  │  ├─ CastedSlotsGrid                                                 │  │
│  │  │  ├─ [TrickSlotUI] Empty                                          │  │
│  │  │  ├─ [TrickSlotUI] Empty                                          │  │
│  │  │  ├─ [TrickSlotUI] Empty                                          │  │
│  │  │  └─ [TrickSlotUI] Empty                                          │  │
│  │  │                                                                   │  │
│  │  └─ [TrickInfoPanelUI]                                              │  │
│  │     └─ Mostra info da trick selecionada                            │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎬 Fluxo de Interação: Click → Cast

```
┌────────────────────────────────────────────────────────────────────┐
│  ACTION PANEL BUTTON CLICK                                        │
│  SelectTricksButton.onClick() → HandleTricksClick()               │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│  TRICK INVENTORY VIEW.OPEN()                                      │
│  ├─ trickInventoryPanel.SetActive(true)                           │
│  └─ Refresh() [mesma lógica acima]                                │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│  USER SELECTS TRICK FROM LEARNED TRICKS                           │
│  TrickSlotUI.onClick() → TrickSelected event                      │
│     ↓                                                              │
│  TrickInventoryView.HandleTrickSelected(slot)                     │
│     ↓                                                              │
│  TrickInfoPanel exibe detalhes da trick                           │
│     ↓                                                              │
│  User clica botão "CAST"                                          │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│  TRICK INVENTORY INPUT HANDLER.ONTRICKINTERACTION()               │
│  ├─ action = TrickInventoryAction.Cast                            │
│  └─ OnCastTrick(trick)                                            │
│     ├─ Combat.TryCastPlayerTrick(trick)                           │
│     │  ├─ PlayerTrickInventory.CastTrick(trick, out instance)    │
│     │  │  ├─ Valida nível, recursos, slots                       │
│     │  │  ├─ Consome Mind/Body/Heart                             │
│     │  │  ├─ Cria TrickRuntimeInstance                           │
│     │  │  ├─ Binds no castedSlot[i]                              │
│     │  │  └─ TrickService.ApplyTrick() → Perks aplicados         │
│     │  │                                                          │
│     │  └─ Retorna sucesso/falha                                  │
│     │                                                              │
│     └─ trickInventoryView.Refresh() → UI atualizada              │
│        ├─ CastedSlot[i] agora mostra trick + cooldown            │
│        └─ LearnedTricks atualizada                               │
└────────────────────────────────────────────────────────────────────┘
```

## 🔍 Estrutura de Dados em Runtime

```
┌─ TrickInventory (implements ITrickInventory)
│  │
│  ├─ identitySlots: List<TrickSlot>[4]
│  │  ├─ TrickSlot[0]
│  │  │  ├─ Definition: TrickSO (Trick1)
│  │  │  ├─ RuntimeInstance: TrickRuntimeInstance
│  │  │  │  ├─ Definition: TrickSO
│  │  │  │  ├─ Owner: Battler (player)
│  │  │  │  ├─ ActivePerks: List<PerkRuntimeInstance>
│  │  │  │  ├─ RemainingTurns: int
│  │  │  │  └─ SlotType: Identity
│  │  │  └─ IsLocked: true
│  │  │
│  │  ├─ TrickSlot[1]...
│  │  └─ TrickSlot[3]
│  │
│  ├─ learnedTricks: List<TrickSO>
│  │  ├─ TrickSO (TrickA)
│  │  ├─ TrickSO (TrickB)
│  │  └─ TrickSO (TrickC)
│  │
│  └─ castedSlots: List<TrickSlot>[4]
│     ├─ TrickSlot[0]
│     │  ├─ Definition: TrickSO (TrickA)
│     │  ├─ RuntimeInstance: TrickRuntimeInstance
│     │  │  ├─ CooldownTurns: 2
│     │  │  └─ SlotType: Casted
│     │  └─ IsLocked: false
│     │
│     ├─ TrickSlot[1] (vazio)
│     ├─ TrickSlot[2] (vazio)
│     └─ TrickSlot[3] (vazio)
│
└─ Events
   └─ OnChanged ← Fired quando inventory muda (cast, remove)
      └─ TrickInventoryView.Refresh() subscribed
```

## 📈 Sequência de Logs Esperados

### Log Sequence: Startup
```
[CombatManager] Start: Procurando TrickInventoryInputHandler na cena...
[CombatManager] BuildPlayerTrickInventory: Construindo TrickInventory para Player
[CombatManager] BuildPlayerTrickInventory: Snapshot contém 4 identity tricks, 3 learned tricks, 0 casted slots
[TrickInventory] Inicializando para Player. IdentitySlotCount=4, CastedSlotCount=4
[TrickInventory] RestoreIdentitySlots: Restaurando 4 identity tricks para Player
[TrickInventory] IdentitySlot[0]: Restaurado trick 'Fireball' (ID: trick_fire)
[TrickInventory] IdentitySlot[1]: Restaurado trick 'Frostbolt' (ID: trick_frost)
[TrickInventory] IdentitySlot[2]: Restaurado trick 'Lightning' (ID: trick_lightning)
[TrickInventory] IdentitySlot[3]: Restaurado trick 'Heal' (ID: trick_heal)
[TrickInventory] RestoreIdentitySlots concluído. IdentitySlots preenchidos: 4/4
[TrickInventory] RestoreLearnedTricks: Restaurando 3 learned tricks para Player
[TrickInventory] Inicialização concluída. IdentitySlots=4, LearnedTricks=3, CastedSlots=4
[CombatManager] Start: Ativando identity tricks para jogador e inimigo...
[CombatManager] ActivatePlayerIdentityTricks: Ativando identity tricks para Player
[CombatManager] ActivatePlayerIdentityTricks[0]: Ativando 'Fireball' (ID: trick_fire)
[CombatManager] ActivatePlayerIdentityTricks[1]: Ativando 'Frostbolt' (ID: trick_frost)
[CombatManager] ActivatePlayerIdentityTricks[2]: Ativando 'Lightning' (ID: trick_lightning)
[CombatManager] ActivatePlayerIdentityTricks[3]: Ativando 'Heal' (ID: trick_heal)
[CombatManager] ActivatePlayerIdentityTricks: Conclusão. Identity tricks ativadas: 4/4
[CombatManager] Start: Inicializando TrickInventoryInputHandler com PlayerTrickInventory...
[TrickInventoryInputHandler] Init: Inicializando TrickInventoryInputHandler.
[TrickInventoryInputHandler] Init: Inventário de Tricks do jogador: encontrado. TrickInventoryView: encontrada.
[TrickInventoryInputHandler] Init: Vinculando inventário à TrickInventoryView...
[TrickInventoryView] BindInventory: Vinculando novo inventário de tricks. Anterior: não, Novo: sim
[TrickInventoryView] BindInventory: Inscrito ao evento OnChanged.
[TrickInventoryView] Refresh: Atualizando display de tricks. boundInventory null: False
[TrickInventoryView] Refresh: Iniciando spawn de slots. IdentitySlots=4, LearnedTricks=3, CastedSlots=4
[TrickInventoryView] SpawnSlots: Renderizando 4 slots para IdentitySlot. Parent: identitySlotsRoot
[TrickInventoryView] SpawnSlots[0] (IdentitySlot): Renderizando slot com trick 'Fireball' (ID: trick_fire). IsLocked: True
[TrickInventoryView] SpawnSlots[1] (IdentitySlot): Renderizando slot com trick 'Frostbolt' (ID: trick_frost). IsLocked: True
[TrickInventoryView] SpawnSlots[2] (IdentitySlot): Renderizando slot com trick 'Lightning' (ID: trick_lightning). IsLocked: True
[TrickInventoryView] SpawnSlots[3] (IdentitySlot): Renderizando slot com trick 'Heal' (ID: trick_heal). IsLocked: True
[TrickInventoryView] SpawnSlots: Conclusão para IdentitySlot. Parent children count: 4
[TrickInventoryView] SpawnLearnedTricks: Renderizando 3 learned tricks.
[TrickInventoryView] SpawnLearnedTricks[0]: Renderizando learned trick 'Summon' (ID: trick_summon)
[TrickInventoryView] SpawnLearnedTricks[1]: Renderizando learned trick 'Teleport' (ID: trick_teleport)
[TrickInventoryView] SpawnLearnedTricks[2]: Renderizando learned trick 'TimeWarp' (ID: trick_timewarp)
[TrickInventoryView] SpawnLearnedTricks: Conclusão. LearnedTricksRoot children count: 3
[TrickInventoryView] SpawnSlots: Renderizando 4 slots para CastedSlot. Parent: castedSlotsRoot
[TrickInventoryView] SpawnSlots[0] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[1] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[2] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[3] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots: Conclusão para CastedSlot. Parent children count: 4
[TrickInventoryView] Refresh: Spawn concluído. Total de slots renderizados: 11
[TrickInventoryInputHandler] Init: Inscrito a OnInteractWithInventoryTrick com sucesso.
```

### Log Sequence: Open UI via Button
```
[ActionPanelView] HandleTricksClick: Abrindo TrickInventoryView...
[ActionPanelView] HandleTricksClick: TrickInventoryView encontrada. Chamando Open().
[TrickInventoryView] Refresh: Atualizando display de tricks. boundInventory null: False
[TrickInventoryView] Refresh: Iniciando spawn de slots. IdentitySlots=4, LearnedTricks=3, CastedSlots=4
[... SpawnSlots outputs acima ...]
[TrickInventoryView] Refresh: Spawn concluído. Total de slots renderizados: 11
```

## ✅ Validação do Fluxo

Para confirmar que o fluxo está correto, verificar:

- [ ] Startup: 4 IdentitySlots preenchidas com tricks e marcadas como LOCKED
- [ ] Startup: LearnedTricks count = quantidade de tricks aprendidas no snapshot
- [ ] Startup: CastedSlots vazios inicialmente (4 slots)
- [ ] OnOpen: UI renderizada com 3 grids (Identity, Learned, Casted)
- [ ] OnCast: CastedSlot[i] preenchida com trick + cooldown bar
- [ ] OnRemove: CastedSlot[i] vazia novamente
