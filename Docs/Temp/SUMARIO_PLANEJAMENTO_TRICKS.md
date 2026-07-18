# 📌 SUMÁRIO EXECUTIVO: Planejamento Sistema de Tricks

## 🎯 Status: PLANEJAMENTO COMPLETO

---

## 📄 Documentos de Referência

1. **PLANEJAMENTO_TRICK_MANAGEMENT.md** - Planejamento arquitetônico completo
2. **FLUXO_DADOS_TRICK_MANAGEMENT.md** - Diagramas de fluxo e sequências
3. **Memória Session:** `/memories/session/trick-management-plan.md` - Referência rápida

---

## 🏗️ Resumo da Arquitetura

### Camadas do Sistema
```
INPUT LAYER          DATA LAYER              LOGIC LAYER         VIEW LAYER
─────────────────────────────────────────────────────────────────────────
TrickInventoryInput  ITrickInventory         TrickService        TrickInventoryView
Handler             └─ TrickSlot[]          + PerkService       ├─ TrickBookGrid
(Event routing)     └─ Manage cooldown      (Apply/Remove)      ├─ EquippedSlotsGrid
                                                                 ├─ IdentitySlotsGrid
                                                                 └─ ActiveTricksDisplay
```

### Responsabilidades

| Camada | O quê | Scripts |
|--------|-------|---------|
| **Data** | Armazena estado (slots, cooldown) | TrickSlot.cs, ITrickInventory.cs, TrickInventory.cs |
| **Input** | Roteia eventos, valida | TrickInventoryInputHandler.cs |
| **Logic** | Aplica perks, gerencia eventos | TrickService.cs (existe), TrickCooldownManager.cs (novo) |
| **View** | Renderiza UI, feedback | TrickInventoryView.cs, TrickSlotUI.cs, TrickContextMenu.cs |

---

## 🔴 Componentes Principais

### 1. TrickSlot.cs (Novo)
**Responsabilidade:** Representa um slot individual (vazio ou com trick)

```csharp
Propriedades:
├─ TrickSO Definition
├─ float CooldownTimeRemaining
├─ float MaxCooldown
├─ bool IsIdentitySlot
├─ float CastTime

Métodos:
├─ bool IsEmpty
├─ bool IsOnCooldown
├─ bool IsReady
└─ void DecrementCooldown()
```

**Linhas estimadas:** 30-40

---

### 2. ITrickInventory.cs (Nova Interface)
**Responsabilidade:** Contrato para gerenciamento de tricks (similar a ICombatInventory)

```csharp
Propriedades:
├─ IReadOnlyList<TrickSO> AvailableTricks
├─ TrickSlot[] EquippedTricksSlots (4)
├─ TrickSlot[] IdentityTricksSlots (4)

Métodos:
├─ bool TryCastTrick(trickId, slotIndex)
├─ void RemoveTrickFromSlot(slotIndex)
├─ void SendToTrickBook(slotIndex)
├─ bool IsOnCooldown(trickId)
├─ float GetCooldownRemaining(trickId)
├─ void TickSlotCooldowns()
└─ ITrickInventorySnapshot GetSnapshot()

Eventos:
├─ OnTrickCasted(battler, instance)
├─ OnTrickRemoved(battler, trickId)
└─ OnCooldownExpired(slotIndex)
```

**Linhas estimadas:** 30 (interface)

---

### 3. TrickInventory.cs (Implementação)
**Responsabilidade:** Implementação de ITrickInventory com lógica de slots e cooldown

```csharp
Campos:
├─ TrickSlot[] equippedSlots (4)
├─ TrickSlot[] identitySlots (4)
├─ List<TrickSO> availableTricks
├─ PerkService perkService
└─ TrickDatabase trickDatabase

Métodos principais:
├─ Constructor: initialize slots
├─ TryCastTrick(): validate + apply + start cooldown
├─ RemoveTrickFromSlot(): remove perks + clear slot
├─ TickSlotCooldowns(): decrement all active cooldowns
└─ Helper methods
```

**Linhas estimadas:** 150-180

---

### 4. TrickInventoryInputHandler.cs (Novo)
**Responsabilidade:** Roteia input do UI para data layer + validação

```csharp
Métodos:
├─ Init(CombatManager, ITrickInventory)
├─ SelectTrick(TrickSO)
├─ CastTrick(slotIndex)
├─ RemoveTrickFromSlot(slotIndex)
├─ SendToTrickBook(slotIndex)
└─ HandleItemInteraction() [callback]

Validações:
├─ CanCast checks (level, stats)
├─ Slot availability
└─ Cooldown state
```

