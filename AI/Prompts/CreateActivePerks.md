# ESPECIFICAÇÃO TÉCNICA: Implementação do Sistema de "Active" Tricks

Preciso implementar um novo tipo de habilidade (Trick) ativa no meu jogo de combate em turnos. O jogo utiliza uma arquitetura orientada a dados baseada em dois arquivos de configuração: `TrickTable.csv` e `PerkTable.csv`.

## 1. CONTEXTO DO SISTEMA ATUAL
*   **Tricks (Habilidades):** Gerenciam o ciclo de vida, custos de recursos (Mind, Body, Heart) e cooldowns. Podem ser Ativas (requerem clique do jogador para disparar o efeito) ou Passivas (triggam autonomamente se a condição for satisfeita). Elas injetam Perks no combate.
*   **Perks (Modificadores Atômicos):** Modificações puras que possuem condições de ativação (`ConditionKey`, `ConditionValue`) e aplicam efeitos no combate (`ModifierTarget`, `Operation`, `Value`).

---

## 2. O NOVO REQUISITO: ActiveTricks
Queremos criar uma nova categoria de Trick Ativa que funciona em três fases distintas:
1.  **Fase de Acúmulo (Sustentada):** Ao ser conjurada (cast), ela entra em campo e passa a escutar eventos de combate de forma passiva. Toda vez que suas condições são atendidas, ela acumula "Cargas" em seu estado interno (ex: reter 30% do dano sofrido, +1 carga por bloqueio realizado).
2.  **Fase de Liberação (Ativação Manual):** Enquanto tiver 1 ou mais cargas acumuladas, o botão da Trick fica disponível para o jogador clicar manualmente na UI. Ao clicar, ela consome todas as cargas, dispara um efeito amplificado baseado na quantidade de cargas
3. **Fase de Drawback (debuff quando há)**  aplica um efeito negativo (perk de Debuff ex. exaustão/fadiga) no próprio conjurador.

### Exemplos Práticos de Design:
*   **Back Stab:** Enquanto castada, se sofrer dano, retém 30% do valor. Ao ativar manualmente, causa o dano retido no inimigo e aplica o perk `soreness`.
*   **Endurance:** Enquanto castada, cada bloqueio bem-sucedido acumula +1 carga. Ao ativar, cada carga concede +1 de Defesa e aplica o perk `exhaustion`.
*   **Vengeance:** Acumula +1 carga por dano sofrido. Ao ativar, cada carga concede +1 de Ataque.
*   **Insight:** Acumula +1 carga a cada teste de precisão Alto. Ao ativar,  cada carga concede +1 de Focus.

---

## 3. PROPOSTA DE ESTRUTURA DE DADOS (CSV)

Para implementar isso sem quebrar a arquitetura atômica atual, utilizaremos a estratégia de **Decomposição em Triplo Perk** controlada pelo estado da Trick.

### Modificações na `TrickTable.csv`
A classe/estrutura que representa a Trick em runtime precisa de novas propriedades de controle:
*   `ActivationMode` (String/Enum): Novo tipo `ActiveCharge`.
*   `PerkIds` (String): deve aceitar IDs múltiplos separados por ponto e vírgula `;` (Ex: `endurance_charge;endurance_release;soreness`).
*   **Estado de Runtime (Não vai no CSV, mas na instância da classe - TrickRuntimeInstance):**
    *   `currentCharges` (float/int) - Começa em 0.
    *   `isReadyToTrigger` (bool) - Fica `true` sempre que `currentCharges > 0`.

### Modificações na `PerkTable.csv`
Dividiremos a lógica em um Perk de "Carga", um de "Liberação" e um de Drawback:
1.  **Perk de Carga (Ex: `endurance_charge`):** Escuta o evento de combate normal (`ConditionKey = BlockedAttack`). O `ModifierTarget` será a própria Trick (`TrickCharges`), e a `Operation` será `Add`.
2.  **Perk de Liberação (Ex: `endurance_release`):** Possui a condição `OnManualActivation`. O `ModifierTarget` será o atributo do personagem (`Defense`). Introduzir uma nova `Operation` chamada `AddPerCharge` (ou `MultiplyByTrickCharges`), que somam/multiplica o `Value` do Perk pelas cargas acumuladas na Trick dona dele.
3.  **Debuffs/Efeitos Negativos:** O Perk de liberação deve conter uma tag ou coluna de referência (`ChildPerkId` ou na coluna `Tags` como `apply_exhaustion`) para injetar o efeito negativo correspondente no conjurador no momento do clique.

---

