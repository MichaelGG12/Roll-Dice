using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public interface IFirebaseManager
{
    void SaveMatchPlayedScore(int mpScore);
    void SaveWinScore(int mwScore);
    void SaveTieScore(int mtScore);
    void SaveLossScore(int mlScore);
}

public class FirebaseManager : MonoBehaviour, IFirebaseManager
{
    [Header("General")]
    [SerializeField] private bool _checkFirebaseStatus;
    [SerializeField] private GameManager _gameManager;

    [SerializeField] private DependencyStatus _dependecyStatus;
    [SerializeField] private FirebaseAuth _firebaseAuth;
    [SerializeField] private FirebaseUser _firebaseUser;
    private DatabaseReference _dbReference;

    [Header("Login")]
    [SerializeField] private InputField _loginEmailField;
    [SerializeField] private InputField _loginPasswordField;
    [SerializeField] private Text _loginWarningText;

    [Header("Register")]
    [SerializeField] private InputField _registerEmailField;
    [SerializeField] private InputField _registerUsernameField;
    [SerializeField] private InputField _registerPasswordField;
    [SerializeField] private InputField _registerVerifyPasswordField;
    [SerializeField] private Text _registerWarningText;

    [Header("Menu")]
    [SerializeField] private Text _playerUsernameText;
    [SerializeField] private Text _xpLevelText;
    [SerializeField] private Text _matchesPlayedText;
    [SerializeField] private Text _winScoreText;
    [SerializeField] private Text _TieScoreText;
    [SerializeField] private Text _lossScoreText;

    [Header("Options")]
    [SerializeField] private InputField _updateUsernameField;
    private bool _firstTimePlaying;

