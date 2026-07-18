# Diagramas de Fluxo - Antes vs Depois

## 📊 Diagrama 1: Fluxo Completo do Uso/Equip de Item

### ❌ ANTES (COM BUG):

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   JOGADOR USA/EQUIPA ITEM                                │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryView.OnInteractWithInventoryItem (EVENTO)                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.HandleItemInteraction()                          │
│  ├─ OnUseItem()                                                          │
│  ├─ OnEquipItem()                                                        │
│  ├─ OnUnequipItem()                                                      │
│  └─ OnDischardItem()                                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerInventory.UseItem()                                              │
│  PlayerInventory.UnEquipItem()                                          │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerInventory.ApplyStatBonus()                                       │
│  PlayerInventory.RemoveStatBonus()                                      │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerStatusManager.ApplyStatDelta()                                   │
│                                                                          │
│  ✅ ATUALIZA:                                                           │
│     Attack, Defense, Initiative, Heart, Body, Mind                      │
│                                                                          │
│  ✅ ATUALIZA:                                                           │
│     StatHudBinding (barras de UI)                                       │
│                                                                          │
│  ❌ NÃO ATUALIZA:                                                       │
│     Battler Player do CombatManager                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.Combat.RefreshCombatUI()                         │
│  (chamado no HandleItemInteraction)                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  CombatManager.RefreshCombatUI() ❌ ANTES:                              │
│  └─ View.UpdateView(Player, Enemy)                                      │
│     (Battler Player ainda tem dados antigos!)                           │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  CombatView.UpdateView()                                                 │
│  ├─ PlayerPanel.Bind(Player)                                            │
│  ├─ EnemyPanel.Bind(Enemy)                                              │
│  └─ InfoPanelView.Bind(Player, Enemy)                                   │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  BattlerPanelView.Bind(Battler)                                          │
│                                                                          │
│  ❌ EXIBE VALORES ANTIGOS:                                              │
│     Heart=100, Body=50, Mind=75, Attack=10, Defense=5                  │
│                                                                          │
│  DEVERIAM SER:                                                           │
│     Heart=101, Body=50, Mind=75, Attack=12, Defense=8                  │
└─────────────────────────────────────────────────────────────────────────┘

RESULTADO: ❌ UI não reflete mudanças
```

---

### ✅ DEPOIS (CORRIGIDO):

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   JOGADOR USA/EQUIPA ITEM                                │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryView.OnInteractWithInventoryItem (EVENTO)                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.HandleItemInteraction()                          │
│  ├─ OnUseItem()                                                          │
│  ├─ OnEquipItem()                                                        │
│  ├─ OnUnequipItem()                                                      │
│  └─ OnDischardItem()                                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerInventory.UseItem()                                              │
│  PlayerInventory.UnEquipItem()                                          │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerInventory.ApplyStatBonus()                                       │
│  PlayerInventory.RemoveStatBonus()                                      │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  PlayerStatusManager.ApplyStatDelta()                                   │
│                                                                          │
│  ✅ ATUALIZA:                                                           │
│     Attack, Defense, Initiative, Heart, Body, Mind                      │
│                                                                          │
│  ✅ ATUALIZA:                                                           │
│     StatHudBinding (barras de UI)                                       │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.Combat.RefreshCombatUI()                         │
│  (chamado no HandleItemInteraction)                                     │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  CombatManager.RefreshCombatUI() ✅ AGORA:                              │
│  ├─ if (Player != null && CombatPlayerInventory != null)               │
│  │  └─ SyncPlayerStatsFromStatusManager() 🆕                           │
│  │     └─ Player.Heart = statusManager.GetStatValue("heart")          │
│  │     └─ Player.Body = statusManager.GetStatValue("body")            │
│  │     └─ Player.Mind = statusManager.GetStatValue("mind")            │
│  │     └─ Player.Attack = statusManager.GetAttack()                   │
│  │     └─ Player.Defense = statusManager.GetDefense()                 │
│  │     └─ Player.Initiative = statusManager.GetInitiative()           │
│  │     └─ Debug.Log("Sincronizado stats...")                          │
│  │                                                                      │
│  └─ View.UpdateView(Player, Enemy)                                     │
│     (Battler Player AGORA tem dados atualizados!)                      │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  CombatView.UpdateView()                                                 │
│  ├─ PlayerPanel.Bind(Player)                                            │
│  ├─ EnemyPanel.Bind(Enemy)                                              │
│  └─ InfoPanelView.Bind(Player, Enemy)                                   │
└──────────────────────────────┬──────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  BattlerPanelView.Bind(Battler)                                          │
│                                                                          │
│  ✅ EXIBE VALORES ATUALIZADOS:                                          │
│     Heart=101, Body=50, Mind=75, Attack=12, Defense=8                  │
└─────────────────────────────────────────────────────────────────────────┘

RESULTADO: ✅ UI reflete mudanças imediatamente
```

---

## 🔄 Diagrama 2: Comparação de Sincronização

### Dado: Equipar Arma (+2 Attack, +1 Heart)

