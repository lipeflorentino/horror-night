using System;
using UnityEngine;
using UnityEngine.UI;

public class CombatInputController
{
    public event Action<PlayerActionType> OnPlayerActionSelected;
    public event Action OnAttackMenuRequested;
    public event Action OnSpecialMenuRequested;
    public event Action OnLearnInfoToggleRequested;

    public CombatInputController(
        Button attackButton,
        Button itemButton,
        Button specialButton,
        Button attackHeartButton,
        Button attackBodyButton,
        Button attackMindButton,
        Button defendButton,
        Button parryButton,
        Button fleeButton,
        Button instantKillButton,
        Button learnButton,
        Button learnInfoIconButton)
    {
        RegisterButton(attackButton, () => OnAttackMenuRequested?.Invoke());
        RegisterButton(specialButton, () => OnSpecialMenuRequested?.Invoke());
        RegisterButton(itemButton, () => TriggerPlayerAction(PlayerActionType.Item));

        RegisterButton(attackHeartButton, () => TriggerPlayerAction(PlayerActionType.AttackHeart));
        RegisterButton(attackBodyButton, () => TriggerPlayerAction(PlayerActionType.AttackBody));
        RegisterButton(attackMindButton, () => TriggerPlayerAction(PlayerActionType.AttackMind));

        RegisterButton(defendButton, () => TriggerPlayerAction(PlayerActionType.Defend));
        RegisterButton(parryButton, () => TriggerPlayerAction(PlayerActionType.Parry));

        RegisterButton(fleeButton, () => TriggerPlayerAction(PlayerActionType.Flee));
        RegisterButton(instantKillButton, () => TriggerPlayerAction(PlayerActionType.InstantKill));
        RegisterButton(learnButton, () => TriggerPlayerAction(PlayerActionType.Learn));
        RegisterButton(learnInfoIconButton, () => OnLearnInfoToggleRequested?.Invoke());
    }

    private void TriggerPlayerAction(PlayerActionType action)
    {
        OnPlayerActionSelected?.Invoke(action);
    }

    private static void RegisterButton(Button button, Action callback)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback?.Invoke());
    }
}