    private void Awake()
    {
        if (_dependecyStatus == DependencyStatus.Available) InitializaFirebase();
        else Debug.LogError(_dependecyStatus);

        if (_checkFirebaseStatus) StartCoroutine(CheckAndFixDependenciesCoroutine());
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => _dependecyStatus = task.Result);
    }

    #region Firebase initialization 

    private IEnumerator CheckAndFixDependenciesCoroutine()
    {
        Task<DependencyStatus> checkDependenciesTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => checkDependenciesTask.IsCompleted);
        DependencyStatus dependencyStatus = checkDependenciesTask.Result;

        if (dependencyStatus == DependencyStatus.Available)
        {
            Debug.Log($"Firebase status: {dependencyStatus}");
        }
        else
        {
            Debug.LogError(String.Format("Could not resolve Firebase dependencies: {0}", dependencyStatus));
        }
    }

    private void InitializaFirebase()
    {
        _firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    #endregion

    #region Login and register

    private IEnumerator Login(string email, string password)
    {
        Task<AuthResult> LoginTask = _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
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
            _dbReference = FirebaseDatabase.DefaultInstance.RootReference;

            StartCoroutine(LoadPlayerData());
            yield return new WaitForSeconds(1);

            _gameManager.Username = _firebaseUser.DisplayName;

            _playerUsernameText.text = _gameManager.Username;
            _updateUsernameField.placeholder.GetComponent<Text>().text = _gameManager.Username;

            _gameManager.Menu.OpenMenu(false);
            CleanInputFields();
        }
    }

    private IEnumerator Register(string email, string username, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _registerWarningText.text = "Please complete required fields.";
        }
        else if (_registerPasswordField.text != _registerVerifyPasswordField.text)
        {
            _registerWarningText.text = "Password doesn't match";
        }
        else
        {
            Task<AuthResult> RegisterTask = _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
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
                        message = "Email Already Registered";
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
                    Task ProfileTask = _firebaseUser.UpdateUserProfileAsync(profile);
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        FirebaseException exception = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError error = (AuthError)exception.ErrorCode;
                        _registerWarningText.text = "Username set failed!";
                    }
                    else
                    {
                        _gameManager.Menu.OpenLogin();
                        _loginWarningText.text = "Register Success!";
                    }
                }
            }
        }
    }

    #endregion

    #region Database - Player profile

    private IEnumerator UpdateUsernameAuth(string username)
    {
        UserProfile profile = new UserProfile { DisplayName = username };
        var profileTask = _firebaseUser.UpdateUserProfileAsync(profile);
        yield return new WaitUntil(predicate: () => profileTask.IsCompleted);
        if (profileTask.Exception != null) throw new ApplicationException("Not updated");
    }

    private IEnumerator UpdateUsername(string username)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("Username").SetValueAsync(username);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);
        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator MatchPlayedScore(int mpScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MatchPlayed").SetValueAsync(mpScore);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);
        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator WinScore(int wScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MatchWinScore").SetValueAsync(wScore);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);
        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator TieScore(int dScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MatchTieScore").SetValueAsync(dScore);
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);
        if (dbTask.Exception != null) throw new ApplicationException("Not updated/saved");
    }

    private IEnumerator LossScore(int lScore)
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).Child("MatchLossScore").SetValueAsync(lScore);
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

    #region Load and save player data

    private IEnumerator LoadPlayerData()
    {
        var dbTask = _dbReference.Child("Users").Child(_firebaseUser.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            throw new ApplicationException(dbTask.Exception.Message);
        }
        else if (dbTask.Result.Value == null)
        {
            _matchesPlayedText.text = "MP 0";
            _winScoreText.text = "MW 0";
            _TieScoreText.text = "MT 0";
            _lossScoreText.text = "ML 0";
            _xpLevelText.text = "1";
            _firstTimePlaying = false;
        }
        else
        {
            DataSnapshot dataSnapshot = dbTask.Result;

            _gameManager.MatchPlayedScore = int.Parse(dataSnapshot.Child("MatchPlayed").Value.ToString());
            _gameManager.WinScore = int.Parse(dataSnapshot.Child("MatchWinScore").Value.ToString());
            _gameManager.TieScore = int.Parse(dataSnapshot.Child("MatchTieScore").Value.ToString());
            _gameManager.LossScore = int.Parse(dataSnapshot.Child("MatchLossScore").Value.ToString());

            SetPlayerScoreOnUI();
            _xpLevelText.text = "1";
            _firstTimePlaying = true;
        }

        if (!_firstTimePlaying)
        {
            SaveMatchPlayedScore(0);
            SaveWinScore(0);
            SaveTieScore(0);
            SaveLossScore(0);
            SaveUsername(_firebaseUser.DisplayName);
        }
    }

    public void SetPlayerScoreOnUI()
    {
        _matchesPlayedText.text = $"MP {_gameManager.MatchPlayedScore}";
        _winScoreText.text = $"MW {_gameManager.WinScore}";
        _TieScoreText.text = $"MT {_gameManager.TieScore}";
        _lossScoreText.text = $"ML {_gameManager.LossScore}";
    }

    public void SaveUsername(string username)
    {
        StartCoroutine(UpdateUsername(username));
    }

    public void SaveMatchPlayedScore(int mpScore)
    {
        StartCoroutine(MatchPlayedScore(mpScore));
    }

    public void SaveWinScore(int mpScore)
    {
        StartCoroutine(WinScore(mpScore));
    }

    public void SaveTieScore(int mpScore)
    {
        StartCoroutine(TieScore(mpScore));
    }

    public void SaveLossScore(int mpScore)
    {
        StartCoroutine(LossScore(mpScore));
    }

    #endregion

    #region Clear all input fields

    public void CleanInputFields()
    {
        _loginEmailField.text = string.Empty;
        _loginPasswordField.text = string.Empty;
        _registerEmailField.text = string.Empty;
        _registerUsernameField.text = string.Empty;
        _registerPasswordField.text = string.Empty;
        _registerVerifyPasswordField.text = string.Empty;
    }

    #endregion

    #region Buttons

    public void Login()
    {
        StartCoroutine(Login(_loginEmailField.text, _loginPasswordField.text));
    }

    public void Register()
    {
        StartCoroutine(Register(_registerEmailField.text, _registerUsernameField.text, _registerPasswordField.text));
    }

    public void SignOut()
    {
        _firebaseAuth.SignOut();
        _gameManager.Menu.OpenLogin();
        CleanInputFields();
    }

    public void SaveProfileInfo()
    {
        StartCoroutine(UpdateUsernameAuth(_updateUsernameField.text));
    }

    #endregion
}
