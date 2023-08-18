using System.Security.Cryptography;
using TMPro;
using TwitchChat;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using UnityEditor;

public class CounterTwitchGame : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameTMP;
    [SerializeField] private TextMeshProUGUI currentScoreTMP;
    [SerializeField] private TextMeshProUGUI maxScoreTMP;
    [SerializeField] private TextMeshProUGUI ScoreStepsTMP;

    private int currentScore;
    private int ScoreSteps;//
    public string randomOperator;
    private System.Random randomGenerator = new System.Random();
    private System.Random random = new System.Random();
    public TextMeshProUGUI operatorText;

    [SerializeField] private string CorrectBackgroundColorHTML = "#24A110"; // Color verde predeterminado en formato HTML
    [SerializeField] private string IncorrectBackgroundColorHTML = "#9F231C"; // Color rojo predeterminado en formato HTML
    [SerializeField] private UnityEngine.Color BaseBackgroundColor; // Color rojo predeterminado en formato HTML
    [SerializeField] private float timeToChangeColor=0.2f;



    private string lastUsername = string.Empty;

    private int currentMaxScore;
    private readonly string maxScoreKey = "maxScore";

    private string currentMaxScoreUsername = "RothioTome";
    private readonly string maxScoreUsernameKey = "maxScoreUsername";

    private string lastUserIdVIPGranted;
    private readonly string lastUserIdVIPGrantedKey = "lastVIPGranted";

    private string nextPotentialVIP;

    [SerializeField] private GameObject startingCanvas;

    private void Start()
    {
        Application.targetFrameRate = 30;

        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;
        TwitchController.onChannelJoined += OnChannelJoined;
        
        currentMaxScore = PlayerPrefs.GetInt(maxScoreKey);
        currentMaxScoreUsername = PlayerPrefs.GetString(maxScoreUsernameKey, currentMaxScoreUsername);
        lastUserIdVIPGranted = PlayerPrefs.GetString(lastUserIdVIPGrantedKey, string.Empty);

        UpdateMaxScoreUI();
        UpdateCurrentScoreUI(lastUsername, currentScore.ToString());
        ResetGame();
    }

    private void OnDestroy()
    {
        TwitchController.onTwitchMessageReceived -= OnTwitchMessageReceived;
        TwitchController.onChannelJoined -= OnChannelJoined;
    }
    public void GenerateRandomOperator()
    {
        string[] operators = new string[] { "=", "<", ">" };
        int randomIndex = random.Next(operators.Length);
        randomOperator = operators[randomIndex];

        operatorText.text = randomOperator;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        if (!int.TryParse(chatter.message, out int response)) return;

        string displayName = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;

        if (lastUsername.Equals(displayName)) return;
        if (randomOperator == ">")
        {
            if (response == currentScore + 1)
            {
                HandleCorrectResponse(displayName, chatter);
            }
            else
            {
                HandleIncorrectResponse(displayName, chatter);
            }
        }
        else if (randomOperator == "<")
        {
            if (response == currentScore - 1)
            {
                HandleCorrectResponse(displayName, chatter);
            }
            else
            {
                HandleIncorrectResponse(displayName, chatter);
            }
        }
        else if (randomOperator == "=")
        {
            if (response == currentScore)
            {
                HandleCorrectResponse(displayName, chatter);
            }
            else
            {
                HandleIncorrectResponse(displayName, chatter);
            }
        }
        else
        {
            HandleIncorrectResponse(displayName, chatter);
        }
    }

    private void HandleCorrectResponse(string displayName, Chatter chatter)
    {
        Debug.Log("correcto");

        currentScore = randomGenerator.Next(-100, 100);
        GenerateRandomOperator();
        ScoreSteps++;

        // Convertir la cadena de color en formato HTML a un valor de Color
        UnityEngine.Color parsedColor;
        if (UnityEngine.ColorUtility.TryParseHtmlString(CorrectBackgroundColorHTML, out parsedColor))
        {
            Camera mainCamera = Camera.main; // Obtener la cámara principal
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = parsedColor; // Asignar el color convertido
            }
        }
        else
        {
            Debug.LogError("Error al analizar el color HTML.");
        }
        
        Invoke("ApplyRandomColorToBackground", timeToChangeColor);

        UpdateCurrentScoreUI(displayName, currentScore.ToString());
        UpdateCurrentScorestepsUI(displayName, ScoreSteps.ToString());

        lastUsername = displayName;
        if (ScoreSteps > currentMaxScore)
        {
            SetMaxScore(displayName, ScoreSteps);
            HandleVIPStatusUpdate(chatter);
        }
    }

    private void HandleIncorrectResponse(string displayName, Chatter chatter)
    {
        // Convertir la cadena de color en formato HTML a un valor de Color
        UnityEngine.Color parsedColor;
        if (UnityEngine.ColorUtility.TryParseHtmlString(IncorrectBackgroundColorHTML, out parsedColor))
        {
            Camera mainCamera = Camera.main; // Obtener la cámara principal
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = parsedColor; // Asignar el color convertido
            }
        }
        else
        {
            Debug.LogError("Error al analizar el color HTML.");
        }
        Debug.Log("Incorrecto");

        // Llamar al método para generar un color aleatorio para el fondo
       
        Invoke("ApplyRandomColorToBackground", timeToChangeColor);

        if (currentScore != 0)
        {
            DisplayShameMessage(displayName);

            if (TwitchOAuth.Instance.IsVipEnabled())
            {
                if (lastUserIdVIPGranted.Equals(chatter.tags.userId))
                {
                    RemoveLastVIP();
                }

                HandleNextPotentialVIP();
            }

            HandleTimeout(chatter);
            UpdateMaxScoreUI();
            ResetGame();
        }
    }

    private UnityEngine.Color GenerateRandomColor()
    {
        float r = UnityEngine.Random.value;
        float g = UnityEngine.Random.value;
        float b = UnityEngine.Random.value;
        return new UnityEngine.Color(r, g, b);
    }


    private void ApplyRandomColorToBackground()
    {
        Camera mainCamera = Camera.main; // Obtener la cámara principal
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = BaseBackgroundColor; // Asignar el color proporcionado
        }
    }









    private void HandleNextPotentialVIP()
    {
        if (!string.IsNullOrEmpty(nextPotentialVIP))
        {
            if (nextPotentialVIP == "-1")
            {
                RemoveLastVIP();
            }
            else
            {
                if (!string.IsNullOrEmpty(lastUserIdVIPGranted))
                {
                    RemoveLastVIP();
                }
                GrantVIPToNextPotentialVIP();
            }
            nextPotentialVIP = string.Empty;
        }
    }

    private void HandleTimeout(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsModImmunityEnabled())
        {
            if (!chatter.HasBadge("moderator"))
            {
                TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
            }
        }
        else
        {
            TwitchOAuth.Instance.Timeout(chatter.tags.userId, currentScore);
        }
    }

    private void HandleVIPStatusUpdate(Chatter chatter)
    {
        if (TwitchOAuth.Instance.IsVipEnabled())
        {
            if (!chatter.tags.HasBadge("vip"))
            {
                nextPotentialVIP = chatter.tags.userId;
            }
            else if (chatter.tags.userId == lastUserIdVIPGranted)
            {
                nextPotentialVIP = "";
            }
            else
            {
                nextPotentialVIP = "-1";
            }
        }
    }

    private void RemoveLastVIP()
    {
        TwitchOAuth.Instance.SetVIP(lastUserIdVIPGranted, false);
        lastUserIdVIPGranted = "";
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }

    private void GrantVIPToNextPotentialVIP()
    {
        TwitchOAuth.Instance.SetVIP(nextPotentialVIP, true);
        lastUserIdVIPGranted = nextPotentialVIP;
        PlayerPrefs.SetString(lastUserIdVIPGrantedKey, lastUserIdVIPGranted);
    }

    private void DisplayShameMessage(string displayName)
    {
        usernameTMP.SetText($"<color=#00EAC0>Shame on </color>{displayName}<color=#00EAC0>!</color>");
    }

    private void OnChannelJoined()
    {
        startingCanvas.SetActive(false);
    }

    public void ResetHighScore()
    {
        SetMaxScore("RothioTome", 0);
        RemoveLastVIP();
        ResetGame();
    }

    private void SetMaxScore(string username, int score)
    {
        currentMaxScore = score;
        currentMaxScoreUsername = username;
        PlayerPrefs.SetString(maxScoreUsernameKey, username);
        PlayerPrefs.SetInt(maxScoreKey, score);
        UpdateMaxScoreUI();
    }

    private void UpdateMaxScoreUI()
    {
        string scoreText = $"HIGH SCORE: {currentMaxScore}\nby <color=#00EAC0>";

        if (TwitchOAuth.Instance.IsVipEnabled() &&
            (!string.IsNullOrEmpty(nextPotentialVIP) || !string.IsNullOrEmpty(lastUserIdVIPGranted)))
        {
            scoreText += $"<sprite=0>{currentMaxScoreUsername}</color>";
        }
        else
        {
            scoreText += currentMaxScoreUsername;
        }

        maxScoreTMP.SetText(scoreText);
    }

    private void UpdateCurrentScoreUI(string username, string score)
    {
        usernameTMP.SetText(username);
        currentScoreTMP.SetText(score);
       

    }
    private void UpdateCurrentScorestepsUI(string username, string step)
    {
        usernameTMP.SetText(username);
        ScoreStepsTMP.SetText(step);

    }
    



    private void ResetGame()
    {
        Debug.Log("reinicia");
        lastUsername = "";
        currentScore = randomGenerator.Next(0, 11);
        currentScoreTMP.SetText(currentScore.ToString());
        ScoreSteps = 0;
        ScoreStepsTMP.SetText(ScoreSteps.ToString());
        GenerateRandomOperator();
    }
}