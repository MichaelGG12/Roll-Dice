using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public interface IFirebaseManager
{
    void SaveMatchesPlayedScore();
    void SaveWinScore();
    void SaveTieScore();
    void SaveLossScore();
}

public class FirebaseManager : MonoBehaviour, IFirebaseManager
{
    [Header("Firebase")]
    private readonly UnityEvent _onFirebaseInitialized;
    [SerializeField] private bool _checkFirebaseStatus;

    [SerializeField] private DependencyStatus _dependecyStatus;
    [SerializeField] private FirebaseAuth _firebaseAuth;
    [SerializeField] private FirebaseUser _firebaseUser;
    [SerializeField] private DatabaseReference _dbReference;

    [Header("Login")]
    [SerializeField] private InputField _loginEmailField;
    [SerializeField] private InputField _loginPasswordField;
    [SerializeField] private Text _loginWarningText;

    [Header("Register")]
    [SerializeField] private InputField _registerEmailField;
    [SerializeField] private InputField _registerUsernameField;
    [SerializeField] private InputField _registerPasswordField;
    [SerializeField] private Text _registerWarningText;

    [Header("Menu Info")]
    [SerializeField] private Text _playerUsernameText;
    [SerializeField] private Text _xpLevelText;
    [SerializeField] private Text _matchesPlayedText;
    [SerializeField] private Text _winScoreText;
    [SerializeField] private Text _TieScoreText;
    [SerializeField] private Text _lossScoreText;

    [Header("Player Info")]
    [SerializeField] private InputField _updateUsernameField;

    private bool _firstTimePlaying;

    private void Awake()
    {
        if (_checkFirebaseStatus) StartCoroutine(CheckAndFixDependenciesCoroutine());
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => _dependecyStatus = task.Result);

