# Implementação Prática das Soluções

## 1. Modificar DiceService.cs - RollMany

### Código Atual (Problemático)
```csharp
public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, DiceRollType rollType, int attackerLevel = 1, int defenderLevel = 1)
{
    List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, diceTypes, rollType);
    if (diceSpecs.Count == 0)
        return new List<DiceResult> { Roll(1, attackerLevel, defenderLevel, DiceStatType.Body, rollType) };

    ConsumeDicePool(battler, rollType, diceSpecs.Count);
    // ...
}
```

### Código Proposto
```csharp
public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, DiceRollType rollType, int attackerLevel = 1, int defenderLevel = 1)
{
    // ✅ Validação de entrada
    if (battler == null)
    {
        Logger.LogError("[DiceService.RollMany] Battler is null");
        return new List<DiceResult>();
    }

    if (diceTypes == null || diceTypes.Count == 0)
    {
        Logger.LogWarning($"[DiceService.RollMany] {battler.Name} called RollMany with null or empty diceTypes");
        return new List<DiceResult>();
    }

    List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, diceTypes, rollType);
    
    // ✅ Melhor tratamento quando nenhum dado pode ser criado
    if (diceSpecs.Count == 0)
    {
        Logger.LogWarning($"[DiceService.RollMany] {battler.Name} has no valid dice specs. " +
            $"Requested types: {string.Join(", ", diceTypes)}. " +
            $"Stats - Mind: {battler.Mind}, Heart: {battler.Heart}, Body: {battler.Body}");
        // Retornar lista vazia em vez de d1 falso
        return new List<DiceResult>();
    }

    ConsumeDicePool(battler, rollType, diceSpecs.Count);

    List<DiceResult> rawResults = new(diceSpecs.Count);
    for (int i = 0; i < diceSpecs.Count; i++)
    {
        DiceRollSpec spec = diceSpecs[i];
        rawResults.Add(Roll(spec.MaxValue, attackerLevel, defenderLevel, spec.StatType, spec.RollType));
    }

    return AggregateDuplicateStatResults(rawResults, attackerLevel, defenderLevel);
}
```

---

## 2. Modificar ActionResolverService.cs - Adicionar Validação de Máximo Significativo

### Adições ao ActionResolverService

Adicionar este método privado:
```csharp
/// <summary>
/// Verifica se um máximo é "significativo", ou seja, se foi um máximo real e não apenas um fallback d1.
/// Um máximo é significativo apenas se:
/// 1. O dado foi rolado
/// 2. Alcançou seu valor máximo
/// 3. O valor máximo é maior que 1 (não é um fallback d1)
/// </summary>
private bool IsSignificantMaxRoll(DiceResult dice)
{
    if (dice == null)
        return false;
    
    // MaxValue <= 1 indica um fallback d1 ou erro, não é um máximo significativo
    if (dice.MaxValue <= 1)
        return false;
    
    return dice.IsMaxRoll;
}

/// <summary>
/// Compara dois máximos. Se ambos são significativos, retorna true se defenseValue > attackValue
/// </summary>
private bool DefenseMaxRollBeatsAttackMaxRoll(DiceResult defense, DiceResult attack)
{
    if (defense == null || attack == null)
        return false;
    
    bool defenseIsSignificant = IsSignificantMaxRoll(defense);
    bool attackIsSignificant = IsSignificantMaxRoll(attack);
    
    // Se apenas defesa tem máximo significativo, defesa vence
    if (defenseIsSignificant && !attackIsSignificant)
        return true;
    
    // Se ambos têm máximo significativo, comparar valores
    if (defenseIsSignificant && attackIsSignificant)
        return defense.Value >= attack.Value;
    
    return false;
}

/// <summary>
/// Compara dois máximos. Se ambos são significativos, retorna true se attackValue > defenseValue
/// </summary>
private bool AttackMaxRollBeatsDefenseMaxRoll(DiceResult attack, DiceResult defense)
{
    if (attack == null || defense == null)
        return false;
    
    bool attackIsSignificant = IsSignificantMaxRoll(attack);
    bool defenseIsSignificant = IsSignificantMaxRoll(defense);
    
    // Se apenas ataque tem máximo significativo, ataque vence
    if (attackIsSignificant && !defenseIsSignificant)
        return true;
    
    // Se ambos têm máximo significativo, comparar valores
    if (attackIsSignificant && defenseIsSignificant)
        return attack.Value >= defense.Value;
    
    return false;
}
```

