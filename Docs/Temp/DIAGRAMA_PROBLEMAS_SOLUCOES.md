# Diagrama de Fluxo - Problemas e Soluções

## PROBLEMA 1: RollMany com diceSpecs Vazio

```
┌─────────────────────────────────────────────────────────────┐
│ RollMany(battler, diceTypes, rollType)                     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ├─ Validação Entrada (❌ FALTA NO CÓDIGO)
                 │
                 v
    ┌────────────────────────────┐
    │ BuildDiceRollSpecs()       │
    │                            │
    │ For each diceType:         │
    │  ├─ Get stat value         │
    │  ├─ Calculate diceCount    │
    │  └─ if (stat <= 0 or       │
    │       diceCount <= 0)      │
    │       SKIP → continue      │
    └──────┬─────────────────────┘
           │
           v
    ┌──────────────────────┐
    │ diceSpecs.Count == 0?│
    └──────┬───────────────┘
           │
     ┌─────┴─────┐
     │           │
    NÃO         SIM
     │           │
     │           v
     │      ┌─────────────────────────────┐
     │      │ ❌ PROBLEMA: Retorna d1     │
     │      │                             │
     │      │ Roll(1, attacker, defender, │
     │      │      DiceStatType.Body,     │
     │      │      rollType)              │
     │      │                             │
     │      │ MaxValue = 1                │
     │      │ Value = 1 (sempre)          │
     │      │ IsMaxRoll = true (1 >= 1)   │
     │      └────────────┬────────────────┘
     │                   │
     │                   v (RETORNA d1 FALSO)
     │
     v
┌──────────────────────────────────┐
│ Continua normalmente com dados   │
│ válidos                          │
└──────────────────────────────────┘


❌ CENÁRIOS ONDE OCORRE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. diceTypes = null             → BuildDiceRollSpecs retorna lista vazia
2. diceTypes.Count = 0          → BuildDiceRollSpecs retorna lista vazia
3. Todas as stats = 0           → Todas são skipped
4. battler = null               → Retorna 0 para cada stat, skipped
5. Pool de dados vazio          → (se a lógica depende de pool)
```

---

## PROBLEMA 2: IsMaxRoll Anula Ações Mesmo com Valores Baixos

```
┌─────────────────────────────────────────────────────────────┐
│ ActionResolverService.Resolve(attack, defense, ...)        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 v
    ┌────────────────────────────────────────┐
    │ Cria flags de máximo:                  │
    │                                        │
    │ defenseAccuracyMaxTriggered =          │
    │   defense.AccuracyDice.IsMaxRoll       │
    │                                        │
    │ ❌ PROBLEMA: IsMaxRoll = (Value >= MaxValue)
    │    Sem verificação de significância   │
    │    sem verificação de comparação      │
    └────────────┬───────────────────────────┘
                 │
                 v
    ┌─────────────────────────────────────────────┐
    │ Calcula ignoreAttack:                       │
    │                                             │
    │ ignoreAttack =                              │
    │   (defenseAccuracyMaxTriggered &&           │
    │    !attackAccuracyMaxTriggered)             │
    │   || ...                                    │
    │                                             │
    │ ❌ FALHA: Sem comparar valores!             │
    └────────────┬────────────────────────────────┘
                 │
                 v (IGNORA O ATAQUE)


EXEMPLO DO BUG:
━━━━━━━━━━━━━━━
Defense: d1 = 1   (MaxValue=1, Value=1)
  ├─ IsMaxRoll = (1 >= 1) = TRUE ✓
  └─ Flag = TRUE

Attack: d20 = 15  (MaxValue=20, Value=15)
  ├─ IsMaxRoll = (15 >= 20) = FALSE ✗
  └─ Flag = FALSE

Resultado:
  ├─ defenseAccuracyMaxTriggered = TRUE
  ├─ attackAccuracyMaxTriggered = FALSE
  └─ ignoreAttack = TRUE ❌ BUG!

Deveria ser: FALSE (15 no d20 é pior que 1 no d1)
```

---

## SOLUÇÃO 1: Validação Robusta em RollMany

```
┌──────────────────────────────────────────┐
│ RollMany(battler, diceTypes, rollType)  │
└────────────┬─────────────────────────────┘
             │
             v
    ┌─────────────────────────┐
    │ ✅ Valida Entrada:      │
    │                         │
    │ if (battler == null)    │
    │   ├─ LogError           │
    │   └─ return []          │
    │                         │
    │ if (diceTypes == null   │
    │     || count == 0)      │
    │   ├─ LogWarning         │
    │   └─ return []          │
    └────────────┬────────────┘
                 │
                 v
    ┌────────────────────────────┐
    │ BuildDiceRollSpecs()       │
    └────────────┬───────────────┘
                 │
                 v
    ┌──────────────────────┐
    │ diceSpecs.Count == 0?│
    └──────┬───────────────┘
           │
     ┌─────┴─────┐
     │           │
    NÃO         SIM
     │           │
     │           v
     │      ┌──────────────────────────┐
     │      │ ✅ FIX: Retorna vazio    │
     │      │                          │
     │      │ LogWarning com contexto  │
     │      │ return []                │
     │      │                          │
     │      │ (Sem d1 falso!)          │
     │      └──────────┬───────────────┘
     │                 │
     v                 v
    Continua normalmente (CORRETO)
```

---

## SOLUÇÃO 2: Validação de Máximo Significativo

