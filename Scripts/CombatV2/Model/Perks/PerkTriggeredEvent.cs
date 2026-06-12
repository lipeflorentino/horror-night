/// <summary>
/// Evento disparado quando um Perk é acionado.
/// Contém todas as informações necessárias para feedback visual, auditivo e mecânico.
/// </summary>
public struct PerkTriggeredEvent
{
    /// <summary>
    /// ID único do perk (ex: "six_feet_under").
    /// Para exibir nome do perk, use TrickDatabase ou PerkId como fallback.
    /// </summary>
    public string PerkId { get; set; }

    /// <summary>
    /// Battler que possui o perk ativo.
    /// </summary>
    public Battler Owner { get; set; }

    /// <summary>
    /// ID do Trick que originou o perk, quando existir.
    /// </summary>
    public string SourceTrickId { get; set; }

    /// <summary>
    /// ID runtime da instância de Trick que originou o perk, quando existir.
    /// </summary>
    public string SourceTrickInstanceId { get; set; }

    /// <summary>
    /// Trigger que causou o disparo do perk.
    /// </summary>
    public PerkTrigger Trigger { get; set; }

    /// <summary>
    /// O que foi modificado (dano, dados extras, etc).
    /// </summary>
    public PerkModifierTarget ModifierTarget { get; set; }

    /// <summary>
    /// Valor que foi aplicado (ex: 0.30 para +30% de dano).
    /// </summary>
    public float AppliedValue { get; set; }

    /// <summary>
    /// Número de stacks que o perk tinha quando foi acionado.
    /// </summary>
    public int StacksApplied { get; set; }

    /// <summary>
    /// Contexto completo do evento (CombatRollContext ou CombatActionContext).
    /// Permite análise detalhada de por que o perk foi acionado.
    /// </summary>
    public object FullContext { get; set; }

    /// <summary>
    /// Timestamp de quando o perk foi acionado.
    /// </summary>
    public float TriggerTime { get; set; }
}
