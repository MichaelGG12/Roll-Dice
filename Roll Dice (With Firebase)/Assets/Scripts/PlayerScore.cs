using UnityEngine;
using UnityEngine.UI;

public class PlayerScore : MonoBehaviour
{
    public Text usernameText;
    public Text matchPlayedText;
    public Text matchWinText;
    public Text mathTieText;
    public Text MatchLossText;

    public void NewScoreElement(string _username, int _matchPlayed, int _matchWin, int _matchTie, int _matchLoss)
    {
        usernameText.text = _username;
        matchPlayedText.text = _matchPlayed.ToString();
        matchWinText.text = _matchWin.ToString();
        mathTieText.text = _matchTie.ToString();
        MatchLossText.text = _matchLoss.ToString();
    }
}
