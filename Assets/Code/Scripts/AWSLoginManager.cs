using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AWSLoginManager : MonoBehaviour
{
    [Header("AWS Configuration")]
    public string UserPoolId = "ap-southeast-1_OvQ3GmgFG";
    public string ClientId = "5r2q9cc236gng4437us7mhj82b";
    public RegionEndpoint Region = RegionEndpoint.APSoutheast1;

    [Header("UI References")]
    public TMP_InputField UsernameField;
    public TMP_InputField PasswordField;
    public TextMeshProUGUI FeedbackText;

    private AmazonCognitoIdentityProviderClient _provider;
    private CognitoUserPool _userPool;

    void Awake()
    {
        // Force the dispatcher to initialize now, while we are on the Main Thread
        var forceInit = UnityMainThreadDispatcher.Instance();

        _provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), Region);
        _userPool = new CognitoUserPool(UserPoolId, ClientId, _provider);
    }

    public async void OnLoginButtonClicked()
    {
        FeedbackText.text = "Authenticating...";
        bool loginWasSuccessful = false;

        try
        {
            _provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), Region);
            _userPool = new CognitoUserPool(UserPoolId, ClientId, _provider);
            CognitoUser user = new CognitoUser(UsernameField.text, ClientId, _userPool, _provider);

            // 1. Run the login
            AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest
            {
                Password = PasswordField.text
            }).ConfigureAwait(false);

            // 2. Check the result
            if (authResponse.AuthenticationResult != null)
            {
                string idToken = authResponse.AuthenticationResult.IdToken;
                string enteredUsername = UsernameField.text; // Grab the name

                // Inside the success block
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    // 1. Save the username locally for subsequent requests
                    PlayerPrefs.SetString("Username", enteredUsername);
                    PlayerPrefs.Save();

                    // 2. Find the CloudDataManager and trigger the load
                    CloudDataManager cloudManager = FindObjectOfType<CloudDataManager>();
                    if (cloudManager != null)
                    {
                        // This will call the GET Lambda we just created
                        cloudManager.LoadGameProgress();
                    }

                    // 3. Proceed to the Home Scene or Level Selection
                    SceneManager.LoadScene("HomeScene");
                });
            }
        }
        catch (System.Exception e)
        {
            // 3. ONLY show error if we haven't already succeeded
            if (!loginWasSuccessful)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    Debug.LogError("Actual Error: " + e.Message);
                    FeedbackText.text = "INVALID CREDENTIALS";
                });
            }
            else
            {
                // This was likely a threading error after the success
                Debug.Log("Minor background thread error ignored because login succeeded.");
            }
        }
    }
}