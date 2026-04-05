using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Win32;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.CTG;
using www.SoLaNoSoft.com.BearChessTools;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für SelectInstalledBookWindow.xaml
    /// </summary>
    public partial class SelectInstalledBookWindow : Window
    {
        private readonly string _bookPath;
        private readonly ObservableCollection<BookInfo> _openingBooks;
        private readonly HashSet<string> _installedBooks;
        private readonly ResourceManager _rm;
        public BookInfo SelectedBook => (BookInfo)dataGridBook.SelectedItem;


        public SelectInstalledBookWindow(BookInfo[] openingBooks, string bookPath)
        {
            InitializeComponent();
            _bookPath = bookPath;
            Top = Configuration.Instance.GetWinDoubleValue("InstalledBookWindowTop", Configuration.WinScreenInfo.Top, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth, "300");
            Left = Configuration.Instance.GetWinDoubleValue("InstalledBookWindowLeft", Configuration.WinScreenInfo.Left, SystemParameters.VirtualScreenHeight, SystemParameters.VirtualScreenWidth, "400");
            Height = Configuration.Instance.GetDoubleValue("InstalledBookWindowHeight", "250");
            Width = Configuration.Instance.GetDoubleValue("InstalledBookWindowWidth", "600");
            _rm = SpeechTranslator.ResourceManager;
            var configValue = Configuration.Instance.GetConfigValue("defaultBook", Constants.InternalBookGUIDPerfectCTG);
            var firstOrDefault = openingBooks.FirstOrDefault(b => b.Id.Equals(configValue));
            if (firstOrDefault != null)
            {
                firstOrDefault.IsDefaultBook = true;
            }

            _openingBooks = new ObservableCollection<BookInfo>(openingBooks.OrderBy(o => o.Name));
            dataGridBook.ItemsSource = _openingBooks;
            _installedBooks = new HashSet<string>(openingBooks.Select(b => b.Name));
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedBook == null)
            {
                return;
            }

            var bookName = SelectedBook.Name;
            var bookId = SelectedBook.Id;
            if (SelectedBook.IsInternalBook)
            {
                MessageBox.Show($"{_rm.GetString("CannotUninstallInternalBook")}: '{bookName}'", _rm.GetString("UninstallBook"),
                    MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            if (MessageBox.Show($"{_rm.GetString("UninstallBook")} '{bookName}'?", _rm.GetString("UninstallBook"),
                                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) !=
                MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                File.Delete(Path.Combine(_bookPath, bookId + ".book"));
                _installedBooks.Remove(bookName);
                _openingBooks.Remove(SelectedBook);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{_rm.GetString("ErrorUninstallBook")} '{bookName}'{Environment.NewLine}{ex.Message}", _rm.GetString("UninstallBook"),
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridBook_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedBook != null)
            {
                DialogResult = true;
            }
        }

        private void LoadBook(string fileName)
        {
            if (Configuration.Instance.Standalone)
            {
                fileName = fileName.Replace(Configuration.Instance.BinPath, @".\");
                if (!File.Exists(fileName))
                {
                    MessageBox.Show("For standalone, the file must be local to the BearChess directory",
                        "File is placed incorrectly", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var openingBook = new OpeningBook();
            if (openingBook.LoadBook(fileName, true))
            {

                var fileInfo = new FileInfo(fileName);
                if (_installedBooks.Contains(fileInfo.Name))
                {
                    MessageBox.Show(
                        this,
                        $"{_rm.GetString("OpeningBook")} '{fileInfo.Name}' {_rm.GetString("AlReadyInstalled")}!",
                        _rm.GetString("OpeningBook"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;

                }

                var bookInfo = new BookInfo()
                {
                    Id = "bk" + Guid.NewGuid().ToString("N"),
                    Name = fileInfo.Name,
                    FileName = fileName,
                    Size = fileInfo.Length,
                    PositionsCount = openingBook.PositionsCount,
                    MovesCount = openingBook.MovesCount,
                    GamesCount = openingBook.GamesCount
                };
                var serializer = new XmlSerializer(typeof(BookInfo));
                TextWriter textWriter = new StreamWriter(Path.Combine(_bookPath, bookInfo.Id + ".book"), false);
                serializer.Serialize(textWriter, bookInfo);
                textWriter.Close();
                _installedBooks.Add(bookInfo.Name);
                _openingBooks.Add(bookInfo);
            }
        }

        private void ButtonInstall_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = $"{_rm.GetString("AllBooks")}|*.abk;*.bin;*.ctg|Polyglot|*.bin|ChessBase|*.ctg|Arena|*.abk" };
            var showDialog = openFileDialog.ShowDialog(this);
            if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                LoadBook(openFileDialog.FileName);
                
            }
        }

        private void MenuItemSetDefaultBook_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedBook == null)
            {
                return;
            }

            Configuration.Instance.SetConfigValue("defaultBook",SelectedBook.Id);
            foreach (var openingBook in _openingBooks)
            {
                openingBook.IsDefaultBook = false;
            }

            SelectedBook.IsDefaultBook = true;

        }

        private void SelectInstalledBookWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Configuration.Instance.SetDoubleValue("InstalledBookWindowLeft", Left);
            Configuration.Instance.SetDoubleValue("InstalledBookWindowTop", Top);
            Configuration.Instance.SetDoubleValue("InstalledBookWindowWidth", Width);
            Configuration.Instance.SetDoubleValue("InstalledBookWindowHeight", Height);

        }
        
        private void ButtonInstall_OnDrop(object sender, DragEventArgs e)
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
                LoadBook(fileInfo.FullName);
            }
        }

        private void ButtonInstall_OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length == 1)
            {
                if (files[0].EndsWith(".bin")  || files[0].EndsWith(".ctg")
                                              || files[0].EndsWith(".abk"))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;                    
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }
    }
}
