# 📚 Explicação: Stack, IsPermanentIdentity, Tags + Análise Estrutura Tricks

## 1️⃣ Parâmetros Fundamentais (Explicação Rápida)

### StackMode, MaxStacks
```
StackMode: Como lidar com múltiplas aplicações do mesmo perk
├─ AddStack      → Acumula efeitos (ex: +2 dados + +2 dados = +4 dados)
├─ Replace       → Substitui (ex: +30% dano substitui +30% dano anterior)
└─ RefreshDuration → Reseta duração sem acumular (padrão usado em todos)

MaxStacks: Limite de acúmulos
├─ Valor: 1 = Não acumula, 2+ = Pode acumular
├─ Uso: MaxStacks=1 em identity perks (sempre 1 stack)
└─ Uso: MaxStacks=3+ em temporary perks que podem acumular
```

**Na Prática**:
```
StackMode=RefreshDuration, MaxStacks=1
→ Aplicar novamente apenas reseta duração, não acumula
→ Usado em todos os perks atuais
```

### IsPermanentIdentity
```
Define se o perk é parte da "identidade" permanente do battler

true (Identity Perk)
├─ Sempre ativo, sem aplicar/remover
├─ Carregado automaticamente de PerkDatabase.GetIdentityPerks()
├─ DurationTurns=-1 (infinito)
└─ Exemplos: mind_min_accuracy_30, heart_extra_die, brutal_force_x2

false (Temporary/Skill Perk)
├─ Aplicado durante combate
├─ Pode ter duração limitada
└─ Exemplos: six_feet_under, lucky_number, equal_share
```

**Na Prática**:
```
CSV: IsPermanentIdentity=true, Tags=identity;mind
→ Perk carregado automaticamente = parte da "classe" do jogador
→ Ativo em todos os combates

CSV: IsPermanentIdentity=false, Tags=permanent;power;attack  
→ Perk acionado durante combate = "Skill" ou "Efeito"
→ Pode ser removido/expirar
```

### Tags
```
Categorias separadas por ponto-e-vírgula para filtros e UI

Tipos de Tags (Convenção):
├─ Identidade:  identity, mind, heart, body
├─ Duração:     permanent, temporary, ritual
├─ Contexto:    attack, defense, power, luck
├─ Efeito:      crowd_control, damage_up, defense_up
└─ Raro:        rare, epic, legendary

Uso:
├─ UI: Filtrar perks por categoria
├─ Mechanics: "Perks com tag 'defense'" → aplicar bonus
├─ Database: GetPerksWithTag("power") → listar todos
└─ Debugging: Identificar propósito do perk rapidamente
```

**Na Prática**:
```
Tags=identity;mind
→ "Este perk é de identidade" + "Relacionado a Mind"

Tags=permanent;power;attack
→ "Perk permanente" + "Afeta Power" + "Contexto Attack"
```

---

## 2️⃣ Análise da Estrutura Tricks (Card Game Style)

### Proposta Resumida
```
ANTES: Perks diretos no battler
└─ Player.Perks = [mind_min_accuracy_30, heart_extra_die, ...]

DEPOIS: Tricks que carregam Perks
└─ Player.Tricks = [Trick("Brutal Force"), Trick("Lucky Strike")]
   └─ Trick contém: [brutal_force_x2_perk, brutal_force_x1_5_perk]
```

---

## 3️⃣ Estrutura Proposta: TrickSO

### O que é um Trick?

```csharp
[CreateAssetMenu(fileName = "Trick", menuName = "Combat/Trick")]
public class TrickSO : ScriptableObject
{
    // Metadados (Atualmente em Perk, faz mais sentido aqui)
    public string Id;
    public string DisplayName;              // "Brutal Force" (antes em Perk.Name)
    public string Description;              // UI tooltip
    public Sprite Icon;                     // Card visual
    public int Level;                       // Nível mínimo para usar
    
    // Custo de Casting (NOVO - Não existia em Perk)
    public int MindCost;                    // Custo em Mind
    public int HeartCost;                   // Custo em Heart  
    public int BodyCost;                    // Custo em Body
    
    // Duração (Movido de Perk para cá)
    public int DurationTurns;               // -1 = permanente, 0+ = turnos
    
    // Perks Atômicos que este Trick ativa
    public List<string> PerkIds;            // ["brutal_force_x2", "brutal_force_x1_5"]
    
    // Metadados de Card Game
    public TrickRarity Rarity;              // Common, Rare, Epic, Legendary
    public List<string> Tags;               // ["power", "attack", "buff"]
    public string FlavorText;               // Descrição narrativa da carta
}

public enum TrickRarity { Common, Rare, Epic, Legendary }
```

