# 📊 ANÁLISE COMPLETA: Sistema de Perks Horror Night

## 🎯 Executivo

O sistema de Perks do Combat V2 tem uma **arquitetura sólida mas incompleta**. O evento `OnPerkTriggered` está definido mas **nunca é disparado** em nenhum lugar do código. Além disso, faltam 2 tipos de condição e 1 tipo de modificador necessários para suportar todos os perks do CSV.

**Solução proposta**: Criar `PerkTriggerEvaluator` como classe centralizada que coordena triggers, usando Factory Pattern para extensibilidade ilimitada.

---

## 📋 O QUE CADA PARÂMETRO DO CSV SIGNIFICA

### Colunas Gerais (Metadados)
| Coluna | Tipo | Descrição | Exemplo |
|--------|------|-----------|---------|
| **Id** | `string` | Identificador único, imutável, usado em código | `mind_min_accuracy_30` |
| **Name** | `string` | Nome exibível na UI | `Precisão Analítica` |
| **Description** | `string` | Tooltip ou descrição longa | `Dados de accuracy de mind nunca rolam abaixo de 30%.` |
| **IconName** | `string` | Nome do sprite em Resources/Icons | `analytic_precision` |
| **Tags** | `string` | Categorias separadas por `;` | `identity;mind` ou `permanent;power;attack` |

### Colunas de Tempo (Quando Ativo)
| Coluna | Tipo | Descrição | Exemplo |
|--------|------|-----------|---------|
| **Scope** | `bool` | [LEGADO?] Parece indicar ativo/inativo | `true` |
| **DurationTurns** | `int` | `-1` = infinito, `0+` = número de turnos | `-1` (permanent) ou `3` (3 turns) |
| **StackMode** | `enum` | Como lidar com acúmulos: `AddStack`, `Replace`, `RefreshDuration` | `RefreshDuration` |
| **MaxStacks** | `int` | Quantidade máxima de acúmulos | `1` ou `2` |

### Colunas de Acionamento (QUANDO Disparar)
| Coluna | Tipo | Descrição | Valores |
|--------|------|-----------|---------|
| **Trigger** | `PerkTrigger` | Momento de disparo | `BeforeRoll`, `PowerMultiplier`, `AfterResolve` |
| **OwnerRole** | `BattlerStateRole` | Quem é o dono? | `OwnerAsActor`, `OwnerAsAttacker`, `OwnerAsDefender`, `OwnerAsTarget` |

### Colunas de Filtros (ONDE Aplicar)
| Coluna | Tipo | Descrição | Valores |
|--------|------|-----------|---------|
| **ActionFilter** | `ActionType` | Qual tipo de ação? | `Any`, `Attack`, `Defense` |
| **RollFilter** | `DiceRollType` | Qual tipo de dado? | `Any`, `Power`, `Accuracy` |
| **StatFilter** | `DiceStatType` | Qual stat? | `Any`, `Mind`, `Heart`, `Body` |
| **TierFilter** | `DiceTier` | Qual tier do dado? | `Any`, `Low`, `Medium`, `High` |

### Colunas de Condição (SE Satisfeita)
| Coluna | Tipo | Descrição | Exemplos |
|--------|------|-----------|----------|
| **ConditionKey** | `PerkConditionKey` | Tipo de verificação | `Always`, `RollValueEquals`, `RollTierEquals`, `RollSumEquals` |
| **ConditionValue** | `string` | Valor esperado | `6`, `7`, `High`, `RollSumEqualsAttackersRollSum` |

### Colunas de Efeito (O QUÊ Faz)
| Coluna | Tipo | Descrição | Exemplos |
|--------|------|-----------|----------|
| **ModifierTarget** | `PerkModifierTarget` | O que é modificado | `MinRollPercent`, `ExtraDice`, `DamagePercent`, `MomentumPoints` |
| **Operation** | `PerkOperation` | Como modificar | `Add` (+valor), `Multiply` (×valor), `Override` (=valor) |
| **Value** | `float` | Valor do modificador | `1`, `0.30`, `2`, `-0.50` |

---

## 🔍 Análise de Cada Perk Existente

### 1️⃣ **mind_min_accuracy_30** (Precisão Analítica)
```
Trigger:      BeforeRoll
Condition:    Always
Filter:       Accuracy (RollFilter), Mind (StatFilter)
Effect:       MinRollPercent = 0.30 (Override)
Descrição:    Dados de accuracy de mind NUNCA rolam abaixo de 30%
Fluxo:        Roll (Mind Accuracy) → Check perk → Apply 30% min
Status:       ✅ Implementado
```