```
┌──────────────────────────────────────────────────┐
│ IsSignificantMaxRoll(dice)                       │
├──────────────────────────────────────────────────┤
│                                                  │
│ if (dice == null) → return FALSE                 │
│                                                  │
│ if (dice.MaxValue <= 1) → return FALSE ✅        │
│                          (d1 ou menor = falso)   │
│                                                  │
│ return dice.IsMaxRoll (genuíno)                  │
│                                                  │
└──────────────────────────────────────────────────┘


┌───────────────────────────────────────────────────────────────┐
│ DefenseMaxRollBeatsAttackMaxRoll(defense, attack)            │
├───────────────────────────────────────────────────────────────┤
│                                                               │
│ defSig = IsSignificantMaxRoll(defense)                       │
│ atkSig = IsSignificantMaxRoll(attack)                        │
│                                                               │
│ Cenários:                                                     │
│                                                               │
│ 1. defSig=T, atkSig=F → return TRUE (defesa vence)           │
│ 2. defSig=F, atkSig=T → return FALSE (ataque vence)          │
│ 3. defSig=T, atkSig=T → return (def.Value >= atk.Value)      │
│ 4. defSig=F, atkSig=F → return FALSE (ninguém tem máximo)    │
│                                                               │
└───────────────────────────────────────────────────────────────┘


APLICAÇÃO NO RESOLVE():
━━━━━━━━━━━━━━━━━━━━━━
Substituir:
  ignoreAttack = (defenseAccuracyMaxTriggered && 
                  !attackAccuracyMaxTriggered)

Por:
  ignoreAttack = DefenseMaxRollBeatsAttackMaxRoll(
                   defense.AccuracyDice,
                   attack.AccuracyDice)
```

---

## COMPARAÇÃO: Antes vs Depois

### Cenário 1: Defesa com d1 (Fallback) vs Ataque com d20

```
ANTES (❌ BUG):
═════════════════════════════════════════════════════════════
Defense Accuracy Dice: Value=1, MaxValue=1, IsMaxRoll=TRUE
Attack Accuracy Dice:  Value=15, MaxValue=20, IsMaxRoll=FALSE

ignoreAttack = (TRUE && !FALSE) = TRUE
Result: ❌ ATAQUE IGNORADO (INCORRETO!)

DEPOIS (✅ CORRETO):
═════════════════════════════════════════════════════════════
defenseAccuracyMaxTriggered = IsSignificantMaxRoll(d1)
  → MaxValue=1 → return FALSE

attackAccuracyMaxTriggered = IsSignificantMaxRoll(d20)
  → MaxValue=20, Value=15 → IsMaxRoll=FALSE → return FALSE

ignoreAttack = DefenseMaxRollBeatsAttackMaxRoll(d1, d20)
  → defSig=FALSE, atkSig=FALSE → return FALSE

Result: ✅ ATAQUE NÃO IGNORADO (CORRETO!)
```

### Cenário 2: Defesa com d20=20 vs Ataque com d20=18

```
ANTES (✅ CORRETO ACIDENTALMENTE):
═════════════════════════════════════════════════════════════
Defense Accuracy Dice: Value=20, MaxValue=20, IsMaxRoll=TRUE
Attack Accuracy Dice:  Value=18, MaxValue=20, IsMaxRoll=FALSE

ignoreAttack = (TRUE && !FALSE) = TRUE
Result: ✅ ATAQUE IGNORADO (CORRETO)

DEPOIS (✅ CORRETO):
═════════════════════════════════════════════════════════════
defenseAccuracyMaxTriggered = IsSignificantMaxRoll(d20=20)
  → MaxValue=20 > 1, IsMaxRoll=TRUE → return TRUE

attackAccuracyMaxTriggered = IsSignificantMaxRoll(d20=18)
  → MaxValue=20 > 1, IsMaxRoll=FALSE → return FALSE

ignoreAttack = DefenseMaxRollBeatsAttackMaxRoll(d20=20, d20=18)
  → defSig=TRUE, atkSig=FALSE → return TRUE

Result: ✅ ATAQUE IGNORADO (CORRETO!)
```

### Cenário 3: Defesa com d20=20 vs Ataque com d20=20 (Ambos Máximos)

```
ANTES (✅ CORRETO):
═════════════════════════════════════════════════════════════
Defense Accuracy Dice: Value=20, MaxValue=20, IsMaxRoll=TRUE
Attack Accuracy Dice:  Value=20, MaxValue=20, IsMaxRoll=TRUE

ignoreAttack = (TRUE && !TRUE) = FALSE
Result: ✅ ATAQUE NÃO IGNORADO (CORRETO - Empate)

DEPOIS (✅ CORRETO):
═════════════════════════════════════════════════════════════
defenseAccuracyMaxTriggered = IsSignificantMaxRoll(d20=20)
  → MaxValue=20 > 1, IsMaxRoll=TRUE → return TRUE

attackAccuracyMaxTriggered = IsSignificantMaxRoll(d20=20)
  → MaxValue=20 > 1, IsMaxRoll=TRUE → return TRUE

ignoreAttack = DefenseMaxRollBeatsAttackMaxRoll(d20=20, d20=20)
  → defSig=TRUE, atkSig=TRUE → return (20 >= 20) = TRUE

Result: ✅ ATAQUE IGNORADO (CORRETO - Defesa iguala)
```

---

## Status de Implementação

```
┌─────────────────────────────────────────┐
│ SOLUÇÃO 1: Validação RollMany           │
├─────────────────────────────────────────┤
│ Arquivo: DiceService.cs                 │
│ Impacto: ALTO (previne root cause)      │
│ Complexidade: BAIXA                     │
│ Priority: 🔴 CRÍTICA                    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ SOLUÇÃO 2: Máximo Significativo         │
├─────────────────────────────────────────┤
│ Arquivo: ActionResolverService.cs       │
│ Impacto: MUITO ALTO (fix do bug)        │
│ Complexidade: BAIXA                     │
│ Priority: 🔴 CRÍTICA                    │
└─────────────────────────────────────────┘
```