### Exemplo: "Brutal Force" Trick

```
=== ANTES (Perks separados em CSV) ===
brutal_force_1,Força Brutal High,High power de body vale x2.,brutal_force,true,Dice,PowerMultiplier,-1,OwnerAsActor,Any,Power,Body,High,Always,,PowerMultiplier,Override,2,1,RefreshDuration,identity;body

brutal_force_2,Força Brutal Medium,Medium power de body vale x1.5.,brutal_force,true,Dice,PowerMultiplier,-1,OwnerAsActor,Any,Power,Body,Medium,Always,,PowerMultiplier,Override,1.5,1,RefreshDuration,identity;body

=== DEPOIS (Um Trick que agrupa dois Perks) ===
TrickSO: "Brutal Force"
├─ Id: brutal_force
├─ DisplayName: "Força Brutal"
├─ Description: "Multiplicadores de poder aumentados para high e medium"
├─ Icon: brutal_force.png
├─ Level: 1 (disponível desde o início)
├─ MindCost: 0
├─ BodyCost: 0
├─ HeartCost: 0
├─ DurationTurns: -1 (permanente - é uma identity trick)
├─ PerkIds: ["brutal_force_x2", "brutal_force_x1_5"]
├─ Rarity: Common
└─ Tags: ["power", "identity", "passive"]
```

---

## 4️⃣ Arquitetura Nova: Fluxo Tricks → Perks

### Estrutura de Dados

```csharp
// NOVO: Representa um Trick em tempo de jogo
public class TrickRuntimeInstance
{
    public TrickSO Definition;
    public Battler Owner;
    public int RemainingTurns;      // -1 = infinito
    public int CastTurn;             // Quando foi castado (para UI)
    
    public List<PerkRuntimeInstance> ActivePerks { get; set; }
}

// MODIFICADO: Battler
public class Battler
{
    public List<TrickRuntimeInstance> Tricks = new();    // NOVO
    public List<PerkRuntimeInstance> Perks = new();      // ANTIGO (mantém para compat)
    
    // Ou: Perks agregado automaticamente de Tricks
    // public List<PerkRuntimeInstance> GetEffectivePerks()
    // {
    //     List<PerkRuntimeInstance> perks = new();
    //     foreach (var trick in Tricks)
    //         perks.AddRange(trick.ActivePerks);
    //     return perks;
    // }
}
```

### Fluxo: Aplicar Trick → Ativar Perks

```
┌─────────────────────────────────────────┐
│   UI: Player clica em "Brutal Force"    │
└────────────────┬────────────────────────┘
                 ↓
        CombatManager.CastTrick()
                 ↓
        ✓ Verificar Level
        ✓ Verificar Mind/Body/Heart Cost
        ✓ Verificar duração
                 ↓
        TrickService.ApplyTrick()
                 ↓
        ├─ Criar TrickRuntimeInstance
        ├─ Para cada PerkId em Trick:
        │  └─ PerkService.ApplyPerk()
        │     └─ Battler.Perks.Add(perkInstance)
        │
        └─ Battler.Tricks.Add(trickInstance)
                 ↓
        [Perks já estão em Battler.Perks]
        [PerkTriggerEvaluator funciona normalmente]
```

### Integração com PerkService

```
PerkService (SEM MUDANÇAS NECESSÁRIAS)
├─ ApplyPerk() → Aceita perk por ID
├─ Perks armazenados em Battler.Perks
├─ PerkTriggerEvaluator funciona normalmente
└─ Não sabe nem se importa se vem de Trick ou direto

TrickService (NOVO)
├─ ApplyTrick()
├─ RemoveTrick()
├─ TickTrickEnd()
├─ Para cada perk do trick:
│  └─ Chama PerkService.ApplyPerk()
└─ Gerencia duração de Tricks

Battler (MODIFICAÇÃO MÍNIMA)
├─ public List<TrickRuntimeInstance> Tricks = new();
└─ Métodos helper:
   ├─ GetEffectivePerks()     // Agrega perks de tricks
   ├─ HasTrick(trickId)
   └─ RemoveTrick(trickId)
```

