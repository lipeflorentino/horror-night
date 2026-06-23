# Skill: Unity UI Architect & Componentization Analyst

## Purpose
Analyzes UI C# scripts (Views and UIComponents), related to combat on `CombatV2` files, and Unity Scene/Prefab hierarchy layout to propose a decoupled, flattened, and highly scalable UI architecture. The goal is to separate concerns, enforce a strict View-Component hierarchy, and eliminate deep Unity nesting.

---

## Architectural Definitions & Constraints

### 1. The Core UI Core Anchor GameObjects
The UI infrastructure must strictly revolve around and anchor into four main root/top-level GameObjects in the Unity hierarchy:
- **`MainCanvas`** Where most UI scripts are used
- **`PlayerCanvas`** Player Feedbacks
- **`EnemyCanvas`** Enemy HUD and feedbacks
- **`ViewManager`** Where all the View scripts are used

### 2. The Flattened UI Hierarchy Rule
- **Current Issue:** Deeply nested GameObjects (Parent -> Child -> SubChild -> DeepChild) creating maintenance bottlenecks and tight coupling.
- **Target Goal:** A flat Canvas hierarchy where major UI sections (Views) sit side-by-side as siblings directly under their respective Canvas root (`MainCanvas`, `PlayerCanvas`, or `EnemyCanvas`). Sub-elements (UIComponents) should be modular prefabs instantiated dynamically or linked shallowly, rather than hardcoded deep in the scene hierarchy.

### 3. Strict Architectural Separation
- **Views (Atomic Panels):** Large, standalone UI screens or persistent panels (e.g., `CombatMainHUDView`, `SkillSelectionView`, `TurnOrderView`). They handle high-level layout, coordinate their internal components, and sit directly under one of the 3 anchor Canvases. They listen to `CombatV2` data state changes.
- **UIComponents (Modular Elements):** Small, self-contained, and reusable widgets (e.g., `SlotUI`, `ResourceBarUI`, `DiceRollWidget`). They do not know about the global combat data directly; they receive data explicitly from a Parent View or a dedicated Presenter/ViewModel.

---

## Execution Pipeline

### Step 1: Codebase & Unity Meta Inspection
The Agent must scan and parse:
1. All scripts within the designated `View` and `UIComponents` directories.
2. The data properties, events, and states exposed by the `CombatV2` systems to map what UI elements need which data.
3. **Hierarchy Parsing:** Read target `.prefab` or `.unity` scene files (parsed as YAML) to reconstruct the existing GameObject hierarchy tree, tracking how they map under `MainCanvas`, `PlayerCanvas`, `EnemyCanvas`, and `UIManager`.

### Step 2: Diagnostic & De-nesting Report
Before suggesting code changes, present a architectural breakdown covering:
- **Nesting Hotspots:** Identify GameObjects nested deeper than 3-4 levels away from their respective anchor Canvas and explain why they shouldn't be there.
- **Anchor Misplacements:** Detect if player UI elements leaked into `MainCanvas` or enemy elements leaked into `PlayerCanvas`.
- **Responsibility Leaks:** Highlight `UIComponents` that are reaching out to global combat data directly, or `Views` that are micromanaging individual text fields instead of delegating to a custom component.

### Step 3: Flat UI Blueprint Proposal
Generate a structural blueprint mapping out the new layout:
1. **The New Canvas Blueprint:** A text-based tree visualization showing how the Unity Hierarchy should look once flattened, strictly structured under `MainCanvas`, `EnemyCanvas`, `PlayerCanvas`, and `UIManager`.
2. **Componentization Strategy:** Instructions on which nested sections should be extracted into isolated, reusable Prefabs (`UIComponents`).
3. **Data Flow Redirection:** A plan showing how `CombatV2` data will flow cleanly into the flattened Views via `UIManager` and propagate down into UIComponents.

### Step 4: Refactoring Action Plan & Code Generation
Upon user approval of the blueprint:
1. Provide rewritten C# code for the refactored Views and UIComponents using clean abstraction or event-driven patterns.
2. Provide step-by-step instructions on how to reorganize the Unity Hierarchy window manually to match the approved layout anchor system.

---

## Expected Output Format

When invoked, the Agent should start with **Step 1 and Step 2**, providing a structured diagnostic:

### Example Diagnostic Layout:
> ### 🚨 UI Hierarchy Nesting Alert
> Found deep nesting in `PlayerCanvas -> CenterAnchor -> ActionPanel -> SkillLayout -> SkillSlots -> SlotUI`.
> **Proposed Fix:** Extract `SkillSelectionView` so it sits as a direct sibling of `CenterAnchor` under `PlayerCanvas`. Turn `SlotUI` into a dynamic prefab spawned into a shallow grid layout inside that View.
>
> ### 🛠️ Componentization Opportunities
> `CombatMainHUDView` under `MainCanvas` currently manages health bar sliders, resource text, and turn counters individually (over 15 serialized fields).
> **Proposed Fix:** Break these into `ResourceBarUIComponent` and `TurnTrackerUIComponent`.