## 4. O QUE VOCÊ PRECISA IMPLEMENTAR / CODIFICAR:

1.  **Refatoração do Carregamento de Dados:** Permitir que uma `Trick` carregue uma lista de `PerkIds` (separados por `;`) em vez de apenas um.
2.  **Estado da Instância da Trick:** Adicionar as variáveis de controle de carga (`currentCharges`, `isReadyToTrigger`) na estrutura de dados que roda no combate.
3.  **Lógica do Modificador `TrickCharges`:** No interpretador de Perks, certifique-se de que se o `ModifierTarget == "TrickCharges"`, o valor calculado seja adicionado à propriedade `currentCharges` da Trick que originou aquele Perk.
4.  **Validação de Ativação (UI):** Criar a regra de que se `ActivationMode == ActiveCharge`, o botão da habilidade na interface só ficará interativo se `isReadyToTrigger == true` (ou `currentCharges > 0`).
5.  **Resolução da Liberação (Clique):** Ao clicar no botão:
    *   Executar o Perk do tipo `Release`.
    *   Calcular o impacto usando a fórmula: $\text{Efeito} = \text{Valor do Perk} \times \text{currentCharges}$.
    *   Instanciar e aplicar o Perk negativo (debuff) associado ao conjurador.
    *   Zerar o contador `currentCharges` e atualizar o estado da Trick.

Gere o código necessário (classes, métodos de atualização de combate, gerenciadores de Perks/Tricks e modificações de estado) para que essa mecânica passe a funcionar perfeitamente no motor do meu jogo.



# COMPLEMENTO DE ESPECIFICAÇÃO: Pipeline de Resolução e Injeção de Debuffs (Perks Negativos)

Considere que os efeitos negativos (como `exhaustion` e `soreness`) já são Perks válidos e estruturados na nossa `PerkTable.csv`. Eles possuem suas próprias durações, condições e modificadores de redução de status.

Para implementar a ativação manual da `ActiveCharge` Trick de forma segura, a resolução do clique do jogador deve seguir um **Pipeline de Execução em 3 Etapas OBRIGATÓRIAS e sequenciais** dentro do motor de combate, sem interrupção ou nova escolha do jogador.

### 1. Ajuste na Estrutura da `TrickTable.csv`
Adicione um novo campo na tabela de configuração das Tricks:
*   `DrawbackPerkId` (String, Opcional): Armazena o ID do Perk maléfico que será aplicado ao conjurador como consequência da liberação. Ex: `exhaustion`.

---

### 2. O Pipeline de Resolução da Trick (Fase de Ativação)

Quando o jogador clicar no botão para ativar manualmente a Trick com cargas, o motor do jogo deve disparar a seguinte sequência lógica:

#### ETAPA A: Resolução dos Benefícios (Positive Release)
*   O motor busca o Perk de liberação contido em `PerkIds` (ex: `endurance_release`).
*   Calcula o bônus com base nas cargas acumuladas: $\text{Valor Final} = \text{Value do Perk} \times \text{CurrentCharges}$.
*   Aplica esse modificador positivo ao jogador (ou alvo) pelas vias normais do sistema de combate.

#### ETAPA B: Aplicação da Repercussão (Drawback Release / Efeito Colateral)
*   O motor verifica se a Trick possui um `NegativePerkId` preenchido.
*   Se houver (ex: `exhaustion`), o motor faz o lookup desse ID na `PerkTable.csv`.
*   Uma instância desse Perk negativo é criada e **injetada diretamente no Conjurador (OwnerAsActor)**, aplicando as penalidades imediatamente (ex: -30% de stamina recovery por 2 turnos).

#### ETAPA C: Limpeza de Estado e Reset (Cleanup)
*   O contador `currentCharges` da Trick é resetado para `0`.
*   O estado `isReadyToTrigger` volta para `false`.
*   O Perk de acúmulo (ex: `endurance_charge`) é removido do gerenciador de combate.
*   A Trick entra em seu estado normal de Cooldown (se aplicável).

---

### 3. O Que Você Precisa Codificar (Foco no Pipeline):
1.  Implementar o campo `DrawbackPerkId` no parser de carregamento do CSV de Tricks.
2.  Criar o fluxo de execução sequencial no gerenciador de tricks que garanta que a **Etapa B** (Injeção do Debuff) aconteça imediatamente após a **Etapa A**, garantindo que o jogador não possa usufruir do bônus sem receber a penalidade.
3.  Garantir que o Perk negativo use o sistema de ticks/turnos padrão do jogo para expirar autonomamente.