**Linhas estimadas:** 140-180

---

### 5. TrickInventoryView.cs (REFATORADO)
**Responsabilidade:** Gerencia 3 grids + refresh quando estado muda

```csharp
Campos:
├─ GridLayoutGroup trickBookGrid
├─ GridLayoutGroup equippedSlotsGrid
├─ GridLayoutGroup identitySlotsGrid
├─ GridLayoutGroup activeDisplayGrid

Métodos:
├─ Initialize(battler, inventory)
├─ RefreshDisplay()
├─ RefreshCooldownBars()
├─ HandleTrickCasted()
├─ HandleTrickRemoved()
├─ HandleCooldownExpired()
└─ HandlePerkTriggered()
```

**Linhas estimadas:** 200-250

---

### 6. TrickSlotUI.cs (Novo)
**Responsabilidade:** UI de um slot individual com cooldown bar e contexto menu

```csharp
Estados visuais:
├─ Empty (cinzento, vazio)
├─ Loaded (icon + rarity border)
├─ OnCooldown (desaturado + progress bar)
├─ Ready (brighten + "READY" indicator)
└─ IdentityLocked (com ícone de cadeado)

Métodos:
├─ Setup(TrickSlot)
├─ RefreshCooldown()
├─ PlayTriggerAnimation()
├─ ShowContextMenu()
└─ UpdateVisualState()
```

**Linhas estimadas:** 150-180

---

### 7. TrickContextMenu.cs (Novo)
**Responsabilidade:** Menu de contexto com opções Remove/SendToBook

```csharp
Opções:
├─ Remove (discard forever)
├─ Send to Book (keep available)
├─ Cancel

Eventos:
├─ OnRemoveSelected(slotIndex)
└─ OnSendToBookSelected(slotIndex)
```

**Linhas estimadas:** 80-120

---

## 📊 Fluxo de Interação Principal

```
1. PLAYER SELECTS TRICK
   TrickIconUI.OnClick() → TrickInventoryInputHandler.SelectTrick()

2. HANDLER VALIDATES
   ├─ Check CanCast (level, stats)
   └─ Show available slots

3. PLAYER SELECTS SLOT
   TrickSlotUI.OnCastClick() → TrickInventoryInputHandler.CastTrick(slotIndex)

4. INVENTORY PROCESSES
   ITrickInventory.TryCastTrick()
   ├─ Validate slot availability
   ├─ Consume stats
   ├─ Set slot (Definition + Cooldown)
   ├─ Call TrickService.ApplyTrick()
   └─ Dispatch OnTrickCasted

5. SERVICES APPLY EFFECTS
   TrickService.ApplyTrick()
   ├─ Iterate PerkIds
   ├─ Call PerkService.ApplyPerk()
   └─ OnPerkTriggered fired

6. UI UPDATES
   TrickInventoryView.HandleTrickCasted()
   ├─ Refresh TrickSlotUI (show icon + cooldown)
   ├─ Refresh TrickBookGrid
   ├─ Show feedback text
   └─ Play animation

7. EACH TURN END
   ITrickInventory.TickSlotCooldowns()
   ├─ Decrement all cooldowns
   ├─ Fire OnCooldownExpired if reaches 0
   └─ TrickInventoryView refreshes bars
```

---

## ⏱️ Cooldown Mechanic

```
State Machine:
Empty
  ↓ [Cast]
Loaded → CooldownTime = 2
  ↓ [Turn End]
OnCooldown (1/2)
  ↓ [Turn End]
OnCooldown (0/2)
  ↓ [Turn End]
Ready (available again)
  ↓ [Cast again or Remove]
...

UI Representation:
├─ Cooldown Bar: percentage fill (0% = ready, 100% = full cooldown)
├─ Timer Text: "1/2", "0/2"
├─ Color: Red (cooling) → Green (ready)
└─ Interactivity: Disable/enable cast buttons based on state
```

---

## 🎨 Visual Feedback

| Evento | Duração | Efeito | Script |
|--------|---------|--------|--------|
| Trick Casted | 0.3s | Slot slide-in + pop | TrickSlotUI |
| Trick Removed | 0.2s | Fade out | TrickSlotUI |
| Cooldown Expired | 0.5s | Glow pulse | TrickSlotUI |
| Perk Triggered | 0.2s | Flash em icon | ActiveDisplay |
| On Cooldown | Contínuo | Desaturado + bar | TrickSlotUI |

---

## 🔗 Integração Pontos

