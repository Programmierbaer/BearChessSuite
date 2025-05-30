﻿using System;
using System.Xml.Serialization;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessBase.Implementations
{
    [Serializable]
    public class BCServerMove : ICloneable
    {

        
        private string _pgnMove;

        public int Figure
        {
            get;
            set;
        }

        public int FigureColor
        {
            get;
            set;
        }

        public int FromField
        {
            get;
            set;
        }

        public string FromFieldName
        {
            get;
            set;
        }

        public int ToField
        {
            get;
            set;
        }

        public string ToFieldName
        {
            get;
            set;
        }

        public int CapturedFigure
        {
            get;
            set;
        }

        public int PromotedFigure
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }


        public int CapturedFigureMaterial
        {
            get;
            set;
        }

        public int Identifier
        {
            get;
            set;
        }

        public bool IsEngineMove
        {
            get;
            set;
        }

        public string CheckOrMateSign
        {
            get;
            set;
        }

        public string Comment
        {
            get;
            set;
        }

        public string EvaluationSymbol
        {
            get;
            set;
        }

        public string MoveSymbol
        {
            get;
            set;
        }

        public string OwnSymbol
        {
            get;
            set;
        }

        public string ShortMoveIdentifier
        {
            get;
            set;
        }

        public string ElapsedMoveTime
        {
            get;
            set;
        }

      
        [XmlIgnore]
        public string Fen
        {
            get;
            set;
        }



      

        [XmlIgnore]
        public BoardEvaluation[] BoardEvaluations
        {
            get;
            set;
        }

        public BCServerMove()
        {
        }



        public BCServerMove(Move move)
        {
            Figure = move.Figure;
            FigureColor = move.FigureColor;
            FromField = move.FromField;
            ToField = move.ToField;
            Value = move.Value;
            CapturedFigure = move.CapturedFigure;
            PromotedFigure = move.PromotedFigure;
            Identifier = FromField * 100 + ToField;
            FromFieldName = move.FromFieldName;
            ToFieldName = move.ToFieldName;
            IsEngineMove = move.IsEngineMove;
            Comment = string.Empty;
            EvaluationSymbol = move.EvaluationSymbol;
            MoveSymbol = move.MoveSymbol;
            OwnSymbol = move.OwnSymbol;
            ShortMoveIdentifier = move.ShortMoveIdentifier;
            ElapsedMoveTime = move.ElapsedMoveTime;
            Fen = move.Fen;

        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return $"{FromFieldName}-{ToFieldName} ({Value})";
        }
    }

    [Serializable]
    public class Move : ICloneable
    {

        public static readonly int[] MoveOffsets = { -9, -11, 9, 11, -10, 10, 1, -1, 19, 21, 12, -8, -19, -21, -12, 8 };
        private string _pgnMove;

        public int Figure
        {
            get;
            set;
        }

        public int FigureColor
        {
            get;
            set;
        }

        public int FromField
        {
            get;
            set;
        }

        public string FromFieldName
        {
            get;
            set;
        }

        public int ToField
        {
            get;
            set;
        }

        public string ToFieldName
        {
            get;
            set;
        }

        public int CapturedFigure
        {
            get;
            set;
        }

        public int PromotedFigure
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        }

        public decimal Score
        {
            get;
            set;
        }

        public string BestLine
        {
            get;
            set;
        }

        public int CapturedFigureMaterial
        {
            get;
            set;
        }

        public int Identifier
        {
            get;
            set;
        }

        public bool IsEngineMove
        {
            get;
            set;
        }

        public string CheckOrMateSign
        {
            get;
            set;
        }

        public string Comment
        {
            get;
            set;
        }

        public string EvaluationSymbol
        {
            get;
            set;
        }

        public string MoveSymbol
        {
            get;
            set;
        }

        public string OwnSymbol
        {
            get;
            set;
        }

        public string ShortMoveIdentifier
        {
            get;
            set;
        }

        public string ElapsedMoveTime
        {
            get;
            set;
        }

        public string BuddyEngine
        {
            get;
            set;
        }

        public string BestLineBuddy
        {
            get;
            set;
        }

        public decimal ScoreBuddy
        {
            get;
            set;
        }

        [XmlIgnore]
        public string Fen
        {
            get;
            set;
        }



        [XmlIgnore]
        public string PGNMove
        {
            get => GetPGNMove();
            set => _pgnMove = value;
        }

        [XmlIgnore]
        public BoardEvaluation[] BoardEvaluations
        {
            get;
            set;
        }

        public Move()
        {
        }

        public Move(int fromField, int toField, int color, int figureId)
        {
            Figure = figureId;
            FigureColor = color;
            FromField = fromField;
            ToField = toField;
            Value = int.MinValue;
            CapturedFigure = FigureId.NO_PIECE;
            PromotedFigure = FigureId.NO_PIECE;
            Identifier = fromField * 100 + toField;
            FromFieldName = Fields.GetFieldName(FromField);
            ToFieldName = Fields.GetFieldName(ToField);
            Score = 0;
            ScoreBuddy = 0;
            BestLine = string.Empty;
            BestLineBuddy = string.Empty;
            IsEngineMove = false;
            Comment = string.Empty;
            EvaluationSymbol = string.Empty;
            MoveSymbol = string.Empty;
            OwnSymbol = string.Empty;
            ShortMoveIdentifier = string.Empty;
            ElapsedMoveTime = string.Empty;
            Fen = string.Empty;
            BestLineBuddy = string.Empty;
        }

        public Move(int fromField, int toField, int color, int figureId, IChessFigure capturedFigure) : this(fromField,
            toField, color, figureId)
        {
            CapturedFigure = capturedFigure.FigureId;
            CapturedFigureMaterial = capturedFigure.Material;
        }

        public Move(int fromField, int toField, int color, int figureId, IChessFigure capturedFigure,
            int promotedFigure) : this(fromField, toField, color, figureId)
        {
            CapturedFigure = capturedFigure.FigureId;
            CapturedFigureMaterial = capturedFigure.Material;
            PromotedFigure = promotedFigure;

        }

        public Move(int fromField, int toField, int color, int figureId, int promotedFigure) : this(fromField, toField,
            color, figureId)
        {
            PromotedFigure = promotedFigure;
        }

        public Move(int fromField, int toField, int color, int figureId, IChessFigure capturedFigure,
            int promotedFigure, decimal score, string bestLine) : this(fromField, toField, color, figureId,
            capturedFigure, promotedFigure)
        {
            Score = score;
            BestLine = bestLine;
            IsEngineMove = true;
        }

        public Move(int fromField, int toField, int color, int figureId, int promotedFigure, decimal score,
            string bestLine) :
            this(fromField, toField, color, figureId, promotedFigure)
        {
            Score = score;
            BestLine = bestLine;
            IsEngineMove = true;
        }

        public Move(int fromField, int toField, int color, int figureId, decimal score, string bestLine) :
            this(fromField, toField, color, figureId)
        {
            Score = score;
            BestLine = bestLine;
            IsEngineMove = true;
        }


        public Move(Move move)
        {
            FigureColor = move.FigureColor;
            FromField = move.FromField;
            ToField = move.ToField;
            Value = move.Value;
            CapturedFigure = move.CapturedFigure;
            CapturedFigureMaterial = move.CapturedFigureMaterial;
            PromotedFigure = move.PromotedFigure;
            Identifier = move.Identifier;
            FromFieldName = move.FromFieldName;
            ToFieldName = move.ToFieldName;
            Score = move.Score;
            ScoreBuddy = move.ScoreBuddy;
            BestLine = move.BestLine;
            BestLineBuddy = move.BestLineBuddy;
            IsEngineMove = move.IsEngineMove;
            Comment = move.Comment;
            EvaluationSymbol = move.EvaluationSymbol;
            MoveSymbol = move.MoveSymbol;
            OwnSymbol = move.OwnSymbol;
            ShortMoveIdentifier = move.ShortMoveIdentifier;
            ElapsedMoveTime = move.ElapsedMoveTime;
            Fen = move.Fen;
            BestLineBuddy = move.BestLineBuddy;
        }

        private string GetPGNMove()
        {
            if (!string.IsNullOrWhiteSpace(_pgnMove))
            {
                return _pgnMove;
            }

            var figureSymbol = FigureId.FigureIdToFenCharacter[Figure].ToUpper();
            if (figureSymbol.Equals("P"))
            {
                _pgnMove = figureSymbol.Equals("P") ? ToFieldName : figureSymbol + ToFieldName;
            }

            return _pgnMove;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            return $"{FromFieldName}-{ToFieldName} ({Value})";
        }
    }
}