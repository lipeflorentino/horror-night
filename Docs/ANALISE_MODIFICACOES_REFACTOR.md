# Análise de Modificações - Refactor Perks e Tricks

## Contexto
Perks e Tricks foram separados: Perks são apenas **mecânicas puras** (sem metadados), Tricks têm **metadados + UI** e envolvem um ou mais Perks.

## Arquivos Modificados e Status

### ✅ Camada de Modelo (Model Layer)

#### PerkSO.cs
- **Antes:** Tinha DisplayName, Description, Icon, Rules, Tags, etc
- **Depois:** Apenas mecânicas - Id, IsPermanentIdentity, Rules, Tags, MaxStacks, DefaultDurationTurns
- **Impacto:** ✅ Sem breaking changes na lógica, apenas metadados removidos
- **Requer Reimport:** ✅ Menu `Tools > Import Perks CSV` para recarregar dados

#### PerkTriggeredEvent.cs
- **Antes:** PerkId + PerkName (string com DisplayName)
- **Depois:** Apenas PerkId (metadados em Trick agora)
- **Impacto:** ✅ PerkActivationFeedback atualizado para usar PerkId
- **Breaking Change:** Qualquer listener de OnPerkTriggered que usava evt.PerkName precisa adaptar

#### PerkTriggerEvaluator.cs
- **Antes:** Criava PerkName com `perk.Definition.DisplayName`
- **Depois:** Usa `perk.Definition.Id` (sem DisplayName disponível)
- **Impacto:** ✅ Logs agora mostram ID em vez de nome legível
- **Análise:** Debug logs continuam funcionando, apenas menos legíveis. Considerar usar TrickDatabase para resolver nome.

### ✅ Camada de Importação (Parser/Importer)

#### PerkCsvParser.cs
- **Antes:** Carregava IconName e procurava Icon em Resources/Sprites/Perks/
- **Depois:** Ignora IconName (metadados migraram para TrickTable.csv)
- **Impacto:** ✅ Parser simplificado
- **Nota:** PerkTable.csv removeu colunas Name, Description, IconName

#### PerkCSVImporter.cs
- **Antes:** Copy() copiava DisplayName, Description, Icon
- **Depois:** Copy() apenas copia mecânicas (Id, Rules, Tags, Stacks, etc)
- **Impacto:** ✅ Sem breaking changes, apenas campos ignorados

### ✅ Camada de View (UI Layer)

#### PerkActivationFeedback.cs
- **Antes:** Exibia `evt.PerkName` no popup
- **Depois:** Exibe `evt.PerkId` (sem acesso a DisplayName)
- **Impacto:** ⚠️ Feedback visual menos legível - considerar resolver via TrickDatabase
- **Solução Futura:** Integrar TrickDatabase para exibir nome do Trick associado

#### PerkIconUI.cs
- **Antes:** `icon.sprite = definition.Icon`
- **Depois:** Icon não existe mais em PerkSO, apenas exibe perkIdText com ID
- **Impacto:** ✅ Exibe ID do perk em vez de ícone
- **Nota Importante:** Não deveria mais ser usado para Perks - PerkDisplayView agora apenas mostra stacks

#### PerkTooltip.cs
- **Antes:** Exibia DisplayName, Description, Icon
- **Depois:** Exibe apenas Id e Tags
- **Impacto:** ✅ Tooltip simplificado, apenas info técnica

### ✅ Novos Arquivos (Tricks)

#### TrickIconUI.cs (Novo)
- Exibe Trick com Icon, Level, Cost, Rarity
- Usa TrickSO como source de metadados
- Integrado com TrickTooltip
- **Status:** ✅ Completo

#### TrickTooltip.cs (Novo)
- Exibe informações completas do Trick
- Usa TrickSO.DisplayName, Description, Icon
- **Status:** ✅ Completo

#### TrickDisplayView.cs (Novo)
- Grid de Tricks disponíveis para cast
- Integra com TrickService e TrickDatabase
- **Status:** ✅ Completo
- **Integração Faltante:** Precisa ser adicionado a CombatView/BattlerPanelView

