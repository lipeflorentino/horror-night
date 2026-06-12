# RefreshCombatUI - Documentação e Recomendações

## 📌 O que faz?

`CombatManager.RefreshCombatUI()` atualiza toda a interface de combate refletindo o estado atual do `Battler Player` e `Battler Enemy`.

### Novo Comportamento (v2):

```csharp
public void RefreshCombatUI()
{
    // 1️⃣ Sincroniza Player Battler com PlayerStatusManager
    if (Player != null && CombatPlayerInventory?.GetComponent<PlayerStatusManager>() != null)
    {
        SyncPlayerStatsFromStatusManager();
    }
    
    // 2️⃣ Atualiza todos os painéis de UI com dados atualizados
    View.UpdateView(Player, Enemy);
}
```

---

## 🔄 Fluxo Completo

### O que `View.UpdateView()` faz:

```csharp
public void UpdateView(Battler player, Battler enemy)
{
    // Atualiza painel do jogador
    PlayerPanel.Bind(player);
    
    // Atualiza painel do inimigo
    EnemyPanel.Bind(enemy);
    
    // Atualiza painel de informações
    InfoPanelView.Bind(player, enemy);
}
```

### O que `BattlerPanelView.Bind()` atualiza:

```
┌─ HP (barra + texto)
├─ Heart / Body / Mind (valores)
├─ Attack / Defense / Initiative (valores)
├─ Name / Level
├─ Dice Count (em progresso)
└─ Feedback visual
```

---

## 📍 Locais onde RefreshCombatUI é chamado

| Local | Razão | Status |
|---|---|---|
| `CombatManager.Start()` | Inicialização da UI | ✅ Correto |
| `CombatManager.Resolve()` | Após resolver ação | ✅ Correto |
| `CombatManager.EndTurn()` | Após finalizar turno | ✅ Correto |
| `InventoryInputHandler.HandleItemInteraction()` | **NOVO: após usar/equipar item** | ✅ Agora sincroniza |
| `CombatInputHandler.SetAllowedAction()` | Após mudar turno | ✅ Correto |

---

## ⚙️ Sincronização de Stats

### O que é sincronizado:

```
PlayerStatusManager → Battler Player

Heart      ← GetStatValue("heart")
Body       ← GetStatValue("body")
Mind       ← GetStatValue("mind")
Attack     ← GetAttack()
Defense    ← GetDefense()
Initiative ← GetInitiative()
```

### O que NÃO é sincronizado (propositalmente):

```
❌ HP           (só muda por dano em combate)
❌ MaxHP        (não deve mudar durante combate)
❌ Level        (não deve mudar durante combate)
❌ Dice Count   (controlado por turno)
```

---

## 🚀 Casos de Uso Comuns

### Caso 1: Usar Item Consumível
```csharp
// InventoryInputHandler.HandleItemInteraction()
Combat.RefreshCombatUI();  // ✅ Sincroniza e atualiza UI
```

### Caso 2: Após Dano em Combate
```csharp
// CombatManager.Resolve()
result.FinalTarget.ReceiveDamage(result.Damage);
View.UpdateView(Player, Enemy);  // ✅ Direto, sem sincronização (HP não muda via items)
```

### Caso 3: Após Fim do Turno
```csharp
// CombatManager.EndTurn()
Player.RecoverDices(1);
Enemy.RecoverDices(1);
View.UpdateView(Player, Enemy);  // ✅ Direto, sem sincronização
```

---

## 🔍 Debug - Como Verificar se está Funcionando

### No Console Unity:

```
[CombatManager] Sincronizado stats do Player Battler: 
  Heart=101, Body=50, Mind=75, Attack=12, Defense=8, Initiative=10
```

### Visualmente:

1. Equipar item que dá +2 Attack
2. Painel de combate deve mostrar novo valor imediatamente
3. Se não mudar → sincronização falhou (verificar logs)

---

## ⚠️ Possíveis Problemas

### Problema 1: UI não atualiza após equipar item

**Causa**: `RefreshCombatUI()` não está sendo chamado

**Solução**:
```csharp
// InventoryInputHandler.HandleItemInteraction()
Combat.RefreshCombatUI();  // ✅ Adicione esta linha se faltar
```

### Problema 2: Stats mostram valor antigo mesmo após sincronização

**Causa**: `PlayerStatusManager` não foi atualizado (ApplyStatDelta falhou)

**Verificação**:
```csharp
// No PlayerStatusManager.ApplyStatDelta()
Logger.Log($"[PlayerStatusManager] Aplicando stat delta: {statName} {value}");
// Se este log não aparecer, o método não foi chamado
```

### Problema 3: Sincronização gera erro "NullReferenceException"

**Causa**: `CombatPlayerInventory` é null

**Verificação**:
```csharp
// No CombatManager.Start()
CombatPlayerInventory = ResolveCombatPlayerInventory();
if (CombatPlayerInventory == null) 
    Debug.LogError("[CombatManager] CombatPlayerInventory não foi inicializado!");
```

---

## 💡 Recomendações

### ✅ Boas Práticas:

1. **Sempre chamar `RefreshCombatUI()` após modificar stats do jogador**
   ```csharp
   playerInventory.UseItem(item);
   Combat.RefreshCombatUI();  // ✅ Sincroniza e atualiza
   ```

2. **Não chamar `View.UpdateView()` diretamente se houve mudança de stats**
   ```csharp
   // ❌ Errado
   playerInventory.UseItem(item);
   Combat.View.UpdateView(Combat.Player, Combat.Enemy);  // Stats não sincronizados
   
   // ✅ Correto
   playerInventory.UseItem(item);
   Combat.RefreshCombatUI();  // Sincroniza primeiro
   ```

3. **Para debug, habilitar logs**
   ```csharp
   // Em PlayerStatusManager.ApplyStatDelta()
   Logger.Log($"[PlayerStatusManager] Aplicando stat delta: {statName} {value}");
   
   // Em CombatManager.SyncPlayerStatsFromStatusManager()
   Debug.Log($"[CombatManager] Sincronizado stats...");
   ```

### ❌ Evitar:

- Modificar `Player` ou `Enemy` diretamente sem chamar `RefreshCombatUI()`
- Chamar `View.UpdateView()` sem sincronizar (pode exibir dados desatualizado)
- Assumir que `PlayerStatusManager` e `Battler` estão sempre sincronizados (nunca estão, a sincronização é chamada por demanda)

---

## 🧪 Teste Rápido

### Para verificar se a correção funciona:

1. **Iniciar combate**
2. **Abrir inventário e equipar arma que +1 Attack**
3. **Verificar no painel de combate:**
   - ❌ Se Attack não aumentou → Problema!
   - ✅ Se Attack aumentou → Funcionando!

---

## 📞 Contato

Se encontrar problemas com RefreshCombatUI ou sincronização de stats:
1. Verificar os logs em Console (procurar por `[CombatManager]` e `[PlayerStatusManager]`)
2. Consultar a seção "Possíveis Problemas" acima
3. Revisar o fluxo em `ANALISE_UI_REFRESH_COMBAT.md`
