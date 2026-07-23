# Resumo do Sistema de Dados, Tiers e Thresholds

Exemplo numérico usado em todo o documento: **Mind = Heart = Body = 12** (valor base do personagem, fixo, definido na criação).

---

## 1. Visão Macro — Arquitetura e Fluxo

| Etapa | Componente | Responsabilidade | Saída |
|---|---|---|---|
| 1. Seleção do jogador | `CombatInputHandler` (Presenter) | Guarda `PowerDiceTypes`/`AccuracyDiceTypes` (lista bruta de cliques) | Listas de `DiceStatType` |
| 2. Conversão em specs | `DiceService.BuildDiceRollSpecs` | Divide a stat base entre dados alocados + adiciona dados extras de perk | Lista de `DiceRollSpec` (min/max face por dado) |
| 3. Alvo de referência | `DiceService.GetTierReferenceMaxValue` | Define o "teto" de cada stat = **valor BASE**, nunca a soma das faces | Um inteiro por stat (ex.: 12) |
| 4. Thresholds | `DiceService.GetThresholds` | Calcula os cortes Low/Medium/High (0.0–1.0), aplicando modificadores | `(Low, High)` normalizado |
| 5. Boundaries | `DiceService.GetTierBoundaries` | Converte os cortes normalizados em valores inteiros (`lowMax`, `mediumMax`, `highMin`) | `(lowMax, mediumMax, highMin, maxValue)` |
| 6. Rolagem real | `DiceService.RollMany` + `CombatDiceRollManager` | Rola os dados, agrega por tipo, escolhe o melhor grupo | `DiceResult` final |
| 7. Preview (UI) | `CombatInputHandler` + `DiceAllocationView` | Recalcula tudo acima *sem rolar dados de verdade*, para mostrar chances | Texto do painel de resultado |

**Regra de ouro do sistema**: o teto usado para calcular tiers (Low/Medium/High) é sempre o **valor base da stat**, nunca a soma das faces dos dados alocados. Isso é o que permite que dados extras de perk ajudem sem "inflar" o alvo.

---

## 2. Alocação de Dados: Base vs. Extra

Quando o jogador aloca N dados do mesmo tipo, eles **dividem** o valor da stat entre si. Dados extras (concedidos por perk) **não dividem** — cada um usa o valor cheio da stat como sua própria face.

**Exemplo: Heart = 12**

| Dados alocados pelo jogador (mesmo tipo) | Face de cada dado base | Dado extra de perk (se houver) |
|---|---|---|
| 1 | `1d12` | `+1d12` |
| 2 | `2× 1d6` | `+1d12` |
| 3 | `3× 1d4` (resto distribuído se não dividir exato) | `+1d12` |
| 4 | `4× 1d3` | `+1d12` |

> Dividir a stat em mais dados **não muda o valor total possível**, mas muda a *forma* da distribuição — menos dados = mais variância (mais fácil bater no extremo, mas dado único também é mais fácil de falhar).

---

## 3. Cálculo de Tiers (Low / Medium / High)

Fórmula base (antes de qualquer modificador):

```
Low threshold  = 0.25
High threshold = 0.75
normalized = valor_rolado / valor_base_da_stat
```

- `normalized ≤ Low` → **Low**
- `Low < normalized ≤ High` → **Medium**
- `normalized > High` → **High**

### Baseline sem nenhum modificador (stat = 12, ≥3 dados alocados, sem diferença de nível, sem Focus/Strength)

| lowMax | mediumMax | highMin | maxValue (referência) |
|---|---|---|---|
| 3 | 9 | 10 | 12 |

Ou seja: rolar **1–3 = Low**, **4–9 = Medium**, **10–12 = High**.

---

## 4. Modificadores de Threshold

Todos os modificadores abaixo **deslocam** os cortes Low/High antes de virarem `lowMax`/`mediumMax`/`highMin`. Tabelas isolam cada variável (mantendo as demais no baseline).

