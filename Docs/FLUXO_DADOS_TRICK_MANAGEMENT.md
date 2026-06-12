# 🔀 FLUXO DE DADOS: Sistema de Gestão de Tricks

---

## 1️⃣ Inicialização (StartUp)

```
CombatManager.Start()
    ├─ 🔧 TrickService = new TrickService(PerkService)
    ├─ 🔧 TrickDatabase.GetOrCreateRuntimeDatabase()
    │
    ├─ 🎨 BattlerPanelView.Bind(Player)
    │   └─ TrickInventoryView trickView = FindObjectOfType<TrickInventoryView>()
    │       ├─ 📦 ITrickInventory inventory = BuildTrickInventory()
    │       │   └─ [4 Equipped Slots + 4 Identity Slots (empty initially)]
    │       │
    │       ├─ 🎮 TrickInventoryInputHandler.Init(CombatManager, inventory)
    │       │   └─ Subscribe: OnItemInteraction events
    │       │
    │       └─ 🎨 TrickInventoryView.Initialize(Player, inventory)
    │           └─ RefreshDisplay()
    │               ├─ Populate TrickBookGrid (all available tricks)
    │               ├─ Populate EquippedSlotsGrid (4 empty slots initially)
    │               └─ Populate IdentitySlotsGrid (4 empty/locked slots)
    │
    └─ 🔗 Subscribe to TrickService events
        ├─ OnTrickCasted → TrickInventoryView.HandleTrickCasted()
        └─ OnTrickRemoved → TrickInventoryView.HandleTrickRemoved()
```

---

## 2️⃣ Player Casting a Trick

```
┌─────────────────────────────────────────────────────────┐
│           PLAYER CASTING WORKFLOW                       │
└─────────────────────────────────────────────────────────┘

Step 1: SELECT
────────────────
Player clicks TrickIconUI in TrickBookGrid
    │
    └─→ TrickIconUI.OnClicked()
        └─→ TrickInventoryInputHandler.SelectTrick(TrickSO)
            ├─ 🔍 Validate: CanCast(battler)?
            │   ├─ Check Level >= trick.Level
            │   ├─ Check Mind >= trick.MindCost
            │   ├─ Check Body >= trick.BodyCost
            │   └─ Check Heart >= trick.HeartCost
            │
            └─ 🎨 ShowSlotSelection()
                ├─ Highlight available slots
                └─ Enable "Cast" buttons on slots

Step 2: TARGET SLOT
────────────────────
Player clicks on available slot (Equipped)
    │
    └─→ TrickSlotUI.OnCastClicked()
        └─→ TrickInventoryInputHandler.CastTrick(trickId, slotIndex)

Step 3: VALIDATE & RESERVE
───────────────────────────
TrickInventoryInputHandler validates:
    ├─ 🔍 Slot exists and is available?
    │   ├─ TrickSlot.IsEmpty OR IsReady?
    │   └─ If cooldown > 0: reject with feedback
    │
    └─ 📦 ITrickInventory.TryCastTrick(trickId, slotIndex)
        ├─ ✓ Get TrickSO from TrickDatabase
        ├─ ✓ Validate CanCast one more time
        ├─ ✓ Consume stats: battler.Mind/Body/Heart -= costs
        ├─ ✓ Set TrickSlot:
        │   ├─ Definition = TrickSO
        │   ├─ CooldownTimeRemaining = TrickSO.CooldownTurns (2)
        │   ├─ MaxCooldown = TrickSO.CooldownTurns
        │   └─ CastTime = Time.time
        │
        └─→ Dispatch OnTrickCasted event

Step 4: APPLY PERK EFFECTS
──────────────────────────
TrickService.ApplyTrick(battler, trickSO)
    ├─ Iterate trickSO.PerkIds
    ├─ For each perkId: PerkService.ApplyPerk(battler, perkId)
    │   └─ PerkRuntimeInstance created
    │       └─ Dispatch OnPerkTriggered (pode disparar imediatamente se Instant)
    │
    └─→ Dispatch OnTrickCasted event

Step 5: UI FEEDBACK
──────────────────
TrickInventoryView.HandleTrickCasted(battler, instance)
    ├─ 🎨 TrickSlotUI.Refresh()
    │   ├─ Show TrickIconUI in slot
    │   ├─ Show CooldownBar (2/2)
    │   ├─ Play EnterAnimation (0.3s slide-in)
    │   └─ statusText = "Castou {trickName}"
    │
    └─ 🎨 TrickBookGrid.Refresh()
        └─ Keep trick visible (disponível mesmo após cast)

Step 6: PERK EFFECTS (if Instant)
──────────────────────────────────
If TrickTiming.Instant:
    ├─ Perks are applied immediately
    ├─ OnPerkTriggered events fire
    └─ ActiveTricksDisplay shows feedback (flash on perk icon)
```

