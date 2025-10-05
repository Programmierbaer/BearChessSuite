using System;
using System.Collections.Generic;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChess.EChessBoard;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessServerLib
{
    public interface IBearChessController : IDisposable
    {
        event EventHandler<IMove> ChessMoveMade;
        event EventHandler<CurrentGame> NewGame;
        event EventHandler BCServerStarted;
        event EventHandler BCServerStopped;
        event EventHandler WebServerStarted;
        event EventHandler WebServerStopped;
        event EventHandler<string> ClientConnected;
        event EventHandler<string> ClientDisconnected;
        event EventHandler<string> ControllerMessage;
        event EventHandler<BearChessServerMessage> ClientMessage;
        bool BCServerIsOpen { get; }
        bool WebServerIsOpen { get; }
        bool PublishingActive { get; }
        string UciPath { get; }
        void MoveMade(string identification, string fromField, string toField, string awaitedFen, string pgnGame, int color);
        void StartStopBCServer();
        void StartStopWebServer();
        void StartStopPublishing();
        bool PublishIsConfigured();
        void AddWhiteEBoard(IElectronicChessBoard eBoard);
        void AddBlackEBoard(IElectronicChessBoard eBoard);
        void RemoveEBoard(IElectronicChessBoard eBoard);
        List<UciInfo> InstalledEngines();
        UciInfo SelectedEngine { get; set; }
        void TokenAssigned(string boardId, string token);
        void SendToClient(string clientAddr, BearChessServerMessage message);
        BearChessClientInformation[] GetCurrentConnectionList();
    }
}
