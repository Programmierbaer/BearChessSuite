using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessWin;

namespace www.SoLaNoSoft.com.BearChessServerWin.UserControls
{
    /// <summary>
    /// Interaktionslogik für DetailChessBoardUserControl.xaml
    /// </summary>
    public partial class DetailChessBoardUserControl : UserControl
    {
        private class ArrowDescription
        {
            public SolidColorBrush Color;
            public int FromField;
            public int ToField;
        }

        public static readonly DependencyProperty ChessFieldSizeProperty = DependencyProperty.Register(
            "ChessFieldSize", typeof(double), typeof(DetailChessBoardUserControl), new PropertyMetadata((double)38));

        private readonly Dictionary<int, Image> _piecesBorderBitmaps = new Dictionary<int, Image>();
        private readonly Dictionary<string, BitmapImage> _piecesBitmaps = new Dictionary<string, BitmapImage>();
        private readonly List<ArrowDescription> _arrowList = new List<ArrowDescription>();
        private readonly ConcurrentDictionary<int, bool> _markedGreenFields = new ConcurrentDictionary<int, bool>();
        private static object _lock = new object();
        private string _whitePlayer = string.Empty;
        private string _blackPlayer = string.Empty;
        private Canvas _canvas;
        private SolidColorBrush _hintArrowColor;
        private SolidColorBrush _bookArrowColor;
        private int _fromFieldTag = 0;
        private int _toFieldTag = 0;

        public bool WhiteOnTop { get; set; } = true;

        public DetailChessBoardUserControl()
        {
            InitializeComponent();
            TagFields();
            SetPiecesMaterial();
            DrawAnalyses(0);
        }
        private BitmapImage GetImageResource(int convert)
        {
            
            var fieldName = Fields.GetFieldName(convert);
            var name = $"bitmapStone{fieldName}";

            return FindResource(name) as BitmapImage;
        }

        public void SetCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }


        public void SetPiecesMaterial()
        {
            _piecesBitmaps["K"] = FindResource("bitmapStoneSymbolKingW") as BitmapImage;
            _piecesBitmaps["Q"] = FindResource("bitmapStoneSymbolQueenW") as BitmapImage;
            _piecesBitmaps["R"] = FindResource("bitmapStoneSymbolRookW") as BitmapImage;
            _piecesBitmaps["B"] = FindResource("bitmapStoneSymbolBishopW") as BitmapImage;
            _piecesBitmaps["N"] = FindResource("bitmapStoneSymbolKnightW") as BitmapImage;
            _piecesBitmaps["P"] = FindResource("bitmapStoneSymbolPawnW") as BitmapImage;
            _piecesBitmaps["k"] = FindResource("bitmapStoneSymbolKingB") as BitmapImage;
            _piecesBitmaps["q"] = FindResource("bitmapStoneSymbolQueenB") as BitmapImage;
            _piecesBitmaps["r"] = FindResource("bitmapStoneSymbolRookB") as BitmapImage;
            _piecesBitmaps["b"] = FindResource("bitmapStoneSymbolBishopB") as BitmapImage;
            _piecesBitmaps["n"] = FindResource("bitmapStoneSymbolKnightB") as BitmapImage;
            _piecesBitmaps["p"] = FindResource("bitmapStoneSymbolPawnB") as BitmapImage;
            _piecesBitmaps[""] = null;
            _piecesBitmaps[" "] = null;
        }