### Modificar o Método Resolve

**Seção Atual:**
```csharp
bool attackPowerMaxTriggered = attack.PowerDice != null && attack.PowerDice.IsMaxRoll;
bool defensePowerMaxTriggered = defense.PowerDice != null && defense.PowerDice.IsMaxRoll;
bool attackAccuracyMaxTriggered = attack.AccuracyDice != null && attack.AccuracyDice.IsMaxRoll;
bool defenseAccuracyMaxTriggered = defense.AccuracyDice != null && defense.AccuracyDice.IsMaxRoll;

bool ignoreAttack = (defenseAccuracyMaxTriggered && !attackAccuracyMaxTriggered) || attackAccuracy == ActionAccuracy.Missed;
bool ignoreDefense = (attackAccuracyMaxTriggered && !defenseAccuracyMaxTriggered) || defenseAccuracy == ActionAccuracy.Missed;
```

**Novo:**
```csharp
// ✅ Usar validação de máximo significativo
bool attackPowerMaxTriggered = IsSignificantMaxRoll(attack.PowerDice);
bool defensePowerMaxTriggered = IsSignificantMaxRoll(defense.PowerDice);
bool attackAccuracyMaxTriggered = IsSignificantMaxRoll(attack.AccuracyDice);
bool defenseAccuracyMaxTriggered = IsSignificantMaxRoll(defense.AccuracyDice);

// ✅ Melhorada lógica de comparação
bool ignoreAttack = (
    DefenseMaxRollBeatsAttackMaxRoll(defense.AccuracyDice, attack.AccuracyDice)
) || attackAccuracy == ActionAccuracy.Missed;

bool ignoreDefense = (
    AttackMaxRollBeatsDefenseMaxRoll(attack.AccuracyDice, defense.AccuracyDice)
) || defenseAccuracy == ActionAccuracy.Missed;
```

---

## Resumo das Mudanças

### DiceService.cs

| Linha | Tipo | Descrição |
|-------|------|-----------|
| `RollMany()` | Refactor | Adicionar validação de parâmetros |
| `RollMany()` | Fix | Retornar lista vazia em vez de d1 |
| `RollMany()` | Enhancement | Melhor logging de diagnóstico |

### ActionResolverService.cs

| Método | Tipo | Descrição |
|--------|------|-----------|
| `IsSignificantMaxRoll()` | New | Validar se máximo é real |
| `DefenseMaxRollBeatsAttackMaxRoll()` | New | Comparar máximos de defesa vs ataque |
| `AttackMaxRollBeatsDefenseMaxRoll()` | New | Comparar máximos de ataque vs defesa |
| `Resolve()` | Fix | Usar nova validação para ignoreAttack/ignoreDefense |

---

## Testes Recomendados

### Teste 1: Máximo Falso (d1)
```
Cenário: Defesa rola d1 (fallback), Ataque rola d20 com valor 15
Esperado: Ataque NOT ignorado
Resultado Atual: ❌ Ataque ignorado (bug)
Resultado com Fix: ✅ Ataque NOT ignorado
```

### Teste 2: Ambos com Máximo Significativo
```
Cenário: Defesa rola d20=20, Ataque rola d20=18
Esperado: Ataque ignorado (20 > 18)
Resultado Atual: ✅ Ataque ignorado
Resultado com Fix: ✅ Ataque ignorado (mantém funcionamento correto)
```

### Teste 3: Ataque Máximo vs Defesa Mínima
```
Cenário: Ataque rola d20=20, Defesa rola d6=2
Esperado: Defesa ignorada (20 > 2)
Resultado Atual: ❌ Pode não ignorar se defesa for d20=6 (máximo)
Resultado com Fix: ✅ Defesa ignorada corretamente
```

### Teste 4: Pool Vazio
```
Cenário: Inimigo com stats zeradas chama RollMany
Esperado: Logger warning, retorna lista vazia
Resultado Atual: ❌ Retorna d1 (causa comportamento estranho)
Resultado com Fix: ✅ Retorna lista vazia com warning
```