### 4.1 Quantidade de dados alocados ("risco")

Alocar poucos dados do mesmo tipo aumenta a variância: alarga a banda Low **e** a banda High ao mesmo tempo (mais extremos, menos Medium).

| Dados alocados | risk | Low | High | lowMax | mediumMax | highMin |
|---|---|---|---|---|---|---|
| 1 | 1.0 | 0.35 | 0.65 | 4 | 7 | 8 |
| 2 | 0.5 | 0.30 | 0.70 | 3 | 8 | 9 |
| 3+ | 0.0 | 0.25 | 0.75 | 3 | 9 | 10 |

### 4.2 Diferença de nível (atacante − defensor)

Com stat=12, a granularidade máxima da stat gera `deltaScale = 4` e `maxShift = 0.18` (valores no teto da escala, por ser stat "grande").

| Delta de nível | Low | High | lowMax | mediumMax | highMin |
|---|---|---|---|---|---|
| −4 (defensor muito mais forte) | 0.43 | 0.93 | 5 | 11 | 12 |
| 0 | 0.25 | 0.75 | 3 | 9 | 10 |
| +2 | 0.16 | 0.66 | 1 | 7 | 8 |
| +4 (atacante muito mais forte) | 0.07\* | 0.57 | 0\*\* | 6 | 7 |

\* Clamp mínimo é 0.05, então valores mais extremos ficam presos em 0.07 aqui (não chegou no clamp).
\*\* `lowMax = 0` significa que **não existe resultado Low** — até o pior valor cai em Medium ou melhor.

### 4.3 Combat stat (Focus para Accuracy / Strength para Power)

Cada ponto desloca ambos os thresholds para baixo em 0.05 (facilita Medium e High).

| Focus/Strength | shift | Low | High | lowMax | mediumMax | highMin |
|---|---|---|---|---|---|---|
| 0 | 0.00 | 0.25 | 0.75 | 3 | 9 | 10 |
| 1 | 0.05 | 0.20 | 0.70 | 2 | 8 | 9 |
| 2 | 0.10 | 0.15 | 0.65 | 1 | 7 | 8 |
| 4 | 0.20 | 0.05\* | 0.55 | 0 | 6 | 7 |

\* Clamp mínimo atingido (0.05).

### 4.4 Sinergia por stat (somente em rolagens de Accuracy)

| Stat | Low shift | High shift | Efeito |
|---|---|---|---|
| Mind | −0.05 | +0.05 | Banda Medium mais larga → mais consistente (menos Miss, menos Crítico) |
| Heart | +0.05 | −0.05 | Banda Medium mais estreita → mais extremo (mais Miss, mais Crítico) |
| Body | +0.03 | 0 | Leve penalidade de consistência, sem ganho de Crítico |

| Stat | Low | High | lowMax | mediumMax | highMin |
|---|---|---|---|---|---|
| Mind | 0.20 | 0.80 | 2 | 9 | 10 |
| Heart | 0.30 | 0.70 | 3 | 8 | 9 |
| Body | 0.28 | 0.75 | 3 | 9 | 10 |

### 4.5 Clamps finais (sempre aplicados por último)

- `Low` final é travado entre **0.05** e **0.45**.
- `High` final é travado entre **0.55** e **0.95**.
- Se `High < Low + 0.20`, `High` é forçado para `Low + 0.20` (garante banda Medium mínima).
- Perks podem ainda modificar os thresholds via `PerkService.GetModifiedRollThresholds` **antes** desses clamps.

---

## 5. Chance de Rolagem Máxima / Mínima

**Rolagem Máxima** = chance de a soma dos dados de um grupo (stat) atingir **o valor base da stat**, não a soma das faces do grupo. Isso é o que corrige o problema do dado extra.

**Rolagem Mínima** = chance de todos os dados do grupo caírem exatamente na própria face mínima ao mesmo tempo (sem correção especial — mais dados sempre torna o piso mais raro, o que é correto).

