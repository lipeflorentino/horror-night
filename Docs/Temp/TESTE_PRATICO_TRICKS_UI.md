# 🧪 GUIA PRÁTICO DE TESTE - Fluxo de Tricks UI

## ✅ Pré-requisitos para Teste

### 1. Verificar CharacterSO do Jogador
- [ ] Abrir `Assets/Resources/Data/Characters/` (ou onde estiver)
- [ ] Selecionar SO do jogador
- [ ] Na seção `[Header("Tricks")]`, verificar campo `IdentityTricks`
- [ ] Confirmar que há **exatamente 4 tricks** (ou menos, se design permite)
  - Exemplo: `[Fireball, Frostbolt, Lightning, Heal]`
- [ ] Se vazio, adicionar tricks antes do teste

### 2. Preparar Scene de Teste
- [ ] Ter uma cena de combate funcional (ou usar cena existente)
- [ ] Confirmar que a cena tem:
  - [x] `CombatManager` GameObject
  - [x] `ActionPanelView` (com botão SelectTricksButton)
  - [x] `TrickInventoryView` (painel com grids de identity/learned/casted)
  - [x] `TrickInventoryInputHandler` (será criado automaticamente se não existir)

### 3. Abrir Console Unity
- [ ] Keyboard: `Ctrl + Shift + C`
- [ ] Ou: Windows → General → Console

---

## 🎬 TESTE 1: Inicialização e Identity Tricks

### Objetivo
Confirmar que Identity Tricks são carregadas corretamente na inicialização do combate.

### Passos
1. **Jogar a cena** de combate
2. **Aguardar inicialização** (alguns frames)
3. **No Console, procurar por esta sequência de logs:**

```
[TrickInventory] Inicializando para Player. IdentitySlotCount=4, CastedSlotCount=4
[TrickInventory] RestoreIdentitySlots: Restaurando 4 identity tricks para Player
[TrickInventory] IdentitySlot[0]: Restaurado trick 'XXX' (ID: trick_xxx)
[TrickInventory] IdentitySlot[1]: Restaurado trick 'YYY' (ID: trick_yyy)
[TrickInventory] IdentitySlot[2]: Restaurado trick 'ZZZ' (ID: trick_zzz)
[TrickInventory] IdentitySlot[3]: Restaurado trick 'WWW' (ID: trick_www)
[TrickInventory] RestoreIdentitySlots concluído. IdentitySlots preenchidos: 4/4
```

### ✅ Critério de Sucesso
- Todos os logs acima aparecem
- Não há `LogWarning` ou `LogError` relacionados a Tricks
- `IdentitySlots preenchidos: 4/4` mostra na última linha

### ❌ Se Falhar
| Log Não Aparece | Problema | Solução |
|-----------------|----------|---------|
| `RestoreIdentitySlots: Restaurando 4...` | Snapshot vazio | Verificar `CharacterSO.IdentityTricks` |
| `IdentitySlot[X]: Slot vazio` para todos | Tricks não no database | Verificar `TrickDatabase` |
| `IdentitySlots preenchidos: 0/4` | Nenhuma trick restaurada | Verificar se IDs no SO batem com database |

---

## 🎬 TESTE 2: Verificar LearnedTricks no Snapshot

### Objetivo
Confirmar que Learned Tricks (aprendidas fora do combate) também são carregadas.

### Passos
1. **Manter o combate rodando** do Teste 1
2. **Procurar no Console por:**

```
[TrickInventory] RestoreLearnedTricks: Restaurando X learned tricks para Player
[TrickInventory] LearnedTrick[0]: Trick 'AAA' (ID: trick_aaa) - Aprendido: True
[TrickInventory] LearnedTrick[1]: Trick 'BBB' (ID: trick_bbb) - Aprendido: True
[TrickInventory] RestoreLearnedTricks concluído. Total de learned tricks: X
```

