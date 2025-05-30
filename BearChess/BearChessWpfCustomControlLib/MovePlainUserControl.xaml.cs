﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für MovePlainUserControl.xaml
    /// </summary>
    public partial class MovePlainUserControl : UserControl
    {
        private readonly Window _parent;
        private readonly FontFamily _fontFamily;
        private DisplayFigureType _figureType = DisplayFigureType.Symbol;
        private DisplayMoveType _moveType = DisplayMoveType.FromToField;
        private DisplayCountryType _countryType = DisplayCountryType.GB;
        private bool _showFullInformation;
        private bool _showOnlyMoves;
        private bool _showComments;
        private bool _showForWhite;
        private bool _showBuddy;
        private readonly ResourceManager _rm;

        public MovePlainUserControl(Window parent)
        {
            _parent = parent;
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            _fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Chess Merida");
            _showOnlyMoves = false;
            _showFullInformation = true;
        }
        public Move CurrentMove { get; private set; }
        public int CurrentMoveNumber { get; private set; }

        public event EventHandler SelectedChanged;
        public event EventHandler ContentChanged;
        public event EventHandler RestartEvent;

        public void SetDisplayTypes(DisplayFigureType figureType, DisplayMoveType moveType, DisplayCountryType countryType)
        {
            _figureType = figureType;
            _moveType = moveType;
            _countryType = countryType;
        }

        public void SetInformationDetails(bool showOnlyMoves, bool showFullInformation, bool showComments, bool showForWhite, bool showBuddy)
        {
            _showOnlyMoves = showOnlyMoves;
            _showFullInformation = showFullInformation;
            _showComments = showComments;
            _showForWhite = showForWhite;
            _showBuddy = showBuddy;
        }

        public void UnMark()
        {
            button.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }

        public void SetMove(Move move, int moveNumber, bool newPanelAdded, List<Move> moveList)
        {
            CurrentMove = move;
            CurrentMoveNumber = moveNumber;
            if (move.FigureColor == Fields.COLOR_WHITE)
            {
                textBlockMoveNumber.Text = $"{moveNumber}. ";
            }
            else
            {
                if (newPanelAdded)
                {
                    textBlockMoveNumber.Text = $"{moveNumber}. ... ";
                }
                else
                {
                    textBlockMoveNumber.Visibility = Visibility.Collapsed;
                }
            }

            textBlockMove.Text = GetMoveDisplay(
                $"{move.FromFieldName}{move.ToFieldName}".ToLower(),
                move.Figure, move.CapturedFigure, FigureId.NO_PIECE, move.ShortMoveIdentifier);
            if (!string.IsNullOrWhiteSpace(move.EvaluationSymbol))
            {
                textBlockMoveEvaluation.Text = move.EvaluationSymbol;
                textBlockMoveEvaluation.Visibility = Visibility.Visible;
            }


            if (!string.IsNullOrWhiteSpace(move.MoveSymbol))
            {
                textBlockMoveSymbol.Text = move.MoveSymbol;
                textBlockMoveSymbol.Visibility = Visibility.Visible;
            }
            if (!string.IsNullOrWhiteSpace(move.CheckOrMateSign))
            {
                textBlockCheckMateSymbol.Text = move.CheckOrMateSign;
                textBlockCheckMateSymbol.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrWhiteSpace(move.OwnSymbol))
            {
                textBlockOwnSymbol.Text = move.OwnSymbol;
                textBlockOwnSymbol.Visibility = Visibility.Visible;
            }

            var s = FigureId.FigureIdToFenCharacter[move.Figure];
            textBlockComment.Visibility = Visibility.Collapsed;
            textBlockBestLine.Visibility = Visibility.Collapsed;
            textBlockMoveValue.Visibility = Visibility.Collapsed;
            textBlockBestLineBuddy.Visibility = Visibility.Collapsed;
            textBlockMoveValueBuddy.Visibility = Visibility.Collapsed;
            textBlockCommentInternal.Visibility = Visibility.Collapsed;
            if (!_showOnlyMoves)
            {
                if (!string.IsNullOrWhiteSpace(move.BestLine))
                {
                    if (move.IsEngineMove)
                    {
                        var score = move.Score;
                        textBlockBestLine.Visibility = Visibility.Visible;
                        textBlockMoveValue.Visibility = Visibility.Visible;
                        if (move.FigureColor == Fields.COLOR_BLACK && _showForWhite)
                        {
                            score = -score;
                        }

                        textBlockMoveValue.Text = score.ToString(CultureInfo.InvariantCulture);

                        textBlockMoveValue.Foreground =
                            score < 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                        if (_showFullInformation)
                        {
                            textBlockBestLine.Text = GetBestLine(moveList, move.BestLine);
                        }
                        else
                        {
                            var strings = move.BestLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            textBlockBestLine.Text = strings.Length > 1 ? strings[0] : string.Empty;
                        }
                    }

                }

                if (_showBuddy && !string.IsNullOrWhiteSpace(move.BestLineBuddy))
                {
                    var score = move.ScoreBuddy;
                    textBlockBestLineBuddy.Visibility = Visibility.Visible;
                    textBlockMoveValueBuddy.Visibility = Visibility.Visible;
                    // Buddy score is always point of view for white => Need to invert
                    if (move.FigureColor == Fields.COLOR_BLACK && !_showForWhite)
                    {
                        score = -score;
                    }

                    textBlockMoveValueBuddy.Text = score.ToString(CultureInfo.InvariantCulture);

                    textBlockMoveValueBuddy.Foreground =
                        score < 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                    if (_showFullInformation)
                    {
                        textBlockBestLineBuddy.Text = GetBestLine(moveList, move.BestLineBuddy);
                    }
                    else
                    {
                        var strings = move.BestLineBuddy.Split(" ".ToCharArray(),
                            StringSplitOptions.RemoveEmptyEntries);
                        textBlockBestLineBuddy.Text = strings.Length > 1 ? strings[0] : string.Empty;
                    }
                }

                if (_showComments)
                {
                    var comment = move.Comment?.Replace("(", " ").Replace(")", " ").Replace("{", " ").Replace("}", " ");
                    textBlockCommentInternal.Text = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment;
                    textBlockCommentInternal.Text += string.IsNullOrWhiteSpace(move.ElapsedMoveTime) ? string.Empty : ConvertEMT(move.ElapsedMoveTime);
                    textBlockCommentInternal.Visibility = textBlockCommentInternal.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                if (_showComments)
                {
                    var comment = move.Comment?.Replace("(", " ").Replace(")", " ").Replace("{", " ").Replace("}", " ");
                    textBlockCommentInternal.Text = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment;
                    textBlockCommentInternal.Text += string.IsNullOrWhiteSpace(move.ElapsedMoveTime) ? string.Empty : ConvertEMT(move.ElapsedMoveTime);
                    textBlockCommentInternal.Visibility = textBlockCommentInternal
                        .Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }


            if (!textBlockMove.Text.StartsWith("0-"))
            {
                if (!s.ToUpper().Equals("P") && s.Length > 0)
                {
                    if (_figureType == DisplayFigureType.Symbol)
                    {
                        textBlockFigureSymbol.FontFamily = _fontFamily;
                        textBlockFigureSymbol.Text = FontConverter.ConvertFont(s, "Chess Merida");
                    }
                    else
                    {
                        textBlockFigureSymbol.Text = DisplayCountryHelper.CountryLetter(s.ToUpper(), _countryType);
                    }
                }
                else
                {
                    textBlockFigureSymbol.Visibility = Visibility.Collapsed;
                }

                if (move.PromotedFigure != FigureId.NO_PIECE)
                {

                    var fenCharacter = FigureId.FigureIdToFenCharacter[move.PromotedFigure];
                    if (_figureType == DisplayFigureType.Symbol)
                    {
                        textBlockPromotionSymbol.FontFamily = _fontFamily;
                        textBlockPromotionSymbol.Text = FontConverter.ConvertFont(fenCharacter, "Chess Merida");
                    }
                    else
                    {
                        textBlockPromotionSymbol.Text = DisplayCountryHelper.CountryLetter(fenCharacter.ToUpper(), _countryType);
                    }
                    textBlockPromotionSymbol.Visibility = Visibility.Visible;
                }

                if (_moveType == DisplayMoveType.ToField)
                {
                    if (s.Equals("P", StringComparison.OrdinalIgnoreCase) &&
                        textBlockMove.Text.StartsWith("x"))
                    {
                        textBlockMove.Text = move.FromFieldName.Substring(0, 1).ToLower() + textBlockMove.Text;
                    }
                }
            }
            else
            {
                textBlockFigureSymbol.Visibility = Visibility.Collapsed;
            }
        }

        private string ConvertEMT(string emt)
        {
            var result = string.Empty;
            var strings = emt.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length != 3)
            {
                return emt;
            }

            if (!strings[0].Equals("0") && !strings[0].Equals("00"))
            {
                result += strings[0] + "h ";
            }
            if (!strings[1].Equals("0") && !strings[1].Equals("00"))
            {
                result += strings[1] + "m ";
            }
            if (!strings[2].Equals("0") && !strings[2].Equals("00"))
            {
                result += strings[2] + "s";
            }

            return string.IsNullOrWhiteSpace(result) ? "0s" : result;
        }

        private string GetMoveDisplay(string move, int figureId, int capturedFigureId, int promotedFigureId, string shortMoveIdentifier)
        {
            if (move.StartsWith("0"))
            {
                return move;
            }

            if (figureId == FigureId.WHITE_KING)
            {
                if (move.Equals("e1g1",StringComparison.OrdinalIgnoreCase))
                {
                    return "0-0";
                }

                if (move.Equals("e1c1", StringComparison.OrdinalIgnoreCase))
                {
                    return "0-0-0";
                }
            }

            if (figureId == FigureId.BLACK_KING)
            {
                if (move.Equals("e8g8", StringComparison.OrdinalIgnoreCase))
                {
                    return "0-0";
                }

                if (move.Equals("e8c8", StringComparison.OrdinalIgnoreCase))
                {
                    return "0-0-0";
                }
            }

            var p = string.Empty;
            if (promotedFigureId != FigureId.NO_PIECE)
            {
                p = DisplayCountryHelper.CountryLetter(FigureId.FigureIdToFenCharacter[promotedFigureId].ToUpper(), _countryType);
            }

            if (_moveType == DisplayMoveType.FromToField)
            {
                if (capturedFigureId == FigureId.OUTSIDE_PIECE || capturedFigureId == FigureId.NO_PIECE)
                {
                    return move.Substring(0, 2) + "-" + move.Substring(2) + p;
                }

                return move.Substring(0, 2) + "x" + move.Substring(2) + p;
            }

            if (capturedFigureId == FigureId.OUTSIDE_PIECE || capturedFigureId == FigureId.NO_PIECE)
            {
                return shortMoveIdentifier + move.Substring(2) + p;
            }

            return shortMoveIdentifier + "x" + move.Substring(2) + p;
        }

        private void MenuItemMoveSymbol_OnClick(object sender, RoutedEventArgs e)
        {
            HandleMoveContextMenu(sender, textBlockMoveSymbol, true);
        }

        private void MenuItemMoveEvaluation_OnClick(object sender, RoutedEventArgs e)
        {
            HandleMoveContextMenu(sender, textBlockMoveEvaluation, false);
        }

        private void HandleMoveContextMenu(object sender, TextBlock textBlock, bool moveSymbol)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.HasHeader)
                {
                    var iconSymbol = menuItem.Icon.ToString();
                    if (textBlock.Visibility == Visibility.Visible ||
                        iconSymbol.Equals("System.Windows.Controls.Image"))
                    {
                        if (textBlock.Text.Equals(iconSymbol) || iconSymbol.Equals("System.Windows.Controls.Image"))
                        {
                            textBlock.Text = string.Empty;
                            textBlock.Visibility = Visibility.Collapsed;
                            if (moveSymbol)
                            {
                                CurrentMove.MoveSymbol = string.Empty;
                            }
                            else
                            {
                                CurrentMove.EvaluationSymbol = string.Empty;
                            }

                            return;
                        }
                    }

                    textBlock.Visibility = Visibility.Visible;
                    textBlock.Text = iconSymbol;
                    if (moveSymbol)
                    {
                        CurrentMove.MoveSymbol = iconSymbol;
                    }
                    else
                    {
                        CurrentMove.EvaluationSymbol = iconSymbol;
                    }

                    ContentChanged?.Invoke(this, null);
                }
            }
        }

        public void SetFocus()
        {
            Keyboard.Focus(button);
            ButtonBase_OnClick(this, null);
        }

        public void Mark()
        {
            button.BorderBrush = new SolidColorBrush(Colors.Salmon);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedChanged?.Invoke(this, e);
        }

        private void MenuItemEditComment_OnClick(object sender, RoutedEventArgs e)
        {
            var editCommentWindow = new EditCommentWindow(_rm.GetString("Comment"), CurrentMove.Comment)
            {
                Owner = _parent
            };
            var showDialog = editCommentWindow.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                textBlockComment.Text = editCommentWindow.Comment;
                textBlockComment.Visibility =
                    textBlockComment.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                CurrentMove.Comment = textBlockComment.Text;
                ContentChanged?.Invoke(this, e);
            }
        }

        private void MenuItemEditSymbol_OnClick(object sender, RoutedEventArgs e)
        {
            var editCommentWindow = new EditCommentWindow(_rm.GetString("Symbol"), CurrentMove.OwnSymbol)
            {
                Owner = _parent
            };
            var showDialog = editCommentWindow.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                textBlockOwnSymbol.Text = editCommentWindow.Comment;
                textBlockOwnSymbol.Visibility =
                    textBlockOwnSymbol.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                CurrentMove.OwnSymbol = textBlockOwnSymbol.Text;
                ContentChanged?.Invoke(this, e);
            }
        }

        private void MenuItemRestart_Click(object sender, RoutedEventArgs e)
        {
            RestartEvent?.Invoke(this, null);
        }

        private string GetBestLine(List<Move> moveList, string bestLine)
        {
            var fastChessBoard = new FastChessBoard();
            fastChessBoard.SetDisplayTypes(_figureType, _moveType, _countryType);
            var allMoves = moveList.Select(move => move.FromFieldName.ToLower() + move.ToFieldName.ToLower()).ToArray();
            var allMovesMinusOne= new string[allMoves.Length-1];
            Array.Copy(allMoves,allMovesMinusOne,allMoves.Length-1);
            fastChessBoard.Init(allMovesMinusOne);
            var ml = string.Empty;
            var strings = bestLine.Split(" ".ToCharArray());
            try
            {
                foreach (var s in strings)
                {
                    ml = ml + " " + fastChessBoard.GetMove(s);
                }
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(ml))
                {
                    ml = bestLine;
                }
                //
            }
            return ml;
        }
    }
}