---

## Modificações Necessárias (Próximas Tarefas)

### 1. BattlerPanelView.cs - ⏳ PENDENTE
```csharp
// Adicionar:
[SerializeField] private TrickDisplayView trickDisplayView;

// Em Bind():
if (trickDisplayView != null)
    trickDisplayView.Initialize(battler, TrickService); // Obter TrickService de CombatManager
```

### 2. TrickDisplayView Integration - ⏳ PENDENTE
- Adicionar listener no TrickIconUI para chamar CombatManager.OnPlayerSelectTrick()
- Conectar botão de cast (castTrickButton) ao método CastSelectedTrick()

### 3. Debug Logs - ⏳ Opcional
- **Impacto Atual:** Logs mostram PerkId em vez de DisplayName (menos legível)
- **Solução:** Integrar TrickDatabase para resolver PerkId → TrickId → DisplayName
- **Exemplo Melhorado:**
  ```csharp
  var trick = TrickDatabase.GetTrickByPerkId(perk.Definition.Id);
  Debug.Log($"[Perk Triggered] {trick?.DisplayName ?? perk.Definition.Id}");
  ```

### 4. CombatView Setup - ⏳ PENDENTE
- Inicializar TrickDisplayView em Start()
- Passar referência a TrickService de CombatManager

---

## Estrutura Atual Após Refactor

```
Perk System (Mecânicas Puras):
├─ PerkSO.cs [Simplificado]
│  └─ Id, Rules, Tags, MaxStacks, DefaultDurationTurns
├─ PerkTriggeredEvent [Simplificado]
│  └─ PerkId (sem PerkName)
├─ PerkTriggerEvaluator [Atualizado]
│  └─ Usa perk.Definition.Id
├─ PerkActivationFeedback [Atualizado]
│  └─ Usa evt.PerkId
└─ PerkDisplayView / PerkIconUI / PerkTooltip
   └─ Mostram apenas mecânicas (stacks, id, tags)

Trick System (Interface + Metadados):
├─ TrickSO.cs
│  └─ DisplayName, Description, Icon, Level, Costs, Timing, Rarity, Tags
├─ TrickDatabase.cs [Existente]
├─ TrickService.cs [Existente]
├─ TrickIconUI.cs [Novo] ✅
├─ TrickTooltip.cs [Novo] ✅
└─ TrickDisplayView.cs [Novo] ✅

Integração:
└─ CombatView / BattlerPanelView [Pendente adição de TrickDisplayView]
```

---

## Summary de Breaking Changes

| Arquivo | Campo Removido | Impacto | Solução |
|---------|---|---|---|
| PerkSO | DisplayName | Perks sem nome legível | Use TrickDatabase para resolver |
| PerkSO | Description | Info técnica removida | Metadados em TrickSO |
| PerkSO | Icon | Sem visual de Perk | Tricks têm ícone |
| PerkTriggeredEvent | PerkName | Listeners devem adaptar | Use PerkId ou TrickDatabase |
| PerkActivationFeedback | evt.PerkName | Feedback menos legível | Integrar TrickDatabase |

---

## Testes Recomendados

1. ✅ Importar PerkTable.csv (menu Tools > Import Perks CSV)
2. ✅ Verificar se PerkSO assets foram criados sem erros
3. ⏳ Testar gameplay com perks funcionando (triggers, stacks)
4. ⏳ Verificar tooltips de perks
5. ⏳ Testar TrickDisplayView renderizando tricks
6. ⏳ Testar casting de tricks
7. ⏳ Verificar feedback visual de trigger de perks

---

## Conclusão

**Status Geral:** ✅ 85% Completo
- ✅ Refactor de Perks: Completo
- ✅ Criação de Tricks UI: Completo  
- ⏳ Integração UI em CombatView: Faltando
- ⏳ Testes End-to-End: Faltando
