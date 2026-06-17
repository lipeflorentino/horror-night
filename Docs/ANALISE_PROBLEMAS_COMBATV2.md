# Análise e Proposta de Solução - CombatV2

## Problema 1: RollMany Retorna Caso Mínimo com Stats Não Zeradas

### Análise Técnica

No método `RollMany` do `DiceService`:

```csharp
public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, DiceRollType rollType, int attackerLevel = 1, int defenderLevel = 1)
{
    List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, diceTypes, rollType);
    if (diceSpecs.Count == 0)
        return new List<DiceResult> { Roll(1, attackerLevel, defenderLevel, DiceStatType.Body, rollType) };
    // ...
}
```

**Quando `diceSpecs.Count == 0` ocorre:**

1. **`diceTypes` é nulo** - Chamada sem especificação de tipos de dados
2. **`diceTypes` é vazio** - Lista vazia passada como parâmetro
3. **`battler` é nulo** - Referência inválida
4. **Todas as stats solicitadas estão <= 0** - Mesmo que uma stat exista, se for <= 0, é skipped:
   ```csharp
   if (totalValue <= 0 || diceCount <= 0)
       continue;
   ```

### O Problema Real

Quando o fallback `Roll(1, ...)` é acionado:
- **MaxValue = 1** (1d1)
- **Value = 1** (sempre será 1 em um d1)
- **IsMaxRoll = true** (porque Value >= MaxValue → 1 >= 1)

Isso faz o sistema acreditar que houve um "máximo" quando na verdade foi apenas um fallback de erro.

### Cenários Não Óbvios

- O inimigo pode estar com pool de dados zerado (CurrentPowerDices = 0, CurrentAccuracyDices = 0)
- A ação solicitada requer tipos de dados que o personagem não possui
- `ConsumeDicePool` pode deixar o pool vazio entre turnos

---

## Problema 2: IsMaxRoll Anula Ações com Valores Muito Baixos

### Análise Técnica

Em `DiceResult.cs`:
```csharp
public class DiceResult
{
    public int Value;
    public int MaxValue;
    public bool IsMaxRoll => Value >= MaxValue;  // ❌ PROBLEMA
    // ...
}
```

Em `ActionResolverService.cs`:
```csharp
bool defenseAccuracyMaxTriggered = defense.AccuracyDice != null && defense.AccuracyDice.IsMaxRoll;
bool attackAccuracyMaxTriggered = attack.AccuracyDice != null && attack.AccuracyDice.IsMaxRoll;

bool ignoreAttack = (defenseAccuracyMaxTriggered && !attackAccuracyMaxTriggered) || attackAccuracy == ActionAccuracy.Missed;
```

### O Cenário Problemático

**Exemplo:**
- Defesa rola **1d1** (MaxValue=1, Value=1) → IsMaxRoll = **true** ✓
- Ataque rola **1d20** e tira **15** (MaxValue=20, Value=15) → IsMaxRoll = **false** ✗
- **Resultado:** Ataque é ignorado, mesmo sendo uma rolagem muito melhor!

### Por Que Isso Ocorre

A condição `ignoreAttack = (defenseAccuracyMaxTriggered && !attackAccuracyMaxTriggered)` não leva em conta:
- Se o MaxValue da defesa é significativo
- Se o Value da defesa foi realmente melhor que o ataque
- Se ambos rolaram máximos, quem deveria prevalecer?

---

## Soluções Propostas

### Solução 1: Evitar Fallback d1 e Melhorar BuildDiceRollSpecs

**Objetivo:** Não retornar um fallback d1 que causa IsMaxRoll falso positivo.

**Opções:**

#### Opção 1A: Adicionar um Erro de Validação
```csharp
public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, DiceRollType rollType, ...)
{
    List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, diceTypes, rollType);
    
    if (diceSpecs.Count == 0)
    {
        Logger.LogError($"[DiceService] No valid dice specs for {battler?.Name ?? "null"}. " +
            $"diceTypes: {(diceTypes?.Count ?? 0)}, stats - Mind: {battler?.Mind}, " +
            $"Heart: {battler?.Heart}, Body: {battler?.Body}");
        
        // Retornar resultado neutro em vez de d1
        return new List<DiceResult> { 
            new DiceResult(0, DiceTier.Low, 1, DiceStatType.Body, rollType) 
        };
    }
    // ...
}
```

#### Opção 1B: Verificar Pool de Dados Antes de Chamar RollMany
Na camada que chama `RollMany`, validar:
```csharp
if (battler.CurrentPowerDices <= 0 && rollType == DiceRollType.Power)
{
    Logger.Log($"{battler.Name} has no Power Dices available");
    return GetDefaultEmptyResult();
}
```

