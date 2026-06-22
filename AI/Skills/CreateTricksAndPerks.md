# Skill: Creative Combat Trick & Perk Generator

## Purpose
Enables the AI to creatively design and fully implement a combat Trick and all its supporting atomic Perks from just a name and a high-level gameplay description. The AI acts as a Systems Game Designer, ensuring technical adherence to existing data schemas while mapping abstract ideas into concrete game mechanics.

---

## Architecture Context & Schema Alignment

### 1. Tricks (`TrickTable.csv`)
Tricks manage the lifecycle, resource costs, and high-level activation flow.
- **Columns:** `Id,DisplayName,Description,Level,MindCost,BodyCost,HeartCost,TimingTurns,DurationTurns,CooldownTurns,PerkIds,ActivationMode,Rarity,Tags,FlavorText,IconName,NegativePerkId`

### 2. Perks (`PerkTable.csv`)
Perks are atomic modifiers triggered under specific combat conditions.
- **Columns:** `Id,OwnerRole,ActionFilter,RollFilter,StatFilter,TierFilter,ConditionKey,ConditionValue,ModifierTarget,Operation,Value,MaxStacks,StackMode`

### 3. Codebase Ecosystem (Enums & ScriptableObjects)
When a Trick or Perk is added, the following code structures must be checked and updated if necessary:
- `TrickEnum` / `PerkEnum` (Code registries for IDs)
- `TrickSO` / `PerkSO` (ScriptableObjects data containers)
- Game Systems: Triggers, Conditions, Operations, and Rules.

---

## User Input Format
The user will provide the request in natural language, typically specifying:
- **Name:** The desired name of the ability.
- **Description/Concept:** What the ability should do mechanically or its thematic fantasy.

---

## Core Execution Pipeline

### Step 1: Creative Analysis & Design Deductions
Upon receiving the user's idea, analyze how to break it down atomically:
1. Determine the appropriate `ActivationMode` (e.g., `Passive`, `Active`, `ActiveCharge`).
2. Deduce fair resource costs (`MindCost`, `BodyCost`, `HeartCost`), cooldowns, and durations based on the ability's perceived power level.
3. **Decompose into Perks:** Break the description into one or multiple atomic Perks. 
   - Identify the exact filters (`ActionFilter`, `RollFilter`, `StatFilter`, `TierFilter`).
   - Identify the correct `ConditionKey` and `ModifierTarget` based on existing game patterns.
4. **Identify Negative Effects:** If the concept implies a high-risk/high-reward or active-charge mechanism, design a corresponding balancing debuff to assign to `NegativePerkId`.

### Step 2: Validation, Cross-Reference & Interactive Clearance
Before writing any file, cross-reference your proposed mechanics against existing project code:
1. **Check Enums & Conditions:** Ensure the proposed `ConditionKey`, `ModifierTarget`, or `Operation` values exist in the codebase.
2. **Interactive Prompts (Mandatory User Dialogue):**
   *If there is ANY ambiguity, potential balance issues, or a requirement to introduce new enums/system types, you **MUST** stop and ask the user for confirmation.*
   
   *Example Interruption:*
   > "Para a habilidade 'Vengeance', pretendo criar uma operação chamada `AddPerCharge`. Notei que essa operação não está mapeada explicitamente no seu enum de Operations atual. Você deseja que eu crie esse novo caso no enum ou prefere que eu adapte usando uma regra existente?"

### Step 3: File Generation & Modifications
Once the design is locked and cleared by the user, apply the changes across all files:

1. **Append to `PerkTable.csv`:** Generate rows for all required atomic perks (including the negative ones if applicable). Ensure strings are properly formatted for CSV.
2. **Append to `TrickTable.csv`:** Generate the orchestrator row linking the `PerkIds` (separated by semicolons) and `NegativePerkId`. Write a descriptive, clear mechanics description and auto-generate a thematic `FlavorText`.
3. **Update Code Records:** Append the new entries to `TrickEnum`, `PerkEnum`, and generate/update the matching ScriptableObjects (`TrickSO` / `PerkSO`) boilerplate code if needed.

---

## Expected Output Behavior

### Input Example:
> "Crie uma habilidade chamada 'Sede de Sangue'. O conceito é que toda vez que eu atacar e tirar um resultado Alto em Body, eu ganho +2 de Ataque no próximo turno, mas se eu passar 2 turnos sem atacar, eu sofro dano de fadiga."

### Expected Process:
1. **AI Output Analysis:** Explains the breakdown (Trick `Active/Passive`, Perk 1 for Bloodlust Buff, Perk 2 for Fatigue Debuff).
2. **AI Inquiry:** "Notei que você mencionou 'passar 2 turnos sem atacar'. Isso vai precisar de um `ConditionKey` customizado como `OnTurnsWithoutAttack`. Posso registrar esse novo termo no seu sistema de triggers e enums?"
3. **CSV Generation:** Updates both files with immaculate structural formatting.