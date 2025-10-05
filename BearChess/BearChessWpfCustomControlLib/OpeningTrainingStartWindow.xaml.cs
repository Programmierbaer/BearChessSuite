using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.CTG;
using www.SoLaNoSoft.com.BearChessWin;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für OpeningTrainingStartWindow.xaml
    /// </summary>
    public partial class OpeningTrainingStartWindow : Window
    {
        private readonly ObservableCollection<BookInfo> _openingBooks;
        private readonly ObservableCollection<EcoCode> _ecoCodes;

        public OpeningTrainingConfig Config { get; set; }
        
        public BookInfo SelectedBook => (BookInfo)dataGridBook.SelectedItem;
        public EcoCode SelectedCode => (EcoCode)dataGridEcoCodes.SelectedItem;

        public OpeningTrainingStartWindow(BookInfo[] openingBooks, EcoCode[] ecoCodes, OpeningTrainingConfig config)
        {
            InitializeComponent();
            _openingBooks = new ObservableCollection<BookInfo>(openingBooks.OrderBy(o => o.Name));
            _ecoCodes = new ObservableCollection<EcoCode>(ecoCodes.OrderBy(o => o.Code).ThenBy(o => o.Name));
            dataGridBook.ItemsSource = _openingBooks;
            dataGridEcoCodes.ItemsSource = _ecoCodes;
            if (!string.IsNullOrEmpty(config.OpeningBookId) && _openingBooks.Count>0)
            {
                var firstOrDefault = _openingBooks.FirstOrDefault(u => u.Id.Equals(config.OpeningBookId));
                if (firstOrDefault != null)
                {
                    dataGridBook.SelectedIndex = _openingBooks.IndexOf(firstOrDefault);
                    if (dataGridBook.SelectedItem != null)
                    {
                        dataGridBook.ScrollIntoView(dataGridBook.SelectedItem);
                    }
                }
            }

            if (config.EcoCode != null && _ecoCodes.Count>0)
            {
                var firstOrDefault = _ecoCodes.FirstOrDefault(u => u.Name.Equals(config.EcoCode.Name));
                if (firstOrDefault != null)
                {
                    dataGridEcoCodes.SelectedIndex = _ecoCodes.IndexOf(firstOrDefault);
                    if (dataGridEcoCodes.SelectedItem != null)
                    {
                        dataGridEcoCodes.ScrollIntoView(dataGridEcoCodes.SelectedItem);
                    }
                }
            }
            checkBoxShowNextMove.IsChecked = config.ShowNextMove;
            numericUpDownUserControlInterval.Value = config.NextMoveSeconds;
            checkBoxMakeMove.IsChecked = config.ExecuteMoveAutomatically;
            radioButtonWhite.IsChecked = config.ExecuteForColor == Fields.COLOR_WHITE;
            radioButtonBlack.IsChecked = config.ExecuteForColor == Fields.COLOR_BLACK;
            radioButtonBest.IsChecked = config.Variations == OpeningBook.VariationsEnum.BestMove;
            radioButtonFlexible.IsChecked = config.Variations == OpeningBook.VariationsEnum.Flexible;
            radioButtonWide.IsChecked = config.Variations == OpeningBook.VariationsEnum.Wide;
            checkBoxAllowEcoChange.IsChecked = config.AllowChangeEcodeCode;
            Config = config;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedBook == null || SelectedCode == null)
            {
                return;
            }
            Config.EcoCode = SelectedCode;
            Config.OpeningBookId = SelectedBook.Id;
            Config.ShowNextMove = checkBoxShowNextMove.IsChecked.HasValue && checkBoxShowNextMove.IsChecked.Value;
            Config.NextMoveSeconds = numericUpDownUserControlInterval.Value;
            Config.ExecuteMoveAutomatically = checkBoxMakeMove.IsChecked.HasValue && checkBoxMakeMove.IsChecked.Value;
            Config.ExecuteForColor = radioButtonWhite.IsChecked.HasValue && radioButtonWhite.IsChecked.Value ? Fields.COLOR_WHITE : Fields.COLOR_BLACK;
            Config.Variations = radioButtonBest.IsChecked.HasValue && radioButtonBest.IsChecked.Value
                ?
                OpeningBook.VariationsEnum.BestMove
                : radioButtonFlexible.IsChecked.HasValue && radioButtonFlexible.IsChecked.Value
                    ? OpeningBook.VariationsEnum.Flexible
                    : OpeningBook.VariationsEnum.Wide;
            Config.AllowChangeEcodeCode = checkBoxAllowEcoChange.IsChecked.HasValue && checkBoxAllowEcoChange.IsChecked.Value;
            DialogResult = true;
        }

        private void TextBoxFilter_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var strings = textBoxFilter.Text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length > 0)
            {
                var ecoCodes = new List<EcoCode>(_ecoCodes);
                foreach (var s in strings)
                {
                    ecoCodes.RemoveAll(r => !r.Name.ContainsCaseInsensitive(s) && !r.Code.ContainsCaseInsensitive(s)
                      && !r.Moves.ContainsCaseInsensitive(s));
                }

                dataGridEcoCodes.ItemsSource = ecoCodes.OrderBy(o => o.Code).ThenBy(o => o.Name);
                return;
            }

            dataGridEcoCodes.ItemsSource = _ecoCodes;
        }

        private void DataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonOk.IsEnabled = (SelectedBook != null && SelectedCode != null);
            if (SelectedCode != null)
            {
                simpleChessboard.RepaintBoard(SelectedCode.FenCode);
            }
        }

        private void CheckBoxShowNextMove_OnChecked(object sender, RoutedEventArgs e)
        {
            numericUpDownUserControlInterval.IsEnabled = true;
        }

        private void CheckBoxShowNextMove_OnUnchecked(object sender, RoutedEventArgs e)
        {
            numericUpDownUserControlInterval.IsEnabled = false;
        }

        private void CheckBoxMakeMove_OnChecked(object sender, RoutedEventArgs e)
        {
            radioButtonBest.IsEnabled = true;
            radioButtonFlexible.IsEnabled = true;
            radioButtonWide.IsEnabled = true;
            radioButtonWhite.IsEnabled = true;
            radioButtonBlack.IsEnabled = true;
        }

        private void CheckBoxMakeMove_OnUnchecked(object sender, RoutedEventArgs e)
        {
            radioButtonBest.IsEnabled = false;
            radioButtonFlexible.IsEnabled = false;
            radioButtonWide.IsEnabled = false;
            radioButtonWhite.IsEnabled = false;
            radioButtonBlack.IsEnabled = false;
        }
    }
}
