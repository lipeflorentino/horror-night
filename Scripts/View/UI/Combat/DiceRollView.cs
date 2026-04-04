using System.Collections;
using UnityEngine;

public class DiceRollView
{
    private readonly MonoBehaviour runner;
    private readonly DiceRollUI dice;

    public DiceRollView(MonoBehaviour runner, DiceRollUI dice)
    {
        this.runner = runner;
        this.dice = dice;
    }

    public IEnumerator Roll(int value)
    {
        yield return runner.StartCoroutine(dice.PlayRollAnimation(value));
    }

    public void Reset()
    {
        dice.ClearValue();
    }
}