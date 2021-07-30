using System;
using System.Collections.Generic;
using System.Text;

public class Room
{
    public enum RoomState
    {
        Await,//等待
        Gaming,//遊戲開始
    }

    public int RoomID = 0;
    public GamePlay gamePlay;
    public RoomState state = RoomState.Await;

    public const int MAX_PLAYER_AMOUNT = 2;
    public const int MAX_OBSERVER_AMOUNT = 3;

    public List<Player> players = new List<Player>();
    public List<Player> observers = new List<Player>();

    public Room(int _roomID)
    {
        RoomID = _roomID;
        gamePlay = new GamePlay();
    }

    public void Close()
    {
        foreach (var each in players)
        {
            each.ExitRoom();
        }

        foreach (var each in observers)
        {
            each.ExitRoom();
        }

        Server.rooms.Remove(RoomID);

    }
}