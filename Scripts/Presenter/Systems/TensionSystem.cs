using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TensionSystem : MonoBehaviour
{
    public static TensionSystem Instance;

    [Header("Config")]
    [SerializeField] private float baseModifier = 1f;
    [SerializeField] private int encounterThreshold = 5;
    [SerializeField] private int currentTension;

    [Header("Passive Tension")]
    [SerializeField, Range(0f, 1f)] private float lowStatThreshold = 0.5f;
    [SerializeField] private float lowStatTickInterval = 10f;
    [SerializeField] private int lowStatTensionGain = 1;

    [Header("UI")]
    [SerializeField] private Image tensionFill;
    [SerializeField] private TMP_Text tensionValueText;

    private float lowStatTickTimer;
    private PlayerStatusManager cachedStatus;

    public int CurrentTension => currentTension;

    private void Awake()
    {
        Instance = this;
        RefreshUI();
    }

    private void Update()
    {
        TickLowStatsTension();
    }

    public void SetBaseModifier(float value)
    {
        baseModifier = Mathf.Max(0f, value);
        currentTension = 0;
        lowStatTickTimer = 0f;
        RefreshUI();
    }

    public void OnPlayerMove()
    {
        AddTension(Mathf.RoundToInt(baseModifier));
    }

    public void AddTension(int amount)
    {
        if (amount <= 0)
            return;

        currentTension += amount;
        CheckEncounterThreshold();
        RefreshUI();
    }

    public void ReduceTension(int amount)
    {
        if (amount <= 0)
            return;

        currentTension = Mathf.Max(0, currentTension - amount);
        RefreshUI();
    }

    private void CheckEncounterThreshold()
    {
        if (currentTension < GetEncounterThreshold())
            return;

        if (EncounterSystem.Instance != null)
            EncounterSystem.Instance.TriggerEncounterFromTension();

        currentTension = 0;
    }

    private void TickLowStatsTension()
    {
        if (lowStatTickInterval <= 0f)
            return;

        if (cachedStatus == null)
            cachedStatus = FindObjectOfType<PlayerStatusManager>();

        if (cachedStatus == null)
            return;

        lowStatTickTimer += Time.deltaTime;
        if (lowStatTickTimer < lowStatTickInterval)
            return;

        lowStatTickTimer = 0f;

        bool lowHeart = cachedStatus.GetHeartRatio() <= lowStatThreshold;
        bool lowBody = cachedStatus.GetBodyRatio() <= lowStatThreshold;
        bool lowMind = cachedStatus.GetMindRatio() <= lowStatThreshold;

        if (lowHeart || lowBody || lowMind)
            AddTension(lowStatTensionGain);
    }

    private int GetEncounterThreshold()
    {
        return Mathf.Max(1, encounterThreshold);
    }

    private void RefreshUI()
    {
        int threshold = GetEncounterThreshold();

        if (tensionFill != null)
            tensionFill.fillAmount = Mathf.Clamp01(currentTension / (float)threshold);

        if (tensionValueText != null)
            tensionValueText.text = $"{currentTension}/{threshold}";
    }
}
