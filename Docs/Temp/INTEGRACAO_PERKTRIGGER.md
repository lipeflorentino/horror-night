# Guia de Integração: PerkTriggerEvaluator com PerkService

## 📋 Resumo da Refatoração

O novo sistema centraliza toda a lógica de trigger de perks em `PerkTriggerEvaluator`, removendo essa responsabilidade de `PerkService`.

### Antes ❌
```
PerkService:
├─ ApplyRollModifiers (verifica condições mas não dispara evento)
├─ ApplyDiceModifiers (verifica condições mas não dispara evento)
└─ OnPerkTriggered (definido mas NUNCA chamado)
```

### Depois ✅
```
PerkTriggerEvaluator:
├─ EvaluateRollTriggers (valida + dispara OnPerkTriggered)
├─ EvaluateDiceTriggers (valida + dispara OnPerkTriggered)
└─ ValidateCondition / ValidateDiceCondition (factory pattern)

PerkService:
├─ ApplyRollModifiers (chama EvaluateRollTriggers)
├─ ApplyDiceModifiers (chama EvaluateDiceTriggers)
└─ OnPerkTriggered (forwarda eventos do PerkTriggerEvaluator)
```

## 🔧 Passos de Integração

### 1. Atualizar PerkService para usar PerkTriggerEvaluator

```csharp
public class PerkService
{
    private readonly PerkDatabase database;
    private readonly PerkTriggerEvaluator triggerEvaluator;
    
    public event System.Action<PerkTriggeredEvent> OnPerkTriggered;
    
    public PerkService()
    {
        database = PerkDatabase.GetOrCreateRuntimeDatabase();
        database.EnsureLoaded();
        
        // Criar o avaliador
        triggerEvaluator = new PerkTriggerEvaluator(database);
        
        // Forward eventos do avaliador
        triggerEvaluator.OnPerkTriggered += (evt) => OnPerkTriggered?.Invoke(evt);
    }
    
    public int GetExtraDiceCount(Battler actor, Battler opponent, CombatRollContext context)
    {
        float value = 0f;
        
        // ✅ Novo: Dispara triggers ANTES de aplicar modificadores
        triggerEvaluator.EvaluateRollTriggers(actor, context, PerkTrigger.BeforeRoll);
        
        ApplyRollModifiers(actor, opponent, context, PerkTrigger.BeforeRoll, PerkModifierTarget.ExtraDice, ref value);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }
    
    // ... resto do código similar
}
```

### 2. Integrar em DiceService (onde os dados são rolados)

```csharp
public class DiceService
{
    private PerkTriggerEvaluator triggerEvaluator;
    
    // No método de roll que retorna resultados
    private List<DiceResult> RollAndApplyPerks(...)
    {
        // ... código de roll
        
        // ✅ Novo: Dispara triggers para todos os dados rolados
        for (int i = 0; i < allRolledDices.Count; i++)
        {
            triggerEvaluator.EvaluateDiceTriggers(
                battler, 
                new CombatActionContext(actor, opponent, actionType),
                allRolledDices[i],
                PerkTrigger.AfterResolve,
                allRolledDices
            );
        }
        
        return allRolledDices;
    }
}
```

### 3. Atualizar PerkActivationFeedback para novos eventos

```csharp
public class PerkActivationFeedback : MonoBehaviour
{
    private PerkService perkService;
    
    public void Initialize(PerkService service)
    {
        perkService = service;
        // ✅ Agora recebe PerkTriggeredEvent ao invés de 3 parâmetros separados
        perkService.OnPerkTriggered += HandlePerkTriggered;
    }
    
    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        // Dados completos disponíveis
        Debug.Log($"Perk {evt.PerkName} triggered with value {evt.AppliedValue}");
        
        // Criar popup com ícone do perk
        ShowPerkNotification(evt.PerkId, evt.PerkName, evt.Owner);
        
        // Efeitos sonoros baseados no tipo
        PlayPerkSound(evt.ModifierTarget);
        
        // Análise do contexto completo
        AnalyzeContext(evt.FullContext);
    }
}
```

