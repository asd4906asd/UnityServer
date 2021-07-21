using System;
using UnityServerDLL;
using UnityServerDLL.VolleyBall;

public class Network
{
    public Network(string ip)
    {
        //註冊
        Server.Register(MessageType.HeartBeat, _tcpHeartBeat);
        Server.Register(MessageType.Enroll, _Enroll);
        Server.Register(MessageType.CreateRoom, _CreateRoom);
        Server.Register(MessageType.EnterRoom, _EnterRoom);
        Server.Register(MessageType.ExitRoom, _ExitRoom);
        Server.Register(MessageType.StartGame, _StartGame);
        Server.Register(MessageType.PlayBall, _PlayBall);

        //啟動
        Server.Start(ip);
    }

    #region TCP註冊
    /// <summary>
    /// 心跳包 僅TCP傳送 UDP另有機制
    /// </summary>
    /// <param name="player"></param>
    /// <param name="data"></param>
    private void _tcpHeartBeat(Player player, byte[] data)
    {
        player.tcpSend(MessageType.HeartBeat);
    }

    private void _Enroll(Player player, byte[] data)
    {
        Enroll result = new Enroll();
        Enroll receive = NetworkUtils.Deserialize<Enroll>(data);

        //玩家註冊名字顯示
        Console.WriteLine($"玩家{player.playerName}註冊為{receive.Name}");

        //設定玩家名字
        player.playerName = receive.Name;

        result.Suc = true;
        result.Name = receive.Name;
        data = NetworkUtils.Serialize(result);
        player.tcpSend(MessageType.Enroll, data);
    }

    private void _CreateRoom(Player player, byte[] data)
    {
        CreateRoom result = new CreateRoom();
        CreateRoom receive = NetworkUtils.Deserialize<CreateRoom>(data);

        //邏輯檢測(開房的玩家不在房內且伺服器內沒有這個房號的房間)
        if (!player.inRoom && !Server.rooms.ContainsKey(receive.RoomID))
        {
            Room room = new Room(receive.RoomID);
            Server.rooms.Add(room.RoomID, room);

            room.players.Add(player);
            player.EnterRoom(receive.RoomID);

            Console.WriteLine($"玩家:{player.playerName}創建房間成功, 房號為{receive.RoomID}");

            result.Suc = true;
            result.RoomID = receive.RoomID;
            data = NetworkUtils.Serialize(result);
            player.tcpSend(MessageType.CreateRoom, data);
        }
    }

    private void _EnterRoom(Player player, byte[] data)
    {
        EnterRoom result = new EnterRoom();
        EnterRoom receive = NetworkUtils.Deserialize<EnterRoom>(data);

        if (!player.inRoom && Server.rooms.ContainsKey(receive.RoomID))
        {
            Room room = Server.rooms[receive.RoomID];

            if (room.players.Count < Room.MAX_PLAYER_AMOUNT && !room.players.Contains(player))
            {
                room.players.Add(player);
                player.EnterRoom(receive.RoomID);

                Console.WriteLine($"玩家:{player.playerName}成為了房間:{receive.RoomID}的玩家");

                result.RoomID = receive.RoomID;
                result.result = EnterRoom.Result.Player;
                data = NetworkUtils.Serialize(result);
                player.tcpSend(MessageType.EnterRoom, data);
            }
            else if (room.observers.Count < Room.MAX_OBSERVER_AMOUNT && !room.observers.Contains(player))
            {
                room.observers.Add(player);
                player.EnterRoom(receive.RoomID);

                Console.WriteLine($"玩家:{player.playerName}成為了房間:{receive.RoomID}的觀戰者");

                result.RoomID = receive.RoomID;
                result.result = EnterRoom.Result.Observer;
                data = NetworkUtils.Serialize(result);
                player.tcpSend(MessageType.EnterRoom, data);
            }
        }
        else
        {
            Console.WriteLine($"玩家:{player.playerName}加入房間失敗");

            data = NetworkUtils.Serialize(result);
            player.tcpSend(MessageType.EnterRoom, data);
        }
    }