### ✅ Critério de Sucesso
- Log mostra número de learned tricks > 0
- Todas as tricks foram aprendidas ("Aprendido: True")

### ℹ️ Nota
Se não há learned tricks no snapshot, isso é esperado (logs não aparecem ou mostram "Restaurando 0").

---

## 🎬 TESTE 3: Ativar Identity Tricks (Aplicar Perks)

### Objetivo
Confirmar que Identity Tricks são ativadas e seus Perks aplicados ao jogador.

### Passos
1. **Manter o combate rodando** do Teste 1
2. **Procurar no Console por:**

```
[CombatManager] ActivatePlayerIdentityTricks: Ativando identity tricks para Player
[CombatManager] ActivatePlayerIdentityTricks[0]: Ativando 'XXX' (ID: trick_xxx)
[CombatManager] ActivatePlayerIdentityTricks[1]: Ativando 'YYY' (ID: trick_yyy)
[CombatManager] ActivatePlayerIdentityTricks[2]: Ativando 'ZZZ' (ID: trick_zzz)
[CombatManager] ActivatePlayerIdentityTricks[3]: Ativando 'WWW' (ID: trick_www)
[CombatManager] ActivatePlayerIdentityTricks: Conclusão. Identity tricks ativadas: 4/4
```

### ✅ Critério de Sucesso
- Todos os 4 identity tricks aparecem como ativados
- `Identity tricks ativadas: 4/4` na última linha

---

## 🎬 TESTE 4: TrickInventoryInputHandler Binding

### Objetivo
Confirmar que o handler foi inicializado e vinculado corretamente.

### Passos
1. **Manter o combate rodando** do Teste 1
2. **Procurar no Console por:**

```
[CombatManager] Start: Inicializando TrickInventoryInputHandler com PlayerTrickInventory...
[TrickInventoryInputHandler] Init: Inicializando TrickInventoryInputHandler.
[TrickInventoryInputHandler] Init: Inventário de Tricks do jogador: encontrado. TrickInventoryView: encontrada.
[TrickInventoryInputHandler] Init: Vinculando inventário à TrickInventoryView...
[TrickInventoryView] BindInventory: Vinculando novo inventário de tricks. Anterior: não, Novo: sim
[TrickInventoryView] BindInventory: Inscrito ao evento OnChanged.
[TrickInventoryInputHandler] Init: Inscrito a OnInteractWithInventoryTrick com sucesso.
```

### ✅ Critério de Sucesso
- Todos os logs acima aparecem **na ordem**
- Nenhum `LogWarning` "TrickInventoryView não encontrada"

### ❌ Se Falhar
| Log não aparecer | Problema | Solução |
|------------------|----------|---------|
| "encontrado" na View | TrickInventoryView não na scene | Adicionar prefab da UI na scene |
| "Inscrito a OnInteractWithInventoryTrick" | Handler não subscreveu event | Verificar se BindInventory foi chamado |

---

## 🎬 TESTE 5: Abrir TrickInventory via Button (PRINCIPAL)

### Objetivo
Teste completo: clicar no botão SelectTricksButton abre a UI e renderiza os slots corretamente.

### Passos

#### Passo 1: Procurar pelo Console
1. **Manter o combate rodando**
2. **Procurar por esta sequência:**

```
[TrickInventoryView] Refresh: Atualizando display de tricks. boundInventory null: False
[TrickInventoryView] Refresh: Iniciando spawn de slots. IdentitySlots=4, LearnedTricks=3, CastedSlots=4
```

