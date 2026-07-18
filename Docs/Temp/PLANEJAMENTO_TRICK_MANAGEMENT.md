# 📋 PLANEJAMENTO: Sistema Completo de Gestão de Tricks

> **Status:** Planejamento Fase de Design  
> **Objetivo:** Transformar TrickView atual em sistema robusto de gerenciamento com slots, cooldown e feedback visual  
> **Referência:** Sistema de Inventário de Itens (InventoryView + InventoryInputHandler)

---

## 🎯 Resumo Executivo

O sistema será baseado em **3 grids de UI** que refletem o estado do inventário de tricks:

| Grid | Slots | Propósito | Interação |
|------|-------|----------|-----------|
| **Trick Book** | ∞ | Todos os tricks disponíveis | Selecionar para casticar |
| **Equipped** | 4 | Tricks em cooldown após cast | Remove, Send to Book, Ver cooldown |
| **Identity** | 4 | Tricks de identidade (permanentes) | View only, locked se não tem |

---

## 🏗️ Arquitetura de Alto Nível

```
CombatManager
    ├── TrickService (exists)
    ├── ITrickInventory (NEW - data layer)
    │   └── TrickSlot[] (equipped + identity)
    │
    ├── TrickInventoryInputHandler (NEW - input routing)
    │   └── Valida, roteia, dispara eventos
    │
    └── TrickInventoryView (NEW - UI layer)
        ├── TrickBookGrid (TrickIconUI[])
        ├── EquippedSlotsGrid (TrickSlotUI[])
        ├── IdentitySlotsGrid (TrickSlotUI[])
        └── ActiveTricksDisplay (read-only, apenas feedback)
```

---

## 🔄 Fluxo de Casting (Sequência Completa)

```
1. SELEÇÃO
   Player clica em TrickIconUI (Trick Book)
   ↓
2. VALIDAÇÃO
   TrickInventoryInputHandler valida:
   - Level suficiente?
   - Stats suficientes?
   - Slots disponíveis?
   ↓
3. SLOT SELECTION
   UI mostra slots disponíveis (1/2/3/4)
   ↓
4. CONFIRM CAST
   Player clica em slot específico
   ↓
5. EXECUTE
   TrickInventory.TryCastTrick(trickId, slotIndex)
   ├─ Consume stats
   ├─ Start cooldown (2 turnos)
   ├─ TrickService.ApplyTrick() → perks disparados
   └─ Dispatch OnTrickCasted event
   ↓
6. FEEDBACK
   TrickInventoryView.Refresh()
   ├─ Slot mostra icon + cooldown bar
   ├─ Trick Book atualiza (remove de available se necessário)
   └─ Status text: "Castou {TrickName}"
   ↓
7. COOLDOWN
   A cada OnTurnEnd:
   ├─ TrickInventory.TickSlotCooldowns()
   ├─ cooldownTime -= 1
   └─ Se cooldownTime == 0: slot fica READY
```

---

## 💾 Estrutura de Dados

### TrickSlot (Nova)
```csharp
public class TrickSlot
{
    public TrickSO Definition { get; set; }          // null = vazio
    public float CooldownTimeRemaining { get; set; } // 0 = ready
    public float MaxCooldown { get; set; }           // tempo total
    public bool IsIdentitySlot { get; set; }
    public float CastTime { get; set; }
    
    public bool IsEmpty => Definition == null;
    public bool IsOnCooldown => CooldownTimeRemaining > 0;
    public bool IsReady => !IsEmpty && !IsOnCooldown;
}
```

### ITrickInventory (Nova Interface)
```csharp
public interface ITrickInventory
{
    IReadOnlyList<TrickSO> AvailableTricks { get; }
    TrickSlot[] EquippedTricksSlots { get; }  // 4 slots
    TrickSlot[] IdentityTricksSlots { get; }  // 4 slots
    
    bool TryCastTrick(string trickId, int slotIndex);
    void RemoveTrickFromSlot(int slotIndex);
    void SendToTrickBook(int slotIndex);
    bool IsOnCooldown(string trickId);
    float GetCooldownRemaining(string trickId);
    
    void TickSlotCooldowns();  // Chamado cada turn end
    ITrickInventorySnapshot GetSnapshot();
}
```

---

## 🎨 Hierarquia de UI

```
TrickInventoryView (MonoBehaviour)
│
├─ TrickBookPanel
│  └─ GridLayoutGroup (AvailableTricks)
│     └─ TrickIconUI[] (renderizado dinamicamente)
│        └─ Clique → Seleciona para casticar
│
├─ EquippedPanel
│  └─ GridLayoutGroup (4 slots)
│     └─ TrickSlotUI[] x4
│        ├─ Icon (trick visual)
│        ├─ CooldownBar (visual + timer text)
│        ├─ RarityBorder (glow effect)
│        └─ ContextMenu button → Remove/SendToBook
│
├─ IdentityPanel
│  └─ GridLayoutGroup (4 slots)
│     └─ TrickSlotUI[] x4 (identity tricks - locked se não tem)
│        └─ ContextMenu → view only
│
└─ ActiveTricksDisplay (Status Display)
   ├─ TrickIconUI[] (apenas read-only, mostra tricks ativos)
   └─ StatusText (feedback: "Castou X", "X expirou", etc)
```

---

## 📊 Estados de TrickSlot

