using UnityEngine;

public class EnemyBattler : MonoBehaviour
{
    public EnemyInstance enemyData;

    public void Setup(EnemyInstance enemy)
    {
        enemyData = enemy;
    }
}
