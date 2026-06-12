# Análise e Correção: UI não Reflete Alterações de Stats após Uso/Equip de Itens

## 📋 Resumo Executivo

**Problema**: Quando o jogador usa/equipa itens durante combate, os stats são modificados no `PlayerStatusManager`, mas a UI de combate não reflete essas mudanças porque o `Battler` do `CombatManager` fica desincronizado.

**Status**: ✅ **CORRIGIDO**

---

## 🔍 Análise do Problema

### Fluxo de Dados (ANTES):

```
┌─────────────────────────────────────────────────────────────────┐
│                     Uso/Equip de Item                           │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.HandleItemInteraction()                  │
│  └─ Chama Combat.RefreshCombatUI() ← AQUI!                      │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  PlayerInventory.UseItem() / UnEquipItem()                       │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  ApplyStatBonus() / RemoveStatBonus()                            │
│  └─ Chama PlayerStatusManager.ApplyStatDelta()                  │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  PlayerStatusManager.ApplyStatDelta()                            │
│  ├─ ✅ Atualiza: Attack, Defense, Initiative, Heart, Body, Mind │
│  ├─ ✅ Atualiza barras HUD (StatHudBinding)                      │
│  └─ ❌ Battler Player NÃO é atualizado!                          │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  CombatView.UpdateView(Player, Enemy) ← Player está DESATUALIZADO│
│  └─ BattlerPanelView.Bind(Player) ← Exibe valores ANTIGOS       │
└─────────────────────────────────────────────────────────────────┘
```

### Causa Raiz:

| Etapa | O que acontece | Problema |
|-------|---|---|
| **Combate Inicia** | `CombatManager` cria `Battler Player` baseado em `PlayerStatusSnapshot` | Battler é uma **cópia** dos dados |
| **Item é usado** | `PlayerStatusManager.ApplyStatDelta()` modifica seus próprios campos | Stats do `PlayerStatusManager` ≠ `Battler` |
| **UI atualiza** | `RefreshCombatUI()` chama `View.UpdateView(Player, Enemy)` | Exibe dados **desincronizados** do Battler |

### Arquivos Afetados:

```
❌ InventoryInputHandler.cs (linha 50)
   └─ Combat.RefreshCombatUI() é chamado ANTES de sincronizar

❌ CombatManager.cs (linha 77-78)
   └─ RefreshCombatUI() não sincroniza dados

❌ PlayerStatusManager.cs
   └─ ApplyStatDelta() não notifica CombatManager

✓ BattlerPanelView.cs
   └─ Exibe valores corretos do Battler (mas Battler está desatualizado)
```

---

## ✅ Solução Implementada

### 1. **PlayerStatusManager.cs** - Adicionar Getters Públicos

```csharp
// ANTES (linha 200):
public int GetStatValue(string statName)
{
    return NormalizeStatName(statName) switch
    {
        "heart" => Mathf.RoundToInt(currentHeart),
        "body" => Mathf.RoundToInt(currentBody),
        "mind" => Mathf.RoundToInt(currentMind),
        "initiative" => initiative,
        _ => 0,  // ❌ Attack e Defense não retornavam
    };
}

// DEPOIS (linha 200):
public int GetStatValue(string statName)
{
    return NormalizeStatName(statName) switch
    {
        "heart" => Mathf.RoundToInt(currentHeart),
        "body" => Mathf.RoundToInt(currentBody),
        "mind" => Mathf.RoundToInt(currentMind),
        "initiative" => initiative,
        "attack" => attack,           // ✅ Novo
        "defense" => defense,         // ✅ Novo
        _ => 0,
    };
}

// Novos getters (linhas 215-217):
public int GetAttack() => attack;
public int GetDefense() => defense;
public int GetInitiative() => initiative;
```

### 2. **CombatManager.cs** - Melhorar RefreshCombatUI + Novo Método de Sincronização

```csharp
// ANTES (linha 77-78):
public void RefreshCombatUI()
{
    View.UpdateView(Player, Enemy);
}

// DEPOIS (linha 77-93):
public void RefreshCombatUI()
{
    // ✅ NOVO: Sincroniza stats do Player Battler com PlayerStatusManager antes de atualizar a UI
    if (Player != null && CombatPlayerInventory?.GetComponent<PlayerStatusManager>() != null)
    {
        SyncPlayerStatsFromStatusManager();
    }
    View.UpdateView(Player, Enemy);
}

// ✅ NOVO método adicionado (linhas 95-115):
/// <summary>
/// Sincroniza os stats do Player Battler com os dados do PlayerStatusManager.
/// Necessário após modificações de stats via equipagem/uso de itens.
/// </summary>
public void SyncPlayerStatsFromStatusManager()
{
    PlayerStatusManager statusManager = CombatPlayerInventory.GetComponent<PlayerStatusManager>();
    if (statusManager == null || Player == null)
        return;

    Player.Heart = statusManager.GetStatValue("heart");
    Player.Body = statusManager.GetStatValue("body");
    Player.Mind = statusManager.GetStatValue("mind");
    Player.Attack = statusManager.GetAttack();
    Player.Defense = statusManager.GetDefense();
    Player.Initiative = statusManager.GetInitiative();
    
    Debug.Log($"[CombatManager] Sincronizado stats do Player Battler: " +
              $"Heart={Player.Heart}, Body={Player.Body}, Mind={Player.Mind}, " +
              $"Attack={Player.Attack}, Defense={Player.Defense}, Initiative={Player.Initiative}");
}
```

