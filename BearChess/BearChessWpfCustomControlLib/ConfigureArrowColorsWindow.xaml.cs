using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessWin;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ConfigureArrowColorsWindow.xaml
    /// </summary>
    public partial class ConfigureArrowColorsWindow : Window
    {
        private readonly Configuration _configuration;

        public event EventHandler ValueChangeEvent;
        
        private SolidColorBrush _bookMoveArrowColor;
        private SolidColorBrush _bookMoveArrowColorSaved;
        private SolidColorBrush _possibleMoveArrowColor;
        private SolidColorBrush _possibleMoveArrowColorSaved;
        private SolidColorBrush _bestMove1ArrowColor;
        private SolidColorBrush _bestMove1ArrowColorSaved;
        private SolidColorBrush _bestMove2ArrowColor;
        private SolidColorBrush _bestMove2ArrowColorSaved;
        private SolidColorBrush _bestMove3ArrowColor;
        private SolidColorBrush _bestMove3ArrowColorSaved;
        private SolidColorBrush _bestMoveNArrowColor;
        private SolidColorBrush _bestMoveNArrowColorSaved;
        private SolidColorBrush _lastMoveArrowColor;
        private SolidColorBrush _lastMoveArrowColorSaved;
        
        private bool _ignoreChanges = false;
        private readonly bool _combineArrowColor;
        private readonly bool _showLastMove;
        private readonly bool _showPossibleMoves;
        private readonly bool _showBestBookMove;
        private readonly bool _showBestMove;
        private readonly bool _showBestMove2;
        private readonly bool _showBestMove3;
        private readonly bool _showBestMoveN;

        public ConfigureArrowColorsWindow(Configuration configuration)
        {

            InitializeComponent();
            _configuration = configuration;
            _ignoreChanges = true;
            _bookMoveArrowColor = _configuration.GetColorValue("BookArrowColor", -16744193);           
            borderBookMoves.Background = _bookMoveArrowColor;
            _bookMoveArrowColorSaved = _bookMoveArrowColor;

            _possibleMoveArrowColor =  _configuration.GetColorValue("ArrowColor", -32513);
            borderPossibleMoves.Background = _possibleMoveArrowColor;
            _possibleMoveArrowColorSaved = _possibleMoveArrowColor;

            _bestMove1ArrowColor = _configuration.GetColorValue("BestMove1ArrowColor", -16711936);            
            borderBestMoves1.Background = _bestMove1ArrowColor;
            _bestMove1ArrowColorSaved = _bestMove1ArrowColor;

            _bestMove2ArrowColor = _configuration.GetColorValue("BestMove2ArrowColor", -16744384);           
            borderBestMoves2.Background = _bestMove2ArrowColor;
            _bestMove2ArrowColorSaved = _bestMove2ArrowColor;

            _bestMove3ArrowColor = _configuration.GetColorValue("BestMove3ArrowColor", -8388480);
            borderBestMoves3.Background = _bestMove3ArrowColor;
            _bestMove3ArrowColorSaved = _bestMove3ArrowColor;

            _bestMoveNArrowColor = _configuration.GetColorValue("BestMoveNArrowColor", -65408);           
            borderBestMovesN.Background = _bestMoveNArrowColor;
            _bestMoveNArrowColorSaved = _bestMoveNArrowColor;

            _lastMoveArrowColor = _configuration.GetColorValue("LastMoveArrowColor", -16744448);         
            borderLastMove.Background = _lastMoveArrowColor;
            _lastMoveArrowColorSaved = _lastMoveArrowColor;
            
             _combineArrowColor = _configuration.GetBoolValue("CombineArrowColor", true);
            checkboxCombineBestMoves.IsChecked = _combineArrowColor;
            
            _showLastMove = _configuration.GetBoolValue("ShowLastMove", true);
            checkboxLastMove.IsChecked = _showLastMove;
            _showPossibleMoves =  _configuration.GetBoolValue("showPossibleMoves", true);
            checkboxPossibleMoves.IsChecked = _showPossibleMoves;
            _showBestBookMove = _configuration.GetBoolValue("ShowBestBookMove", true);
            checkboxBookMoves.IsChecked = _showBestBookMove;
            _showBestMove = _configuration.GetBoolValue("ShowBestMove", true);
            checkboxBestMoves1.IsChecked = _showBestMove;
            _showBestMove2 = _configuration.GetBoolValue("ShowBestMove2", true);
            checkboxBestMoves2.IsChecked = _showBestMove2;
            _showBestMove3 = _configuration.GetBoolValue("ShowBestMove3", true);
            checkboxBestMoves3.IsChecked = _showBestMove3;
            _showBestMoveN = _configuration.GetBoolValue("ShowBestMoveN", true);
            checkboxBestMovesN.IsChecked = _showBestMoveN;
            SetBestMovesCheckboxes();
        }

        private void SetBestMovesCheckboxes()
        {
            _ignoreChanges = true;
            if (checkboxBestMoves2.IsChecked.HasValue && checkboxBestMoves2.IsChecked.Value)
            {
                checkboxBestMoves2.IsChecked = checkboxBestMoves1.IsChecked;
            }
            if (checkboxBestMoves3.IsChecked.HasValue && checkboxBestMoves3.IsChecked.Value)
            {
                checkboxBestMoves3.IsChecked = checkboxBestMoves2.IsChecked;
            }
            if (checkboxBestMovesN.IsChecked.HasValue && checkboxBestMovesN.IsChecked.Value)
            {
                checkboxBestMovesN.IsChecked = checkboxBestMoves3.IsChecked;
            }
            checkboxBestMoves2.IsEnabled = checkboxBestMoves1.IsChecked.HasValue && checkboxBestMoves1.IsChecked.Value;
            checkboxBestMoves3.IsEnabled = checkboxBestMoves2.IsChecked.HasValue && checkboxBestMoves2.IsChecked.Value;
            checkboxBestMovesN.IsEnabled = checkboxBestMoves3.IsChecked.HasValue && checkboxBestMoves3.IsChecked.Value;
            _ignoreChanges = false;
        }

        private void buttonBookMove_Click(object sender, RoutedEventArgs e)
        {
            _bookMoveArrowColor = buttonBestMoves_Click(_bookMoveArrowColor, borderBookMoves);
            PresetConfig();
        }

        private void buttonDefaultBookMove_OnClick(object sender, RoutedEventArgs e)
        {
            _bookMoveArrowColor = ColorHelper.GetSolidColorBrush(-16744193);
            borderBookMoves.Background = _bookMoveArrowColor;
            PresetConfig();
        }

        private void buttonPossibleMoves_Click(object sender, RoutedEventArgs e)
        {
            _possibleMoveArrowColor = buttonBestMoves_Click(_possibleMoveArrowColor, borderPossibleMoves);
            PresetConfig();
        }

        private void buttonDefaultPossibleMoves_OnClick(object sender, RoutedEventArgs e)
        {
            _possibleMoveArrowColor = ColorHelper.GetSolidColorBrush(-32513);        
            borderPossibleMoves.Background = _possibleMoveArrowColor;
            PresetConfig();
        }

        private void buttonBestMoves1_Click(object sender, RoutedEventArgs e)
        {
            _bestMove1ArrowColor = buttonBestMoves_Click(_bestMove1ArrowColor, borderBestMoves1);
            PresetConfig();
        }

        private void buttonBestMoves2_Click(object sender, RoutedEventArgs e)
        {
            _bestMove2ArrowColor = buttonBestMoves_Click(_bestMove2ArrowColor, borderBestMoves2);
            PresetConfig();
        }

        private void buttonBestMoves3_Click(object sender, RoutedEventArgs e)
        {
            _bestMove3ArrowColor = buttonBestMoves_Click(_bestMove3ArrowColor, borderBestMoves3);
            PresetConfig();
        }

        private void buttonBestMovesN_Click(object sender, RoutedEventArgs e)
        {
            _bestMoveNArrowColor = buttonBestMoves_Click(_bestMoveNArrowColor, borderBestMovesN);
            PresetConfig();
        }

        private SolidColorBrush buttonBestMoves_Click(SolidColorBrush color, Border border)
        {
            var colorDialog = new ColorDialog();
            colorDialog.Color = System.Drawing.Color.FromArgb(color.Color.A, color.Color.R, color.Color.G, color.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                border.Background = color;
            }
            return color;
        }

     
        private void checkboxBestMoves_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ignoreChanges)
            {
                SetBestMovesCheckboxes();
                PresetConfig();
            }
        }

        private void checkboxBestMoves_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_ignoreChanges)
            {
                SetBestMovesCheckboxes();
                PresetConfig();
            }
        }

        private void buttonDefaultBestMoves1_OnClick(object sender, RoutedEventArgs e)
        {            
            _bestMove1ArrowColor = ColorHelper.GetSolidColorBrush(-16711936);
            borderBestMoves1.Background = _bestMove1ArrowColor;
            PresetConfig();
        }

        private void buttonDefaultBestMoves2_OnClick(object sender, RoutedEventArgs e)
        {
            _bestMove2ArrowColor = ColorHelper.GetSolidColorBrush(-16744384);
            borderBestMoves2.Background = _bestMove2ArrowColor;
            PresetConfig();
        }
        private void buttonDefaultBestMoves3_OnClick(object sender, RoutedEventArgs e)
        {
            _bestMove3ArrowColor = ColorHelper.GetSolidColorBrush(-8388480);
            borderBestMoves3.Background = _bestMove3ArrowColor;
            PresetConfig();
        }

        private void buttonDefaultBestMovesN_OnClick(object sender, RoutedEventArgs e)
        {
            _bestMoveNArrowColor = ColorHelper.GetSolidColorBrush(-65408);
            borderBestMovesN.Background = _bestMoveNArrowColor;
            PresetConfig();
        }

        private void PresetConfig()
        {
            if (_ignoreChanges)
            {
                return;
            }
            _configuration.SaveColorValue("BookArrowColor", _bookMoveArrowColor);
            _configuration.SaveColorValue("ArrowColor", _possibleMoveArrowColor);
            _configuration.SaveColorValue("BestMove1ArrowColor", _bestMove1ArrowColor);
            _configuration.SaveColorValue("BestMove2ArrowColor", _bestMove2ArrowColor);
            _configuration.SaveColorValue("BestMove3ArrowColor", _bestMove3ArrowColor);
            _configuration.SaveColorValue("BestMoveNArrowColor", _bestMoveNArrowColor);
            _configuration.SaveColorValue("LastMoveArrowColor", _lastMoveArrowColor);                     
            _configuration.SetBoolValue("CombineArrowColor", checkboxCombineBestMoves.IsChecked.HasValue && checkboxCombineBestMoves.IsChecked.Value);
            _configuration.SetBoolValue("showPossibleMoves", checkboxPossibleMoves.IsChecked.HasValue && checkboxPossibleMoves.IsChecked.Value);
            _configuration.SetBoolValue("ShowBestBookMove", checkboxBookMoves.IsChecked.HasValue && checkboxBookMoves.IsChecked.Value);
            _configuration.SetBoolValue("ShowBestMove", checkboxBestMoves1.IsChecked.HasValue && checkboxBestMoves1.IsChecked.Value);
            _configuration.SetBoolValue("ShowBestMove2", checkboxBestMoves2.IsChecked.HasValue && checkboxBestMoves2.IsChecked.Value);
            _configuration.SetBoolValue("ShowBestMove3", checkboxBestMoves3.IsChecked.HasValue && checkboxBestMoves3.IsChecked.Value);
            _configuration.SetBoolValue("ShowBestMoveN", checkboxBestMovesN.IsChecked.HasValue && checkboxBestMovesN.IsChecked.Value);
            _configuration.SetBoolValue("ShowLastMove", checkboxLastMove.IsChecked.HasValue && checkboxLastMove.IsChecked.Value);
            ValueChangeEvent?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            PresetConfig();
            _configuration.Save();
            Close();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            RestoreSaved();
            Close();
        }

        private void RestoreSaved()
        {
            _configuration.SaveColorValue("BookArrowColor", _bookMoveArrowColorSaved);
            _configuration.SaveColorValue("ArrowColor", _possibleMoveArrowColorSaved);
            _configuration.SaveColorValue("BestMove1ArrowColor", _bestMove1ArrowColorSaved);
            _configuration.SaveColorValue("BestMove2ArrowColor", _bestMove2ArrowColorSaved);
            _configuration.SaveColorValue("BestMove3ArrowColor", _bestMove3ArrowColorSaved);
            _configuration.SaveColorValue("BestMoveNArrowColor", _bestMoveNArrowColorSaved);
            _configuration.SaveColorValue("LastMoveArrowColor", _lastMoveArrowColorSaved);
            _configuration.SetBoolValue("CombineArrowColor", _combineArrowColor);
            _configuration.SetBoolValue("showPossibleMoves", _showPossibleMoves);
            _configuration.SetBoolValue("ShowBestBookMove", _showBestBookMove);
            _configuration.SetBoolValue("ShowBestMove", _showBestMove);
            _configuration.SetBoolValue("ShowBestMove2", _showBestMove2);
            _configuration.SetBoolValue("ShowBestMove3", _showBestMove3);
            _configuration.SetBoolValue("ShowBestMoveN", _showBestMoveN);
            _configuration.SetBoolValue("ShowLastMove", _showLastMove);
            ValueChangeEvent?.Invoke(this, EventArgs.Empty);
        
        }

        private void CheckboxPossibleMoves_OnChecked(object sender, RoutedEventArgs e)
        {
            PresetConfig();
        }

        private void CheckboxPossibleMoves_OnUnchecked(object sender, RoutedEventArgs e)
        {
            PresetConfig();
        }

        private void buttonLastMove_Click(object sender, RoutedEventArgs e)
        {
            _lastMoveArrowColor = buttonBestMoves_Click(_lastMoveArrowColor, borderLastMove);
            PresetConfig();
        }

        private void buttonDefaultLastMove_OnClick(object sender, RoutedEventArgs e)
        {
            _lastMoveArrowColor = ColorHelper.GetSolidColorBrush(-16744448);
            borderLastMove.Background = _lastMoveArrowColor;
            PresetConfig();
        }
    }
}