```
╔══════════════════════════════════════════════════════════════════════╗
║                         ANTES (❌ BUG)                               ║
╚══════════════════════════════════════════════════════════════════════╝

PlayerStatusManager                  Battler Player (CombatManager)
    ├─ Attack: 10                        ├─ Attack: 10
    ├─ Heart: 100                        ├─ Heart: 100
    └─ ...                               └─ ...
    
         ↓ ApplyStatDelta(+2 Attack, +1 Heart)
    
    ├─ Attack: 12 ✅                     ├─ Attack: 10 ❌ DESINCRONIZADO
    ├─ Heart: 101 ✅                     ├─ Heart: 100 ❌ DESINCRONIZADO
    └─ ...                               └─ ...
    
    UI exibe valores do Battler:
    Heart=100, Attack=10 ❌ ERRADO


╔══════════════════════════════════════════════════════════════════════╗
║                        DEPOIS (✅ CORRIGIDO)                         ║
╚══════════════════════════════════════════════════════════════════════╝

PlayerStatusManager                  Battler Player (CombatManager)
    ├─ Attack: 10                        ├─ Attack: 10
    ├─ Heart: 100                        ├─ Heart: 100
    └─ ...                               └─ ...
    
         ↓ ApplyStatDelta(+2 Attack, +1 Heart)
    
    ├─ Attack: 12 ✅                     ├─ Attack: 10 (ainda)
    ├─ Heart: 101 ✅                     ├─ Heart: 100 (ainda)
    └─ ...                               └─ ...
    
         ↓ SyncPlayerStatsFromStatusManager() 🆕
    
    ├─ Attack: 12 ✅                     ├─ Attack: 12 ✅ SINCRONIZADO
    ├─ Heart: 101 ✅                     ├─ Heart: 101 ✅ SINCRONIZADO
    └─ ...                               └─ ...
    
    UI exibe valores do Battler:
    Heart=101, Attack=12 ✅ CORRETO
```

---

## 📐 Diagrama 3: Método SyncPlayerStatsFromStatusManager()

```
SyncPlayerStatsFromStatusManager()
{
    // 1. Obter referência ao PlayerStatusManager
    PlayerStatusManager statusManager = CombatPlayerInventory.GetComponent<PlayerStatusManager>();
    ↓
    
    // 2. Validação
    if (statusManager == null || Player == null)
        return;
    ↓
    
    // 3. Copiar valores
    ┌─ Player.Heart      ← statusManager.GetStatValue("heart")
    ├─ Player.Body       ← statusManager.GetStatValue("body")
    ├─ Player.Mind       ← statusManager.GetStatValue("mind")
    ├─ Player.Attack     ← statusManager.GetAttack()
    ├─ Player.Defense    ← statusManager.GetDefense()
    └─ Player.Initiative ← statusManager.GetInitiative()
    ↓
    
    // 4. Debug
    Debug.Log("[CombatManager] Sincronizado stats...")
}
```

---

## 🎬 Diagrama 4: Timeline de Execução

### Cenário: Equipar Arma com +2 Attack

```
TIMELINE                        PlayerStatusManager    Battler Player      UI
──────────────────────────────────────────────────────────────────────────────

00ms: Botão clicado
      └─ inventoryView.OnInteract("Arma", Equip)

05ms: InventoryInputHandler ativa
      └─ HandleItemInteraction("Arma", Equip)

10ms: PlayerInventory.UseItem("Arma")

15ms: ApplyStatBonus("Arma")
      └─ ApplyStatDelta("attack", +2)
      
      Attack: 10 → 12 ✅              Attack: 10 ❌
                                      
20ms: RefreshCombatUI() chamado

25ms: ❌ ANTES: UpdateView sem sync   Attack: 10 ❌
      ✅ DEPOIS: SyncPlayerStatFromStatusManager()
                                      Attack: 12 ✅
                                      
30ms: BattlerPanelView.Bind(Player)
      └─ AttackText = Player.Attack
      
      ✅ EXIBE: 12                   Attack: 12 ✅      Mostra: 12 ✅
```

---

## 💾 Diagram 5: Estrutura de Dados

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Jugador em Combate                                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────┐    ┌─────────────────────────────┐
│   PlayerStatusManager        │    │   CombatManager.Player      │
│   (Gameplay Scene)           │    │   (Combat Scene)            │
├─────────────────────────────┤    ├─────────────────────────────┤
│ currentHeart: 100            │    │ Heart: 100                  │
│ currentBody: 50              │    │ Body: 50                    │
│ currentMind: 75              │    │ Mind: 75                    │
│ attack: 10                   │    │ Attack: 10                  │
│ defense: 5                   │    │ Defense: 5                  │
│ initiative: 12               │    │ Initiative: 12              │
└─────────────────────────────┘    └─────────────────────────────┘
              ↓                               ↓
        ApplyStatDelta()            
        ("attack", +2)               Ainda tem dados antigos
              ↓                               
        attack: 12 ✅                       ❌
              
              ↓ SyncPlayerStatsFromStatusManager() (NOVO)
              
              └───────────────────────────→ Attack: 12 ✅
                                                ↓
                                            BattlerPanelView
                                            AttackText = 12 ✅
```

---

## 🎯 Resumo Visual

| Etapa | Antes | Depois |
|-------|-------|--------|
| 1. Item equipado | PlayerStatusManager atualizado | ✓ PlayerStatusManager atualizado |
| 2. RefreshCombatUI | Battler não sincronizado | ✓ Battler sincronizado |
| 3. UI atualiza | Exibe valores antigos ❌ | ✓ Exibe valores corretos |

---

## 📍 Localização das Mudanças

```
Assets/Scripts/
├── Gameplay/
│   └── Presenter/
│       └── Player/
│           └── PlayerStatusManager.cs 📝 EXPANDIU GetStatValue()
│               ├─ GetAttack() 🆕
│               ├─ GetDefense() 🆕
│               └─ GetInitiative() 🆕
│
└── CombatV2/
    └── Presenter/
        └── CombatManager.cs 📝 MELHOROU RefreshCombatUI()
            └─ SyncPlayerStatsFromStatusManager() 🆕
```

Todas as mudanças são **localizadas** e **não invasivas**.