    private void _ExitRoom(Player player, byte[] data)
    {
        ExitRoom result = new ExitRoom();
        ExitRoom receive = NetworkUtils.Deserialize<ExitRoom>(data);

        if (Server.rooms.ContainsKey(receive.RoomID))
        {
            if (Server.rooms[receive.RoomID].players.Contains(player) || Server.rooms[receive.RoomID].observers.Contains(player))
            {
                result.Suc = true;

                if (Server.rooms[receive.RoomID].players.Contains(player))
                {
                    Server.rooms[receive.RoomID].players.Remove(player);
                }
                else if (Server.rooms[receive.RoomID].observers.Contains(player))
                {
                    Server.rooms[receive.RoomID].observers.Remove(player);
                }

                if (Server.rooms[receive.RoomID].players.Count == 0)
                {
                    Server.rooms.Remove(receive.RoomID);
                }

                Console.WriteLine($"玩家:{player.playerName}退出房間成功");

                player.ExitRoom();

                data = NetworkUtils.Serialize(result);
                player.tcpSend(MessageType.ExitRoom, data);
            }
            else
            {
                Console.WriteLine($"玩家:{player.playerName}退出房間失敗");

                data = NetworkUtils.Serialize(result);
                player.tcpSend(MessageType.ExitRoom, data);
            }
        }
        else
        {
            Console.WriteLine($"玩家:{player.playerName}退出房間失敗");

            data = NetworkUtils.Serialize(result);
            player.tcpSend(MessageType.ExitRoom, data);
        }
    }

    private void _StartGame(Player player, byte[] data)
    {
        StartGame result = new StartGame();
        StartGame receive = NetworkUtils.Deserialize<StartGame>(data);

        if (Server.rooms.ContainsKey(receive.RoomID))
        {
            if (Server.rooms[receive.RoomID].players.Contains(player) && Server.rooms[receive.RoomID].players.Count == Room.MAX_PLAYER_AMOUNT)
            {
                Server.rooms[receive.RoomID].state = Room.RoomState.Gaming;

                Console.WriteLine($"玩家:{player.playerName}開始遊戲成功");

                foreach (var each in Server.rooms[receive.RoomID].players)
                {
                    if (each == player)
                    {
                        result.Suc = true;
                        result.First = true;
                        data = NetworkUtils.Serialize(result);
                        each.tcpSend(MessageType.StartGame, data);
                    }
                    else
                    {
                        result.Suc = true;
                        result.First = false;
                        data = NetworkUtils.Serialize(result);
                        each.tcpSend(MessageType.StartGame, data);
                    }
                }

                if (Server.rooms[receive.RoomID].observers.Count > 0)
                {
                    result.Suc = true;
                    result.Watch = true;
                    data = NetworkUtils.Serialize(result);

                    foreach (var each in Server.rooms[receive.RoomID].observers)
                    {
                        each.tcpSend(MessageType.StartGame, data);
                    }
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.playerName}開始遊戲失敗");

                data = NetworkUtils.Serialize(result);
                player.tcpSend(MessageType.StartGame, data);
            }
        }
        else//這邊配合上面是當初為了檢測哪裡的邏輯有問題才弄成這樣的 有空可以改掉
        {
            Console.WriteLine($"玩家:{player.playerName}開始遊戲失敗");

            data = NetworkUtils.Serialize(result);
            player.tcpSend(MessageType.StartGame, data);
        }
    }
    #endregion

    #region UDP註冊
    private void _PlayBall(Player player, byte[] data)
    {
        PlayBall result = new PlayBall();
        PlayBall receive = NetworkUtils.Deserialize<PlayBall>(data);

        //邏輯判斷 如果這邊進不去的話可以把每個條件分開篩選
        if (Server.rooms.ContainsKey(receive.RoomID) &&
            Server.rooms[receive.RoomID].players.Contains(player) &&
            Server.rooms[receive.RoomID].state == Room.RoomState.Gaming)
        {
            Baller baller = Server.rooms[receive.RoomID].gamePlay.Calculate(receive.input);

            bool over = _inputResult(baller, result);

            if (result.Suc)
            {
                result.player = receive.player;
                string result_name = receive.name;
                result.input = receive.input;
                result.x = receive.x;
                result.y = receive.y;
                result.z = receive.z;

                //向該房間中玩家與觀察者廣播結果
                data = NetworkUtils.Serialize(result);

                foreach (var each in Server.rooms[receive.RoomID].players)
                {
                    each.udpSend(MessageType.PlayBall, data);
                }

                //當遊戲結束
                if (over)
                {
                    Console.WriteLine("遊戲結束, 房間關閉");
                    Server.rooms[receive.RoomID].Close();
                }
            }
            else
            {
                Console.WriteLine($"玩家 : {player.playerName}操作失敗, result.Suc == false");

                //向玩家發送操作失敗結果
                data = NetworkUtils.Serialize(result);
                player.udpSend(MessageType.PlayBall, data);
            }
        }
        else
        {
            Console.WriteLine($"玩家 : {player.playerName}操作失敗, 邏輯有誤");

            //向玩家發送操作失敗結果
            data = NetworkUtils.Serialize(result);
            player.udpSend(MessageType.PlayBall, data);
        }
    }
    #endregion

    private bool _inputResult(Baller baller, PlayBall result)
    {
        bool over = false;

        result.Suc = true;

        return over;
    }
}