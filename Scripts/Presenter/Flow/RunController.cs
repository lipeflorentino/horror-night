using UnityEngine;

public class RunController : MonoBehaviour
{
    [Header("Level Flow")]
    [SerializeField] private LevelSO startingLevel;
    [SerializeField] private LevelSO[] possibleLevels;

    [Header("References")]
    [SerializeField] private LevelController levelController;
    [SerializeField] private LevelUpUI levelUpUI;
    private LevelSO currentLevel;
    private bool waitingForLevelUpInput;
    private bool waitingForPortalInput = false;
    [SerializeField] private PlayerGridMovement playerMovement;

    private void Update()
    {
        if (!waitingForLevelUpInput)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            waitingForLevelUpInput = false;
            levelUpUI.Hide();
            GoToNextLevel();
        }
    }

    private void Awake()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();
    }

    private void Start()
    {
        currentLevel = startingLevel;
        StartRun();
    }

    public void StartRun()
    {
        LoadLevel(startingLevel);
    }

    private void LoadLevel(LevelSO level)
    {
        levelController.Initialize(level);
        levelController.OnNodeChanged += HandleNodeChanged;
        levelController.OnLevelCompleted += HandleLevelCompleted;
        levelUpUI.OnContinuePressed += HandleContinuePressed;
    }

    private void HandleLevelCompleted()
    {
        playerMovement.enabled = false;
        waitingForLevelUpInput = true;
        levelUpUI.Show();
    }

    private void HandleContinuePressed()
    {
        levelUpUI.Hide();
        playerMovement.enabled = true;
        GoToNextLevel();
    }

    private void HandleNodeChanged(int index)
    {
        LevelNode currentNode = levelController.GetCurrentNode();

        if (currentNode.definition.nodeType == NodeType.Portal)
        {
            if (waitingForPortalInput) HandlePortal();
        }
    }

    private void HandlePortal()
    {
        LevelSO nextLevel = GetNextLevel();

        if (nextLevel == null)
            return;

        levelController.OnNodeChanged -= HandleNodeChanged;
        LoadLevel(nextLevel);
    }

    private void GoToNextLevel()
    {
        LevelSO next = GetNextLevel();

        if (next == null)
            return;

        currentLevel = next;
        levelController.Initialize(currentLevel);
    }

    private LevelSO GetNextLevel()
    {
        if (possibleLevels == null || possibleLevels.Length == 0)
            return null;

        int randomIndex = Random.Range(0, possibleLevels.Length);
        return possibleLevels[randomIndex];
    }
}