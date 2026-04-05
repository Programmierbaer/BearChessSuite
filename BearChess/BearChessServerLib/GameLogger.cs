using System;
using System.Collections.Generic;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChessServerLib;

public class GameLogger
{
    private class FenClass
    {
        public DateTime TimeStamp { get; set; }
        public string Fen { get; set; }
    }
    
    private readonly IBearChessController _bearChessController;

    public string ConnectionId { get; private set; }
    private  List<FenClass> _boardFenList { get; set; }
    private  List<FenClass> _publishFenList { get; set; }
        
    public GameLogger(string connectionId, IBearChessController bearChessController)
    {
        _bearChessController = bearChessController;
        ConnectionId = connectionId;
        _bearChessController.ClientMessage += BearChessControllerOnClientMessage;
    }

    private void BearChessControllerOnClientMessage(object sender, BearChessServerMessage e)
    {
        if (!e.Address.Equals(ConnectionId))
        {
            return;
        }

        if (e.ActionCode == "BOARDFEN")
        {
            _boardFenList.Add(new FenClass() {TimeStamp = DateTime.UtcNow,Fen = e.Message});
        }
        if (e.ActionCode == "PUBLISHFEN")
        {
            _publishFenList.Add(new FenClass() {TimeStamp = DateTime.UtcNow,Fen = e.Message});
        }
    }
}