---

## 5️⃣ Mudanças Propostas (Detalhado)

### ✅ O QUE MUDA

| Componente | Mudança | Motivo |
|-----------|---------|--------|
| **PerkSO** | Remove Name, Description, Icon, DurationTurns | Agora em TrickSO |
| **TrickSO** | NOVO arquivo | Agrega metadados que fazem sentido aqui |
| **Battler** | +Tricks list | Para rastrear tricks ativos |
| **PerkService** | Sem mudanças | Continua aplicando perks normalmente |
| **TrickService** | NOVO serviço | Gerencia aplicação/remoção de tricks |
| **CSV Perks** | Sem mudanças | Perks permanecem atômicos |
| **CSV/ScriptableObjects** | NOVO TrickTable | Define todas as tricks do jogo |

### ❌ O QUE NÃO MUDA

| Componente | Motivo |
|-----------|--------|
| **PerkTriggerEvaluator** | Funciona com perks em Battler.Perks |
| **DiceService** | Não conhece tricks, trabalha com perks |
| **ActionResolverService** | Não conhece tricks, trabalha com perks |
| **PerkActivationFeedback** | Continua mostrando perks acionados |

---

## 6️⃣ Nova Estrutura: Exemplo Completo

### Exemplo: "Lucky Strike" Trick (com duração limitada)

```csharp
=== TrickSO Asset ===
Id: lucky_strike
DisplayName: "Golpe da Sorte"
Description: "Próximos 3 ataques têm chance de dar momentum"
Icon: lucky_strike.png
Level: 2

CastCosts:
├─ MindCost: 5
├─ BodyCost: 0
└─ HeartCost: 3

DurationTurns: 3           // Dura 3 turnos (diferente de perks!)
PerkIds: ["lucky_number"]  // Ativa apenas 1 perk
Rarity: Rare
Tags: ["attack", "luck", "temporary", "momentum"]

=== Aplicação no Combate ===
Turn 1: Player clica em "Lucky Strike"
   → Cost: -5 Mind, -3 Heart
   → PerkService.ApplyPerk("lucky_number")
   → TrickService.ApplyTrick(player, "lucky_strike")
   → Battler.Tricks.Add(TrickRuntimeInstance {..., RemainingTurns: 3})

Turn 1-3: Cada ataque
   → lucky_number dispara se soma = 7
   → Ganha +1 momentum

Turn 4: TrickService.TickTrickEnd()
   → RemainingTurns: 3 → 2 → 1 → 0
   → Remove trick
   → PerkService.RemovePerk("lucky_number")
```

---

## 7️⃣ Viabilidade & Benefícios

### ✅ VIÁVEL? SIM, TOTALMENTE

**Porquê**:
1. Perks já são atômicos (modificam 1 coisa)
2. PerkService funciona independentemente
3. Apenas adicionamos camada intermediária (Trick)
4. Sem quebrar código existente
5. Factory Pattern já preparado para extensão

### 📈 BENEFÍCIOS

| Benefício | Descrição |
|-----------|-----------|
| **UI/UX de Card Game** | Tricks como "cartas" visuais é intuitivo |
| **Economy Balancing** | Custo de cast em stats = limite de uso |
| **Cooldown Management** | DurationTurns permite cooldown por turno |
| **Composição Tática** | Player escolhe quais Tricks levar |
| **Feedback Melhorado** | "Castou Brutal Force" > "Ativou brutal_force_x2" |
| **Progressão** | Level em Trick define when it's unlocked |
| **Escalabilidade** | Adicionar novo Trick = novo file TrickSO |

### ⚠️ CONSIDERAÇÕES

| Aspecto | Consideração |
|---------|--------------|
| **Complexidade** | +1 camada, mas separação clara de responsabilidade |
| **Performance** | Negligível (lookup O(1) em dicts) |
| **Persistência** | Salvar Battler.Tricks em save game |
| **Networking** | Se multiplayer: sincronizar Tricks + Perks |

---

## 8️⃣ Planejamento de Implementação

### Fase 1: Criar Estrutura TrickSO

