# Guia: Segundo Cérebro no Obsidian para o Projeto

## 1. Plugins essenciais (instalar antes de começar)

| Plugin | Para que serve |
|---|---|
| **Dataview** | Consultas dinâmicas (ex: listar todas as tarefas abertas, todos os SOs de um sistema) |
| **Templater** | Templates com variáveis automáticas (data, nome do arquivo) |
| **Kanban** | Board de tarefas (To Do / Doing / Done) por sistema |
| **Excalidraw** ou **Canvas nativo** | Diagramas de fluxo (MVP, dice resolution, etc.) |
| **Tag Wrangler** | Gerenciar tags sem bagunça |

Core plugins do próprio Obsidian que já bastam para começar: **Backlinks**, **Graph View**, **Outgoing Links**.

---

## 2. Estrutura de pastas (Macro → Micro)

```
00_Home.md                     ← MOC principal (ponto de entrada)
01_Macro/
  Visao_Geral_Projeto.md
  Arquitetura_MVP.md
  Roadmap.md
02_Sistemas/
  Combate/
    MOC_Combate.md
    Dice_Resolution.md
    Momentum.md
    Tricks_Perks.md
    Action_Resolution.md
    Feedback_System.md
  Exploracao/
    MOC_Exploracao.md
    Tension_Presence.md
  UI/
    MOC_UI.md
    Tooltip_System.md
03_Micro/
  Classes/
    DiceRollView.md
    ActionResolverService.md
    Battler.md
    ...
  ScriptableObjects/
    TrickSO.md
    CombatVariationSO.md
04_Tarefas/
  Backlog.md
  Board_Kanban.md
05_Decisoes_e_Aprendizados/
  Principios_Tecnicos.md
  Decisoes_Arquiteturais.md
99_Templates/
  Template_Sistema.md
  Template_Classe.md
  Template_Tarefa.md
```

**Lógica**: pastas numeradas fixam a ordem no explorador de arquivos. `01_Macro` é a visão de cima; `03_Micro` é o nível de código; `04_Tarefas` é operacional; `05_Decisoes` evita repetir discussões já resolvidas.

---

## 3. MOCs (Maps of Content) — o "índice vivo"

Uma MOC é uma nota que só linka outras notas, servindo de sumário navegável. Não é uma pasta — é um arquivo `.md` com links.

**00_Home.md** (exemplo):
```markdown
# Home

## Macro
- [[Visao_Geral_Projeto]]
- [[Arquitetura_MVP]]
- [[Roadmap]]

## Sistemas
- [[MOC_Combate]]
- [[MOC_Exploracao]]
- [[MOC_UI]]

## Operacional
- [[Backlog]]
- [[Board_Kanban]]

## Decisões
- [[Principios_Tecnicos]]
```

Cada MOC de sistema (ex: `MOC_Combate.md`) lista as sub-notas daquele sistema e o status geral (✅ estável / 🔧 em refino / 📋 planejado).

---

## 4. Templates recomendados

### `Template_Sistema.md`
```markdown
# {{title}}

**Status:** 📋 Planejado / 🔧 Em desenvolvimento / ✅ Estável
**Camada MVP envolvida:** Model / View / Presenter

## Objetivo
(o que esse sistema resolve)

## Componentes principais
- 

## Decisões de design
- 

## Pendências
- [ ] 

## Notas relacionadas
- 
```

### `Template_Classe.md`
```markdown
# {{title}}

**Camada:** Model / View / Presenter
**Arquivo:** `Assets/Scripts/.../{{title}}.cs`

## Responsabilidade

## Dependências (o que ela usa)

## Usada por (o que depende dela)

## Métodos-chave
- 

## Pendências / TODO
- [ ] 
```

### `Template_Tarefa.md`
```markdown
---
status: todo
sistema: 
prioridade: media
---

# {{title}}

## Contexto

## Critério de conclusão
- [ ] 
```

O front matter (`status`, `sistema`, `prioridade`) é o que permite ao Dataview montar listas automáticas depois.

---

## 5. Visão macro vs micro na prática

- **Macro**: `Arquitetura_MVP.md` descreve a separação Model/View/Presenter em geral e linka para cada MOC de sistema. Não entra em detalhe de classe.
- **Meso** (nível sistema): `MOC_Combate.md` explica como Dice Resolution, Momentum, Tricks e Action Resolution se conectam entre si.
- **Micro**: cada classe (`DiceRollView.md`, `ActionResolverService.md`) tem sua nota própria com responsabilidade, dependências e pendências.

Regra prática: se a informação muda quando você mexe em uma única classe, ela é micro. Se muda quando o sistema inteiro é redesenhado, é macro/meso.

---

## 6. Gestão de tarefas com Dataview

Em `04_Tarefas/Backlog.md`, uma query automática:

````markdown
```dataview
TASK
FROM "02_Sistemas" OR "03_Micro"
WHERE !completed
GROUP BY file.link
```
````

Isso varre todas as notas do projeto e lista os checkboxes `- [ ]` abertos, agrupados por arquivo — sem precisar manter uma lista central manualmente.

Para o Kanban plugin, crie `Board_Kanban.md` com colunas **Backlog / Em Progresso / Concluído**, e arraste cartões referentes a cada pendência (pode linkar a nota da tarefa dentro do cartão).

---

## 7. Passo a passo para começar (primeira sessão, ~1h)

1. Criar o vault e instalar os plugins da seção 1.
2. Criar a estrutura de pastas da seção 2 (vazia).
3. Criar `00_Home.md` e os templates da seção 4.
4. Escrever `Arquitetura_MVP.md` com o que já está consolidado (a divisão Model/View/Presenter que você já usa).
5. Criar `MOC_Combate.md` e, a partir dela, uma nota por sistema já existente: Dice Resolution, Momentum, Tricks/Perks, Action Resolution, Feedback System — só o resumo do estado atual, sem detalhar cada classe ainda.
6. Criar `Backlog.md` com a query do Dataview e migrar as pendências conhecidas (ex: `ActionResolutionResult.cs` precisa trocar `bool PowerMax` por `PowerMaxSource`).
7. Só depois, ir preenchendo `03_Micro/Classes` conforme for mexendo em cada arquivo — não tente documentar tudo de uma vez.

---

## 8. Regra de manutenção contínua

- Toda vez que uma sessão de implementação terminar, atualize a nota do sistema/classe afetado (status + pendências), não crie nota nova para cada sessão.
- Decisões técnicas recorrentes (ex: "tier ancorado ao baseValue") vão para `Principios_Tecnicos.md`, não dentro da nota da classe — evita duplicar a mesma explicação em vários lugares.
- Uma vez por semana (ou a cada marco), revisitar as MOCs para ver se o grafo ainda reflete a realidade do projeto.
