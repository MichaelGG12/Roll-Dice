using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public static Menu instance;

    [Header("Screens")]
    [SerializeField] private GameObject _loginScreen;
    [SerializeField] private GameObject _registerScreen;
    [SerializeField] private GameObject _menuScreen;
    [SerializeField] private GameObject _profileScreen;
    [SerializeField] private GameObject _gameScreen;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    private void TurnPanelsOff()
    {
        _loginScreen.SetActive(false);
        _registerScreen.SetActive(false);
        _menuScreen.SetActive(false);
        _profileScreen.SetActive(false);
        _gameScreen.SetActive(false);
    }

    public void OpenRegisterPanel()
    {
        TurnPanelsOff();
        _registerScreen.SetActive(true);
    }

    public void OpenLoginPanelAfterRegister()
    {
        TurnPanelsOff();
        _loginScreen.SetActive(true);
    }

    public void OpenMenuPanel()
    {
        TurnPanelsOff();
        _menuScreen.SetActive(true);
    }

    public void OpenLoginPanelAfterSignOut()
    {
        TurnPanelsOff();
        _loginScreen.SetActive(true);
    }

    public void OpenPlayerProfilePanel()
    {
        TurnPanelsOff();
        _profileScreen.SetActive(true);
    }

    public void OpenGamePanel()
    {
        TurnPanelsOff();
        _gameScreen.SetActive(true);
    }
}