        public void RepaintBoard(IChessBoard chessBoard)
        {
            imageFA1.Source = GetImageResource(Fields.FA1) as BitmapImage;
            imageFA2.Source = GetImageResource(Fields.FA2) as BitmapImage;
            imageFA3.Source = GetImageResource(Fields.FA3) as BitmapImage;
            imageFA4.Source = GetImageResource(Fields.FA4) as BitmapImage;
            imageFA5.Source = GetImageResource(Fields.FA5) as BitmapImage;
            imageFA6.Source = GetImageResource(Fields.FA6) as BitmapImage;
            imageFA7.Source = GetImageResource(Fields.FA7) as BitmapImage;
            imageFA8.Source = GetImageResource(Fields.FA8) as BitmapImage;
            imageFB1.Source = GetImageResource(Fields.FB1) as BitmapImage;
            imageFB2.Source = GetImageResource(Fields.FB2) as BitmapImage;
            imageFB3.Source = GetImageResource(Fields.FB3) as BitmapImage;
            imageFB4.Source = GetImageResource(Fields.FB4) as BitmapImage;
            imageFB5.Source = GetImageResource(Fields.FB5) as BitmapImage;
            imageFB6.Source = GetImageResource(Fields.FB6) as BitmapImage;
            imageFB7.Source = GetImageResource(Fields.FB7) as BitmapImage;
            imageFB8.Source = GetImageResource(Fields.FB8) as BitmapImage;
            imageFC1.Source = GetImageResource(Fields.FC1) as BitmapImage;
            imageFC2.Source = GetImageResource(Fields.FC2) as BitmapImage;
            imageFC3.Source = GetImageResource(Fields.FC3) as BitmapImage;
            imageFC4.Source = GetImageResource(Fields.FC4) as BitmapImage;
            imageFC5.Source = GetImageResource(Fields.FC5) as BitmapImage;
            imageFC6.Source = GetImageResource(Fields.FC6) as BitmapImage;
            imageFC7.Source = GetImageResource(Fields.FC7) as BitmapImage;
            imageFC8.Source = GetImageResource(Fields.FC8) as BitmapImage;
            imageFD1.Source = GetImageResource(Fields.FD1) as BitmapImage;
            imageFD2.Source = GetImageResource(Fields.FD2) as BitmapImage;
            imageFD3.Source = GetImageResource(Fields.FD3) as BitmapImage;
            imageFD4.Source = GetImageResource(Fields.FD4) as BitmapImage;
            imageFD5.Source = GetImageResource(Fields.FD5) as BitmapImage;
            imageFD6.Source = GetImageResource(Fields.FD6) as BitmapImage;
            imageFD7.Source = GetImageResource(Fields.FD7) as BitmapImage;
            imageFD8.Source = GetImageResource(Fields.FD8) as BitmapImage;
            imageFE1.Source = GetImageResource(Fields.FE1) as BitmapImage;
            imageFE2.Source = GetImageResource(Fields.FE2) as BitmapImage;
            imageFE3.Source = GetImageResource(Fields.FE3) as BitmapImage;
            imageFE4.Source = GetImageResource(Fields.FE4) as BitmapImage;
            imageFE5.Source = GetImageResource(Fields.FE5) as BitmapImage;
            imageFE6.Source = GetImageResource(Fields.FE6) as BitmapImage;
            imageFE7.Source = GetImageResource(Fields.FE7) as BitmapImage;
            imageFE8.Source = GetImageResource(Fields.FE8) as BitmapImage;
            imageFF1.Source = GetImageResource(Fields.FF1) as BitmapImage;
            imageFF2.Source = GetImageResource(Fields.FF2) as BitmapImage;
            imageFF3.Source = GetImageResource(Fields.FF3) as BitmapImage;
            imageFF4.Source = GetImageResource(Fields.FF4) as BitmapImage;
            imageFF5.Source = GetImageResource(Fields.FF5) as BitmapImage;
            imageFF6.Source = GetImageResource(Fields.FF6) as BitmapImage;
            imageFF7.Source = GetImageResource(Fields.FF7) as BitmapImage;
            imageFF8.Source = GetImageResource(Fields.FF8) as BitmapImage;
            imageFG1.Source = GetImageResource(Fields.FG1) as BitmapImage;
            imageFG2.Source = GetImageResource(Fields.FG2) as BitmapImage;
            imageFG3.Source = GetImageResource(Fields.FG3) as BitmapImage;
            imageFG4.Source = GetImageResource(Fields.FG4) as BitmapImage;
            imageFG5.Source = GetImageResource(Fields.FG5) as BitmapImage;
            imageFG6.Source = GetImageResource(Fields.FG6) as BitmapImage;
            imageFG7.Source = GetImageResource(Fields.FG7) as BitmapImage;
            imageFG8.Source = GetImageResource(Fields.FG8) as BitmapImage;
            imageFH1.Source = GetImageResource(Fields.FH1) as BitmapImage;
            imageFH2.Source = GetImageResource(Fields.FH2) as BitmapImage;
            imageFH3.Source = GetImageResource(Fields.FH3) as BitmapImage;
            imageFH4.Source = GetImageResource(Fields.FH4) as BitmapImage;
            imageFH5.Source = GetImageResource(Fields.FH5) as BitmapImage;
            imageFH6.Source = GetImageResource(Fields.FH6) as BitmapImage;
            imageFH7.Source = GetImageResource(Fields.FH7) as BitmapImage;
            imageFH8.Source = GetImageResource(Fields.FH8) as BitmapImage;
            if (_piecesBitmaps.Count > 0)
            {
                if (WhiteOnTop)
                {
                    imageA1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA1).FigureCharacter];
                    imageA2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA2).FigureCharacter];
                    imageA3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA3).FigureCharacter];
                    imageA4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA4).FigureCharacter];
                    imageA5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA5).FigureCharacter];
                    imageA6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA6).FigureCharacter];
                    imageA7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA7).FigureCharacter];
                    imageA8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA8).FigureCharacter];
                    imageB1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB1).FigureCharacter];
                    imageB2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB2).FigureCharacter];
                    imageB3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB3).FigureCharacter];
                    imageB4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB4).FigureCharacter];
                    imageB5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB5).FigureCharacter];
                    imageB6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB6).FigureCharacter];
                    imageB7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB7).FigureCharacter];
                    imageB8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB8).FigureCharacter];
                    imageC1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC1).FigureCharacter];
                    imageC2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC2).FigureCharacter];
                    imageC3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC3).FigureCharacter];
                    imageC4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC4).FigureCharacter];
                    imageC5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC5).FigureCharacter];
                    imageC6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC6).FigureCharacter];
                    imageC7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC7).FigureCharacter];
                    imageC8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC8).FigureCharacter];
                    imageD1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD1).FigureCharacter];
                    imageD2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD2).FigureCharacter];
                    imageD3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD3).FigureCharacter];
                    imageD4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD4).FigureCharacter];
                    imageD5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD5).FigureCharacter];
                    imageD6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD6).FigureCharacter];
                    imageD7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD7).FigureCharacter];
                    imageD8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD8).FigureCharacter];
                    imageE1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE1).FigureCharacter];
                    imageE2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE2).FigureCharacter];
                    imageE3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE3).FigureCharacter];
                    imageE4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE4).FigureCharacter];
                    imageE5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE5).FigureCharacter];
                    imageE6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE6).FigureCharacter];
                    imageE7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE7).FigureCharacter];
                    imageE8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE8).FigureCharacter];
                    imageF1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF1).FigureCharacter];
                    imageF2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF2).FigureCharacter];
                    imageF3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF3).FigureCharacter];
                    imageF4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF4).FigureCharacter];
                    imageF5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF5).FigureCharacter];
                    imageF6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF6).FigureCharacter];
                    imageF7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF7).FigureCharacter];
                    imageF8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF8).FigureCharacter];
                    imageG1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG1).FigureCharacter];
                    imageG2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG2).FigureCharacter];
                    imageG3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG3).FigureCharacter];
                    imageG4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG4).FigureCharacter];
                    imageG5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG5).FigureCharacter];
                    imageG6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG6).FigureCharacter];
                    imageG7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG7).FigureCharacter];
                    imageG8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG8).FigureCharacter];
                    imageH1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH1).FigureCharacter];
                    imageH2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH2).FigureCharacter];
                    imageH3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH3).FigureCharacter];
                    imageH4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH4).FigureCharacter];
                    imageH5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH5).FigureCharacter];
                    imageH6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH6).FigureCharacter];
                    imageH7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH7).FigureCharacter];
                    imageH8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH8).FigureCharacter];
                }
                else
                {
                    imageA1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH8).FigureCharacter];
                    imageA2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH7).FigureCharacter];
                    imageA3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH6).FigureCharacter];
                    imageA4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH5).FigureCharacter];
                    imageA5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH4).FigureCharacter];
                    imageA6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH3).FigureCharacter];
                    imageA7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH2).FigureCharacter];
                    imageA8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FH1).FigureCharacter];
                    imageB1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG8).FigureCharacter];
                    imageB2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG7).FigureCharacter];
                    imageB3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG6).FigureCharacter];
                    imageB4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG5).FigureCharacter];
                    imageB5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG4).FigureCharacter];
                    imageB6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG3).FigureCharacter];
                    imageB7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG2).FigureCharacter];
                    imageB8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FG1).FigureCharacter];
                    imageC1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF8).FigureCharacter];
                    imageC2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF7).FigureCharacter];
                    imageC3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF6).FigureCharacter];
                    imageC4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF5).FigureCharacter];
                    imageC5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF4).FigureCharacter];
                    imageC6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF3).FigureCharacter];
                    imageC7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF2).FigureCharacter];
                    imageC8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FF1).FigureCharacter];
                    imageD1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE8).FigureCharacter];
                    imageD2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE7).FigureCharacter];
                    imageD3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE6).FigureCharacter];
                    imageD4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE5).FigureCharacter];
                    imageD5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE4).FigureCharacter];
                    imageD6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE3).FigureCharacter];
                    imageD7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE2).FigureCharacter];
                    imageD8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FE1).FigureCharacter];
                    imageE1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD8).FigureCharacter];
                    imageE2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD7).FigureCharacter];
                    imageE3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD6).FigureCharacter];
                    imageE4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD5).FigureCharacter];
                    imageE5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD4).FigureCharacter];
                    imageE6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD3).FigureCharacter];
                    imageE7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD2).FigureCharacter];
                    imageE8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FD1).FigureCharacter];
                    imageF1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC8).FigureCharacter];
                    imageF2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC7).FigureCharacter];
                    imageF3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC6).FigureCharacter];
                    imageF4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC5).FigureCharacter];
                    imageF5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC4).FigureCharacter];
                    imageF6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC3).FigureCharacter];
                    imageF7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC2).FigureCharacter];
                    imageF8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FC1).FigureCharacter];
                    imageG1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB8).FigureCharacter];
                    imageG2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB7).FigureCharacter];
                    imageG3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB6).FigureCharacter];
                    imageG4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB5).FigureCharacter];
                    imageG5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB4).FigureCharacter];
                    imageG6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB3).FigureCharacter];
                    imageG7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB2).FigureCharacter];
                    imageG8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FB1).FigureCharacter];
                    imageH1.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA8).FigureCharacter];
                    imageH2.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA7).FigureCharacter];
                    imageH3.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA6).FigureCharacter];
                    imageH4.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA5).FigureCharacter];
                    imageH5.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA4).FigureCharacter];
                    imageH6.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA3).FigureCharacter];
                    imageH7.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA2).FigureCharacter];
                    imageH8.Source = _piecesBitmaps[chessBoard.GetFigureOn(Fields.FA1).FigureCharacter];
                }
            }

            if (WhiteOnTop)
            {
                textBlockWhiteKing.Visibility = Visibility.Visible;
                textBlockBlackKing.Visibility = Visibility.Collapsed;
                textBlockWhiteClock.Visibility = chessBoard.CurrentColor == Fields.COLOR_WHITE
                                                     ? Visibility.Visible
                                                     : Visibility.Hidden;
                textBlockBlackClock.Visibility = chessBoard.CurrentColor == Fields.COLOR_BLACK
                                                     ? Visibility.Visible
                                                     : Visibility.Hidden;
            }
            else
            {
                textBlockWhiteKing.Visibility = Visibility.Collapsed;
                textBlockBlackKing.Visibility = Visibility.Visible;
                textBlockWhiteClock.Visibility = chessBoard.CurrentColor == Fields.COLOR_BLACK
                                                     ? Visibility.Visible
                                                     : Visibility.Hidden;
                textBlockBlackClock.Visibility = chessBoard.CurrentColor == Fields.COLOR_WHITE
                                                     ? Visibility.Visible
                                                     : Visibility.Hidden;
            }
        }

        public void DrawAnalyses(double analysisValue)
        {
            lock (_lock)
            {
                var barHeight =  396d;
                canvasAnalyses.ToolTip = analysisValue;
                analysisValue *= 10;
                if (analysisValue > 100)
                {
                    analysisValue = 100;
                }

                if (analysisValue < -100)
                {
                    analysisValue = -100;
                }

                double middleValue = barHeight / 2d;

                var heightWhite = middleValue + (middleValue / 100d * analysisValue);
                if (heightWhite < 0)
                {
                    heightWhite = 0;
                }

                var heightBlack = barHeight - heightWhite;
                if (heightBlack < 0)
                {
                    heightBlack = 0;
                }

                var lineBlack = new Rectangle
                {
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 0.5,
                    Width = 9,
                    Height = heightBlack,
                    Fill = System.Windows.Media.Brushes.DarkGray
                };
                var lineWhite = new Rectangle
                {
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 0.5,
                    Width = 9,
                    Height = heightWhite,
                    Fill = System.Windows.Media.Brushes.White
                };
                canvasAnalyses.Children.Clear();
                canvasAnalyses.Children.Add(lineBlack);
                canvasAnalyses.Children.Add(lineWhite);
                if (WhiteOnTop)
                {
                    Canvas.SetTop(lineBlack, 1);
                    Canvas.SetLeft(lineBlack, 1);
                    Canvas.SetTop(lineWhite, heightBlack);
                    Canvas.SetLeft(lineWhite, 1);
                }
                else
                {
                    Canvas.SetTop(lineWhite, 1);
                    Canvas.SetLeft(lineWhite, 1);
                    Canvas.SetTop(lineBlack, heightWhite);
                    Canvas.SetLeft(lineWhite, 1);
                }
            }
        }

        public void SetPlayer(string whitePlayer, string blackPlayer)
        {
            _whitePlayer = whitePlayer;
            _blackPlayer = blackPlayer;
            textBlockWhitePlayer.Text = WhiteOnTop ? _whitePlayer : _blackPlayer;
            textBlockBlackPlayer.Text = WhiteOnTop ? _blackPlayer : _whitePlayer;
        }

        public void RedrawArrows()
        {
            _canvas?.Children.Clear();
            foreach (var arrowDescription in _arrowList)
            {
                DrawArrow(arrowDescription);
            }
        }

        public void MarkFields(int[] fields, bool green)
        {
            if (green)
            {
                if (fields.Length > 1)
                {
                    _canvas?.Children.Clear();
                    _arrowList.Clear();
                    var arrowDescription = new ArrowDescription
                    {
                        FromField = fields[0],
                        ToField = fields[1],
                        Color = Brushes.Green
                    };
                    _arrowList.Add(arrowDescription);
                    DrawArrow(arrowDescription);
                    return;
                }
            }

            foreach (var field in fields)
            {
                if (field < 0)
                {
                    continue;
                }


                if (_markedGreenFields.ContainsKey(field))
                {
                    _markedGreenFields[field] = false;
                }

                if (fields.Length > 1)
                {
                    for (var i = 0; i < _canvas.Children.Count; i++)
                    {
                        if (_canvas.Children[i] is ArrowLine)
                        {
                            var canvasChild = _canvas.Children[i] as ArrowLine;
                            if (canvasChild.Stroke.Equals(Brushes.Orange))
                            {
                                _canvas.Children.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    var firstOrDefault = _arrowList.FirstOrDefault(a => a.Color.Equals(Brushes.Orange));
                    if (firstOrDefault != null)
                    {
                        _arrowList.Remove(firstOrDefault);
                    }

                    var arrowDescription = new ArrowDescription
                    {
                        FromField = fields[0],
                        ToField = fields[1],
                        Color = Brushes.Orange
                    };
                    _arrowList.Add(arrowDescription);
                    DrawArrow(arrowDescription);
                    return;
                }
            }
        }


        public void UnMarkAllFields(bool clearFieldTags = false)
        {
            RemoveHintArrows();
            _canvas?.Children.Clear();
            _arrowList.Clear();
            _markedGreenFields.Clear();
            if (clearFieldTags)
            {
                if (_fromFieldTag != 0)
                {
                    _piecesBorderBitmaps[_fromFieldTag].Source = FindResource("bitmapEmpty") as BitmapImage;
                }

                if (_toFieldTag != 0)
                {
                    _piecesBorderBitmaps[_toFieldTag].Source = FindResource("bitmapEmpty") as BitmapImage;
                }

                _fromFieldTag = 0;
                _toFieldTag = 0;
            }
        }

        public void MarkFields(int[] fields, SolidColorBrush color)
        {
            _hintArrowColor = color;
            if (fields.Length > 1)
            {
                var arrowDescription = new ArrowDescription
                {
                    FromField = fields[0],
                    ToField = fields[1],
                    Color = color
                };

                _arrowList.Add(arrowDescription);
                DrawArrow(arrowDescription);
            }
        }

        public void MarkBookFields(int[] fields, SolidColorBrush color)
        {
            _arrowList.RemoveAll(f => f.Color.Equals(color));
            if (fields.Length > 1)
            {
                var arrowDescription = new ArrowDescription
                {
                    FromField = fields[0],
                    ToField = fields[1],
                    Color = color
                };

                _arrowList.Add(arrowDescription);
                RedrawArrows();
            }
        }


        private void RemoveHintArrows()
        {

            _arrowList.RemoveAll(f => f.Color.Equals(_hintArrowColor));
            RedrawArrows();
        }

        public double ChessFieldSize
        {
            get => (double)GetValue(ChessFieldSizeProperty);
            set => SetValue(ChessFieldSizeProperty, value);
        }
        private void DrawArrow(ArrowDescription arrowDescription)
        {
            if (_canvas == null || !_piecesBorderBitmaps.ContainsKey(arrowDescription.FromField) || !_piecesBorderBitmaps.ContainsKey(arrowDescription.ToField))
            {
                return;
            }


            var gridWidth = (ChessFieldSize + 10) / 2;
            var gridWidth2 = gridWidth / 2;
            var gridWidth3 = gridWidth - gridWidth2;
            var gridWidth4 = gridWidth + gridWidth2;
            var dx1 = gridWidth;
            var dx2 = gridWidth;
            var dy1 = gridWidth;
            var dy2 = gridWidth;
            var pointToScreen1 = _piecesBorderBitmaps[arrowDescription.FromField]
                .TranslatePoint(new Point(0, 0), _canvas);
            var pointToScreen2 =
                _piecesBorderBitmaps[arrowDescription.ToField].TranslatePoint(new Point(0, 0), _canvas);
            var aline2 = new ArrowLine
            {
                ArrowEnds = ArrowEnds.End,
                Stroke = arrowDescription.Color,
                StrokeThickness = 2
            };

            if (!pointToScreen1.X.Equals(pointToScreen2.X))
            {
                if (pointToScreen1.X < pointToScreen2.X)
                {
                    dx1 = gridWidth4;
                    dx2 = gridWidth2;
                }
                else
                {
                    dx1 = gridWidth2;
                    dx2 = gridWidth4;
                }
            }

            if (!pointToScreen1.Y.Equals(pointToScreen2.Y))
            {
                if (pointToScreen1.Y < pointToScreen2.Y)
                {
                    dy1 = gridWidth4;
                    dy2 = gridWidth3;
                }
                else
                {
                    dy1 = gridWidth3;
                    dy2 = gridWidth4;
                }
            }

            aline2.X1 = pointToScreen1.X + dx1;
            aline2.Y1 = pointToScreen1.Y + dy1;
            aline2.X2 = pointToScreen2.X + dx2;
            aline2.Y2 = pointToScreen2.Y + dy2;
            aline2.IsArrowClosed = true;
            aline2.Fill = arrowDescription.Color;
            _canvas.Children.Add(aline2);
        }


        private void TagFields()
        {
            imageA1.Tag = WhiteOnTop ? Fields.FA1 : Fields.FH8;
            imageA2.Tag = WhiteOnTop ? Fields.FA2 : Fields.FH7;
            imageA3.Tag = WhiteOnTop ? Fields.FA3 : Fields.FH6;
            imageA4.Tag = WhiteOnTop ? Fields.FA4 : Fields.FH5;
            imageA5.Tag = WhiteOnTop ? Fields.FA5 : Fields.FH4;
            imageA6.Tag = WhiteOnTop ? Fields.FA6 : Fields.FH3;
            imageA7.Tag = WhiteOnTop ? Fields.FA7 : Fields.FH2;
            imageA8.Tag = WhiteOnTop ? Fields.FA8 : Fields.FH1;
            imageB1.Tag = WhiteOnTop ? Fields.FB1 : Fields.FG8;
            imageB2.Tag = WhiteOnTop ? Fields.FB2 : Fields.FG7;
            imageB3.Tag = WhiteOnTop ? Fields.FB3 : Fields.FG6;
            imageB4.Tag = WhiteOnTop ? Fields.FB4 : Fields.FG5;
            imageB5.Tag = WhiteOnTop ? Fields.FB5 : Fields.FG4;
            imageB6.Tag = WhiteOnTop ? Fields.FB6 : Fields.FG3;
            imageB7.Tag = WhiteOnTop ? Fields.FB7 : Fields.FG2;
            imageB8.Tag = WhiteOnTop ? Fields.FB8 : Fields.FG1;
            imageC1.Tag = WhiteOnTop ? Fields.FC1 : Fields.FF8;
            imageC2.Tag = WhiteOnTop ? Fields.FC2 : Fields.FF7;
            imageC3.Tag = WhiteOnTop ? Fields.FC3 : Fields.FF6;
            imageC4.Tag = WhiteOnTop ? Fields.FC4 : Fields.FF5;
            imageC5.Tag = WhiteOnTop ? Fields.FC5 : Fields.FF4;
            imageC6.Tag = WhiteOnTop ? Fields.FC6 : Fields.FF3;
            imageC7.Tag = WhiteOnTop ? Fields.FC7 : Fields.FF2;
            imageC8.Tag = WhiteOnTop ? Fields.FC8 : Fields.FF1;
            imageD1.Tag = WhiteOnTop ? Fields.FD1 : Fields.FE8;
            imageD2.Tag = WhiteOnTop ? Fields.FD2 : Fields.FE7;
            imageD3.Tag = WhiteOnTop ? Fields.FD3 : Fields.FE6;
            imageD4.Tag = WhiteOnTop ? Fields.FD4 : Fields.FE5;
            imageD5.Tag = WhiteOnTop ? Fields.FD5 : Fields.FE4;
            imageD6.Tag = WhiteOnTop ? Fields.FD6 : Fields.FE3;
            imageD7.Tag = WhiteOnTop ? Fields.FD7 : Fields.FE2;
            imageD8.Tag = WhiteOnTop ? Fields.FD8 : Fields.FE1;
            imageE1.Tag = WhiteOnTop ? Fields.FE1 : Fields.FD8;
            imageE2.Tag = WhiteOnTop ? Fields.FE2 : Fields.FD7;
            imageE3.Tag = WhiteOnTop ? Fields.FE3 : Fields.FD6;
            imageE4.Tag = WhiteOnTop ? Fields.FE4 : Fields.FD5;
            imageE5.Tag = WhiteOnTop ? Fields.FE5 : Fields.FD4;
            imageE6.Tag = WhiteOnTop ? Fields.FE6 : Fields.FD3;
            imageE7.Tag = WhiteOnTop ? Fields.FE7 : Fields.FD2;
            imageE8.Tag = WhiteOnTop ? Fields.FE8 : Fields.FD1;
            imageF1.Tag = WhiteOnTop ? Fields.FF1 : Fields.FC8;
            imageF2.Tag = WhiteOnTop ? Fields.FF2 : Fields.FC7;
            imageF3.Tag = WhiteOnTop ? Fields.FF3 : Fields.FC6;
            imageF4.Tag = WhiteOnTop ? Fields.FF4 : Fields.FC5;
            imageF5.Tag = WhiteOnTop ? Fields.FF5 : Fields.FC4;
            imageF6.Tag = WhiteOnTop ? Fields.FF6 : Fields.FC3;
            imageF7.Tag = WhiteOnTop ? Fields.FF7 : Fields.FC2;
            imageF8.Tag = WhiteOnTop ? Fields.FF8 : Fields.FC1;
            imageG1.Tag = WhiteOnTop ? Fields.FG1 : Fields.FB8;
            imageG2.Tag = WhiteOnTop ? Fields.FG2 : Fields.FB7;
            imageG3.Tag = WhiteOnTop ? Fields.FG3 : Fields.FB6;
            imageG4.Tag = WhiteOnTop ? Fields.FG4 : Fields.FB5;
            imageG5.Tag = WhiteOnTop ? Fields.FG5 : Fields.FB4;
            imageG6.Tag = WhiteOnTop ? Fields.FG6 : Fields.FB3;
            imageG7.Tag = WhiteOnTop ? Fields.FG7 : Fields.FB2;
            imageG8.Tag = WhiteOnTop ? Fields.FG8 : Fields.FB1;
            imageH1.Tag = WhiteOnTop ? Fields.FH1 : Fields.FA8;
            imageH2.Tag = WhiteOnTop ? Fields.FH2 : Fields.FA7;
            imageH3.Tag = WhiteOnTop ? Fields.FH3 : Fields.FA6;
            imageH4.Tag = WhiteOnTop ? Fields.FH4 : Fields.FA5;
            imageH5.Tag = WhiteOnTop ? Fields.FH5 : Fields.FA4;
            imageH6.Tag = WhiteOnTop ? Fields.FH6 : Fields.FA3;
            imageH7.Tag = WhiteOnTop ? Fields.FH7 : Fields.FA2;
            imageH8.Tag = WhiteOnTop ? Fields.FH8 : Fields.FA1;

            imageFA1.Tag = WhiteOnTop ? Fields.FA1 : Fields.FH8;
            imageFA2.Tag = WhiteOnTop ? Fields.FA2 : Fields.FH7;
            imageFA3.Tag = WhiteOnTop ? Fields.FA3 : Fields.FH6;
            imageFA4.Tag = WhiteOnTop ? Fields.FA4 : Fields.FH5;
            imageFA5.Tag = WhiteOnTop ? Fields.FA5 : Fields.FH4;
            imageFA6.Tag = WhiteOnTop ? Fields.FA6 : Fields.FH3;
            imageFA7.Tag = WhiteOnTop ? Fields.FA7 : Fields.FH2;
            imageFA8.Tag = WhiteOnTop ? Fields.FA8 : Fields.FH1;
            imageFB1.Tag = WhiteOnTop ? Fields.FB1 : Fields.FG8;
            imageFB2.Tag = WhiteOnTop ? Fields.FB2 : Fields.FG7;
            imageFB3.Tag = WhiteOnTop ? Fields.FB3 : Fields.FG6;
            imageFB4.Tag = WhiteOnTop ? Fields.FB4 : Fields.FG5;
            imageFB5.Tag = WhiteOnTop ? Fields.FB5 : Fields.FG4;
            imageFB6.Tag = WhiteOnTop ? Fields.FB6 : Fields.FG3;
            imageFB7.Tag = WhiteOnTop ? Fields.FB7 : Fields.FG2;
            imageFB8.Tag = WhiteOnTop ? Fields.FB8 : Fields.FG1;
            imageFC1.Tag = WhiteOnTop ? Fields.FC1 : Fields.FF8;
            imageFC2.Tag = WhiteOnTop ? Fields.FC2 : Fields.FF7;
            imageFC3.Tag = WhiteOnTop ? Fields.FC3 : Fields.FF6;
            imageFC4.Tag = WhiteOnTop ? Fields.FC4 : Fields.FF5;
            imageFC5.Tag = WhiteOnTop ? Fields.FC5 : Fields.FF4;
            imageFC6.Tag = WhiteOnTop ? Fields.FC6 : Fields.FF3;
            imageFC7.Tag = WhiteOnTop ? Fields.FC7 : Fields.FF2;
            imageFC8.Tag = WhiteOnTop ? Fields.FC8 : Fields.FF1;
            imageFD1.Tag = WhiteOnTop ? Fields.FD1 : Fields.FE8;
            imageFD2.Tag = WhiteOnTop ? Fields.FD2 : Fields.FE7;
            imageFD3.Tag = WhiteOnTop ? Fields.FD3 : Fields.FE6;
            imageFD4.Tag = WhiteOnTop ? Fields.FD4 : Fields.FE5;
            imageFD5.Tag = WhiteOnTop ? Fields.FD5 : Fields.FE4;
            imageFD6.Tag = WhiteOnTop ? Fields.FD6 : Fields.FE3;
            imageFD7.Tag = WhiteOnTop ? Fields.FD7 : Fields.FE2;
            imageFD8.Tag = WhiteOnTop ? Fields.FD8 : Fields.FE1;
            imageFE1.Tag = WhiteOnTop ? Fields.FE1 : Fields.FD8;
            imageFE2.Tag = WhiteOnTop ? Fields.FE2 : Fields.FD7;
            imageFE3.Tag = WhiteOnTop ? Fields.FE3 : Fields.FD6;
            imageFE4.Tag = WhiteOnTop ? Fields.FE4 : Fields.FD5;
            imageFE5.Tag = WhiteOnTop ? Fields.FE5 : Fields.FD4;
            imageFE6.Tag = WhiteOnTop ? Fields.FE6 : Fields.FD3;
            imageFE7.Tag = WhiteOnTop ? Fields.FE7 : Fields.FD2;
            imageFE8.Tag = WhiteOnTop ? Fields.FE8 : Fields.FD1;
            imageFF1.Tag = WhiteOnTop ? Fields.FF1 : Fields.FC8;
            imageFF2.Tag = WhiteOnTop ? Fields.FF2 : Fields.FC7;
            imageFF3.Tag = WhiteOnTop ? Fields.FF3 : Fields.FC6;
            imageFF4.Tag = WhiteOnTop ? Fields.FF4 : Fields.FC5;
            imageFF5.Tag = WhiteOnTop ? Fields.FF5 : Fields.FC4;
            imageFF6.Tag = WhiteOnTop ? Fields.FF6 : Fields.FC3;
            imageFF7.Tag = WhiteOnTop ? Fields.FF7 : Fields.FC2;
            imageFF8.Tag = WhiteOnTop ? Fields.FF8 : Fields.FC1;
            imageFG1.Tag = WhiteOnTop ? Fields.FG1 : Fields.FB8;
            imageFG2.Tag = WhiteOnTop ? Fields.FG2 : Fields.FB7;
            imageFG3.Tag = WhiteOnTop ? Fields.FG3 : Fields.FB6;
            imageFG4.Tag = WhiteOnTop ? Fields.FG4 : Fields.FB5;
            imageFG5.Tag = WhiteOnTop ? Fields.FG5 : Fields.FB4;
            imageFG6.Tag = WhiteOnTop ? Fields.FG6 : Fields.FB3;
            imageFG7.Tag = WhiteOnTop ? Fields.FG7 : Fields.FB2;
            imageFG8.Tag = WhiteOnTop ? Fields.FG8 : Fields.FB1;
            imageFH1.Tag = WhiteOnTop ? Fields.FH1 : Fields.FA8;
            imageFH2.Tag = WhiteOnTop ? Fields.FH2 : Fields.FA7;
            imageFH3.Tag = WhiteOnTop ? Fields.FH3 : Fields.FA6;
            imageFH4.Tag = WhiteOnTop ? Fields.FH4 : Fields.FA5;
            imageFH5.Tag = WhiteOnTop ? Fields.FH5 : Fields.FA4;
            imageFH6.Tag = WhiteOnTop ? Fields.FH6 : Fields.FA3;
            imageFH7.Tag = WhiteOnTop ? Fields.FH7 : Fields.FA2;
            imageFH8.Tag = WhiteOnTop ? Fields.FH8 : Fields.FA1;

            imageBA1.Tag = WhiteOnTop ? Fields.FA1 : Fields.FH8;
            imageBA2.Tag = WhiteOnTop ? Fields.FA2 : Fields.FH7;
            imageBA3.Tag = WhiteOnTop ? Fields.FA3 : Fields.FH6;
            imageBA4.Tag = WhiteOnTop ? Fields.FA4 : Fields.FH5;
            imageBA5.Tag = WhiteOnTop ? Fields.FA5 : Fields.FH4;
            imageBA6.Tag = WhiteOnTop ? Fields.FA6 : Fields.FH3;
            imageBA7.Tag = WhiteOnTop ? Fields.FA7 : Fields.FH2;
            imageBA8.Tag = WhiteOnTop ? Fields.FA8 : Fields.FH1;
            imageBB1.Tag = WhiteOnTop ? Fields.FB1 : Fields.FG8;
            imageBB2.Tag = WhiteOnTop ? Fields.FB2 : Fields.FG7;
            imageBB3.Tag = WhiteOnTop ? Fields.FB3 : Fields.FG6;
            imageBB4.Tag = WhiteOnTop ? Fields.FB4 : Fields.FG5;
            imageBB5.Tag = WhiteOnTop ? Fields.FB5 : Fields.FG4;
            imageBB6.Tag = WhiteOnTop ? Fields.FB6 : Fields.FG3;
            imageBB7.Tag = WhiteOnTop ? Fields.FB7 : Fields.FG2;
            imageBB8.Tag = WhiteOnTop ? Fields.FB8 : Fields.FG1;
            imageBC1.Tag = WhiteOnTop ? Fields.FC1 : Fields.FF8;
            imageBC2.Tag = WhiteOnTop ? Fields.FC2 : Fields.FF7;
            imageBC3.Tag = WhiteOnTop ? Fields.FC3 : Fields.FF6;
            imageBC4.Tag = WhiteOnTop ? Fields.FC4 : Fields.FF5;
            imageBC5.Tag = WhiteOnTop ? Fields.FC5 : Fields.FF4;
            imageBC6.Tag = WhiteOnTop ? Fields.FC6 : Fields.FF3;
            imageBC7.Tag = WhiteOnTop ? Fields.FC7 : Fields.FF2;
            imageBC8.Tag = WhiteOnTop ? Fields.FC8 : Fields.FF1;
            imageBD1.Tag = WhiteOnTop ? Fields.FD1 : Fields.FE8;
            imageBD2.Tag = WhiteOnTop ? Fields.FD2 : Fields.FE7;
            imageBD3.Tag = WhiteOnTop ? Fields.FD3 : Fields.FE6;
            imageBD4.Tag = WhiteOnTop ? Fields.FD4 : Fields.FE5;
            imageBD5.Tag = WhiteOnTop ? Fields.FD5 : Fields.FE4;
            imageBD6.Tag = WhiteOnTop ? Fields.FD6 : Fields.FE3;
            imageBD7.Tag = WhiteOnTop ? Fields.FD7 : Fields.FE2;
            imageBD8.Tag = WhiteOnTop ? Fields.FD8 : Fields.FE1;
            imageBE1.Tag = WhiteOnTop ? Fields.FE1 : Fields.FD8;
            imageBE2.Tag = WhiteOnTop ? Fields.FE2 : Fields.FD7;
            imageBE3.Tag = WhiteOnTop ? Fields.FE3 : Fields.FD6;
            imageBE4.Tag = WhiteOnTop ? Fields.FE4 : Fields.FD5;
            imageBE5.Tag = WhiteOnTop ? Fields.FE5 : Fields.FD4;
            imageBE6.Tag = WhiteOnTop ? Fields.FE6 : Fields.FD3;
            imageBE7.Tag = WhiteOnTop ? Fields.FE7 : Fields.FD2;
            imageBE8.Tag = WhiteOnTop ? Fields.FE8 : Fields.FD1;
            imageBF1.Tag = WhiteOnTop ? Fields.FF1 : Fields.FC8;
            imageBF2.Tag = WhiteOnTop ? Fields.FF2 : Fields.FC7;
            imageBF3.Tag = WhiteOnTop ? Fields.FF3 : Fields.FC6;
            imageBF4.Tag = WhiteOnTop ? Fields.FF4 : Fields.FC5;
            imageBF5.Tag = WhiteOnTop ? Fields.FF5 : Fields.FC4;
            imageBF6.Tag = WhiteOnTop ? Fields.FF6 : Fields.FC3;
            imageBF7.Tag = WhiteOnTop ? Fields.FF7 : Fields.FC2;
            imageBF8.Tag = WhiteOnTop ? Fields.FF8 : Fields.FC1;
            imageBG1.Tag = WhiteOnTop ? Fields.FG1 : Fields.FB8;
            imageBG2.Tag = WhiteOnTop ? Fields.FG2 : Fields.FB7;
            imageBG3.Tag = WhiteOnTop ? Fields.FG3 : Fields.FB6;
            imageBG4.Tag = WhiteOnTop ? Fields.FG4 : Fields.FB5;
            imageBG5.Tag = WhiteOnTop ? Fields.FG5 : Fields.FB4;
            imageBG6.Tag = WhiteOnTop ? Fields.FG6 : Fields.FB3;
            imageBG7.Tag = WhiteOnTop ? Fields.FG7 : Fields.FB2;
            imageBG8.Tag = WhiteOnTop ? Fields.FG8 : Fields.FB1;
            imageBH1.Tag = WhiteOnTop ? Fields.FH1 : Fields.FA8;
            imageBH2.Tag = WhiteOnTop ? Fields.FH2 : Fields.FA7;
            imageBH3.Tag = WhiteOnTop ? Fields.FH3 : Fields.FA6;
            imageBH4.Tag = WhiteOnTop ? Fields.FH4 : Fields.FA5;
            imageBH5.Tag = WhiteOnTop ? Fields.FH5 : Fields.FA4;
            imageBH6.Tag = WhiteOnTop ? Fields.FH6 : Fields.FA3;
            imageBH7.Tag = WhiteOnTop ? Fields.FH7 : Fields.FA2;
            imageBH8.Tag = WhiteOnTop ? Fields.FH8 : Fields.FA1;

            _piecesBorderBitmaps[Fields.FA1] = WhiteOnTop ? imageBA1 : imageBH8;
            _piecesBorderBitmaps[Fields.FA2] = WhiteOnTop ? imageBA2 : imageBH7;
            _piecesBorderBitmaps[Fields.FA3] = WhiteOnTop ? imageBA3 : imageBH6;
            _piecesBorderBitmaps[Fields.FA4] = WhiteOnTop ? imageBA4 : imageBH5;
            _piecesBorderBitmaps[Fields.FA5] = WhiteOnTop ? imageBA5 : imageBH4;
            _piecesBorderBitmaps[Fields.FA6] = WhiteOnTop ? imageBA6 : imageBH3;
            _piecesBorderBitmaps[Fields.FA7] = WhiteOnTop ? imageBA7 : imageBH2;
            _piecesBorderBitmaps[Fields.FA8] = WhiteOnTop ? imageBA8 : imageBH1;
            _piecesBorderBitmaps[Fields.FB1] = WhiteOnTop ? imageBB1 : imageBG8;
            _piecesBorderBitmaps[Fields.FB2] = WhiteOnTop ? imageBB2 : imageBG7;
            _piecesBorderBitmaps[Fields.FB3] = WhiteOnTop ? imageBB3 : imageBG6;
            _piecesBorderBitmaps[Fields.FB4] = WhiteOnTop ? imageBB4 : imageBG5;
            _piecesBorderBitmaps[Fields.FB5] = WhiteOnTop ? imageBB5 : imageBG4;
            _piecesBorderBitmaps[Fields.FB6] = WhiteOnTop ? imageBB6 : imageBG3;
            _piecesBorderBitmaps[Fields.FB7] = WhiteOnTop ? imageBB7 : imageBG2;
            _piecesBorderBitmaps[Fields.FB8] = WhiteOnTop ? imageBB8 : imageBG1;
            _piecesBorderBitmaps[Fields.FC1] = WhiteOnTop ? imageBC1 : imageBF8;
            _piecesBorderBitmaps[Fields.FC2] = WhiteOnTop ? imageBC2 : imageBF7;
            _piecesBorderBitmaps[Fields.FC3] = WhiteOnTop ? imageBC3 : imageBF6;
            _piecesBorderBitmaps[Fields.FC4] = WhiteOnTop ? imageBC4 : imageBF5;
            _piecesBorderBitmaps[Fields.FC5] = WhiteOnTop ? imageBC5 : imageBF4;
            _piecesBorderBitmaps[Fields.FC6] = WhiteOnTop ? imageBC6 : imageBF3;
            _piecesBorderBitmaps[Fields.FC7] = WhiteOnTop ? imageBC7 : imageBF2;
            _piecesBorderBitmaps[Fields.FC8] = WhiteOnTop ? imageBC8 : imageBF1;
            _piecesBorderBitmaps[Fields.FD1] = WhiteOnTop ? imageBD1 : imageBE8;
            _piecesBorderBitmaps[Fields.FD2] = WhiteOnTop ? imageBD2 : imageBE7;
            _piecesBorderBitmaps[Fields.FD3] = WhiteOnTop ? imageBD3 : imageBE6;
            _piecesBorderBitmaps[Fields.FD4] = WhiteOnTop ? imageBD4 : imageBE5;
            _piecesBorderBitmaps[Fields.FD5] = WhiteOnTop ? imageBD5 : imageBE4;
            _piecesBorderBitmaps[Fields.FD6] = WhiteOnTop ? imageBD6 : imageBE3;
            _piecesBorderBitmaps[Fields.FD7] = WhiteOnTop ? imageBD7 : imageBE2;
            _piecesBorderBitmaps[Fields.FD8] = WhiteOnTop ? imageBD8 : imageBE1;
            _piecesBorderBitmaps[Fields.FE1] = WhiteOnTop ? imageBE1 : imageBD8;
            _piecesBorderBitmaps[Fields.FE2] = WhiteOnTop ? imageBE2 : imageBD7;
            _piecesBorderBitmaps[Fields.FE3] = WhiteOnTop ? imageBE3 : imageBD6;
            _piecesBorderBitmaps[Fields.FE4] = WhiteOnTop ? imageBE4 : imageBD5;
            _piecesBorderBitmaps[Fields.FE5] = WhiteOnTop ? imageBE5 : imageBD4;
            _piecesBorderBitmaps[Fields.FE6] = WhiteOnTop ? imageBE6 : imageBD3;
            _piecesBorderBitmaps[Fields.FE7] = WhiteOnTop ? imageBE7 : imageBD2;
            _piecesBorderBitmaps[Fields.FE8] = WhiteOnTop ? imageBE8 : imageBD1;
            _piecesBorderBitmaps[Fields.FF1] = WhiteOnTop ? imageBF1 : imageBC8;
            _piecesBorderBitmaps[Fields.FF2] = WhiteOnTop ? imageBF2 : imageBC7;
            _piecesBorderBitmaps[Fields.FF3] = WhiteOnTop ? imageBF3 : imageBC6;
            _piecesBorderBitmaps[Fields.FF4] = WhiteOnTop ? imageBF4 : imageBC5;
            _piecesBorderBitmaps[Fields.FF5] = WhiteOnTop ? imageBF5 : imageBC4;
            _piecesBorderBitmaps[Fields.FF6] = WhiteOnTop ? imageBF6 : imageBC3;
            _piecesBorderBitmaps[Fields.FF7] = WhiteOnTop ? imageBF7 : imageBC2;
            _piecesBorderBitmaps[Fields.FF8] = WhiteOnTop ? imageBF8 : imageBC1;
            _piecesBorderBitmaps[Fields.FG1] = WhiteOnTop ? imageBG1 : imageBB8;
            _piecesBorderBitmaps[Fields.FG2] = WhiteOnTop ? imageBG2 : imageBB7;
            _piecesBorderBitmaps[Fields.FG3] = WhiteOnTop ? imageBG3 : imageBB6;
            _piecesBorderBitmaps[Fields.FG4] = WhiteOnTop ? imageBG4 : imageBB5;
            _piecesBorderBitmaps[Fields.FG5] = WhiteOnTop ? imageBG5 : imageBB4;
            _piecesBorderBitmaps[Fields.FG6] = WhiteOnTop ? imageBG6 : imageBB3;
            _piecesBorderBitmaps[Fields.FG7] = WhiteOnTop ? imageBG7 : imageBB2;
            _piecesBorderBitmaps[Fields.FG8] = WhiteOnTop ? imageBG8 : imageBB1;
            _piecesBorderBitmaps[Fields.FH1] = WhiteOnTop ? imageBH1 : imageBA8;
            _piecesBorderBitmaps[Fields.FH2] = WhiteOnTop ? imageBH2 : imageBA7;
            _piecesBorderBitmaps[Fields.FH3] = WhiteOnTop ? imageBH3 : imageBA6;
            _piecesBorderBitmaps[Fields.FH4] = WhiteOnTop ? imageBH4 : imageBA5;
            _piecesBorderBitmaps[Fields.FH5] = WhiteOnTop ? imageBH5 : imageBA4;
            _piecesBorderBitmaps[Fields.FH6] = WhiteOnTop ? imageBH6 : imageBA3;
            _piecesBorderBitmaps[Fields.FH7] = WhiteOnTop ? imageBH7 : imageBA2;
            _piecesBorderBitmaps[Fields.FH8] = WhiteOnTop ? imageBH8 : imageBA1;
        }
    }
}
