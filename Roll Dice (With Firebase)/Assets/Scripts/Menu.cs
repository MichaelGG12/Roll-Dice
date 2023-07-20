using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private GameManager _gameManager;

    [Header("Screens")]
    [SerializeField] private GameObject _loginScreen;
    [SerializeField] private GameObject _registerScreen;
    [SerializeField] private GameObject _menuScreen;
    [SerializeField] private GameObject _profileScreen;
    [SerializeField] private GameObject _gameScreen;

    private void Start()
    {
        if (!_loginScreen.activeInHierarchy)
        {
            OpenLogin();
        }
    }

    private void SetInactiveScreens()
    {
        _loginScreen.SetActive(false);
        _registerScreen.SetActive(false);
        _menuScreen.SetActive(false);
        _profileScreen.SetActive(false);
        _gameScreen.SetActive(false);
    }

    public void OpenRegister()
    {
        SetInactiveScreens();
        _registerScreen.SetActive(true);
    }

    public void OpenLogin()
    {
        SetInactiveScreens();
        _loginScreen.SetActive(true);
        _gameManager.FirebaseManager.CleanInputFields();
    }

    public void OpenMenu(bool backFromGame)
    {
        SetInactiveScreens();
        _menuScreen.SetActive(true);

        if (backFromGame)
        {
            _gameManager.FirebaseManager.SetPlayerScoreOnUI();
        }
    }

    public void OpenOptions()
    {
        SetInactiveScreens();
        _profileScreen.SetActive(true);
    }

    public void OpenGame()
    {
        SetInactiveScreens();
        _gameScreen.SetActive(true);
    }
}