---

## 3️⃣ Cooldown Management (Turn End)

```
┌─────────────────────────────────────────────────────────┐
│         COOLDOWN TICK (EVERY TURN END)                 │
└─────────────────────────────────────────────────────────┘

CombatManager.EndTurn()
    │
    ├─ 🎮 PerkService.TickTurnEnd(battler)
    │   └─ Reduce perk duration
    │
    ├─ 🎯 [NEW] TrickInventory.TickSlotCooldowns()
    │   │
    │   └─→ For each TrickSlot in EquippedTricks:
    │       │
    │       ├─ if IsEmpty → skip
    │       │
    │       ├─ if IsOnCooldown:
    │       │   ├─ cooldownTime -= 1  // Decrement
    │       │   │
    │       │   └─ if cooldownTime <= 0:
    │       │       ├─ CooldownExpired = true
    │       │       └─ Dispatch OnCooldownExpired(slotIndex)
    │       │
    │       └─ if !IsOnCooldown:
    │           └─ Mark as READY
    │
    ├─ 🎨 TrickInventoryView.RefreshCooldownBars()
    │   │
    │   └─→ For each TrickSlotUI:
    │       ├─ if OnCooldown:
    │       │   ├─ Update CooldownBar fill (1/2, 0/2, etc)
    │       │   ├─ Update timer text "1/2" → "0/2"
    │       │   └─ Disable cast button
    │       │
    │       └─ if Ready:
    │           ├─ Hide CooldownBar
    │           ├─ Show "READY" indicator
    │           └─ Enable cast button / Allow drag

    └─ 🎯 Trick duration also ticks (separate from slot cooldown)
        └─ TrickService.TickTrickEnd() handles perk expiration
```

---

## 4️⃣ Removing Trick from Slot

```
┌─────────────────────────────────────────────────────────┐
│      REMOVE TRICK FROM SLOT                            │
└─────────────────────────────────────────────────────────┘

Player right-clicks TrickSlotUI
    │
    └─→ TrickContextMenu appears
        ├─ Option 1: "Remove"
        ├─ Option 2: "Send to Trick Book"
        └─ Option 3: "Cancel"

Player selects "Remove"
    │
    └─→ TrickInventoryInputHandler.RemoveTrickFromSlot(slotIndex)
        │
        ├─ 📦 ITrickInventory.RemoveTrickFromSlot(slotIndex)
        │   │
        │   ├─ Get TrickSO from slot
        │   │
        │   ├─ 🔧 TrickService.RemoveTrick(battler, trickId)
        │   │   └─ For each perkId in trick.PerkIds:
        │   │       └─ PerkService.RemovePerk(battler, perkId)
        │   │           └─ Dispatch OnPerkRemoved
        │   │
        │   ├─ 📦 TrickSlot.Definition = null
        │   ├─ 📦 TrickSlot.CooldownTimeRemaining = 0
        │   └─ Dispatch OnTrickRemoved(slotIndex)
        │
        └─→ TrickInventoryView.HandleTrickRemoved(slotIndex)
            ├─ 🎨 TrickSlotUI.Clear()
            │   ├─ Hide icon
            │   ├─ Play ExitAnimation (0.2s fade)
            │   └─ statusText = "Removeu {trickName}"
            │
            └─ 🎨 TrickBookGrid remains unchanged
                └─ Trick still visible (re-castable)

Player selects "Send to Trick Book"
    │
    └─→ Same as above, but:
        └─ Could add logic to highlight in TrickBook or auto-select
```

---

## 5️⃣ Perk Triggered → Feedback in ActiveTricksDisplay

