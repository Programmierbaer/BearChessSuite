﻿using System.Collections.Generic;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessBase.Interfaces
{
    public interface IChessBoard
    {
        /// <summary>
        /// Current figures on board
        /// </summary>
        HashSet<int> CurrentFigureList { get; }

        /// <summary>
        /// Returns <see cref="IChessFigure"/> on field <paramref name="field"/>.
        /// </summary>        
        IChessFigure GetFigureOn(int field);

        /// <summary>
        /// Returns all <see cref="IChessFigure"/>s on row <paramref name="row"/>.
        /// </summary>        
        IChessFigure[] GetFiguresOnRow(int row);

        /// <summary>
        /// Returns all <see cref="IChessFigure"/>s on line <paramref name="line"/>.
        /// </summary>        
        IChessFigure[] GetFiguresOnLine(Fields.Lines line);

        /// <summary>
        /// Returns all figures for color <paramref name="color"/>.
        /// </summary>
        IChessFigure[] GetFigures(int color);

        /// <summary>
        /// Returns the <see cref="IChessFigure"/> king for color <paramref name="color"/>.
        /// </summary>        
        IChessFigure GetKingFigure(int color);

        /// <summary>
        /// Static material value for color <paramref name="color"/>.
        /// </summary>
        int GetMaterialFor(int color);

        /// <summary>
        /// Current color 
        /// </summary>
        int CurrentColor { get; set; }

        /// <summary>
        /// Opposite color of <see cref="CurrentColor"/>
        /// </summary>
        int EnemyColor { get; }

        /// <summary>
        /// Only true if color <paramref name="color"/> already castled.
        /// </summary>
        bool Castled(int color);

        /// <summary>
        /// True if color <paramref name="color"/> can castling <paramref name="castlingEnum"/>.
        /// </summary>        
        bool CanCastling(int color, CastlingEnum castlingEnum);

        /// <summary>
        /// Sets the castling right <paramref name="castlingEnum"/> for <paramref name="color"/>.
        /// </summary>        
        void SetCanCastling(int color, CastlingEnum castlingEnum, bool canCastling);

        /// <summary>
        /// Initialize a new game to base position and values
        /// </summary>
        void NewGame();

        /// <summary>
        /// Make a move <paramref name="move"/>. Sets <see cref="CurrentColor"/> and <see cref="EnemyColor"/>.
        /// </summary>        
        void MakeMove(Move move);

        /// <summary>
        /// Make a move <paramref name="fromField"/> to <paramref name="toField"/>. Sets <see cref="CurrentColor"/> and <see cref="EnemyColor"/>.
        /// </summary>        
        void MakeMove(int fromField, int toField);

        /// <summary>
        /// Make a move from <paramref name="fromField"/> to <paramref name="toField"/>. Adjust <see cref="CurrentColor"/> and <see cref="EnemyColor"/>.
        /// </summary>
        void MakeMove(string fromField, string toField);

        /// <summary>
        /// Make a pawn move <paramref name="fromField"/> to <paramref name="toField"/> and the promotion figure <see cref="promotionFigureId"/>.
        /// </summary>     
        void MakeMove(string fromField, string toField, int promotionFigure);

        /// <summary>
        /// Make a pawn move <paramref name="fromField"/> to <paramref name="toField"/> and the promotion figure <see cref="promotionFigureId"/>.
        /// </summary>     
        void MakeMove(int fromField, int toField, int promotionFigureId);

        /// <summary>
        /// Make a pawn move <paramref name="fromField"/> to <paramref name="toField"/> and the promotion figure as fen symbol.
        /// </summary>     
        void MakeMove(string fromField, string toField, string promotionFigure);

        /// <summary>
        /// Make a move in PGN notation
        /// </summary>
        /// <param name="pgnMove">PGN notation</param>
        void MakePgnMove(string pgnMove, string comment, string emt);
        

        /// <summary>
        /// Returns the previous move
        /// </summary>
        /// <returns></returns>
        AllMoveClass GetPrevMove();

        /// <summary>
        /// Returns true if the move from <paramref name="fromField"/> to <paramref name="toField"/> is valid.
        /// </summary>
        bool MoveIsValid(int fromField, int toField);

        /// <summary>
        /// Set the board positions based on <paramref name="fenPosition"/>.
        /// </summary>        
        void SetPosition(string fenPosition, bool calculateCastle);

      
        /// <summary>
        /// Set the board positions based on <paramref name="moveNumber"/> and <paramref name="color"/>.
        /// </summary>        
        void SetPosition(int moveNumber, int color);

        /// <summary>
        /// Set the board current move based on <paramref name="moveNumber"/> and <paramref name="color"/>.
        /// </summary>
        void SetCurrentMove(int moveNumber, int color, string startFenPosition);

        /// <summary>
        /// Returns the fen string of the current position.
        /// </summary>        
        string GetFenPosition();

        /// <summary>
        /// Returns the fen string of the initial position if not started from base position
        /// </summary>        
        string GetInitialFenPosition();

        /// <summary>
        /// Set the figure <paramref name="figureId"/> on field <paramref name="field"/>.
        /// </summary>
        void SetFigureOnPosition(int figureId, int field);

        /// <summary>
        /// Removes a figure from field <paramref name="field"/>.
        /// </summary>
        void RemoveFigureFromField(int field);

        /// <summary>
        /// Generates and returns a new move list.
        /// </summary>
        List<Move> GenerateMoveList();

        /// <summary>
        /// Returns the move list for the current color.
        /// </summary>
        List<Move> CurrentMoveList { get; }

        /// <summary>
        /// Returns the move list of played moves
        /// </summary>
        Move[] GetPlayedMoveList();

        BCServerMove[] GetPlayedBCServerMoveList();

        /// <summary>
        /// Returns the move based on <paramref name="moveNumber"/> and <paramref name="color"/>.
        /// </summary>
        Move GetPlayedMove(int moveNumber, int color);

        /// <summary>
        /// Returns the fen position based on <paramref name="moveNumber"/> and <paramref name="color"/>.
        /// </summary>
        string GetPlayedFenPosition(int moveNumber, int color);

        /// <summary>
        /// Returns the fen positions for all possible moves/>.
        /// </summary>
        List<Move> GenerateFenPositionList();


        /// <summary>
        /// Returns the move list for the enemy color.
        /// </summary>
        List<Move> EnemyMoveList { get; }

        /// <summary>
        /// Initialized a new empty chessboard.
        /// </summary>
        void Init();

        /// <summary>
        /// Initialize a new chessboard as copy of <paramref name="chessBoard"/>.
        /// </summary>
        void Init(IChessBoard chessBoard);

        void SetGameAnalyzeMode(bool analyzeMode);

        string EnPassantTargetField { get; set; }
        int EnPassantTargetFieldNumber { get; set; }
        int HalfMoveClock { get; set; }
        int FullMoveNumber { get; set; }
        int GetBoardHash();

        /// <summary>
        /// Last captured figure.
        /// </summary>
        IChessFigure CapturedFigure { get; }

        /// <summary>
        /// Returns true if the <see cref="color"/> is in check.
        /// </summary>
        bool IsInCheck(int color);

        /// <summary>
        /// Returns the potential move to the next fen position <paramref name="fenPosition"/>. If <paramref name="ignoreRule"/> is true, the detected
        /// move is not checked against the rules. Otherwise it return "".
        /// </summary>
        string GetMove(string fenPosition, bool ignoreRule);

        /// <summary>
        /// Returns the changed figure between the fen positions <paramref name="oldFenPosition"/> and <paramref name="newFenPosition"/>.
        /// </summary>
        string GetChangedFigure(string oldFenPosition, string newFenPosition);

        /// <summary>
        /// Indicates if the given <paramref name="fenPosition"/> chess board start position
        /// </summary>
        bool IsBasePosition(string fenPosition);

        /// <summary>
        /// Indicates if there a draw by position repetition
        /// </summary>
        bool DrawByRepetition { get; }

        bool IsDraw { get; }

        /// <summary>
        /// Indicates if there a draw by to less material on board
        /// </summary>
        bool DrawByMaterial { get; }

        bool DrawBy50Moves { get; }

        int RemainingMovesFor50MovesDraw { get; }

        /// <summary>
        /// Take back the last move
        /// </summary>
        void TakeBack();
    }
}
