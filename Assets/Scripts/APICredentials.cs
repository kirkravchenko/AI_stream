using UnityEngine;

[CreateAssetMenu(fileName = "APIKey", menuName = "API key")]
public class ApiCredentials : ScriptableObject
{
    [Space(10),Header("    [ - - - SENSITIVE DATA - - - ]\n\n NEVER share your API keys or other credentials and keep it private! \n" +
        "\nManage your API keys and credentials for various API calls."), Space(10)]

    [Header("API Key")]
    [SerializeField, Tooltip("If the API requires a API Key, insert it here")] string apiKey;

    [Header("Username and Password")]
    [SerializeField, Tooltip("If the API requires a username and password, fill them in.")] string username;
    [SerializeField, Tooltip("If the API requires a username and password, fill them in.")] string password;

    [Header("Other Credentials")]
    [SerializeField, Tooltip("If the API requires a Secret Key, insert it here")] string secretKey;
    [SerializeField, Tooltip("If the API requires a Authentication Token, insert it here ")] string authToken;


    public string GetKey()
    {
        return Verify(apiKey, "api key") ? apiKey : null;
    }
    public string GetSecretKey()
    {
        return Verify(secretKey, "secret key") ? secretKey : null;
    }

    public string GetUsername()
    {
        return Verify(username, "login username field") ? username : null;
    }
    public string GetPassword()
    {
        return Verify(password, "login password field") ? password : null;
    }

    public string GetAuthToken()
    {
        return Verify(authToken, "authToken") ? authToken : null;
    }


    private bool Verify(string info, string displayName)
    {
        if (info == null || string.IsNullOrEmpty(info))
        {

            Debug.Log($"Your {displayName} is empty! Please add it, otherwise it won't work!");
            return false;
        }
        return true;
    }
}