#### Passo 2: Verificar Identity Slots
Procurar por:
```
[TrickInventoryView] SpawnSlots: Renderizando 4 slots para IdentitySlot. Parent: identitySlotsRoot
[TrickInventoryView] SpawnSlots[0] (IdentitySlot): Renderizando slot com trick 'XXX' (ID: trick_xxx). IsLocked: True
[TrickInventoryView] SpawnSlots[1] (IdentitySlot): Renderizando slot com trick 'YYY' (ID: trick_yyy). IsLocked: True
[TrickInventoryView] SpawnSlots[2] (IdentitySlot): Renderizando slot com trick 'ZZZ' (ID: trick_zzz). IsLocked: True
[TrickInventoryView] SpawnSlots[3] (IdentitySlot): Renderizando slot com trick 'WWW' (ID: trick_www). IsLocked: True
[TrickInventoryView] SpawnSlots: Conclusão para IdentitySlot. Parent children count: 4
```

**✅ Validação:** 4 slots renderizados com tricks, todos marcados como `IsLocked: True`

#### Passo 3: Verificar LearnedTricks
Procurar por:
```
[TrickInventoryView] SpawnLearnedTricks: Renderizando 3 learned tricks.
[TrickInventoryView] SpawnLearnedTricks[0]: Renderizando learned trick 'AAA' (ID: trick_aaa)
[TrickInventoryView] SpawnLearnedTricks[1]: Renderizando learned trick 'BBB' (ID: trick_bbb)
[TrickInventoryView] SpawnLearnedTricks[2]: Renderizando learned trick 'CCC' (ID: trick_ccc)
[TrickInventoryView] SpawnLearnedTricks: Conclusão. LearnedTricksRoot children count: 3
```

**✅ Validação:** N learned tricks renderizadas (N = quantidade aprendida)

#### Passo 4: Verificar CastedSlots
Procurar por:
```
[TrickInventoryView] SpawnSlots: Renderizando 4 slots para CastedSlot. Parent: castedSlotsRoot
[TrickInventoryView] SpawnSlots[0] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[1] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[2] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots[3] (CastedSlot): Slot vazio.
[TrickInventoryView] SpawnSlots: Conclusão para CastedSlot. Parent children count: 4
```

**✅ Validação:** 4 slots vazios inicialmente

#### Passo 5: Verificar UI Visualmente
1. **Em Game View (não Scene View)**
2. **Procurar pelo painel TrickInventoryView**
3. **Verificar:**
   - [ ] **IdentitySlots section:** 4 ícones com locks (não clicáveis)
   - [ ] **LearnedTricks section:** 3+ ícones (clicáveis)
   - [ ] **CastedSlots section:** 4 slots vazios

### ✅ Critério de Sucesso COMPLETO
- [x] Console mostra todos os logs acima sem erros
- [x] Identity Tricks: 4 slots com locks visíveis na UI
- [x] Learned Tricks: N slots visíveis, clicáveis
- [x] Casted Slots: 4 slots vazios
- [x] Nenhum erro relacionado a Tricks no Console

---

## 🎬 TESTE 6: Cast uma Trick (Opcional - Avançado)

### Objetivo
Confirmar que clicar em uma Learned Trick permite castá-la.

### Passos
1. **Manter o combate rodando**
2. **Na UI, clicar em uma Learned Trick** (ex: "Summon")
3. **No Console, procurar por:**

```
[TrickInventoryView] HandleTrickSelected: ...
[TrickInventoryView] ShowInteractionPanel(true)
```

4. **Clicar no botão "CAST" no painel de detalhes**
5. **No Console, procurar por:**

```
[TrickInventoryInputHandler] HandleTrickInteraction: Ação 'Cast' no trick 'Summon' (ID: trick_summon) em LearnedTricks.
[TrickInventoryInputHandler] OnCastTrick: Tentando castar 'Summon' (ID: trick_summon).
[TrickInventory] CastTrick: Tentando castar 'Summon' (ID: trick_summon) para Player
[TrickInventory] CastTrick: Slot encontrado no índice 0 para 'Summon'.
[TrickInventory] CastTrick: Custos consumidos. Mind-=5, Body-=0, Heart-=3.
[TrickInventory] CastTrick: 'Summon' castado com sucesso no slot 0. Perks aplicados.
```

