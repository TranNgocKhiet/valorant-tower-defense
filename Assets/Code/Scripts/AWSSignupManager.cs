using UnityEngine;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.SceneManagement;

public class AWSSignupManager : MonoBehaviour
{
    [Header("AWS Config")]
    public string ClientId = "your_app_client_id_here";
    private AmazonCognitoIdentityProviderClient _provider;

    [Header("Signup UI")]
    public TMP_InputField UsernameField;
    public TMP_InputField EmailField;
    public TMP_InputField PasswordField;

    [Header("Verification UI")]
    public GameObject VerificationContainer; // Holds the code input/button
    public TMP_InputField VerificationCodeField;

    [Header("Feedback")]
    public TextMeshProUGUI FeedbackText;

    private string _tempUsername; // Stores username between signup and verify

    void Awake()
    {
        _provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), RegionEndpoint.APSoutheast1);

        // Ensure UI state
        VerificationContainer.SetActive(false);
    }

    public async void OnSignupButtonClicked()
    {
        FeedbackText.text = "Creating account...";
        _tempUsername = UsernameField.text;

        try
        {
            var request = new SignUpRequest
            {
                ClientId = ClientId,
                Username = _tempUsername,
                Password = PasswordField.text,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = EmailField.text }
                }
            };

            await _provider.SignUpAsync(request);

            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                FeedbackText.text = "Check your email for the code!";
                VerificationContainer.SetActive(true);
            });
        }
        catch (System.Exception e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => FeedbackText.text = e.Message);
        }
    }

    public async void OnVerifyButtonClicked()
    {
        FeedbackText.text = "Verifying...";

        try
        {
            var confirmRequest = new ConfirmSignUpRequest
            {
                ClientId = ClientId,
                Username = _tempUsername,
                ConfirmationCode = VerificationCodeField.text
            };

            await _provider.ConfirmSignUpAsync(confirmRequest);

            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                // SUCCESS: Send them back to the Login Scene
                SceneManager.LoadScene("LoginScene");
            });
        }
        catch (System.Exception e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => FeedbackText.text = e.Message);
        }
    }

    public void BackToLogin() => SceneManager.LoadScene("LoginScene");
}