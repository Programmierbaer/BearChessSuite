using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessWin;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;
using Path = System.IO.Path;

namespace www.SoLaNoSoft.com.BearChessServerWin.Windows
{
    /// <summary>
    /// Interaktionslogik für UciConfigWindow.xaml
    /// </summary>
    public partial class UciConfigWindow : Window
    {

        private readonly Configuration _configuration;
        private readonly UciInfo _uciInfo;
        private bool _showButtons;
        public bool SaveAsNew { get; private set; }

        public class ButtonConfigEventArgs : EventArgs
        {
            public string EngineName { get; }
            public string ConfigCmd { get; }

            public ButtonConfigEventArgs(string engineName, string configCmd)
            {
                EngineName = engineName;
                ConfigCmd = configCmd;
            }
        }

        public event EventHandler<ButtonConfigEventArgs> ButtonConfigEvent;
        public UciConfigWindow()
        {
            InitializeComponent();
            _configuration = Configuration.Instance;
        }

        public UciConfigWindow(UciInfo uciInfo, bool canChangeName, bool showButtons, bool canSaveAs) : this()
        {
            Title += $" {uciInfo.OriginName}";
            _uciInfo = uciInfo;
            _showButtons = showButtons;
            BorderLogo.Visibility =  Visibility.Visible;

            textBlockName.ToolTip = uciInfo.OriginName;
            textBoxName.Text = uciInfo.Name;
            textBoxName.IsEnabled = canChangeName;
            buttonSaveAs.Visibility = canSaveAs ? Visibility.Visible : Visibility.Hidden;
            textBoxName.ToolTip = !canChangeName ? uciInfo.Name : "Name of the configuration";
            textBlockFileName.Text = uciInfo.FileName;
            textBlockFileName.ToolTip = uciInfo.FileName;
            textBlockLogoFileName.Text = uciInfo.LogoFileName;
            textBlockLogoFileName.ToolTip = uciInfo.LogoFileName;

            int optionsLength = uciInfo.Options.Length / 2;
            int count = 0;
            int rowIndex = 0;
            int colIndex = 0;
            int prevColIndex = 0;
            int inputWidth = 0;
            if ((uciInfo.Options.Length % 2) != 0)
            {
                optionsLength++;
            }

            foreach (var option in uciInfo.Options)
            {
                if (option.StartsWith("option name UCI_EngineAbout", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (option.StartsWith("option name Licensed To", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var currentValue = string.Empty;
                foreach (var optionValue in uciInfo.OptionValues)
                {
                    var optionSplit = optionValue.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (optionSplit.Length < 3
                        || !optionSplit[0].Equals("setoption", StringComparison.OrdinalIgnoreCase)
                        || !optionSplit[1].Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var oName = string.Empty;
                    for (int i = 2; i < optionSplit.Length - 2; i++)
                    {
                        oName = oName + optionSplit[i] + " ";
                    }

                    if (option.StartsWith($"option name {oName.Trim()} type"))
                    {
                        currentValue = optionSplit[optionSplit.Length - 1];

                        break;
                    }
                }

                count++;
                colIndex = (count <= optionsLength || optionsLength == 1) ? 0 : 2;
                if (prevColIndex != colIndex)
                {
                    prevColIndex = colIndex;
                    rowIndex = 0;
                }
            
                AddControl(option, currentValue, colIndex, rowIndex, inputWidth);
                rowIndex++;
            }

        }

        public UciInfo GetUciInfo()
        {
            var uciInfo = new UciInfo(_uciInfo.FileName)
            {
                Id = SaveAsNew ? "uci" + Guid.NewGuid().ToString("N") : _uciInfo.Id,
                Author = _uciInfo.Author,
                Name = string.IsNullOrWhiteSpace(textBoxName.Text) ? _uciInfo.OriginName : textBoxName.Text,
                OriginName = _uciInfo.OriginName,
                Valid = _uciInfo.Valid,
                OpeningBook =  string.Empty,
                OpeningBookVariation = string.Empty,
                AdjustStrength = false,
                CommandParameter = string.Empty,
                LogoFileName = textBlockLogoFileName.Text,
                WaitForStart = false,
                WaitSeconds = 0,
                IsChessComputer = _uciInfo.IsChessComputer,
                IsChessServer = _uciInfo.IsChessServer
            };
            foreach (var uciInfoOption in _uciInfo.Options)
            {
                uciInfo.AddOption(uciInfoOption);
            }
            foreach (var uciConfigValue in GetValues())
            {
                if (!string.IsNullOrWhiteSpace(uciConfigValue.CurrentValue))
                {
                    uciInfo.AddOptionValue(
                        $"setoption name {uciConfigValue.OptionName.Trim()} value {uciConfigValue.CurrentValue}");
                }
            }


            return uciInfo;
        }

        private UciConfigValue[] GetValues()
        {
            var result = new List<UciConfigValue>();
            foreach (UIElement gridMainChild in gridMain.Children)
            {
                if (!(gridMainChild is IUciConfigUserControl))
                {
                    continue;
                }

                var uciConfigUserControl = gridMainChild as IUciConfigUserControl;
                if (uciConfigUserControl.ConfigValue.Ignore)
                {
                    continue;
                }
                result.Add(uciConfigUserControl.ConfigValue);
            }

            return result.ToArray();
        }

        private void AddControl(string option, string currentValue, int colIndex, int rowIndex, int inputWidth)
        {
            var optionSplit = option.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (optionSplit.Length < 3
                || !optionSplit[0].Equals("option", StringComparison.OrdinalIgnoreCase)
                || !optionSplit[1].Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var uciConfigValue = new UciConfigValue
            {
                CurrentValue = currentValue,
                InputWidth = inputWidth
            };

            var i = 2;
            do
            {
                if (optionSplit[i].Equals("type"))
                {
                    i++;
                    uciConfigValue.OptionType = optionSplit[i];
                    i++;
                    continue;
                }

                if (optionSplit[i].Equals("default"))
                {
                    i++;
                    while (true)
                    {

                        if (i < optionSplit.Length)
                        {
                            if (string.IsNullOrWhiteSpace(uciConfigValue.DefaultValue))
                            {
                                uciConfigValue.DefaultValue = optionSplit[i];
                            }
                            else
                            {
                                uciConfigValue.DefaultValue += " " + optionSplit[i];
                            }
                            i++;
                        }
                        else
                        {
                            break;
                        }

                        if (i < optionSplit.Length)
                        {
                            if (uciConfigValue.OptionType.Equals("combo", StringComparison.OrdinalIgnoreCase))
                            {
                                if (optionSplit[i].Equals("var", StringComparison.OrdinalIgnoreCase))
                                {
                                    break;
                                }
                            }
                            else if (uciConfigValue.OptionType.Equals("string", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            else
                            {
                                break;
                            }

                        }
                        else
                        {
                            break;
                        }
                    }

                    continue;
                }

                if (optionSplit[i].Equals("min"))
                {
                    i++;
                    uciConfigValue.MinValue = optionSplit[i];
                    i++;
                    continue;
                }

                if (optionSplit[i].Equals("max"))
                {
                    i++;
                    uciConfigValue.MaxValue = optionSplit[i];
                    i++;
                    continue;
                }
                if (optionSplit[i].Equals("var"))
                {
                    i++;
                    var comboItem = string.Empty;
                    while (true)
                    {

                        if (i < optionSplit.Length)
                        {
                            if (string.IsNullOrWhiteSpace(comboItem))
                            {
                                comboItem = optionSplit[i];

                            }
                            else
                            {
                                comboItem += " " + optionSplit[i];
                            }

                            i++;
                        }
                        else
                        {
                            break;
                        }

                        if (i < optionSplit.Length)
                        {
                            if (optionSplit[i].Equals("var", StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                    }

                    uciConfigValue.AddComboItem(comboItem);

                    continue;
                }

                uciConfigValue.OptionName = uciConfigValue.OptionName + optionSplit[i] + " ";


                i++;
            } while (i < optionSplit.Length);
            var firstOrDefault = _uciInfo.OptionValues.FirstOrDefault(o => o.Contains(uciConfigValue.OptionName));
            if (string.IsNullOrWhiteSpace(currentValue))
            {
                if (!string.IsNullOrWhiteSpace(firstOrDefault))
                {
                    if (uciConfigValue.OptionType.Equals("string", StringComparison.OrdinalIgnoreCase))
                    {
                        uciConfigValue.CurrentValue =
                            firstOrDefault.Substring(firstOrDefault.IndexOf(" value ") + 7);
                    }
                    else
                    {
                        var strings = firstOrDefault.Split(" ".ToCharArray());
                        uciConfigValue.CurrentValue = strings[strings.Length - 1];
                    }
                }
                else
                {
                    uciConfigValue.CurrentValue = string.Empty;
                }
            }

            switch (uciConfigValue.OptionType)
            {
                case "spin":
                    AddNumericUpDown(uciConfigValue, colIndex, rowIndex);
                    break;
                case "check":
                    AddCheckBox(uciConfigValue, colIndex, rowIndex);
                    break;
                case "combo":
                    AddCombo(uciConfigValue, colIndex, rowIndex);
                    break;
                case "string":
                    AddTextBox(uciConfigValue, colIndex, rowIndex);
                    break;
                case "button":
                    AddButton(uciConfigValue, colIndex, rowIndex);
                    break;
                default:
                    AddUnknown(uciConfigValue, colIndex, rowIndex);
                    break;
            }

        }
        private void AddCheckBox(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }

            var textBlock = new TextBlock()
            {
                Text = uciConfigValue.OptionName,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var uciCheckBoxUserControl = new UciCheckBoxUserControl(uciConfigValue);
            Grid.SetRow(textBlock, rowIndex);
            Grid.SetColumn(textBlock, colIndex);
            Grid.SetRow(uciCheckBoxUserControl, rowIndex);
            Grid.SetColumn(uciCheckBoxUserControl, colIndex + 1);
            gridMain.Children.Add(textBlock);
            gridMain.Children.Add(uciCheckBoxUserControl);
        }
        private void AddButton(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }

            var button = new Button()
            {
                Content = new TextBlock()
                {
                    Text = uciConfigValue.OptionName,
                    Margin = new Thickness(1),

                },
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = uciConfigValue.OptionName
            };
            button.Click += ButtonConfig_Click;
            Grid.SetRow(button, rowIndex);
            Grid.SetColumn(button, colIndex);
            gridMain.Children.Add(button);
        }
        private void ButtonConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!_showButtons)
            {
                MessageBox.Show(this, "Buttons only work when the engine is loaded", "Not applicable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var s = ((Button)sender).Tag.ToString();
            ButtonConfigEvent?.Invoke(this, new ButtonConfigEventArgs(textBoxName.Text, $"setoption name {s}"));
        }


        private void AddNumericUpDown(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }
            var textBlock = new TextBlock
            {
                Text = uciConfigValue.OptionName,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };
            var uciNumericUpDownUserControl = new UciNumericUpDownUserControl(uciConfigValue);

            Grid.SetRow(textBlock, rowIndex);
            Grid.SetColumn(textBlock, colIndex);
            Grid.SetRow(uciNumericUpDownUserControl, rowIndex);
            Grid.SetColumn(uciNumericUpDownUserControl, colIndex + 1);
            gridMain.Children.Add(textBlock);
            gridMain.Children.Add(uciNumericUpDownUserControl);

        }
        private void AddTextBox(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }

            if (string.IsNullOrWhiteSpace(uciConfigValue.CurrentValue))
            {
                uciConfigValue.CurrentValue = uciConfigValue.DefaultValue;
            }
            var uciTextBoxUserControl = new UciTextBoxUserControl(uciConfigValue);
            var textBlock = new TextBlock()
            {
                Text = uciConfigValue.OptionName,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(textBlock, rowIndex);
            Grid.SetColumn(textBlock, colIndex);
            Grid.SetRow(uciTextBoxUserControl, rowIndex);
            Grid.SetColumn(uciTextBoxUserControl, colIndex + 1);
            gridMain.Children.Add(textBlock);
            gridMain.Children.Add(uciTextBoxUserControl);
        }

        private void AddUnknown(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }
            var textBlock = new TextBlock()
            {
                Text = uciConfigValue.OptionName,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };
            Grid.SetRow(textBlock, rowIndex);
            Grid.SetColumn(textBlock, colIndex);
            gridMain.Children.Add(textBlock);
        }

        private void AddCombo(UciConfigValue uciConfigValue, int colIndex, int rowIndex)
        {
            if (rowIndex == gridMain.RowDefinitions.Count)
            {
                gridMain.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
            }

            var uciComboBoxUserControl = new UciComboBoxUserControl(uciConfigValue);

            var textBlock = new TextBlock()
            {
                Text = uciConfigValue.OptionName,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(textBlock, rowIndex);
            Grid.SetColumn(textBlock, colIndex);
            Grid.SetRow(uciComboBoxUserControl, rowIndex);
            Grid.SetColumn(uciComboBoxUserControl, colIndex + 1);
            gridMain.Children.Add(textBlock);
            gridMain.Children.Add(uciComboBoxUserControl);
            if (_uciInfo.IsMessChessEngine)
            {
                uciComboBoxUserControl.SelectionChanged += UciComboBoxUserControl_SelectionChanged;
            }
        }

        private void UciComboBoxUserControl_SelectionChanged(object sender, string e)
        {
            if (!_uciInfo.IsMessChessEngine)
            {
                return;
            }

            for (int i = 0; i < _uciInfo.MessChessLevels.Length; i = i + 2)
            {
                if (e.StartsWith(_uciInfo.MessChessLevels[i]))
                {
                    foreach (UIElement gridMainChild in gridMain.Children)
                    {
                        if (gridMainChild is UciTextBoxUserControl control)
                        {
                            control.SetInputValue(_uciInfo.MessChessLevels[i]);
                            break;
                        }
                        if (gridMainChild is UciNumericUpDownUserControl userControl)
                        {
                            if (userControl.ConfigValue.OptionName.StartsWith("Level"))
                            {
                                userControl.SetInputValue(_uciInfo.MessChessLevels[i]);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ButtonLogoClear_OnClick(object sender, RoutedEventArgs e)
        {
            textBlockLogoFileName.Text = string.Empty;
            textBlockLogoFileName.ToolTip = null;
        }

        private void ButtonLogoFile_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Logo|*.bmp;*.jpg;*.png|All Files|*.*" };
            var checkFilename = !string.IsNullOrWhiteSpace(textBlockLogoFileName.Text) ? textBlockLogoFileName.Text : textBlockFileName.Text;
            if (!File.Exists(checkFilename))
            {
                checkFilename = textBlockFileName.Text;
            }
            if (!File.Exists(checkFilename))
            {
                checkFilename = string.Empty;

            }

            if (!string.IsNullOrWhiteSpace(checkFilename))
            {
                var fileInfo = new FileInfo(checkFilename);
                openFileDialog.InitialDirectory = fileInfo.DirectoryName;
            }

            var showDialog = openFileDialog.ShowDialog(this);
            if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                textBlockLogoFileName.Text = openFileDialog.FileName;
                textBlockLogoFileName.ToolTip = openFileDialog.FileName;
                if (Configuration.Instance.Standalone)
                {
                    textBlockLogoFileName.Text = textBlockLogoFileName.Text.Replace(Configuration.Instance.BinPath, @".\");
                    textBlockLogoFileName.ToolTip = textBlockLogoFileName.Text;
                }
            }
        }

     
        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAsNew = false;
            DialogResult = true;
        }

        private void ButtonSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonDefault_OnClick(object sender, RoutedEventArgs e)
        {
            textBoxName.Text = _uciInfo.Name;
            foreach (UIElement gridMainChild in gridMain.Children)
            {
                if (!(gridMainChild is IUciConfigUserControl))
                {
                    continue;
                }

                var uciConfigUserControl = gridMainChild as IUciConfigUserControl;
                uciConfigUserControl.ResetToDefault();
            }
        }

        private void ButtonLog_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(_configuration.FolderPath, "uci", _uciInfo.Id));
        }
    }
}