## 🎯 Benefícios

| Aspecto | Antes | Depois |
|--------|-------|--------|
| **Centralização** | Lógica espalhada | Tudo em PerkTriggerEvaluator |
| **Disparo de eventos** | Manual/Esquecível | Automático |
| **Dados de evento** | 3 parâmetros básicos | Objeto PerkTriggeredEvent completo |
| **Extensibilidade** | Modificar enums | Factory pattern, sem limite |
| **Testabilidade** | Difícil | PerkTriggerEvaluator isolável |
| **Debugging** | Sem logs centralizados | Logs automáticos e rastreáveis |

## 📊 Fluxo de Execução Novo

```
DiceService.RollDice()
    ↓
PerkService.GetExtraDiceCount()
    ├─ triggerEvaluator.EvaluateRollTriggers(actor, context, BeforeRoll)
    │   ├─ Valida condições
    │   ├─ Dispara OnPerkTriggered ✅
    │   └─ Listeners recebem PerkTriggeredEvent completo
    ├─ ApplyRollModifiers() [sem lógica de trigger]
    └─ Retorna valor modificado
    ↓
DiceService.ResolveDiceResults()
    ↓
ActionResolverService.ResolveAction()
    ├─ PerkService.ApplyDamageModifiers()
    │   ├─ triggerEvaluator.EvaluateDiceTriggers(owner, context, dice, AfterResolve)
    │   │   ├─ Valida condições
    │   │   ├─ Dispara OnPerkTriggered ✅
    │   │   └─ Listeners recebem PerkTriggeredEvent completo
    │   └─ ApplyDiceModifiers() [sem lógica de trigger]
    └─ Retorna dano modificado
```

## 🔌 Como Adicionar Novas Condições (Escalável!)

### Exemplo: Adicionar RollSumGraterThan

1. Criar classe de condição:
```csharp
public class RollSumGreaterThanCondition : IPerkCondition
{
    public PerkConditionKey ConditionType => PerkConditionKey.RollSumGreaterThan;
    
    public bool Evaluate(object context, string conditionValue)
    {
        if (!(context is DiceRollSumContext sumContext))
            return false;
        
        return int.TryParse(conditionValue, out int threshold) && 
               sumContext.TotalSum > threshold;
    }
}
```

2. Registrar na factory:
```csharp
static PerkConditionFactory()
{
    // ... existing registrations
    Register(PerkConditionKey.RollSumGreaterThan, new RollSumGreaterThanCondition());
}
```

3. Adicionar ao enum:
```csharp
public enum PerkConditionKey
{
    // ... existing
    RollSumGreaterThan
}
```

4. Usar no CSV:
```
ConditionKey: RollSumGreaterThan
ConditionValue: 15
```

**SEM modificar PerkService, PerkTriggerEvaluator ou DiceService!**

## ⚠️ Pontos de Atenção

1. **Timing**: EvaluateRollTriggers deve ser chamado ANTES dos modificadores serem aplicados
2. **Context**: RollSumEquals requer a lista completa de dados (allDices)
3. **Performance**: Avaliação é O(perks × rules), otimizada com early exit
4. **Identity Perks**: Automaticamente avaliados sem estar em battler.Perks

## 📝 Checklist de Integração

- [ ] Adicionar PerkTriggerEvaluator a PerkService.__ctor__
- [ ] Chamar EvaluateRollTriggers em GetExtraDiceCount
- [ ] Chamar EvaluateRollTriggers em GetMinimumRollValue
- [ ] Chamar EvaluateDiceTriggers em GetPowerMultiplier
- [ ] Chamar EvaluateDiceTriggers em ApplyDamageModifiers
- [ ] Adicionar MomentumPoints e novos tipos de condição aos enums
- [ ] Testar lucky_number perk (RollSumEquals 7)
- [ ] Testar equal_share perk (RollSumEqualsAttackersRollSum)
- [ ] Testar six_feet_under perk (RollValueEquals 6)
- [ ] Verificar PerkActivationFeedback recebe eventos corretamente