```csharp
// 1. Enum para Rarity
public enum TrickRarity { Common, Rare, Epic, Legendary }

// 2. TrickSO ScriptableObject
[CreateAssetMenu(fileName = "Trick", menuName = "Combat/Trick")]
public class TrickSO : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public int Level;
    public int MindCost, BodyCost, HeartCost;
    public int DurationTurns;
    public List<string> PerkIds;
    public TrickRarity Rarity;
    public List<string> Tags;
    public string FlavorText;
}

// 3. TrickRuntimeInstance
public class TrickRuntimeInstance
{
    public TrickSO Definition;
    public Battler Owner;
    public int RemainingTurns;
    public List<PerkRuntimeInstance> ActivePerks { get; set; }
}

// 4. TrickDatabase (como PerkDatabase)
public class TrickDatabase : MonoBehaviour
{
    public List<TrickSO> allTricks = new();
    // GetById(), FilterByTag(), GetByLevel(), etc
}
```

### Fase 2: Criar TrickService

```csharp
public class TrickService
{
    private readonly TrickDatabase database;
    private readonly PerkService perkService;
    
    public event System.Action<Battler, TrickRuntimeInstance> OnTrickCasted;
    public event System.Action<Battler, string> OnTrickRemoved;
    
    public void ApplyTrick(Battler target, string trickId, Battler source = null)
    {
        TrickSO definition = database.GetById(trickId);
        if (definition == null) return;
        
        // 1. Verificar level
        // 2. Consumir custo (Mind/Body/Heart)
        // 3. Aplicar perks
        // 4. Adicionar trick ao battler
        
        TrickRuntimeInstance trickInstance = new(definition, target, definition.DurationTurns);
        
        foreach (var perkId in definition.PerkIds)
        {
            var perkInstance = perkService.ApplyPerk(target, perkId, source, definition.DurationTurns);
            trickInstance.ActivePerks.Add(perkInstance);
        }
        
        target.Tricks.Add(trickInstance);
        OnTrickCasted?.Invoke(target, trickInstance);
    }
    
    public void RemoveTrick(Battler target, string trickId)
    {
        // 1. Encontrar trick
        // 2. Remover todos os perks associados
        // 3. Remover trick do battler
        OnTrickRemoved?.Invoke(target, trickId);
    }
    
    public void TickTrickEnd(Battler battler)
    {
        // Reduzir RemainingTurns
        // Remover tricks expirados
    }
}
```

### Fase 3: Modificar Battler

```csharp
public class Battler
{
    public List<TrickRuntimeInstance> Tricks = new();   // NOVO
    public List<PerkRuntimeInstance> Perks = new();     // MANTÉM
    
    // Helper para agregar perks de tricks
    public List<PerkRuntimeInstance> GetEffectivePerks()
    {
        List<PerkRuntimeInstance> perks = new();
        
        // Perks diretos (legado)
        perks.AddRange(Perks);
        
        // Perks de tricks
        foreach (var trick in Tricks)
            perks.AddRange(trick.ActivePerks);
        
        return perks;
    }
}
```

### Fase 4: Integrar CombatManager

```csharp
public class CombatManager
{
    private TrickService trickService;
    
    public void OnPlayerSelectTrick(string trickId)
    {
        TrickSO trick = trickDatabase.GetById(trickId);
        
        // 1. Verificar se player tem suficiente Mind/Body/Heart
        if (player.Mind < trick.MindCost) return; // Sem recursos
        
        // 2. Aplicar trick
        trickService.ApplyTrick(player, trickId);
        
        // 3. Consumir custo
        player.Mind -= trick.MindCost;
        player.Body -= trick.BodyCost;
        player.Heart -= trick.HeartCost;
        
        // 4. UI feedback
        OnTrickCasted(trick);
    }
}
```

### Fase 5: TrickDatabase em CSV ou ScriptableObjects

```
Opção A: CSV (como Perks)
├─ TrickTable.csv
├─ Id, DisplayName, Description, Icon, Level
├─ MindCost, BodyCost, HeartCost
├─ DurationTurns, PerkIds, Rarity, Tags

Opção B: ScriptableObjects (melhor para card game)
├─ Tricks/
│  ├─ brutal_force.asset
│  ├─ lucky_strike.asset
│  └─ divine_intervention.asset
└─ Inspector visual para design
```

---

## 9️⃣ Dados que Mudam

### CSV PerkTable (Perks ficam mais simples)

**ANTES** (Atual):
```
Id,Name,Description,IconName,...,DurationTurns,...,Tags
brutal_force_1,Força Brutal High,...,brutal_force,...,-1,...,identity;body
```