#### Opção 1C (RECOMENDADA): Melhorar a Verificação de diceTypes
```csharp
public List<DiceResult> RollMany(Battler battler, IReadOnlyList<DiceStatType> diceTypes, ...)
{
    // Validação robusta
    if (battler == null || diceTypes == null || diceTypes.Count == 0)
    {
        Logger.LogError($"[RollMany] Invalid parameters: battler={battler}, diceTypes count={diceTypes?.Count}");
        return new List<DiceResult>();
    }
    
    List<DiceRollSpec> diceSpecs = BuildDiceRollSpecs(battler, diceTypes, rollType);
    
    if (diceSpecs.Count == 0)
    {
        Logger.LogError($"[RollMany] No valid dice built for {battler.Name} from {diceTypes.Count} requested types");
        return new List<DiceResult>();
    }
    
    // ... resto do código
}
```

---

### Solução 2: Validar IsMaxRoll com Comparação Entre Ações

**Objetivo:** IsMaxRoll só deve anular se for genuinamente superior.

#### Opção 2A: Comparar Valores Absolutos
```csharp
bool ignoreAttack = (
    defenseAccuracyMaxTriggered && 
    !attackAccuracyMaxTriggered &&
    defense.AccuracyDice.Value > attack.AccuracyDice.Value  // ✅ Adicionar esta verificação
) || attackAccuracy == ActionAccuracy.Missed;
```

#### Opção 2B (RECOMENDADA): Adicionar Validação de Significância

Criar um método que valida se um máximo é "real":
```csharp
private bool IsSignificantMaxRoll(DiceResult dice)
{
    if (dice == null || dice.MaxValue <= 1)
        return false;
    
    return dice.IsMaxRoll && dice.MaxValue > 1;  // Máximo só é válido se MaxValue > 1
}
```

Então usar:
```csharp
bool defenseAccuracyMaxTriggered = IsSignificantMaxRoll(defense.AccuracyDice);
bool attackAccuracyMaxTriggered = IsSignificantMaxRoll(attack.AccuracyDice);

// Se ambos rolaram máximo significativo, compara valores
bool ignoreAttack = (
    defenseAccuracyMaxTriggered && 
    (!attackAccuracyMaxTriggered || defense.AccuracyDice.Value <= attack.AccuracyDice.Value)
) || attackAccuracy == ActionAccuracy.Missed;

bool ignoreDefense = (
    attackAccuracyMaxTriggered && 
    (!defenseAccuracyMaxTriggered || attack.AccuracyDice.Value <= defense.AccuracyDice.Value)
) || defenseAccuracy == ActionAccuracy.Missed;
```

#### Opção 2C: Aumentar MaxValue Mínimo
```csharp
public DiceResult Roll(int maxValue, int attackerLevel, int defenderLevel, ...)
{
    int safeMaxValue = Math.Max(6, maxValue);  // Aumentar de 1 para 6 (d6 mínimo)
    // ...
}
```

---

## Recomendação de Implementação

### **Prioritário - Solução 1C + Solução 2B**

Esta combinação fornece:

1. **Robustez na entrada** - Validar parâmetros logo no início
2. **Fallback explícito** - Retornar lista vazia em vez de d1 falso
3. **Validação de máximo significativo** - Apenas d's maiores que 1 podem anular ações
4. **Comparação de valores** - Se ambos forem máximos, vence o maior

### Mudanças Necessárias:

**Arquivo 1:** `DiceService.cs` - Método `RollMany`
- Adicionar validação de parâmetros
- Melhorar fallback

**Arquivo 2:** `ActionResolverService.cs` - Método `Resolve`
- Adicionar `IsSignificantMaxRoll()`
- Atualizar lógica de `ignoreAttack` e `ignoreDefense`

---

## Teste Sugerido

```csharp
// Cenário que deveria falhar com código atual:
var defenseRoll = new DiceResult(1, DiceTier.Low, 1, DiceStatType.Body, DiceRollType.Accuracy);  // d1
var attackRoll = new DiceResult(15, DiceTier.High, 20, DiceStatType.Mind, DiceRollType.Accuracy);  // d20

// Com IsSignificantMaxRoll():
IsSignificantMaxRoll(defenseRoll);  // false (MaxValue = 1)
IsSignificantMaxRoll(attackRoll);   // false (Value=15, MaxValue=20, não é máximo)

// Resultado esperado: Ataque NÃO é ignorado
```