6. **Na UI, verificar que CastedSlots[0] agora mostra a trick castada**

### ✅ Critério de Sucesso
- Logs mostram cast bem-sucedido
- UI atualiza CastedSlots com a trick
- LearnedTricks ainda mostra a trick (pode ser castada novamente se não em cooldown)

---

## 📋 Checklist Final

Use esta checklist para validar o fluxo completo:

### Startup (Inicial)
- [ ] Log: `[TrickInventory] Inicializando para Player`
- [ ] Log: `RestoreIdentitySlots: Restaurando 4 identity tricks`
- [ ] Log: Cada IdentitySlot mostra uma trick restaurada
- [ ] Log: `IdentitySlots preenchidos: 4/4`
- [ ] Log: `ActivatePlayerIdentityTricks: Ativando identity tricks`
- [ ] Log: Cada Identity Trick ativada com sucesso

### Binding
- [ ] Log: `TrickInventoryInputHandler` inicializado
- [ ] Log: `BindInventory` vinculada à View
- [ ] Log: `Inscrito ao evento OnChanged`
- [ ] Log: `Inscrito a OnInteractWithInventoryTrick`

### UI Rendering
- [ ] Log: `Refresh: Atualizando display`
- [ ] Log: 4 IdentitySlots renderizados com tricks e locked
- [ ] Log: N LearnedTricks renderizadas
- [ ] Log: 4 CastedSlots vazios
- [ ] Log: `Total de slots renderizados: 4 + N + 4`

### Visual (Game View)
- [ ] IdentitySlots visíveis com ícones e locks
- [ ] LearnedTricks visíveis e clicáveis
- [ ] CastedSlots visíveis e vazios
- [ ] TrickInfoPanel aparece ao clicar em trick

### Nenhum Error/Warning
- [ ] Nenhum `[WARN]` ou `[ERROR]` no Console relacionado a Tricks
- [ ] Logs indicam sucesso em cada etapa

---

## 🐛 Troubleshooting

### Cenário: "IdentitySlots preenchidos: 0/4"
**Diagnóstico:** Snapshot vazio ou tricks não no database

**Solução:**
1. Abrir `CharacterSO` do jogador
2. Verificar se `IdentityTricks` list tem 4 tricks
3. Verificar se essas tricks existem em `TrickDatabase`
   - Procurar: `TrickDatabase.TrickTable.csv` ou `Resources/Data/Tricks/`
4. Se faltam, importar tricks via CSV importer

---

### Cenário: "TrickInventoryView não encontrada"
**Diagnóstico:** Prefab da UI não na scene

**Solução:**
1. Na scene de combate, procurar por `TrickInventoryView` GameObject
2. Se não existir:
   - [ ] Procurar prefab em `Assets/Resources/Prefabs/` ou similar
   - [ ] Drag & drop na scene
   - [ ] Ou criar novo GameObject e adicionar script `TrickInventoryView`

---

### Cenário: LearnedTricks lista vazia
**Diagnóstico:** Jogador não aprendeu tricks fora do combate

**Solução:**
1. Isso é esperado se o jogo acaba de começar
2. Para teste, adicionar tricks manualmente ao snapshot:
   - [ ] Em `CombatSessionData.PlayerSnapshot.trickInventory.learnedTrickIds`
   - [ ] Adicionar IDs de tricks existentes no database

---

## 📞 Contato / Próximas Etapas

Se todos os testes passarem:
- ✅ Sistema de Tricks inicializando corretamente
- ✅ Identity Tricks carregadas e ativas
- ✅ Learned Tricks exibidas na UI
- ✅ Casting funcional (teste 6)

Se alguns falharem:
- [ ] Verificar logs em `/memories/session/tricks-ui-flow-logging.md`
- [ ] Revisar diagrama em `TRICKS_UI_FLOW_DIAGRAM.md`
- [ ] Conferir se database de tricks está preenchido