**DEPOIS** (Simplificado - Remove Name, Description, Icon, Duration):
```
Id,Trigger,ConditionKey,ConditionValue,ModifierTarget,Operation,Value,MaxStacks,Tags
brutal_force_x2,PowerMultiplier,Always,,PowerMultiplier,Override,2,1,power;identity
brutal_force_x1_5,PowerMultiplier,Always,,PowerMultiplier,Override,1.5,1,power;identity
six_feet_under,AfterResolve,RollValueEquals,6,DamagePercent,Multiply,0.30,1,power;attack
lucky_number,AfterResolve,RollSumEquals,7,MomentumPoints,Add,1,1,luck;attack
```

### NOVA: TrickTable.csv (ou TrickDatabase.cs com assets)

```
Id,DisplayName,Description,Level,MindCost,BodyCost,HeartCost,DurationTurns,PerkIds,Rarity,Tags,FlavorText

brutal_force,Força Brutal,Multiplicadores de poder aumentados,1,0,0,0,-1,"brutal_force_x2;brutal_force_x1_5",Common,power;identity;passive,"Força bruta manifesta"

lucky_strike,Golpe da Sorte,Próximos 3 ataques ganham momentum extra,2,5,0,3,3,lucky_number,Rare,luck;attack;temporary,"A sorte está ao seu lado"

divine_intervention,Intervenção Divina,Defesa aumentada nos próximos 2 turnos,3,10,5,5,2,divine_shield_buff,Epic,defense;buff;temporary,"Os deuses intervêm"
```

---

## 🔟 Comparativo: Estrutura Atual vs Nova

### Arquitetura Atual
```
CSV PerkTable
    ↓
PerkDatabase
    ↓
PerkService.ApplyPerk()
    ↓
Battler.Perks[]
    ↓
PerkTriggerEvaluator
```

### Arquitetura Nova (Proposed)
```
CSV PerkTable                CSV TrickTable
    ↓                              ↓
PerkDatabase          ×     TrickDatabase
    ↓                              ↓
PerkService           ×     TrickService
    ↓                              ↓
Battler.Perks[]  +    Battler.Tricks[]
    ↓                              ↓
    └──────────────┬───────────────┘
                   ↓
         PerkTriggerEvaluator
              (Unchanged)
```

---

## 1️⃣1️⃣ Conclusão & Recomendação

### ✅ A ESTRUTURA TRICK FAZE SENTIDO?

**SIM, ABSOLUTAMENTE**

1. **Separação de Responsabilidade**: Metadados de "Spell/Ability" (Trick) separados de "Efeito Mecânico" (Perk)
2. **Card Game Appeal**: Tricks como "Cartas" é intuitivo para UI
3. **Economy Balancing**: Custo em stats limita uso estrategicamente
4. **Escalabilidade**: Fácil adicionar novos tricks sem mexer em Perks
5. **Progression**: Level em Tricks cria curva de desbloqueio

### 🎯 MUDANÇAS RESUMIDAS

```
Criar:
├─ TrickSO.cs (novo asset type)
├─ TrickRuntimeInstance.cs
├─ TrickDatabase.cs
├─ TrickService.cs
└─ TrickTable.csv (ou AssetsJog/Tricks/*.asset)

Modificar:
├─ Battler.cs (+Tricks list)
├─ CombatManager.cs (+TrickService, OnPlayerSelectTrick)
└─ PerkSO.cs (remove Name, Description, Icon, Duration - move para Trick)

Não Mexer:
├─ PerkService.cs (funciona com Perks como antes)
├─ PerkTriggerEvaluator.cs
├─ DiceService.cs
├─ ActionResolverService.cs
└─ PerkActivationFeedback.cs
```

### 📊 Impacto

| Métrica | Valor |
|---------|-------|
| Novo Arquivos | 4 classes + 1 CSV |
| Modificações | 2 classes |
| Compatibilidade Quebrada | 0 |
| Linhas de Código | ~500 (estimado) |
| Tempo de Implementação | ~2-3 horas |
| Benefício | ⭐⭐⭐⭐⭐ Alto |

---

**RECOMENDAÇÃO**: Implementar essa estrutura. É viável, não quebra código existente, e adiciona dimensão estratégica (card game) ao sistema de perks.
