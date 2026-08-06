# 1. Visão Geral das Responsabilidades (O que cada arquivo faz)

## PerkService: 

É o seu Orquestrador (Facade). Ele gerencia o ciclo de vida (adicionar, remover, expirar por turnos) de Perks, Drawbacks e States. Ele serve como a porta de entrada pública para o resto do jogo solicitar cálculos (delegando para o Resolver) ou avaliações de ações (delegando para o Evaluator).  

## PerkTriggerEvaluator: 

É o Motor de Eventos (Event Emitter). Sua única função teórica é olhar para uma ação ou rolagem de dados, checar se alguma condição de Perk foi satisfeita e, se sim, gritar para o sistema: "O Perk X foi ativado com o valor Y!". Ele não altera o estado do jogo.  

## PerkEffectResolver: 

É a Calculadora (Query). Ele pega os status base do alvo e aplica a matemática bruta (ApplyModifier) para descobrir qual é o poder final, multiplicador de dano ou número de dados extras.  

## PerkStateApplicator: 

É o Mutador (Command). Ele ouve os gritos do Evaluator e aplica as mudanças permanentes ou de turno nos atributos reais do Battler (como Focus ou Strength).  

## PerkRuntimeHelper: 

Caixa de ferramentas utilitárias compartilhadas (matemática de modificadores e checagem de papéis).  

## PerkRuntimeInstance: 

Apenas a representação dos dados na memória (POCO/DTO).  