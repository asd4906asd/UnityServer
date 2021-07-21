using UnityServerDLL;
using UnityServerDLL.VolleyBall;

public class GamePlay
{
    public GamePlay()
    {

    }

    public PlayBall ballerState;

    public Baller baller;

    public bool playing;
    public bool isMove;

    public int score;

    public Baller Calculate(string input)
    {
        //不合邏輯或不對的地方則返回Null
        if (!playing)
        {
            return Baller.Null;
        }

        if (baller == Baller.L_Player)
        {
            ballerState.input = input;
        }

        if (baller == Baller.R_Player)
        {
            ballerState.input = input;
        }

        //計算結果
        bool? result = _CheckWinner();

        //無人獲勝
        return Baller.None;
    }

    private bool? _CheckWinner()
    {
        return true;
    }
}