### 2️⃣ **heart_extra_dice** (Instinto Vivo)
```
Trigger:      BeforeRoll
Condition:    Always
Filter:       Heart (StatFilter)
Effect:       ExtraDice = 1 (Add)
Descrição:    Ao rolar heart adiciona 1 dado extra
Fluxo:        Count dice para Heart → +1 (perk) → Roll
Status:       ✅ Implementado
```

### 3️⃣ **body_high_power_x2** (Força Brutal High)
```
Trigger:      PowerMultiplier
Condition:    Always
Filter:       Power (RollFilter), Body (StatFilter), High (TierFilter)
Effect:       PowerMultiplier = 2 (Override)
Descrição:    High power de body vale x2
Fluxo:        Resolve Power High (Body) → Multiply x2
Status:       ✅ Implementado (provavelmente)
```

### 4️⃣ **body_medium_power_x1_5** (Força Brutal Medium)
```
Trigger:      PowerMultiplier
Condition:    Always
Filter:       Power (RollFilter), Body (StatFilter), Medium (TierFilter)
Effect:       PowerMultiplier = 1.5 (Override)
Descrição:    Medium power de body vale x1.5
Status:       ✅ Implementado (provavelmente)
```

### 5️⃣ **six_feet_under** (Six Feet Under)
```
Trigger:      AfterResolve
Condition:    RollValueEquals = "6" (Power dado específico tirou 6)
Filter:       Power (RollFilter), Any (StatFilter), Attack (ActionFilter)
Effect:       DamagePercent = 0.30 (Multiply → +30% damage)
Descrição:    Se o dado de power tirar 6 ganha 30% de bônus de dano
Fluxo:        Power roll = 6 → damage *= 1.30
Status:       ❌ INCOMPLETO - OnPerkTriggered não é disparado
```

### 6️⃣ **lucky_number** (Número da Sorte)
```
Trigger:      AfterResolve
Condition:    RollSumEquals = "7" (Soma dos dados = 7) ⚠️ FALTA IMPLEMENTAÇÃO
Filter:       Any (RollFilter), Attack (ActionFilter)
Effect:       MomentumPoints = 1 (Add) ⚠️ FALTA NO ENUM
Descrição:    Se a soma dos dados for 7 ganha 1 ponto de momentum
Fluxo:        Soma dados Attack = 7 → momentum += 1
Status:       ❌ CRÍTICO - Faltam 2 recursos
```

### 7️⃣ **equal_share** (Divisão Igualitária)
```
Trigger:      AfterResolve
Condition:    RollSumEqualsAttackersRollSum ⚠️ FALTA IMPLEMENTAÇÃO
Filter:       Any (RollFilter), Defense (ActionFilter)
Effect:       DamagePercent = 0.50 (Multiply → -50% damage)
Descrição:    Se soma defensor = soma atacante → dano dividido
Fluxo:        Defense sum = Attacker sum → damage *= 0.50
Status:       ❌ CRÍTICO - Falta implementação de condição
```

---

## 🏗️ Problemas Identificados

### 🔴 CRÍTICO

| # | Problema | Severidade | Impacto |
|---|----------|-----------|--------|
| 1 | `OnPerkTriggered` nunca é disparado | **CRÍTICO** | Feedback visual/auditivo não funciona |
| 2 | `RollSumEquals` não existe em `PerkConditionKey` | **CRÍTICO** | lucky_number não funciona |
| 3 | `RollSumEqualsAttackersRollSum` não existe | **CRÍTICO** | equal_share não funciona |
| 4 | `MomentumPoints` não existe em `PerkModifierTarget` | **CRÍTICO** | lucky_number não pode aplicar efeito |

### 🟡 IMPORTANTES

| # | Problema | Severidade | Solução |
|---|----------|-----------|---------|
| 5 | Sem Factory Pattern para condições | **ALTA** | Difícil expandir novos perks |
| 6 | Lógica de trigger espalhada | **ALTA** | Difícil debugar e manter |
| 7 | Event payload insuficiente | **MÉDIA** | Listeners não têm contexto completo |
| 8 | PerkRule.ConditionValue é string | **MÉDIA** | Type-unsafe, sem validação |

---

## ✅ Solução Proposta: Arquitetura Escalável

### Componentes Criados

#### 1. **IPerkCondition** (Interface)
```csharp
public interface IPerkCondition
{
    bool Evaluate(object context, string conditionValue);
    PerkConditionKey ConditionType { get; }
}
```
**Propósito**: Permitir novas condições sem modificar código existente.

