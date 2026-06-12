# 🧪 Guia de Verificação e Testes

## ⚡ Quick Check - 5 Minutos

### Passo 1: Verificar se os arquivos foram modificados

```bash
# Verificar CombatManager.cs
grep -n "SyncPlayerStatsFromStatusManager" Assets/Scripts/CombatV2/Presenter/CombatManager.cs

# Verificar PlayerStatusManager.cs
grep -n "GetAttack()" Assets/Scripts/Gameplay/Presenter/Player/PlayerStatusManager.cs
```

**Resultado esperado:**
- ✅ CombatManager.cs tem `SyncPlayerStatsFromStatusManager` 
- ✅ PlayerStatusManager.cs tem `GetAttack()`, `GetDefense()`, `GetInitiative()`

---

## 🎮 Teste em Tempo Real

### Cenário 1: Consumível com Bônus de Attack

**Preparação:**
1. Abrir Unity
2. Iniciar cena de Combate
3. Ter um item consumível com "+2 Attack" no inventário

**Procedimento:**
```
1. [ ] Anotar Attack atual no painel de combate: _______
2. [ ] Abrir inventário (UI)
3. [ ] Usar item consumível (+2 Attack)
4. [ ] Observar painel de combate
5. [ ] Verificar: Attack aumentou em 2? ______ (sim/não)
```

**Resultado esperado:**
- ✅ Attack aumenta imediatamente
- ✅ No console: `[CombatManager] Sincronizado stats...`

**Se falhar:**
```
Verificar:
1. [ ] Item tem statBonus correto? (verificar ItemSO)
2. [ ] Consumível foi usado? (verificar logs PlayerStatusManager)
3. [ ] RefreshCombatUI foi chamado? (verificar logs)
```

---

### Cenário 2: Equipar Arma com Múltiplos Bônus

**Preparação:**
1. Ter arma com "+1 Heart, +3 Defense" no inventário
2. Anotar valores atuais:
   - Heart: _______
   - Defense: _______

**Procedimento:**
```
1. [ ] Abrir inventário
2. [ ] Equipar arma
3. [ ] Observar painel de combate
4. [ ] Verificar:
   - Heart aumentou em 1? _____ (sim/não)
   - Defense aumentou em 3? _____ (sim/não)
5. [ ] Tempo para atualizar: _____ ms (deve ser < 100ms)
```

**Resultado esperado:**
- ✅ Ambos os valores aumentam imediatamente
- ✅ UI reflete múltiplos bônus ao mesmo tempo

---

### Cenário 3: Desequipar Item

**Preparação:**
1. Ter arma equipada (+1 Heart, +3 Defense)
2. Anotar valores com arma:
   - Heart: _______
   - Defense: _______

**Procedimento:**
```
1. [ ] Abrir inventário
2. [ ] Desequipar arma
3. [ ] Observar painel de combate
4. [ ] Verificar:
   - Heart diminuiu em 1? _____ (sim/não)
   - Defense diminuiu em 3? _____ (sim/não)
```

**Resultado esperado:**
- ✅ Valores revertem quando desequipa

---

## 🔍 Verificação de Logs

### No Console do Unity:

```
❌ Esperado NÃO aparecer:
[PlayerInventory] Aplicando stat bonus: ...
[PlayerStatusManager] Aplicando stat delta: ...

✅ Esperado aparecer:
[CombatManager] Sincronizado stats do Player Battler: 
  Heart=XXX, Body=XXX, Mind=XXX, Attack=XXX, Defense=XXX, Initiative=XXX
```

### Como ativar logs:

Se não aparecer nada no console:
1. Verificar se Logger está habilitado
2. Verificar Console Filter (deve estar em "All")
3. Procurar por `[CombatManager]` especificamente

---

## 🐛 Troubleshooting

### Problema 1: UI não atualiza após equipar item

```
Sintoma: Equipar arma, mas stats não mudam na UI

Checklist:
[ ] InventoryInputHandler.cs tem Combat.RefreshCombatUI()?
[ ] CombatManager.RefreshCombatUI() foi compilado sem erros?
[ ] Console mostra erro: NullReferenceException?

Solução:
1. Verificar que CombatPlayerInventory não é null
2. Verificar que PlayerStatusManager está no mesmo GameObject
3. Recompilar projeto
```

### Problema 2: Sincronização aparece com valores antigos

```
Sintoma: Log mostra "[CombatManager] Sincronizado..." mas com valores errados

Causa possível: ApplyStatDelta() não foi chamado

Verificar:
[ ] PlayerInventory.ApplyStatBonus() foi chamado?
[ ] Verificar log: "[PlayerStatusManager] Aplicando stat delta:"
[ ] ItemStatBonusParser está correto?
```

### Problema 3: Sem logs no console

```
Sintoma: Nenhum log de sincronização aparece

Possibilidades:
[ ] RefreshCombatUI() não está sendo chamado
[ ] CombatPlayerInventory é null
[ ] PlayerStatusManager não está no GameObject

Debug:
Adicionar breakpoint em CombatManager.RefreshCombatUI()
e verificar valores das variáveis
```

---

## ✅ Checklist Final

### Arquivos Modificados:
- [ ] PlayerStatusManager.cs - GetAttack(), GetDefense(), GetInitiative()
- [ ] CombatManager.cs - SyncPlayerStatsFromStatusManager()
- [ ] CombatManager.cs - RefreshCombatUI() melhorado

### Funcionalidades Testadas:
- [ ] Consumível aumenta Attack
- [ ] Consumível aumenta Heart
- [ ] Arma aumenta múltiplos stats
- [ ] Desequipar remove bônus
- [ ] Sincronização é automática
- [ ] Sem erros no console

### Performance:
- [ ] UI atualiza em < 100ms
- [ ] Sem lags ou travamentos
- [ ] Logs aparecem normalmente

### Compatibilidade:
- [ ] Gameplay normal não foi afetado
- [ ] Outros sistemas continuam funcionando
- [ ] Sem quebra de backward compatibility

---

## 📞 Se Algo Quebrou

Verificar em ordem:

1. **Erros de compilação?**
   ```
   Solução: Verificar if CombatManager.cs e PlayerStatusManager.cs estão corretos
   ```

2. **UI não atualiza?**
   ```
   Solução: Adicionar Debug.Log() em RefreshCombatUI()
   para verificar se está sendo chamado
   ```

3. **Valores incorretos?**
   ```
   Solução: Verificar se ApplyStatDelta() está atualizando corretamente
   ```

4. **NullReferenceException?**
   ```
   Solução: Verificar se CombatPlayerInventory foi inicializado
   em CombatManager.Start()
   ```

---

## 📊 Matriz de Testes

| Ação | Attack | Heart | Body | Mind | Defense | Initiative | Status |
|------|--------|-------|------|------|---------|------------|--------|
| Usar Consumível +2 Atk | ✅ | - | - | - | - | - | |
| Equipar Arma +1 Hrt +3 Def | - | ✅ | - | - | ✅ | - | |
| Desequipar | - | ✅ | - | - | ✅ | - | |
| Múltiplos itens | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Sincronização é automática | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |

---

## 🎯 Sucesso!

Se todos os testes passarem:

```
✅ Problema corrigido
✅ UI reflete mudanças imediatamente
✅ Sincronização automática funcionando
✅ Pronto para produção
```

Preencha este guia e guarde como prova de que o sistema está funcionando corretamente!