```
┌─────────────────────────────────────────────────────────┐
│  PERK TRIGGERED EVENT CHAIN                            │
└─────────────────────────────────────────────────────────┘

During combat action (roll, attack, defense):

PerkTriggerEvaluator.EvaluateDiceTriggers()
    └─→ Condition met!
        └─→ Dispatch OnPerkTriggered(PerkTriggeredEvent)
            │
            ├─ PerkId: "six_feet_under"
            ├─ Owner: Player
            ├─ Trigger: AfterResolve
            ├─ ModifierTarget: DamagePercent
            ├─ AppliedValue: 0.30
            └─ StacksApplied: 1

TrickInventoryView.HandlePerkTriggered(evt)
    │
    ├─ 🎯 Find trick that contains this perk
    │   ├─ Query TrickDatabase.GetTrickByPerkId(evt.PerkId)
    │   └─ Find in ActiveTricksDisplay
    │
    ├─ 🎨 ActiveTricksDisplay.AnimateTrickTriggered(trickId)
    │   ├─ Quick flash/glow effect (0.2s)
    │   ├─ Play sound effect (damage, heal, buff, etc)
    │   └─ Show popup: "+30% Damage" at trick icon
    │
    └─ 📝 statusText = "{trickName} triggered!"

Also:
    ├─ 🎨 TrickSlotUI.PlayTriggerAnimation() if in slot
    │   └─ Small vibration / pulse effect
    │
    └─ 📊 Log: "[TrickTriggered] six_feet_under - DamagePercent +30%"
```

---

## 6️⃣ End of Combat State

```
CombatEnded = true
    │
    ├─ 💾 TrickInventorySnapshot saved
    │   ├─ Current equipped tricks
    │   ├─ Current cooldowns
    │   └─ Perk state
    │
    └─ 📊 Stats collected
        ├─ Tricks used: N
        ├─ Perks triggered: M
        └─ Average cooldown time: X turns
```

---

## 📊 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  PLAYER INPUT                                                   │
│  ↓                                                              │
│  TrickInventoryInputHandler (Input Router)                     │
│  ├─ Validate                                                   │
│  ├─ Route to data layer                                       │
│  └─ Dispatch events                                           │
│  ↓                                                              │
│  ITrickInventory (Data Layer)                                 │
│  ├─ TrickSlot[] EquippedTricks (4 slots)                     │
│  ├─ TrickSlot[] IdentityTricks (4 slots)                     │
│  ├─ List<TrickSO> AvailableTricks                            │
│  └─ Cooldown management                                       │
│  ↓                                                              │
│  TrickService + PerkService (Logic Layer)                     │
│  ├─ Apply/Remove tricks                                       │
│  ├─ Apply/Remove perks                                        │
│  └─ Trigger events                                            │
│  ↓                                                              │
│  Events fired:                                                 │
│  ├─ OnTrickCasted                                             │
│  ├─ OnTrickRemoved                                            │
│  ├─ OnCooldownExpired                                         │
│  ├─ OnPerkTriggered                                           │
│  └─ etc                                                        │
│  ↓                                                              │
│  TrickInventoryView (View Layer - UI Update)                  │
│  ├─ TrickBookGrid: refresh available                          │
│  ├─ EquippedSlotsGrid: update slots + cooldown bar            │
│  ├─ IdentitySlotsGrid: update identity slots                  │
│  ├─ ActiveTricksDisplay: show feedback                        │
│  └─ statusText: user feedback message                         │
│  ↓                                                              │
│  SCREEN RENDERS                                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Key State Transitions

```
TRICK CASTING:
Available → Selected → Validating → Casting → 
Casted → OnCooldown → Ready → (Castable again)

SLOT STATES:
Empty → [Cast] → Loaded (instant)
Loaded → [Turn End] → OnCooldown 
OnCooldown → [Turn End x N] → Ready
Ready → [Cast Again] → OnCooldown (restart)
Any State → [Remove] → Empty

PERK CHAIN:
ApplyTrick → ApplyPerks (x3) → OnPerkTriggered (0-3x per trigger) →
TrickSlotUI.PlayTrigger() → ActiveTricksDisplay.Flash()
```

---

## 📝 Notes

- **Slot Cooldown ≠ Perk Duration**: 
  - Slot cooldown: quanto tempo até poder casticar novamente (2 turnos)
  - Perk duration: quanto tempo o perk fica ativo (-1 = permanente)

- **ActiveTricksDisplay**: 
  - Read-only mirror of equipped + identity tricks
  - Mostra apenas tricks ativos (não em cooldown)
  - Atualiza com feedback visual quando perk triggered

- **TrickBook is immutable**: 
  - Sempre mostra todos os tricks disponíveis
  - Trickcasting não remove de lá
  - Pode casticar o mesmo trick em múltiplos slots