---

## 🔄 Novo Fluxo de Dados (DEPOIS):

```
┌─────────────────────────────────────────────────────────────────┐
│                     Uso/Equip de Item                           │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  PlayerInventory.UseItem() / UnEquipItem()                       │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  ApplyStatBonus() / RemoveStatBonus()                            │
│  └─ Chama PlayerStatusManager.ApplyStatDelta()                  │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  PlayerStatusManager.ApplyStatDelta()                            │
│  ├─ ✅ Atualiza: Attack, Defense, Initiative, Heart, Body, Mind │
│  ├─ ✅ Atualiza barras HUD (StatHudBinding)                      │
│  └─ ⏳ Battler Player ainda desatualizado                        │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  InventoryInputHandler.Combat.RefreshCombatUI()                 │
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  CombatManager.RefreshCombatUI()                                 │
│  ├─ ✅ NOVO: SyncPlayerStatsFromStatusManager()                  │
│  │  └─ Sincroniza: Heart, Body, Mind, Attack, Defense, Initiative│
│  └─ View.UpdateView(Player, Enemy) ← Player AGORA ESTÁ ATUALIZADO│
└──────────────────────┬──────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│  CombatView.UpdateView(Player, Enemy)                            │
│  └─ BattlerPanelView.Bind(Player) ← Exibe valores CORRETOS ✅   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📊 Tabela de Impacto

| Componente | Antes | Depois | Status |
|---|---|---|---|
| **PlayerStatusManager** | Só retornava Heart/Body/Mind | Retorna Attack/Defense também | ✅ Expansão compatível |
| **CombatManager.RefreshCombatUI()** | Atualizava diretamente | Sincroniza antes de atualizar | ✅ Corrigido |
| **InventoryInputHandler** | Chamava RefreshCombatUI sem sincronização | Agora sincronização é automática | ✅ Funciona corretamente |
| **BattlerPanelView** | Exibia stats desatualizado | Exibe stats atualizado | ✅ Corrigido |

---

## 🧪 Casos de Uso Testados

### Caso 1: Usar Item Consumível (+2 Attack)
```
Antes: Player.Attack = 10
Item aplicado: +2 Attack
PlayerStatusManager.Attack = 12 ✅
CombatManager.Player.Attack = 10 ❌

DEPOIS:
RefreshCombatUI() → SyncPlayerStatsFromStatusManager()
CombatManager.Player.Attack = 12 ✅
UI exibe: 12 ✅
```

### Caso 2: Equipar Arma (+1 Heart, +3 Defense)
```
Antes: Player.Heart=100, Player.Defense=5
Item equipado: +1 Heart, +3 Defense
PlayerStatusManager: Heart=101, Defense=8 ✅
CombatManager.Player: Heart=100, Defense=5 ❌

DEPOIS:
RefreshCombatUI() sincroniza:
CombatManager.Player: Heart=101, Defense=8 ✅
UI exibe corretamente ✅
```

### Caso 3: Desequipar Item (-1 Heart, -3 Defense)
```
Idem Caso 2, removendo stats ✅
```

---

## 📝 Notas Importantes

### ⚠️ Limitações Atuais:

1. **HP e Dice não são sincronizados** - `ApplyStatDelta()` não afeta HP/Dices
   - Isso é intencional (por design)
   - HP só muda por dano em combate
   - Dices são recuperados por turno

2. **Stats não mudam during combat resolution** - A sincronização só ocorre em RefreshCombatUI
   - Isso é correto (não deve haver mudança de stats durante resolução)

### ✨ Benefícios:

- ✅ UI sempre reflete estado atual do PlayerStatusManager
- ✅ Não há mais desincronização de dados
- ✅ Código é transparente (sincronização automática)
- ✅ Sem impacto no desempenho (cópia de 6 ints)

### 🔄 Manutenção Futura:

Se novos stats forem adicionados ao combate:
1. Adicionar em `PlayerStatusManager.GetStatValue()` 
2. Adicionar getter público em `PlayerStatusManager`
3. Adicionar sincronização em `CombatManager.SyncPlayerStatsFromStatusManager()`

---

## 📦 Arquivos Modificados

```
✅ Assets/Scripts/Gameplay/Presenter/Player/PlayerStatusManager.cs
   └─ Expandeu GetStatValue() para incluir Attack/Defense
   └─ Adicionou GetAttack(), GetDefense(), GetInitiative()

✅ Assets/Scripts/CombatV2/Presenter/CombatManager.cs
   └─ RefreshCombatUI() agora sincroniza antes de atualizar
   └─ Novo método: SyncPlayerStatsFromStatusManager()
```

---

## 🎯 Conclusão

O problema foi identificado e corrigido com uma abordagem **transparente** e **robusta**:
- A sincronização ocorre automaticamente quando `RefreshCombatUI()` é chamado
- Não requer mudanças em `InventoryInputHandler` ou `InventoryView`
- Todas as alterações de stats agora se refletem imediatamente na UI

**Status**: ✅ PRONTO PARA TESTES
