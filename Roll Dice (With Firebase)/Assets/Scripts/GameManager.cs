using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    private int _playerDiceNumber;
    [SerializeField] private Text _playerDiceText;

    [Header("CPU")]
    private int _cpuDiceNumber;
    [SerializeField] private Text _cpuDiceText;

    [Header("General")]
    public FirebaseManager FirebaseManager;
    public Menu Menu;
    [SerializeField] private Text _winnerText;
    [SerializeField] private Button _throwButton;

    [HideInInspector] public string Username;
    [HideInInspector] public int MatchPlayedScore;
    [HideInInspector] public int WinScore;
    [HideInInspector] public int TieScore;
    [HideInInspector] public int LossScore;

    private void Start()
    {
        _playerDiceNumber = 0;
        _cpuDiceNumber = 0;
    }

    public void ThrowDice()
    {
        StartCoroutine(DiceController());
        _winnerText.text = string.Empty;

        _throwButton.interactable = false;
    }

    private IEnumerator DiceController()
    {
        _playerDiceNumber = Random.Range(1, 6);
        _cpuDiceNumber = Random.Range(1, 6);

        yield return new WaitForSeconds(.5f);
        CheckResult(_playerDiceNumber, _cpuDiceNumber);

        _playerDiceText.text = _playerDiceNumber.ToString();
        _cpuDiceText.text = _cpuDiceNumber.ToString();
        _throwButton.interactable = true;
    }

    private void CheckResult(int player, int cpu)
    {
        FirebaseManager.SaveMatchPlayedScore(MatchPlayedScore += 1);

        if (player > cpu)
        {
            _winnerText.text = $"{Username} wins";
            FirebaseManager.SaveWinScore(WinScore += 1);
        }
        else if (player == cpu)
        {
            _winnerText.text = "Tie";
            FirebaseManager.SaveTieScore(TieScore += 1);
        }
        else
        {
            _winnerText.text = "CPU wins";
            FirebaseManager.SaveLossScore(LossScore += 1);
        }
    }
}