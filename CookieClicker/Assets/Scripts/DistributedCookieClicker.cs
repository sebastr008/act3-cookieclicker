using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DistributedCookieClicker : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string apiUrl = "https://sid-restapi.onrender.com";

    [Header("Main Panels")]
    [SerializeField] private GameObject panelAuth;
    [SerializeField] private GameObject panelGame;
    [SerializeField] private GameObject panelLeaderboard;

    [Header("Auth SubPanels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToRegisterButton;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button goToLoginButton;

    [Header("Shared UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Game UI")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text localScoreText;
    [SerializeField] private TMP_Text serverScoreText;
    [SerializeField] private Button cookieButton;
    [SerializeField] private Button saveScoreButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button showLeaderboardButton;

    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private Button closeLeaderboardButton;

    [Header("Cookie Animation")]
    [SerializeField] private RectTransform cookieTransform;
    [SerializeField] private float clickScaleMultiplier = 1.08f;
    [SerializeField] private float clickAnimDuration = 0.08f;

    private string token;
    private string username;

    private int localScore = 0;
    private int serverScore = 0;

    private Coroutine cookieAnimCoroutine;
    private Vector3 cookieBaseScale;

    private void Start()
    {
        HookButtons();

        if (cookieTransform != null)
        {
        cookieBaseScale = cookieTransform.localScale;
        }

        token = PlayerPrefs.GetString("Token", "");
        username = PlayerPrefs.GetString("Username", "");

        panelAuth.SetActive(true);
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        UpdateScoreTexts();
        SetStatus("Inicia sesión o regístrate.");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            SetStatus("Verificando sesión...");
            StartCoroutine(GetProfileCoroutine());
        }
    }

    private void HookButtons()
    {
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginButton);
        if (goToRegisterButton != null) goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterButton);
        if (goToLoginButton != null) goToLoginButton.onClick.AddListener(ShowLoginPanel);

        if (cookieButton != null) cookieButton.onClick.AddListener(OnCookieClick);
        if (saveScoreButton != null) saveScoreButton.onClick.AddListener(OnSaveScoreButton);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutButton);
        if (showLeaderboardButton != null) showLeaderboardButton.onClick.AddListener(OnShowLeaderboardButton);

        if (closeLeaderboardButton != null) closeLeaderboardButton.onClick.AddListener(OnCloseLeaderboardButton);
    }

    public void ShowLoginPanel()
    {
        panelLogin.SetActive(true);
        panelRegister.SetActive(false);
        SetStatus("Inicia sesión.");
    }

    public void ShowRegisterPanel()
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(true);
        SetStatus("Crea una cuenta nueva.");
    }

    public void OnRegisterButton()
    {
        string user = registerUsernameInput.text.Trim();
        string pass = registerPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Completa usuario y contraseña para registrarte.");
            return;
        }

        StartCoroutine(RegisterCoroutine(user, pass));
    }

    public void OnLoginButton()
    {
        string user = loginUsernameInput.text.Trim();
        string pass = loginPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Completa usuario y contraseña para iniciar sesión.");
            return;
        }

        StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator RegisterCoroutine(string user, string pass)
    {
        SetStatus("Registrando usuario...");

        RegisterRequest body = new RegisterRequest
        {
            username = user,
            password = pass
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/usuarios", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 25;

        Debug.Log("REGISTER URL: " + apiUrl + "/api/usuarios");
        Debug.Log("REGISTER BODY: " + json);

        yield return req.SendWebRequest();

        Debug.Log("REGISTER RESULT: " + req.result);
        Debug.Log("REGISTER CODE: " + req.responseCode);
        Debug.Log("REGISTER RESPONSE: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Register error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Registro fallido: " + SafeServerMessage(req));
            yield break;
        }

        SetStatus("Registro exitoso. Ahora inicia sesión.");
        ShowLoginPanel();

        loginUsernameInput.text = user;
        loginPasswordInput.text = "";
        registerPasswordInput.text = "";
    }

    private IEnumerator LoginCoroutine(string user, string pass)
    {
        SetStatus("Iniciando sesión...");

        LoginRequest body = new LoginRequest
        {
            username = user,
            password = pass
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/auth/login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 25;

        Debug.Log("LOGIN URL: " + apiUrl + "/api/auth/login");
        Debug.Log("LOGIN BODY: " + json);

        yield return req.SendWebRequest();

        Debug.Log("LOGIN RESULT: " + req.result);
        Debug.Log("LOGIN CODE: " + req.responseCode);
        Debug.Log("LOGIN RESPONSE: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Login error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Login fallido: " + SafeServerMessage(req));
            yield break;
        }

        LoginResponse response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

        if (response == null || response.usuario == null || string.IsNullOrEmpty(response.token))
        {
            SetStatus("La respuesta del login no se pudo interpretar.");
            yield break;
        }

        token = response.token;
        username = response.usuario.username;

        PlayerPrefs.SetString("Token", token);
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        StartCoroutine(GetProfileCoroutine());
    }

    private IEnumerator GetProfileCoroutine()
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
        {
            SetLoggedOutUI();
            SetStatus("No hay sesión válida.");
            yield break;
        }

        string profileUrl = apiUrl + "/api/usuarios/" + username;

        UnityWebRequest req = UnityWebRequest.Get(profileUrl);
        req.SetRequestHeader("x-token", token);
        req.timeout = 25;

        Debug.Log("PROFILE URL: " + profileUrl);

        yield return req.SendWebRequest();

        Debug.Log("PROFILE RESULT: " + req.result);
        Debug.Log("PROFILE CODE: " + req.responseCode);
        Debug.Log("PROFILE RESPONSE: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("GetProfile error: " + req.error + " | " + req.downloadHandler.text);
            ClearSession();
            SetLoggedOutUI();
            SetStatus("Sesión expirada o token inválido.");
            yield break;
        }

        UserResponse response = JsonUtility.FromJson<UserResponse>(req.downloadHandler.text);

        if (response == null || response.usuario == null)
        {
            ClearSession();
            SetLoggedOutUI();
            SetStatus("No se pudo leer el perfil.");
            yield break;
        }

        username = response.usuario.username;
        serverScore = (response.usuario.data != null) ? response.usuario.data.score : 0;
        localScore = serverScore;

        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        SetLoggedInUI();
        UpdateScoreTexts();
        SetStatus("Sesión activa.");
    }

    public void OnLogoutButton()
    {
        ClearSession();
        SetLoggedOutUI();
        SetStatus("Sesión cerrada.");
    }

    private void ClearSession()
    {
        token = "";
        username = "";

        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();

        localScore = 0;
        serverScore = 0;
        UpdateScoreTexts();
    }

    public void OnCookieClick()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        localScore++;
        UpdateScoreTexts();

        if (cookieTransform != null)
        {
            if (cookieAnimCoroutine != null) StopCoroutine(cookieAnimCoroutine);
            cookieAnimCoroutine = StartCoroutine(CookieClickAnimation());
        }
    }

    private IEnumerator CookieClickAnimation()
{
    if (cookieTransform == null)
        yield break;

    Vector3 originalScale = cookieBaseScale;
    Vector3 targetScale = cookieBaseScale * clickScaleMultiplier;

    cookieTransform.localScale = originalScale;

    float t = 0f;
    while (t < clickAnimDuration)
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / clickAnimDuration);
        cookieTransform.localScale = Vector3.Lerp(originalScale, targetScale, p);
        yield return null;
    }

    t = 0f;
    while (t < clickAnimDuration)
    {
        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / clickAnimDuration);
        cookieTransform.localScale = Vector3.Lerp(targetScale, originalScale, p);
        yield return null;
    }

    cookieTransform.localScale = cookieBaseScale;
}

    public void OnSaveScoreButton()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        StartCoroutine(UpdateScoreCoroutine(localScore));
    }

    private IEnumerator UpdateScoreCoroutine(int newScore)
    {
        SetStatus("Guardando score...");

        UpdateUserRequest body = new UpdateUserRequest
        {
            username = username,
            data = new UserData
            {
                score = newScore
            }
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/usuarios", "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-token", token);
        req.timeout = 25;

        Debug.Log("PATCH URL: " + apiUrl + "/api/usuarios");
        Debug.Log("PATCH BODY: " + json);

        yield return req.SendWebRequest();

        Debug.Log("PATCH RESULT: " + req.result);
        Debug.Log("PATCH CODE: " + req.responseCode);
        Debug.Log("PATCH RESPONSE: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Update score error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("No se pudo guardar el score: " + SafeServerMessage(req));
            yield break;
        }

        serverScore = newScore;
        UpdateScoreTexts();
        SetStatus("Score guardado correctamente.");
    }

    public void OnShowLeaderboardButton()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        StartCoroutine(GetUsersCoroutine());
    }

    public void OnCloseLeaderboardButton()
    {
        panelLeaderboard.SetActive(false);
        panelGame.SetActive(true);
    }

    private IEnumerator GetUsersCoroutine()
    {
        SetStatus("Cargando leaderboard...");

        string url = apiUrl + "/api/usuarios?limit=100&skip=0&sort=true";
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("x-token", token);
        req.timeout = 25;

        Debug.Log("LEADERBOARD URL: " + url);

        yield return req.SendWebRequest();

        Debug.Log("LEADERBOARD RESULT: " + req.result);
        Debug.Log("LEADERBOARD CODE: " + req.responseCode);
        Debug.Log("LEADERBOARD RESPONSE: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get users error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Error cargando usuarios: " + SafeServerMessage(req));
            yield break;
        }

        UsersResponse response = JsonUtility.FromJson<UsersResponse>(req.downloadHandler.text);

        if (response == null || response.usuarios == null)
        {
            SetStatus("No se pudo leer la lista de usuarios.");
            yield break;
        }

        UserModel[] users = response.usuarios;

        Array.Sort(users, (a, b) =>
        {
            int scoreA = (a != null && a.data != null) ? a.data.score : 0;
            int scoreB = (b != null && b.data != null) ? b.data.score : 0;
            return scoreB.CompareTo(scoreA);
        });

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("TABLA DE PUNTAJES");
        sb.AppendLine("--------------------");

        int top = Mathf.Min(10, users.Length);

        for (int i = 0; i < top; i++)
        {
            string user = (users[i] != null && !string.IsNullOrEmpty(users[i].username)) ? users[i].username : "sin_nombre";
            int score = (users[i] != null && users[i].data != null) ? users[i].data.score : 0;
            sb.AppendLine($"{i + 1}. {user} - {score}");
        }

        leaderboardText.text = sb.ToString();
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(true);

        SetStatus("Leaderboard cargado.");
    }

    private bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username);
    }

    private void SetLoggedInUI()
    {
        panelAuth.SetActive(false);
        panelGame.SetActive(true);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        welcomeText.text = "Bienvenido, " + username;
    }

    private void SetLoggedOutUI()
    {
        panelAuth.SetActive(true);
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        welcomeText.text = "Bienvenido,  -";
        leaderboardText.text = "";
        UpdateScoreTexts();
    }

    private void UpdateScoreTexts()
    {
        if (localScoreText != null) localScoreText.text = "Score local: " + localScore;
        if (serverScoreText != null) serverScoreText.text = "Score guardado: " + serverScore;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private string SafeServerMessage(UnityWebRequest req)
    {
        if (req == null) return "sin detalles";

        if (!string.IsNullOrEmpty(req.error))
            return req.error;

        if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
            return req.downloadHandler.text;

        return "sin detalles";
    }
}

[Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class UpdateUserRequest
{
    public string username;
    public UserData data;
}

[Serializable]
public class UserData
{
    public int score;
}

[Serializable]
public class UserModel
{
    public string _id;
    public string username;
    public bool state;
    public UserData data;
}

[Serializable]
public class LoginResponse
{
    public UserModel usuario;
    public string token;
}

[Serializable]
public class UserResponse
{
    public UserModel usuario;
}

[Serializable]
public class UsersResponse
{
    public UserModel[] usuarios;
}