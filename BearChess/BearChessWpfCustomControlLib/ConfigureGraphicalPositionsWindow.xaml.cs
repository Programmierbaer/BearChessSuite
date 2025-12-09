using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using www.SoLaNoSoft.com.BearChessBase;
using Color = System.Drawing.Color;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ConfigureEnginesWindow.xaml
    /// </summary>
    public partial class ConfigureGraphicalPositionsWindow : Window
    {
        private readonly Configuration _configuration;
        private SolidColorBrush _whiteEngineColor;
        private SolidColorBrush _blackEngineColor;
        private SolidColorBrush _whiteBuddyColor;
        private SolidColorBrush _blackBuddyColor;
        private SolidColorBrush _backgroundColor;
        private readonly List<SolidColorBrush> _colorList = new List<SolidColorBrush>();
        public event EventHandler ColorsChanged;

        public ConfigureGraphicalPositionsWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            checkBoxShowGraphicScoreLine.IsChecked = _configuration.GetBoolValue("showGraphicScore", true);
            checkBoxLimitGraphicScoreLine.IsChecked = _configuration.GetBoolValue("limitGraphicScore", true);
            numericUpDownValue.Value = _configuration.GetIntValue("valueGraphMaxValueLimit", 9);
            numericUpDownValue.IsEnabled = _configuration.GetBoolValue("limitGraphicScore", true);
            checkBoxLimitGraphicScoreLine.IsEnabled = _configuration.GetBoolValue("showGraphicScore", true);
            gridBackgroundColor.IsEnabled = checkBoxLimitGraphicScoreLine.IsEnabled;
            gridEngineColors.IsEnabled = checkBoxLimitGraphicScoreLine.IsEnabled;
            SetToDefault();
            var aRGB = _configuration.GetIntValue("backgroundGraphColor", 0);
            if (aRGB != 0)
            {
                var color = Color.FromArgb(aRGB);
                _backgroundColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
          

            aRGB = _configuration.GetIntValue("whiteEngineGraphColor", 0);
            if (aRGB != 0)
            {
                var color = Color.FromArgb(aRGB);
                _whiteEngineColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
          
           
            aRGB = _configuration.GetIntValue("blackEngineGraphColor", 0);
            if (aRGB != 0)
            {
                var color = Color.FromArgb(aRGB);
                _blackEngineColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
          

            aRGB = _configuration.GetIntValue("whiteBuddyGraphColor", 0);
            if (aRGB != 0)
            {
                var color = Color.FromArgb(aRGB);
                _whiteBuddyColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
          

            aRGB = _configuration.GetIntValue("blackBuddyGraphColor", 0);
            if (aRGB != 0)
            {
                var color = Color.FromArgb(aRGB);
                _blackBuddyColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
            _colorList.Add(_blackEngineColor);
            _colorList.Add(_whiteEngineColor);
            _colorList.Add(_blackBuddyColor);
            _colorList.Add(_whiteBuddyColor);
            _colorList.Add(_backgroundColor);
            DrawColors();
        }

        private void SetToDefault()
        {
            _backgroundColor = Brushes.AliceBlue;
            _whiteEngineColor = Brushes.DarkGoldenrod;
            _blackEngineColor = Brushes.Black;
            _whiteBuddyColor = Brushes.CornflowerBlue;
            _blackBuddyColor = Brushes.Coral;
        }

        private void DrawColors()
        {       
            borderEngineWhite.Background = _whiteEngineColor;
            borderEngineBlack.Background = _blackEngineColor;
            borderBuddyWhite.Background = _whiteBuddyColor;
            borderBuddyBlack.Background = _blackBuddyColor;
            borderEngineWhiteBackground.Background = _backgroundColor;
            borderEngineBlackBackground.Background = _backgroundColor;
            borderBuddyWhiteBackground.Background = _backgroundColor;
            borderBuddyBlackBackground.Background = _backgroundColor;
            SaveColorsToConfig();
            ColorsChanged?.Invoke(this, EventArgs.Empty);
        }


        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            _configuration.SetBoolValue("showGraphicScore", checkBoxShowGraphicScoreLine.IsChecked.HasValue && checkBoxShowGraphicScoreLine.IsChecked.Value);
            _configuration.SetBoolValue("limitGraphicScore", checkBoxLimitGraphicScoreLine.IsChecked.HasValue && checkBoxLimitGraphicScoreLine.IsChecked.Value);
            _configuration.SetIntValue("valueGraphMaxValueLimit", numericUpDownValue.Value);
            SaveColorsToConfig();
            Close();
        }

        private void SaveColorsToConfig()
        {
            var color = Color.FromArgb(_whiteEngineColor.Color.A, _whiteEngineColor.Color.R, _whiteEngineColor.Color.G, _whiteEngineColor.Color.B);
            _configuration.SetIntValue("whiteEngineGraphColor", color.ToArgb());
            color = Color.FromArgb(_blackEngineColor.Color.A, _blackEngineColor.Color.R, _blackEngineColor.Color.G, _blackEngineColor.Color.B);
            _configuration.SetIntValue("blackEngineGraphColor", color.ToArgb());
            color = Color.FromArgb(_whiteBuddyColor.Color.A, _whiteBuddyColor.Color.R, _whiteBuddyColor.Color.G, _whiteBuddyColor.Color.B);
            _configuration.SetIntValue("whiteBuddyGraphColor", color.ToArgb());
            color = Color.FromArgb(_blackBuddyColor.Color.A, _blackBuddyColor.Color.R, _blackBuddyColor.Color.G, _blackBuddyColor.Color.B);
            _configuration.SetIntValue("blackBuddyGraphColor", color.ToArgb());
            color = Color.FromArgb(_backgroundColor.Color.A, _backgroundColor.Color.R, _backgroundColor.Color.G, _backgroundColor.Color.B);
            _configuration.SetIntValue("backgroundGraphColor", color.ToArgb());
            _configuration.Save();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            _backgroundColor = _colorList[0];
            _whiteEngineColor = _colorList[1];
            _blackBuddyColor =  _colorList[2];
            _whiteBuddyColor = _colorList[3];
            _backgroundColor = _colorList[4];
            SaveColorsToConfig();
            ColorsChanged?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void CheckBoxLimitGraphicScoreLine_OnChecked(object sender, RoutedEventArgs e)
        {
            numericUpDownValue.IsEnabled = true;
        }

        private void CheckBoxLimitGraphicScoreLine_OnUnchecked(object sender, RoutedEventArgs e)
        {
            numericUpDownValue.IsEnabled = false;
        }

        private void CheckBoxShowGraphicScoreLine_OnChecked(object sender, RoutedEventArgs e)
        {
            checkBoxLimitGraphicScoreLine.IsEnabled = true;
            numericUpDownValue.IsEnabled = checkBoxLimitGraphicScoreLine.IsChecked.HasValue && checkBoxLimitGraphicScoreLine.IsChecked.Value;
            gridBackgroundColor.IsEnabled = true;
            gridEngineColors.IsEnabled = true;
        }

        private void CheckBoxShowGraphicScoreLine_OnUnchecked(object sender, RoutedEventArgs e)
        {
            checkBoxLimitGraphicScoreLine.IsEnabled = false;
            numericUpDownValue.IsEnabled = false;
            gridBackgroundColor.IsEnabled = false;
            gridEngineColors.IsEnabled = false;
        }

        private void buttonEngineWhite_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = Color.FromArgb(_whiteEngineColor.Color.A, _whiteEngineColor.Color.R, _whiteEngineColor.Color.G, _whiteEngineColor.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var argb = colorDialog.Color.ToArgb();            
                _whiteEngineColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                DrawColors();
            }
        }

        private void buttonEngineBlack_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = Color.FromArgb(_blackEngineColor.Color.A, _blackEngineColor.Color.R, _blackEngineColor.Color.G, _blackEngineColor.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var argb = colorDialog.Color.ToArgb();
                _blackEngineColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                DrawColors();
            }
        }

        private void buttonBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = Color.FromArgb(_backgroundColor.Color.A, _backgroundColor.Color.R, _backgroundColor.Color.G, _backgroundColor.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var argb = colorDialog.Color.ToArgb();                
                _backgroundColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                DrawColors();
            }

        }

        private void buttonBuddyBlack_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = Color.FromArgb(_blackBuddyColor.Color.A, _blackBuddyColor.Color.R, _blackBuddyColor.Color.G, _blackBuddyColor.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var argb = colorDialog.Color.ToArgb();                
                _blackBuddyColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                DrawColors();
            }
        }

        private void buttonBuddyWhite_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.Color = Color.FromArgb(_whiteBuddyColor.Color.A, _whiteBuddyColor.Color.R, _whiteBuddyColor.Color.G, _whiteBuddyColor.Color.B);
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var argb = colorDialog.Color.ToArgb();                
                _whiteBuddyColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                DrawColors();
            }
        }

        private void buttonDefaultBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            _backgroundColor = Brushes.AliceBlue;
            DrawColors();
        }

        private void ButtonDefaultEngineWhiteColor_OnClick(object sender, RoutedEventArgs e)
        {
            _whiteEngineColor = Brushes.DarkGoldenrod;
            DrawColors();
        }

        private void ButtonDefaultEngineBlackColor_OnClick(object sender, RoutedEventArgs e)
        {
            _blackEngineColor = Brushes.Black;
            DrawColors();
        }

        private void ButtonDefaultBuddyWhiteColor_OnClick(object sender, RoutedEventArgs e)
        {
            _whiteBuddyColor = Brushes.CornflowerBlue;
            DrawColors();
        }

        private void ButtonDefaultBuddyBlackColor_OnClick(object sender, RoutedEventArgs e)
        {
            _blackBuddyColor = Brushes.Coral;
            DrawColors();
        }
    }
}