**Exemplo: Heart = 12**

| Configuração | Dados | Alvo (Máxima) | Chance de Rolagem Máxima | Chance de Rolagem Mínima |
|---|---|---|---|---|
| 1 dado alocado | `1d12` | 12 | **8,3%** | 8,3% |
| 1 dado + 1 extra de perk | `1d12 + 1d12` | 12 | **61,8%** | 0,7% |
| 2 dados alocados (sem perk) | `2× 1d6` | 12 | 2,8% | 2,8% |
| 3 dados alocados (sem perk) | `3× 1d4` | 12 | 1,6% | 1,6% |

Pontos-chave:
- O **dado extra de perk aumenta muito** a chance de rolagem máxima (8,3% → 61,8%), pois não exige que os dois dados batam 12 ao mesmo tempo — basta a soma alcançar 12.
- **Dividir voluntariamente em mais dados do mesmo tipo reduz** a chance de rolagem máxima (é uma escolha de risco do jogador: mais dados = resultado mais previsível/mediano, mas mais difícil de "estourar" no máximo).

---

## 6. Hit / Miss / Critical (somente Accuracy)

Usa os mesmos `lowMax`/`mediumMax`/`highMin` calculados acima, sobre a distribuição de soma do **grupo primário** (maior potencial entre os tipos alocados):

| Resultado | Condição | Threshold (baseline, stat=12) |
|---|---|---|
| **Miss** | soma ≤ `lowMax` | 1–3 |
| **Hit** | soma > `lowMax` (Medium + High) | 4+ |
| **Critical** | soma ≥ `highMin` | 10+ |

As chances de cada faixa são a soma da massa de probabilidade da distribuição real (convolução dos dados do grupo), não uma aproximação — por isso já refletem corretamente múltiplos dados, dados extras, etc.

---

## 7. Resumo do Pipeline (ponta a ponta)

```
Stat Base (12)
   │
   ├─► Alocação do jogador (N dados, tipo(s) escolhidos)
   │        │
   │        ├─► Dados base dividem a stat entre si
   │        └─► Dados extras de perk usam a stat cheia (não dividem)
   │
   ├─► Thresholds (Low/High) = 0.25/0.75 ajustados por:
   │        risco (nº de dados) + nível + combat stat + sinergia (Accuracy) + perks
   │        └─► clamps finais [0.05–0.45] / [0.55–0.95] / gap mínimo 0.20
   │
   ├─► Boundaries inteiros (lowMax, mediumMax, highMin, maxValue=stat base)
   │
   ├─► Rolagem real: soma por tipo → melhor grupo vence (GetBestResult)
   │
   └─► Preview de UI: mesma matemática, sem rolar de verdade
            ├─ Damage (Min/Max) via tier do resultado
            ├─ Chance Low/Medium/High (Power)
            ├─ Chance Miss/Hit/Critical (Accuracy)
            ├─ Chance de Rolagem Máxima/Mínima (ambos)
            └─ "Perfil" (Consistente/Equilibrado/Arriscado)
```

---

## 8. Observações e Limitações Conhecidas

- O cálculo de Hit/Miss/Critical no preview usa o **grupo primário** (maior potencial) como referência para os thresholds exibidos — em alocações com múltiplos tipos diferentes, isso é uma aproximação (o resultado real pode vir de outro grupo, via `GetBestResult`).
- `Chance de Rolagem Mínima` ainda não considera `minFace` individual por dado (ex.: bônus de Agility que eleva o piso) — os parâmetros já foram plumbing-ados até a View (`powerMinFaces`/`accuracyMinFaces`) mas não estão em uso ainda; é um refinamento futuro possível.
- O "Perfil" (Consistente/Equilibrado/Arriscado) é um resumo agregado de `TierChances`; a proposta de evolução para presets de alocação pré-definidos foi discutida, mas não implementada.