        if (_dependecyStatus == DependencyStatus.Available) InitializaFirebase();
        else Debug.LogError(_dependecyStatus);
    }

    #region Awake methods
    private IEnumerator CheckAndFixDependenciesCoroutine()
    {
        var checkDependenciesTask = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => checkDependenciesTask.IsCompleted);

        var dependencyStatus = checkDependenciesTask.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            Debug.Log($"Firebase status: {dependencyStatus}");
            _onFirebaseInitialized.Invoke();
        }
        else
        {
            Debug.LogError(System.String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        }
    }

    private void InitializaFirebase()
    {
        _firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    #endregion

    #region Login

    private IEnumerator Login(string email, string password)
    {
        var LoginTask = _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            FirebaseException exception = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError error = (AuthError)exception.ErrorCode;

            string message = "Login Failed!";

            switch (error)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            _loginWarningText.text = message;
        }
        else
        {
            _firebaseUser = LoginTask.Result.User;

            // If its first time playing, set default values

            StartCoroutine(LoadPlayerData());

            yield return new WaitForSeconds(1);

            _playerUsernameText.text = _firebaseUser.DisplayName;
            Menu.instance.OpenMenuPanel();
            ClearInputFields();
        }
    }

    #endregion 

    #region Register

    private IEnumerator Register(string email, string username, string password)
    {
        if (string.IsNullOrEmpty(email)) throw new ApplicationException("You need an email");

        else if (string.IsNullOrEmpty(username)) throw new ApplicationException("No need an username");

        else if (string.IsNullOrEmpty(password)) throw new ApplicationException("No need a password");

        else
        {
            var RegisterTask = _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null)
            {
                FirebaseException exception = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError error = (AuthError)exception.ErrorCode;

                string message = "Register Failed!";

                switch (error)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                _registerWarningText.text = message;
            }
            else
            {
                _firebaseUser = RegisterTask.Result.User;

                if (_firebaseUser != null)
                {
                    UserProfile profile = new UserProfile { DisplayName = username };

                    var ProfileTask = _firebaseUser.UpdateUserProfileAsync(profile);

                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        FirebaseException exception = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError error = (AuthError)exception.ErrorCode;
                        _registerWarningText.text = "Username Set Failed!";
                    }
                    else
                    {
                        Menu.instance.OpenLoginPanelAfterRegister();
                    }
                }
            }
        }
    }

    #endregion

    #region Database - player profile info

    private IEnumerator UpdateUsernameAuth(string username)
    {
        UserProfile profile = new UserProfile { DisplayName = username };

        var profileTask = _firebaseUser.UpdateUserProfileAsync(profile);

        yield return new WaitUntil(predicate: () => profileTask.IsCompleted);

        if (profileTask.Exception != null) throw new ApplicationException("Not updated");
        else Debug.Log("Updated");
    }
    #endregion

    #region Database - player in game data

    private IEnumerator UpdateUsernameDb(string username)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("Username").SetValueAsync(username);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator MpScore(int mpScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MpScore").SetValueAsync(mpScore);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator WScore(int wScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MwScore").SetValueAsync(wScore);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator DScore(int dScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MdScore").SetValueAsync(dScore);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
        else Debug.Log("Updated");
    }

    private IEnumerator LScore(int lScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MlScore").SetValueAsync(lScore);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator XpLevel(int xp)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("XpLevel").SetValueAsync(xp);

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    #endregion

    #region Load player data

    private IEnumerator LoadPlayerData()
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null) throw new ApplicationException("Not loaded");

        else if (dbTask.Result.Value == null)
        {
            _matchesPlayedText.text = "0";
            _winScoreText.text = "0";
            _TieScoreText.text = "0";
            _lossScoreText.text = "0";
            _xpLevelText.text = "1";
            _firstTimePlaying = false;
        }
        else
        {
            DataSnapshot dataSnapshot = dbTask.Result;

            _matchesPlayedText.text = dataSnapshot.Child("MpScore").Value.ToString();
            _winScoreText.text = dataSnapshot.Child("MwScore").Value.ToString();
            _TieScoreText.text = dataSnapshot.Child("MdScore").Value.ToString();
            _lossScoreText.text = dataSnapshot.Child("MlScore").Value.ToString();
            _xpLevelText.text = dataSnapshot.Child("XpLevel").Value.ToString();
            _firstTimePlaying = true;
        }

        // Check if its first time playing
        if (!_firstTimePlaying)
        {
            SaveMatchesPlayedScore();
            SaveWinScore();
            SaveTieScore();
            SaveLossScore();
            SaveUsername(_firebaseUser.DisplayName);
        }
    }

    #endregion

    #region Clear all input fields

    private void ClearInputFields()
    {
        _loginEmailField.text = string.Empty;
        _loginPasswordField.text = string.Empty;
        _registerEmailField.text = string.Empty;
        _registerUsernameField.text = string.Empty;
        _registerPasswordField.text = string.Empty;
    }

    #endregion

    #region Buttons

    public void LoginButton()
    {
        StartCoroutine(Login(_loginEmailField.text, _loginPasswordField.text));
        _dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void RegisterButton()
    {
        StartCoroutine(Register(_registerEmailField.text, _registerUsernameField.text, _registerPasswordField.text));
    }

    public void SignOutButton()
    {
        _firebaseAuth.SignOut();
        Menu.instance.OpenLoginPanelAfterSignOut();
        ClearInputFields();
    }

    public void SaveProfileInfo()
    {
        StartCoroutine(UpdateUsernameAuth(_updateUsernameField.text));
    }

    #endregion

    #region Save methods

    public void SaveUsername(string username)
    {
        StartCoroutine(UpdateUsernameDb(username));
    }

    public void SaveMatchesPlayedScore()
    {
        //StartCoroutine(MpScore(mpScore));
    }

    public void SaveWinScore()
    {
        //StartCoroutine(WScore(wScore));
    }

    public void SaveTieScore()
    {
        //StartCoroutine(DScore(dScore));
    }

    public void SaveLossScore()
    {
        //StartCoroutine(LScore(lScore));
    }

    #endregion
}