#### 2. **PerkConditionFactory** (Factory Pattern)
```csharp
public static class PerkConditionFactory
{
    private static Dictionary<PerkConditionKey, IPerkCondition> registry;
    
    public static void Register(PerkConditionKey key, IPerkCondition condition);
    public static bool Evaluate(PerkConditionKey key, object context, string value);
}
```
**Propósito**: Centralizar criação e validação de condições. Extensível sem modificação.

#### 3. **PerkTriggerEvaluator** (Classe Principal)
```csharp
public class PerkTriggerEvaluator
{
    public event Action<PerkTriggeredEvent> OnPerkTriggered;
    
    public void EvaluateRollTriggers(...);
    public void EvaluateDiceTriggers(...);
    private bool ValidateCondition(...);
}
```
**Propósito**: Centralizar toda lógica de trigger. Single Responsibility Principle.

#### 4. **PerkTriggeredEvent** (Struct)
```csharp
public struct PerkTriggeredEvent
{
    public string PerkId;
    public string PerkName;
    public Battler Owner;
    public PerkTrigger Trigger;
    public PerkModifierTarget ModifierTarget;
    public float AppliedValue;
    public int StacksApplied;
    public object FullContext;  // CombatRollContext ou CombatActionContext
    public float TriggerTime;
}
```
**Propósito**: Payload completo com todas as informações necessárias.

#### 5. **Enums Atualizados**
- `PerkConditionKey`: +2 novos valores
- `PerkModifierTarget`: +1 novo valor

---

## 🔄 Fluxo Novo vs Antigo

### ANTES ❌
```
1. DiceService.RollDice()
2. PerkService.GetExtraDiceCount()
   ├─ ApplyRollModifiers() 
   │  └─ rule.MatchesRoll() [verifica mas não notifica]
   └─ Retorna valor
3. PerkActivationFeedback aguarda OnPerkTriggered forever...
```

### DEPOIS ✅
```
1. DiceService.RollDice()
2. PerkService.GetExtraDiceCount()
   ├─ triggerEvaluator.EvaluateRollTriggers(actor, context, BeforeRoll)
   │  ├─ ValidateCondition() [com Factory Pattern]
   │  ├─ OnPerkTriggered?.Invoke(PerkTriggeredEvent) ← NOVO!
   │  └─ Listeners recebem evento completo
   ├─ ApplyRollModifiers() [calcula modificadores]
   └─ Retorna valor
3. PerkActivationFeedback.HandlePerkTriggered(evt) ← FUNCIONA!
   └─ Mostra popup com icon, som, etc
```

---

## 📈 Escalabilidade

### Adicionar Novo Perk: Antes vs Depois

#### ❌ ANTES (Impossível Sem Refatorar PerkService)
```
CSV:
  ConditionKey: DamageHigherThan100
  
Código: 
  Modificar PerkRule.MatchesDice()
  Modificar PerkEnums.cs
  Adicionar lógica em ApplyDiceModifiers()
```

#### ✅ DEPOIS (3 Passos Simples)
```
1. Criar classe de condição:
   public class RollSumGreaterThanCondition : IPerkCondition { }

2. Registrar no factory:
   PerkConditionFactory.Register(PerkConditionKey.RollSumGreaterThan, 
                                  new RollSumGreaterThanCondition());

3. Adicionar ao CSV e enum - PRONTO!
```

**NÃO precisa modificar:**
- PerkService.cs
- DiceService.cs
- PerkTriggerEvaluator.cs

---

## 🎮 Caso de Uso: Lucky Number

### Fluxo Completo Com Nova Arquitetura

