﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Win32;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessTools;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für SelectInstalledEngineWindow.xaml
    /// </summary>
    public partial class SelectInstalledEngineWindow : Window
    {
        private  ObservableCollection<UciInfo> _uciInfos;
        private  HashSet<string> _installedEngines;
        private readonly string _uciPath;
        private readonly ISpeech _synthesizer;
        private readonly ResourceManager _rm;
        private readonly bool _blindUserSaySelection;
        private readonly bool _blindUser;

        public UciInfo SelectedEngine => (UciInfo) dataGridEngine.SelectedItem;

        public SelectInstalledEngineWindow()
        {
            InitializeComponent();
            Top = Configuration.Instance.GetWinDoubleValue("InstalledEngineWindowTop", Configuration.WinScreenInfo.Top, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth, "300");
            Left = Configuration.Instance.GetWinDoubleValue("InstalledEngineWindowLeft", Configuration.WinScreenInfo.Left, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth, "400");
            Height = Configuration.Instance.GetDoubleValue("InstalledEngineWindowHeight",  "390");
            Width = Configuration.Instance.GetDoubleValue("InstalledEngineWindowWidth",  "500");
            _rm = SpeechTranslator.ResourceManager;
            _synthesizer = BearChessSpeech.Instance;
            _blindUserSaySelection = Configuration.Instance.GetBoolValue("blindUserSaySelection", false);
            _blindUser = Configuration.Instance.GetBoolValue("blindUser", false);
        }

        public SelectInstalledEngineWindow(IEnumerable<UciInfo> uciInfos, string lastEngineId, string uciPath) : this()
        {
            _uciInfos =  new ObservableCollection<UciInfo>(uciInfos.Where(u => !u.IsPlayer && !u.IsChessServer).OrderBy(e => e.Name).ToList());
            var firstOrDefault = _uciInfos.FirstOrDefault(u => u.Id.Equals(lastEngineId));
            if (firstOrDefault == null)
            {
                firstOrDefault = _uciInfos.Count > 0 ? _uciInfos[0] : null;
            }
            _uciPath = uciPath;
            dataGridEngine.ItemsSource = _uciInfos;
            if (firstOrDefault != null)
            {
                SelectEngine(firstOrDefault);
            }

            _installedEngines = new HashSet<string>(_uciInfos.Select(u => u.Name));
        }

        private void SelectEngine(UciInfo uciInfo)
        {
            dataGridEngine.SelectedIndex = _uciInfos.IndexOf(uciInfo);
            if (dataGridEngine.SelectedItem != null)
            {
                dataGridEngine.ScrollIntoView(dataGridEngine.SelectedItem);
            }
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedEngine != null)
            {
                if (SelectedEngine.IsInternalBearChessEngine || SelectedEngine.IsProbing || SelectedEngine.IsBuddy)
                {
                    BearChessMessageBox.Show($"{_rm.GetString("CannotPlayAgainstBuddy")} '{SelectedEngine.Name}'", _rm.GetString("LoadUCIEngne"),
                             MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            DialogResult = true;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DataGridEngine_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedEngine == null)
            {
                return;
            }

            if (SelectedEngine.IsInternalBearChessEngine || SelectedEngine.IsProbing || SelectedEngine.IsBuddy)
            {
                BearChessMessageBox.Show($"{_rm.GetString("CannotPlayAgainstBuddy")} '{SelectedEngine.Name}'", _rm.GetString("LoadUCIEngne"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DialogResult = true;
        }

        private void ButtonConfigure_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedEngine == null)
            {
                return;
            }
            if (SelectedEngine.IsInternalBearChessEngine)
            {
                BearChessMessageBox.Show($"{_rm.GetString("CannotChangeInternalEngine")} '{SelectedEngine.Name}'", _rm.GetString("ConfigureEngine"),
                         MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var uciConfigWindow = new UciConfigWindow(SelectedEngine, true, false, true) { Owner = this };
            var showDialog = uciConfigWindow.ShowDialog();
            if (!showDialog.HasValue || !showDialog.Value)
            {
                return;
            }

            var uciInfo = uciConfigWindow.GetUciInfo();
            uciInfo.ChangeDateTime = DateTime.UtcNow;
            if (uciConfigWindow.SaveAsNew)
            {
                if (SelectedEngine.Name.Equals(uciInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    uciInfo.Name = SelectedEngine.Name + " (Copy)";
                }
                var uciPath = Path.Combine(_uciPath, uciInfo.Id);
                if (!Directory.Exists(uciPath))
                {
                    Directory.CreateDirectory(uciPath);
                }
                var serializer = new XmlSerializer(typeof(UciInfo));
                TextWriter textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                serializer.Serialize(textWriter, uciInfo);
                textWriter.Close();
                _uciInfos.Add(uciInfo);
            }
            else
            {
                SelectedEngine.Name = uciInfo.Name;
                SelectedEngine.ClearOptionValues();
                SelectedEngine.OpeningBook = uciInfo.OpeningBook;
                SelectedEngine.OpeningBookVariation = uciInfo.OpeningBookVariation;
                SelectedEngine.AdjustStrength = uciInfo.AdjustStrength;
                SelectedEngine.CommandParameter = uciInfo.CommandParameter;
                SelectedEngine.LogoFileName = uciInfo.LogoFileName;
                SelectedEngine.WaitForStart = uciInfo.WaitForStart;
                SelectedEngine.WaitSeconds = uciInfo.WaitSeconds;
                SelectedEngine.ChangeDateTime = uciInfo.ChangeDateTime;
                foreach (var uciInfoOptionValue in uciInfo.OptionValues)
                {
                    SelectedEngine.AddOptionValue(uciInfoOptionValue);
                }
                var uciPath = Path.Combine(_uciPath, SelectedEngine.Id);
                var serializer = new XmlSerializer(typeof(UciInfo));
                TextWriter textWriter = new StreamWriter(Path.Combine(uciPath, SelectedEngine.Id + ".uci"), false);
                serializer.Serialize(textWriter, SelectedEngine);
                textWriter.Close();
            }

            NameChanged();
        }

        private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedEngine == null)
            {
                return;
            }

            if (dataGridEngine.SelectedItems.Count > 1)
            {
                if (MessageBox.Show($"{_rm.GetString("UninstallAllSelectedEngines")}? {dataGridEngine.SelectedItems.Count} ",
                                    _rm.GetString("UninstallEngine"),
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) !=
                    MessageBoxResult.Yes)
                {
                    return;
                }

                var enginesToDelete = new List<UciInfo>();
                foreach (var selectedItem in dataGridEngine.SelectedItems)
                {
                    if (selectedItem is UciInfo uciInfo)
                    {
                        if (!uciInfo.IsInternalBearChessEngine && !uciInfo.IsInternalChessEngine)
                        {
                            enginesToDelete.Add(uciInfo);
                        }
                    }
                }

                foreach (var uciInfo in enginesToDelete)
                {
                    try
                    {
                        var uciId = uciInfo.Id;
                        var uciPath = Path.Combine(_uciPath, uciId);
                        if (Directory.Exists(uciPath))
                        {
                            var fileNames = Directory.GetFiles(uciPath);
                            foreach (var fileName in fileNames)
                            {
                                File.Delete(fileName);
                            }

                            Directory.Delete(uciPath);
                        }
                        Configuration.Instance.DeleteConfigValue($"{uciInfo.Id}_top");
                        Configuration.Instance.DeleteConfigValue($"{uciInfo.Id}_left");
                        Configuration.Instance.DeleteConfigValue($"{uciInfo.Id}_bottom");
                        Configuration.Instance.DeleteConfigValue($"{uciInfo.Id}_right");
                        _installedEngines.Remove(uciInfo.Name);
                        _uciInfos.Remove(uciInfo);
                    }
                    catch
                    {
                        //
                    }

                }
                return;
            }
            if (SelectedEngine.IsInternalBearChessEngine  || SelectedEngine.IsInternalChessEngine)
            {
                BearChessMessageBox.Show($"{_rm.GetString("CannotUninstallInternalEngine")} '{SelectedEngine.Name}'", _rm.GetString("UninstallEngine"),
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var engineName = SelectedEngine.Name;
            var engineId = SelectedEngine.Id;
            if (MessageBox.Show($"{_rm.GetString("UninstallEngine")} '{engineName}'?", _rm.GetString("UninstallEngine"),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) !=
                MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var uciPath = Path.Combine(_uciPath, engineId);
                if (Directory.Exists(uciPath))
                {
                    var fileNames = Directory.GetFiles(uciPath);
                    foreach (var fileName in fileNames)
                    {
                        File.Delete(fileName);
                    }

                    Directory.Delete(uciPath);
                }

                _installedEngines.Remove(engineName);
                _uciInfos.Remove(SelectedEngine);
            }
            catch (Exception ex)
            {
                BearChessMessageBox.Show($"{_rm.GetString("ErrorOnUninstallEngine")} '{engineName}'{Environment.NewLine}{ex.Message}", _rm.GetString("UninstallEngine"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ButtonInstall_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "UCI Engine|*.exe|UCI Engine|*.cmd;*.bat|All Files|*.*" };
            var showDialog = openFileDialog.ShowDialog(this);
            if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                LoadNewEngine(openFileDialog.FileName, string.Empty, string.Empty);
            }
        }

        private void NameChanged()
        {
            _uciInfos = new ObservableCollection<UciInfo>(_uciInfos.OrderBy(e => e.Name).ToList());
            var firstOrDefault = _uciInfos.FirstOrDefault(u => u.Id.Equals(SelectedEngine.Id));
            if (firstOrDefault == null)
            {
                firstOrDefault = _uciInfos.Count > 0 ? _uciInfos[0] : null;
            }
          
            dataGridEngine.ItemsSource = _uciInfos;
            if (firstOrDefault != null)
            {
                dataGridEngine.SelectedIndex = _uciInfos.IndexOf(firstOrDefault);
            }

            _installedEngines = new HashSet<string>(_uciInfos.Select(u => u.Name));
        }


        private void LoadNewEngine(string fileName, string parameters, string engineName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }
            if (Configuration.Instance.Standalone)
            {
                fileName = fileName.Replace(Configuration.Instance.BinPath, @".\");
                if (!File.Exists(fileName))
                {
                    MessageBox.Show("For standalone, the file must be local to the BearChess directory", "File is placed incorrectly", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            if (!File.Exists(fileName))
            {
                return;
            }
            ParameterSelection[] multiParameters = null;
            var multiSelection = false;
            var skipWarnings = false;
            try
            {
                //string parameters = string.Empty;
                var avatarName = string.Empty;
                if (fileName.EndsWith("MessChess.exe", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrWhiteSpace(parameters))
                {
                    var fileInfo = new FileInfo(fileName);
                    var enginesList = Path.Combine(fileInfo.DirectoryName,"Hiarcs","MessChess.lst");
                    var parameterSelectionWindow = new ParameterSelectionWindow() { Owner = this };
                    if (!File.Exists(enginesList))
                    {
                        enginesList = Path.Combine(fileInfo.DirectoryName, "Shredder", "MessChess.lst");
                    }
                    if (!File.Exists(enginesList))
                    {
                        enginesList = Path.Combine(fileInfo.DirectoryName, "Engines.lst");
                    }
                    if (File.Exists(enginesList))
                    {
                        var readAllLines = File.ReadAllLines(enginesList,Encoding.Default);
                        parameterSelectionWindow.ShowList(readAllLines);
                    }
                    parameterSelectionWindow.SetLabel("Engine:");
                    parameterSelectionWindow.SetMultiSelectionMode();
                    multiSelection = true;
                    var showDialog = parameterSelectionWindow.ShowDialog();
                    if (showDialog.HasValue && showDialog.Value)
                    {
                        multiParameters = parameterSelectionWindow.SelectedEngines;
                        parameters = multiParameters.First().ParameterName;
                        skipWarnings = parameterSelectionWindow.SkipWarnings;
                    }
                    else
                    {
                        return;
                    }
                }
                if (fileName.EndsWith("avatar.exe", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrWhiteSpace(parameters))
                {
                    var fileInfo = new FileInfo(fileName);
                    var avatars = Path.Combine(fileInfo.DirectoryName, "avatar_weights");
                    var parameterSelectionWindow = new ParameterSelectionWindow() { Owner = this };
                    if (Directory.Exists(avatars))
                    {
                        var avatarList = Directory.GetFiles(avatars,"*.zip",SearchOption.TopDirectoryOnly);
                        var avList = new List<string>();
                        foreach (var s in avatarList)
                        {
                            if (s.Contains(@"\"))
                            {
                                avList.Add(s.Substring(s.LastIndexOf(@"\")+1));
                            }
                        }

                        parameterSelectionWindow.ShowList(avList.ToArray());
                        parameterSelectionWindow.SetLabel("Avatar:");
                        parameterSelectionWindow.ShowParameterButton(true);
                        var showDialog = parameterSelectionWindow.ShowDialog();
                        if (showDialog.HasValue && showDialog.Value)
                        {
                            avatarName = parameterSelectionWindow.SelectedEngine.ParameterName;
                            parameters = $"--weights \"{Path.Combine(avatars, avatarName)}\"";
                            skipWarnings = parameterSelectionWindow.SkipWarnings;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        var openFileDialog = new OpenFileDialog { Filter = "Avatar Engine|*.zip|All Files|*.*" };
                        var showDialogAvatar = openFileDialog.ShowDialog(this);
                        if (showDialogAvatar.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
                        {
                            var info = new FileInfo(openFileDialog.FileName);
                            avatarName = info.Name;
                            parameters = $"--weights \"{info.FullName}\"";
                        }
                    }
                }

                UciInstaller uciInstaller = null;
                UciInfo uciInfo = null;
                var isAdded = false;
                if (multiSelection && multiParameters.Length>0)
                {
                    foreach (var multiParameter in multiParameters)
                    {
                        uciInstaller = new UciInstaller();
                        uciInfo = uciInstaller.Install(fileName, multiParameter.ParameterName,multiParameter.NewIndicator);
                       
                        if (!uciInfo.Valid)
                        {
                            throw new Exception($"{uciInfo.FileName} {_rm.GetString("IsNotValidEngine")}");
                        }

                        uciInfo.Name = multiParameter.ParameterDisplay;
                        if ( _installedEngines.Contains(uciInfo.Name))
                        {
                            if (!skipWarnings)
                            {
                                MessageBox.Show(
                                    this,
                                    $"Engine '{uciInfo.Name}' already installed!", _rm.GetString("UCIEngine"), MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }

                            continue;
                        }
                        uciInfo.Id = "uci" + Guid.NewGuid().ToString("N");
                        _installedEngines.Add(uciInfo.Name);
                        var uciPath = Path.Combine(_uciPath, uciInfo.Id);
                        if (!Directory.Exists(uciPath))
                        {
                            Directory.CreateDirectory(uciPath);
                        }

                        var serializer = new XmlSerializer(typeof(UciInfo));
                        TextWriter textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                        serializer.Serialize(textWriter, uciInfo);
                        textWriter.Close();
                        for (int i = 0; i < _uciInfos.Count; i++)
                        {
                            if (_uciInfos[i].Name.CompareTo(uciInfo.Name) < 0)
                            {
                                continue;

                            }

                            isAdded = true;
                            _uciInfos.Insert(i, uciInfo);
                            break;
                        }

                        if (!isAdded)
                        {
                            _uciInfos.Add(uciInfo);
                        }

                        SelectEngine(uciInfo);
                    }
                    return;
                }
                uciInstaller = new UciInstaller();
                uciInfo = uciInstaller.Install(fileName,  parameters, string.Empty);
                if (!string.IsNullOrWhiteSpace(engineName))
                {
                    uciInfo.Name = engineName;
                }
                if (!uciInfo.Valid)
                {
                    throw new Exception($"{uciInfo.FileName} {_rm.GetString("IsNotValidEngine")}");
                }

                if (uciInfo.Name.Equals("Avatar", StringComparison.OrdinalIgnoreCase))
                {
                    uciInfo.Name = "Avatar " + avatarName.Replace("_"," ").Replace(".zip",string.Empty);
                }
                if (_installedEngines.Contains(uciInfo.Name))
                {
                    if (!skipWarnings)
                    {
                        MessageBox.Show(
                            this,
                            $"{_rm.GetString("Engine")} '{uciInfo.Name}' {_rm.GetString("AlReadyInstalled")}", _rm.GetString("UCIEngine"), MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    return;
                }

               
                uciInfo.Id = "uci" + Guid.NewGuid().ToString("N");
                if (MessageBox.Show(this, $"{_rm.GetString("InstallEngine")}{Environment.NewLine}{uciInfo.Name}{Environment.NewLine}{_rm.GetString("Author")}: {uciInfo.Author}",
                        _rm.GetString("UCIEngine"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _installedEngines.Add(uciInfo.Name);
                    var uciPath = Path.Combine(_uciPath, uciInfo.Id);
                    if (!Directory.Exists(uciPath))
                    {
                        Directory.CreateDirectory(uciPath);
                    }

                    var serializer = new XmlSerializer(typeof(UciInfo));
                    TextWriter textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                    serializer.Serialize(textWriter, uciInfo);
                    textWriter.Close();
                    var uciConfigWindow = new UciConfigWindow(uciInfo, true, false, false) { Owner = this };
                    var dialog = uciConfigWindow.ShowDialog();

                    if (dialog.HasValue && dialog.Value)
                    {
                        var info = uciConfigWindow.GetUciInfo();
                        for (int i = 0; i < _uciInfos.Count; i++)
                        {
                            if (_uciInfos[i].Name.CompareTo(info.Name) < 0)
                            {
                                continue;

                            }

                            isAdded = true;
                            _uciInfos.Insert(i, info);
                            break;
                        }

                        if (!isAdded)
                        {
                            _uciInfos.Add(info);
                        }
                        SelectEngine(info);
                        //_uciInfos.Add(info);
                        serializer = new XmlSerializer(typeof(UciInfo));
                        textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                        serializer.Serialize(textWriter, info);
                        textWriter.Close();
                    }
                    else
                    {
                        for (int i = 0; i < _uciInfos.Count; i++)
                        {
                            if (_uciInfos[i].Name.CompareTo(uciInfo.Name) < 0)
                            {
                                continue;

                            }
                            isAdded = true;
                            _uciInfos.Insert(i, uciInfo);
                            break;
                        }
                        if (!isAdded)
                        {
                            _uciInfos.Add(uciInfo);
                        }
                        SelectEngine(uciInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, _rm.GetString("ErrorInstallEngine"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DataGridEngine_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null)
                {
                    return;
                }

                e.Handled = true;
                var fileInfo = new FileInfo(files[0]);
                LoadNewEngine(fileInfo.FullName, string.Empty, string.Empty);
            }
        }

        private void DataGridEngine_OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length != 1 || !files[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void ButtonAddConfigure_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFileAndParameterWindow = new SelectFileAndParameterWindow
                                               {
                                                   Owner = this
                                               };
            var showDialog = selectFileAndParameterWindow.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                LoadNewEngine(selectFileAndParameterWindow.Filename,selectFileAndParameterWindow.Parameter, selectFileAndParameterWindow.EngineName);
            }
        }

        private void SelectInstalledEngineWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Configuration.Instance.SetDoubleValue("InstalledEngineWindowLeft", Left);
            Configuration.Instance.SetDoubleValue("InstalledEngineWindowTop", Top);
            Configuration.Instance.SetDoubleValue("InstalledEngineWindowWidth", Width);
            Configuration.Instance.SetDoubleValue("InstalledEngineWindowHeight", Height);
        }

        private void DataGridEngine_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            buttonAddConfigure.IsEnabled = dataGridEngine.SelectedItems.Count < 2;
            buttonConfigure.IsEnabled = dataGridEngine.SelectedItems.Count == 1;
            buttonInstall.IsEnabled = dataGridEngine.SelectedItems.Count < 2;
            buttonOk.IsEnabled = dataGridEngine.SelectedItems.Count == 1;
        }

        private void TextBoxFilter_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var strings = textBoxFilter.Text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length > 0)
            {
                var uciInfos = new List<UciInfo>(_uciInfos);
                foreach (var s in strings)
                {
                    uciInfos.RemoveAll(r => !r.Name.ContainsCaseInsensitive(s));
                }

                dataGridEngine.ItemsSource = uciInfos.Distinct().OrderBy(u => u.Name);
                return;
            }
            dataGridEngine.ItemsSource = _uciInfos.OrderBy(u => u.Name);
        }

        private void MenuItemSetAsBuddy_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedEngine == null)
            {
                return;
            }
            if (!SelectedEngine.ValidForAnalysis)
            {
                BearChessMessageBox.Show(_rm.GetString("NotSuitableForBuddy"), _rm.GetString("NotSuitable"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
            if (SelectedEngine.IsProbing)
            {
                BearChessMessageBox.Show(_rm.GetString("BearChessCannotBuddy"), _rm.GetString("NotSuitable"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
            if (SelectedEngine.IsInternalBearChessEngine)
            {
                BearChessMessageBox.Show(_rm.GetString("NotSuitableForBuddy"), _rm.GetString("NotSuitable"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
            string uciPath;
            XmlSerializer serializer;
            TextWriter textWriter;
            SelectedEngine.IsBuddy = !SelectedEngine.IsBuddy;
            foreach (var uciInfo in _uciInfos)
            {
                if (uciInfo.Id.Equals(SelectedEngine.Id))
                {
                    continue;
                }

                if (!uciInfo.IsBuddy)
                {
                    continue;
                }

                uciInfo.IsBuddy = false;
                uciPath = Path.Combine(_uciPath, uciInfo.Id);
                serializer = new XmlSerializer(typeof(UciInfo));
                textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                serializer.Serialize(textWriter, uciInfo);
                textWriter.Close();
                break;
            }
            uciPath = Path.Combine(_uciPath, SelectedEngine.Id);
            serializer = new XmlSerializer(typeof(UciInfo));
            textWriter = new StreamWriter(Path.Combine(uciPath, SelectedEngine.Id + ".uci"), false);
            serializer.Serialize(textWriter, SelectedEngine);
            textWriter.Close();
            NameChanged();
        }

        private void MenuItemSetAsBearChess_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedEngine == null)
            {
                return;
            }
            if (!SelectedEngine.ValidForAnalysis)
            {
                BearChessMessageBox.Show(_rm.GetString("NotSuitableForBearChess"), _rm.GetString("NotSuitable"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            if (SelectedEngine.IsBuddy)
            {
                BearChessMessageBox.Show(_rm.GetString("BuddyCannotBearChess"), _rm.GetString("NotSuitable"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            string uciPath;
            XmlSerializer serializer;
            TextWriter textWriter;
            SelectedEngine.IsProbing = !SelectedEngine.IsProbing;
            foreach (var uciInfo in _uciInfos)
            {
                if (uciInfo.Id.Equals(SelectedEngine.Id))
                {
                    continue;
                }

                if (!uciInfo.IsProbing)
                {
                    continue;
                }

                uciInfo.IsProbing = false;
                uciPath = Path.Combine(_uciPath, uciInfo.Id);
                serializer = new XmlSerializer(typeof(UciInfo));
                textWriter = new StreamWriter(Path.Combine(uciPath, uciInfo.Id + ".uci"), false);
                serializer.Serialize(textWriter, uciInfo);
                textWriter.Close();
                break;
            }
            uciPath = Path.Combine(_uciPath, SelectedEngine.Id);
            serializer = new XmlSerializer(typeof(UciInfo));
            textWriter = new StreamWriter(Path.Combine(uciPath, SelectedEngine.Id + ".uci"), false);
            serializer.Serialize(textWriter, SelectedEngine);
            textWriter.Close();
            NameChanged();
        }

        private void DataGridEngine_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is UIElement u)
            {
                e.Handled = true;
                DialogResult = true;
            }
        }
    }
}
