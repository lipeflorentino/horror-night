using UnityEngine;

public class RunController : MonoBehaviour
{
    [Header("Level Flow")]
    [SerializeField] private LevelSO startingLevel;

    [Header("References")]
    [SerializeField] private LevelController levelController;
    [SerializeField] private LevelUpUI levelUpUI;
    [SerializeField] private PlayerGridMovement playerMovement;

    private LevelSO currentLevel;
    private bool waitingForLevelUpInput;

    private void Awake()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();
    }

    private void Start()
    {
        currentLevel = startingLevel;
        StartRun();
    }

    private void OnEnable()
    {
        if (levelController != null)
        {
            levelController.OnLevelCompleted += HandleLevelCompleted;
            levelController.OnAreaChanged += HandleAreaChanged;
        }

        if (levelUpUI != null)
            levelUpUI.OnContinuePressed += HandleContinuePressed;
    }

    private void OnDisable()
    {
        if (levelController != null)
        {
            levelController.OnLevelCompleted -= HandleLevelCompleted;
            levelController.OnAreaChanged -= HandleAreaChanged;
        }

        if (levelUpUI != null)
            levelUpUI.OnContinuePressed -= HandleContinuePressed;
    }

    private void Update()
    {
        if (!waitingForLevelUpInput)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            waitingForLevelUpInput = false;

            if (levelUpUI != null)
                levelUpUI.Hide();
        }
    }

    public void StartRun()
    {
        if (startingLevel == null)
            return;

        LoadLevel(startingLevel);
    }

    private void LoadLevel(LevelSO level)
    {
        currentLevel = level;
        levelController.Initialize(level);

        if (playerMovement != null)
        {
            Vector3 startPos = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);
            playerMovement.transform.position = startPos;
            playerMovement.enabled = true;
        }
    }

    private void HandleLevelCompleted()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        waitingForLevelUpInput = true;

        if (levelUpUI != null)
            levelUpUI.Show();
    }

    private void HandleContinuePressed()
    {
        waitingForLevelUpInput = false;

        if (levelUpUI != null)
            levelUpUI.Hide();
    }

    private void HandleAreaChanged(int areaIndex)
    {
        Debug.Log($"[RunController] Área {areaIndex + 1}/{currentLevel.AreaCount} carregada. Reutilize o prefab da área e regenere dados locais.");

        if (playerMovement != null)
        {
            Vector3 areaStartPosition = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);
            playerMovement.transform.position = areaStartPosition;
        }
    }
}
