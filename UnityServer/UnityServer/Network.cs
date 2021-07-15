using System;
using UnityServerDLL;

public class Network
{
    public Network(string ip)
    {

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
    #endregion
}