### Com CombatManager.EndTurn()
```
EndTurn()
├─ BattlerStateService.TickTurnEnd()
├─ PerkService.TickTurnEnd()
├─ TrickService.TickTrickEnd()
└─ [NEW] TrickInventory.TickSlotCooldowns()
```

### Com BattlerPanelView.Initialize()
```
BattlerPanelView.Bind(battler)
├─ perkDisplayView.Initialize()
└─ [NEW] trickInventoryView.Initialize()
```

### Com PerkService
```
TrickInventory.TryCastTrick()
└─ TrickService.ApplyTrick()
    └─ Iterate perks
        └─ PerkService.ApplyPerk()
            └─ Perks fire events
```

---

## 📈 Estimativa de Trabalho

| Fase | Componentes | Linhas | Tempo Estimado |
|------|-------------|--------|---|
| 1: Data Layer | TrickSlot, ITrickInventory, TrickInventory | ~200 | 2-3h |
| 2: Input Layer | TrickInventoryInputHandler | ~160 | 2h |
| 3: View Layer | TrickInventoryView, TrickSlotUI, TrickContextMenu | ~430 | 4-5h |
| 4: Polish | Animações, Audio, Testes | - | 3-4h |
| **TOTAL** | **3 classes, 1 interface, 3 views** | **~800** | **11-15h** |

---

## ✅ Definições de Pronto

### Cada Script está Pronto quando:
- ✅ Compila sem erros
- ✅ Testes unitários passam
- ✅ Integra com dependências
- ✅ Code review aprovado
- ✅ Documentação inline

### Sistema está Pronto quando:
- ✅ Todos os scripts compilam
- ✅ Player pode casticar tricks
- ✅ Cooldown decrementa
- ✅ UI atualiza em tempo real
- ✅ Perks são aplicados corretamente
- ✅ Feedback visual funciona
- ✅ Testes end-to-end passam

---

## 🚀 Fases de Implementação

### **Fase 1: Data Layer** (4-6 horas)
- [ ] TrickSlot.cs
- [ ] ITrickInventory.cs
- [ ] TrickInventory.cs
- [ ] Unit tests

### **Fase 2: Input Layer** (2-3 horas)
- [ ] TrickInventoryInputHandler.cs
- [ ] Event routing
- [ ] Validation logic
- [ ] Integration tests

### **Fase 3: View Layer** (4-5 horas)
- [ ] TrickInventoryView.cs (refactored)
- [ ] TrickSlotUI.cs
- [ ] TrickContextMenu.cs
- [ ] Animations
- [ ] UI tests

### **Fase 4: Integration & Polish** (3-4 horas)
- [ ] CombatManager integration
- [ ] BattlerPanelView updates
- [ ] Audio feedback
- [ ] E2E tests
- [ ] Performance optimization

---

## 📝 Próximas Ações

**ANTES de começar a implementação:**

1. [ ] Review documentos: PLANEJAMENTO_TRICK_MANAGEMENT.md
2. [ ] Review fluxo: FLUXO_DADOS_TRICK_MANAGEMENT.md
3. [ ] Validar arquitetura com lead
4. [ ] Confirmar estimativas de tempo
5. [ ] Alocar recursos

**Depois de aprovação:**

1. [ ] Start Fase 1: TrickSlot + ITrickInventory
2. [ ] Create unit test plan
3. [ ] Begin development sprint

---

## 🎓 Documentação Complementar

Ver também:
- `ANALISE_MODIFICACOES_REFACTOR.md` - Refactor de Perks/Tricks (background)
- `ANALISE_TRICK_SYSTEM.md` - Análise inicial do sistema de Tricks
- Code references:
  - InventoryView.cs (padrão a seguir)
  - InventoryInputHandler.cs (padrão a seguir)
  - TrickService.cs (integração)
  - PerkService.cs (integração)

---

## 💡 Notas Importantes

1. **Slots são fixos:** 4 equipped + 4 identity (não escalam)
2. **Cooldown é por slot:** Cada slot tem seu próprio cooldown
3. **Trick Book é imutável:** Sempre mostra todos (disponível após cast)
4. **Stats consumidos imediatamente:** Mesmo se cast falhar por espaço
5. **Perks independentes:** Cooldown do slot ≠ duração do perk
6. **Identity tricks:** Locked se não tem acesso, mas sempre visíveis
7. **ActiveDisplay é read-only:** Apenas mostra tricks ativos com feedback

---

## 🔗 Links Internos

- Memória Técnica: `/memories/repo/perk-trick-refactor.md`
- Session Plan: `/memories/session/trick-management-plan.md`
- Conversa anterior: [Refactor de Perks e Tricks]