```
Combate: Player ataca com dois dados (Power)

1. DiceService.RollDice()
   └─ Roll Power dice: [3, 4]

2. DiceService.AggregateDuplicates()
   └─ Soma: 3 + 4 = 7

3. PerkService.ApplyDamageModifiers()
   ├─ triggerEvaluator.EvaluateDiceTriggers(player, context, dice, AfterResolve, [dice1, dice2])
   │
   ├─ Para cada perk em player.Perks:
   │  └─ lucky_number encontrado
   │
   ├─ ValidateCondition(lucky_number.rule, [dice1, dice2])
   │  ├─ rule.ConditionKey = RollSumEquals
   │  ├─ rule.ConditionValue = "7"
   │  ├─ Cria DiceRollSumContext { TotalSum = 7, Dices = [...] }
   │  ├─ PerkConditionFactory.Evaluate(RollSumEquals, context, "7")
   │  │  └─ RollSumEqualsCondition.Evaluate()
   │  │     └─ return context.TotalSum == 7 ✅ TRUE!
   │  └─ return true
   │
   ├─ NotifyPerkTriggered() chamado:
   │  ├─ PerkTriggeredEvent criado:
   │  │  {
   │  │    PerkId: "lucky_number",
   │  │    PerkName: "Número da Sorte",
   │  │    Owner: player,
   │  │    Trigger: AfterResolve,
   │  │    ModifierTarget: MomentumPoints, ← Novo!
   │  │    AppliedValue: 1,
   │  │    StacksApplied: 1,
   │  │    FullContext: context,
   │  │    TriggerTime: Time.time
   │  │  }
   │  │
   │  └─ OnPerkTriggered?.Invoke(evt) ✅ DISPARADO!
   │
   ├─ ApplyDiceModifiers()
   │  └─ Aplica modificadores normalmente
   │
   └─ Retorna dano calculado

4. PerkActivationFeedback.HandlePerkTriggered(evt)
   ├─ Mostra popup "Número da Sorte!"
   ├─ Toca som de perk acionado
   ├─ Adiciona +1 momentum ao player
   └─ Log completo com contexto

Resultado: Lucky Number funciona perfeitamente! 🎉
```

---

## 🧪 Teste de Validação

### Perks Que Deveriam Funcionar (Com Nova Arquitetura)

```
✅ identity;mind perks      → Sempre ativos, Always condition
✅ identity;heart perks     → Sempre ativos, Always condition
✅ identity;body perks      → Sempre ativos, Always condition
✅ six_feet_under          → RollValueEquals "6" + AfterResolve
✅ lucky_number            → RollSumEquals "7" + AfterResolve
✅ equal_share             → RollSumEqualsAttackersRollSum + AfterResolve
✅ Novos perks (ilimitado) → Factory Pattern extensível
```

---

## 📊 Comparativo: Arquitetura Antiga vs Nova

| Aspecto | Antiga | Nova |
|--------|--------|------|
| **Localização de Lógica de Trigger** | PerkService + PerkRule.MatchesDice | PerkTriggerEvaluator centralizado |
| **Disparo de Eventos** | Manual (nunca feito) | Automático em PerkTriggerEvaluator |
| **Validação de Condições** | Dispersa em PerkRule | Factory Pattern unificado |
| **Adição de Nova Condição** | Modifica PerkRule + enum | Apenas 2 linhas em Factory |
| **Tipos de Condição Suportados** | 3 | ∞ (extensível) |
| **Payload do Evento** | (Battler, string, PerkTrigger) | PerkTriggeredEvent completo |
| **Context Disponível** | Nenhum | Completo (CombatRollContext, etc) |
| **Testabilidade** | Difícil | PerkTriggerEvaluator isolável |
| **Debugging** | Sem logs centralizados | Logs automáticos |

---

## 🚀 Próximos Passos (Checklist)

- [x] Criar `IPerkCondition` interface
- [x] Criar `PerkConditionFactory` com padrão Factory
- [x] Criar classes de condição: Always, RollValueEquals, RollTierEquals, RollSumEquals, RollSumEqualsAttackersRollSum
- [x] Criar `PerkTriggeredEvent` struct
- [x] Criar `PerkTriggerEvaluator` classe principal
- [x] Atualizar `PerkConditionKey` enum (+2)
- [x] Atualizar `PerkModifierTarget` enum (+1)
- [ ] **Integrar PerkTriggerEvaluator em PerkService**
- [ ] **Chamar EvaluateRollTriggers em GetExtraDiceCount**
- [ ] **Chamar EvaluateRollTriggers em GetMinimumRollValue**
- [ ] **Chamar EvaluateDiceTriggers em GetPowerMultiplier**
- [ ] **Chamar EvaluateDiceTriggers em ApplyDamageModifiers**
- [ ] **Testar lucky_number perk**
- [ ] **Testar equal_share perk**
- [ ] **Testar six_feet_under perk**
- [ ] **Validar PerkActivationFeedback recebe eventos**

---

## 📖 Documentação Gerada

1. **INTEGRACAO_PERKTRIGGER.md** - Guia passo-a-passo de integração
2. **Arquivo atual** - Análise e arquitetura
3. **IPerkCondition.cs** - Interface base
4. **PerkConditionFactory.cs** - Factory com condições padrão
5. **PerkTriggeredEvent.cs** - Struct de evento
6. **PerkTriggerEvaluator.cs** - Lógica centralizada
7. **PerkEnums.cs** - Enums atualizados