```
┌──────────────────────────────────────┐
│           SLOT STATE MACHINE         │
└──────────────────────────────────────┘

Empty
 ├─ [Cast Trick]
 │  └─ Loaded (cooldownTime = maxCooldown)
 │
 └─ [Send from Book]
    └─ Loaded

Loaded
 ├─ [Remove]
 │  └─ Empty
 │
 ├─ [Send to Book]
 │  └─ Empty
 │
 └─ [Turn End - Tick]
    └─ OnCooldown (cooldownTime -= 1)

OnCooldown
 ├─ [Remove]
 │  └─ Empty
 │
 ├─ [Send to Book]
 │  └─ Empty
 │
 └─ [Turn End - Tick]
    └─ Ready (cooldownTime == 0)

Ready
 ├─ [Remove]
 │  └─ Empty
 │
 ├─ [Cast Again]
 │  └─ OnCooldown
 │
 └─ [Turn End]
    └─ Ready (stays)
```

---

## 🎬 Feedback Visual por Evento

| Evento | Duração | Efeito |
|--------|---------|--------|
| **Trick Casted** | 0.3s | Slot slide-in + scale pop |
| **Trick Removed** | 0.2s | Fade out |
| **Cooldown Expired** | 0.5s | Glow effect, timer disappears |
| **Perk Triggered** | 0.2s | Pequeno flash no TrickIconUI |
| **On Cooldown** | Contínuo | Icon desaturado, cooldown bar visible |

---

## 📁 Novos Scripts a Criar

### **Camada de Modelo**
```
├─ TrickSlot.cs (20 linhas)
├─ ITrickInventory.cs (interface - 30 linhas)
└─ TrickInventory.cs (implementação - 150 linhas)
```

### **Camada de Lógica**
```
├─ TrickInventoryInputHandler.cs (150 linhas)
└─ TrickCooldownManager.cs (50 linhas - ou integrado em TrickService)
```

### **Camada de View**
```
├─ TrickInventoryView.cs (REFATORADO - 200 linhas)
├─ TrickSlotUI.cs (150 linhas)
├─ TrickContextMenu.cs (100 linhas)
```

**Total estimado:** ~800 linhas de código novo + refatoração

---

## 🔌 Integração com Sistemas Existentes

### Com TrickService
```
✓ TrickService.TryCastTrick() → Valida + aplica perks
✓ TrickService.RemoveTrick() → Remove perks
✓ TrickService.OnTrickCasted → Dispara feedback
✓ TrickService.OnTrickRemoved → Dispara feedback
✓ TrickService.OnPerkTriggered → Feedback em ActiveTricksDisplay
```

### Com CombatManager
```
CombatManager.EndTurn()
  ├─ BattlerStateService.TickTurnEnd()
  ├─ PerkService.TickTurnEnd()
  ├─ TrickService.TickTrickEnd()
  └─ [NEW] TrickInventory.TickSlotCooldowns()
     └─ Reduz cooldown em cada slot
```

### Com BattlerPanelView
```
BattlerPanelView.Initialize()
  ├─ perkDisplayView.Initialize(battler, perkService)
  └─ [NEW] trickInventoryView.Initialize(battler, trickInventory)
```

---

## 🆕 Campos a Adicionar

### TrickSO (Nova Propriedade)
```csharp
[Header("Cooldown")]
public int CooldownTurns = 2;  // Turnos antes de poder casticar novamente
```

### Battler.cs (Verifica)
```csharp
// Já tem:
public List<TrickRuntimeInstance> Tricks = new();
// Adicionar se não tiver:
public List<TrickSlot> EquippedTrickSlots = new(); // 4 slots
public List<TrickSlot> IdentityTrickSlots = new(); // 4 slots
```

---

## 📋 Checklist de Fases

### **Fase 1: Data Layer** ✋ PLANEJAMENTO (ATUAL)
- [ ] Design TrickSlot.cs
- [ ] Design ITrickInventory.cs + TrickInventory.cs
- [ ] Validar com stakeholders

### **Fase 2: Input Layer** ⏳ PRÓXIMO
- [ ] TrickInventoryInputHandler.cs
- [ ] Event routing + validation
- [ ] Testes unitários

### **Fase 3: View Layer** ⏳ PRÓXIMO
- [ ] TrickInventoryView.cs
- [ ] TrickSlotUI.cs com cooldown bar
- [ ] TrickContextMenu.cs

### **Fase 4: Visual Polish** ⏳ PRÓXIMO
- [ ] Animações
- [ ] Audio feedback
- [ ] Testes end-to-end

---

## ⚠️ Decisões de Design

1. **Slots Fixos**: 4 slots de equipped, 4 de identity (não escalável dinamicamente)
2. **Cooldown Independente**: Cada slot tem seu próprio cooldown (não compartilhado)
3. **No Removal Fee**: Remover de um slot não consome recursos
4. **Identity Locked**: Tricks de identidade são sempre visíveis mas "locked" se não tem acesso
5. **One Trick Per Slot**: Um trick só pode estar em um slot por vez
6. **Stat Consumption**: Custos são consumidos imediatamente ao casticar

---

## 🎓 Referências Externas

Sistema de Inventário Existente:
- `InventoryView.cs` - Padrão de refresh com 3 grids
- `InventoryInputHandler.cs` - Padrão de event routing
- `InventoryItemUI.cs` - Padrão de item slot individual

---

## 🚀 Próximos Passos

1. **Aprovação do Planejamento** ← Você está aqui
2. **Implementar Fase 1** (TrickSlot + ITrickInventory)
3. **Implementar Fase 2** (TrickInventoryInputHandler)
4. **Implementar Fase 3** (UI Views)
5. **Testes e Polish**

