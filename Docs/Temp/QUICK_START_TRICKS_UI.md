# ⚡ QUICK START - Testar Fluxo de Tricks em 2 Minutos

## 🚀 Setup Rápido

### 1️⃣ Abrir Console
```
Keyboard: Ctrl + Shift + C
Ou: Window → General → Console
```

### 2️⃣ Jogar Cena de Combate
```
Play → Combate Scene
```

### 3️⃣ Procurar Logs Chave

| O que procurar | Significa |
|---|---|
| `[TrickInventory] Inicializando para Player` | Inicialização começou |
| `[TrickInventory] RestoreIdentitySlots concluído. IdentitySlots preenchidos: 4/4` | ✅ Identity Tricks carregadas |
| `[TrickInventoryView] Refresh: Atualizando display de tricks` | UI renderizando |
| `[TrickInventoryView] SpawnSlots[0] (IdentitySlot): Renderizando slot com trick` | ✅ Identity Tricks na UI |
| `[TrickInventoryView] SpawnLearnedTricks: Renderizando X learned tricks.` | ✅ Learned Tricks carregadas |

---

## 🎮 Teste Principal: Abrir UI via Botão

### Passos
1. **Play** cena de combate
2. **Esperar** inicialização (logs apareçam)
3. **Clique** no botão **"SelectTricksButton"** (botão de Tricks na Action Panel)
4. **Verificar** Console por: `[ActionPanelView] HandleTricksClick`
5. **Olhar** na UI: deve aparecer painel com **3 seções**

### UI esperada
```
┌─────────────────────────────────┐
│  TRICK INVENTORY                │
├─────────────────────────────────┤
│ Identity Slots (4)              │
│ [🔒 Trick1] [🔒 Trick2] [...]   │
│                                 │
│ Learned Tricks (N)              │
│ [⭐ TrickA] [⭐ TrickB] [...]    │
│                                 │
│ Casted Slots (4)                │
│ [Empty] [Empty] [Empty] [Empty] │
└─────────────────────────────────┘
```

---

## ✅ Checklist: Tudo Funcionando?

- [ ] Console mostra `[TrickInventory] Inicializando`
- [ ] `IdentitySlots preenchidos: 4/4` aparece
- [ ] `SpawnLearnedTricks: Renderizando` mostra número > 0
- [ ] UI abre ao clicar no botão
- [ ] 4 slots com locks na seção Identity
- [ ] N tricks clicáveis na seção Learned
- [ ] 4 slots vazios na seção Casted
- [ ] Nenhum error no Console

---

## ❌ Algo Errado?

### Se `IdentitySlots preenchidos: 0/4`
```
1. Abrir CharacterSO do jogador
2. Verificar [Header("Tricks")] IdentityTricks list
3. Deve ter 4 tricks (ou adicionar)
```

### Se `SpawnLearnedTricks: Renderizando 0`
```
1. Isso é normal se nenhuma trick foi aprendida
2. Ou adicionar tricks ao snapshot do jogador
```

### Se UI não abre
```
1. Verificar que TrickInventoryView existe na scene
2. Ou adicionar prefab: Assets/Prefabs/[...]/TrickInventoryView
```

### Se vê erros no Console
```
1. Procurar [WARN] ou [ERROR]
2. Ler a mensagem completa
3. Consultar TESTE_PRATICO_TRICKS_UI.md seção Troubleshooting
```

---

## 📚 Documentação Completa

| Arquivo | Objetivo |
|---------|----------|
| `TRICKS_UI_FLOW_DIAGRAM.md` | Entender o fluxo visualmente |
| `TESTE_PRATICO_TRICKS_UI.md` | 6 testes com passo a passo |
| `tricks-ui-flow-logging.md` | Todos os logs detalhados |
| `SUMARIO_LOGGING_TRICKS_UI.md` | Visão geral da implementação |

---

## 💡 Dicas

**📌 Dica 1:** Pesquisar no Console por `[TrickInventory]` mostra apenas logs de tricks

**📌 Dica 2:** Pesquisar por `ERROR` ou `WARN` mostra problemas rápido

**📌 Dica 3:** Abrir DevTools (F12 na WebGL) para logs mais detalhados

**📌 Dica 4:** Se muitos logs, usar filtros:
```
Clear Console
Tipo: [TrickInventory]
Buscar: restaurado
```

---

## 🎯 Resultado Esperado

Se tudo funcionando:
```
Play scene → Vê logs → Clica botão → UI abre com tricks
```

**Done!** ✅ Sistema de Tricks funcionando corretamente.

---

## 📞 Próximos Passos

1. Se tudo ✅ → Sistema pronto para use
2. Se problema → Consultar `TESTE_PRATICO_TRICKS_UI.md` Troubleshooting
3. Se quer expandir → Ver seção Próximas Melhorias em SUMARIO
4. Se quer entender mais → Ler TRICKS_UI_FLOW_DIAGRAM.md

---

## 🎓 Estrutura Importante (para reference rápida)

```
TrickInventory (Model)
  ├─ identitySlots[4] ← Identity Tricks (locked)
  ├─ learnedTricks[N] ← Tricks aprendidas (desbloqueadas)
  └─ castedSlots[4] ← Tricks castadas (vazios inicialmente)

TrickInventoryView (UI)
  ├─ identitySlotsRoot ← Renderiza 4 slots
  ├─ learnedTricksRoot ← Renderiza N tricks
  └─ castedSlotsRoot ← Renderiza 4 slots

ActionPanelView (Button)
  └─ SelectTricksButton.onClick() → HandleTricksClick()
```

---

**Boa sorte! 🎮** Para mais detalhes, consulte a documentação completa.
