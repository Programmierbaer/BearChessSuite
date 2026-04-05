using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChessDatabase;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    public static class ExportGames
    {

        public static void ExportAsPuzzle(ILogging logger, IList selectedItems, Database database,
            PgnConfiguration pgnConfiguration, Window owner)
        {
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("No games for export", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Puzzle|*.pgn;" };
                var showDialog = saveFileDialog.ShowDialog(owner);
                var fileName = saveFileDialog.FileName;
                if (showDialog.Value && !string.IsNullOrWhiteSpace(fileName))
                {
                    var puzzleDB = new PuzzleDatabase(logger, fileName);
                    var infoWindow = new ProgressWindow
                    {
                        Owner = owner
                    };

                    var sb = new StringBuilder();

                    infoWindow.SetMaxValue(selectedItems.Count);
                    infoWindow.Show();
                    var i = 0;
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem is DatabaseGameSimple databaseGameSimple)
                        {
                            var pgnGame = database.LoadGame(databaseGameSimple.Id, pgnConfiguration).PgnGame;
                            puzzleDB.SavePuzzle("", pgnGame.GameEvent, pgnGame.GetGame(), pgnGame.MoveCount);

                            i++;
                            infoWindow.SetCurrentValue(i);
                        }
                    }

                    infoWindow.Close();
                    MessageBox.Show(
                        $"{selectedItems.Count} games exported into{Environment.NewLine}{fileName} ",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ExportAsRepertoire(ILogging logger, IList selectedItems, Database database,PgnConfiguration pgnConfiguration, Window owner)
        {
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("No games for export", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Database|*.rep;" };
                var showDialog = saveFileDialog.ShowDialog(owner);
                var fileName = saveFileDialog.FileName;
                if (showDialog.Value && !string.IsNullOrWhiteSpace(fileName))
                {
                    var repDB = new RepertoireDatabase(logger, fileName, owner);
                    var infoWindow = new ProgressWindow
                                     {
                                         Owner = owner
                                     };

                    var sb = new StringBuilder();

                    infoWindow.SetMaxValue(selectedItems.Count);
                    infoWindow.Show();
                    var i = 0;
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem is DatabaseGameSimple databaseGameSimple)
                        {
                            repDB.Save(database.LoadGame(databaseGameSimple.Id, pgnConfiguration),false);
                            i++;
                            infoWindow.SetCurrentValue(i);
                        }
                    }
                    infoWindow.Close();
                    MessageBox.Show(
                        $"{selectedItems.Count} games exported into{Environment.NewLine}{fileName} ",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
         public static void Export(IList selectedItems, PuzzleDatabase database,PgnConfiguration pgnConfiguration, Window owner)
        {
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("No puzzle for export", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Puzzle|*.pgn;" };
                var showDialog = saveFileDialog.ShowDialog(owner);
                var fileName = saveFileDialog.FileName;
                if (showDialog.Value && !string.IsNullOrWhiteSpace(fileName))
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    var infoWindow = new ProgressWindow
                                     {
                                         Owner = owner
                                     };

                    var sb = new StringBuilder();

                    infoWindow.SetMaxValue(selectedItems.Count);
                    infoWindow.Show();
                    var i = 0;
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem is DatabasePuzzle pgnGame)
                        {
                            sb.AppendLine(database.LoadPuzzle(pgnGame.Id).PgnGame.GetGame());
                            sb.AppendLine(string.Empty);
                            i++;
                            infoWindow.SetCurrentValue(i);
                        }
                    }

                    File.WriteAllText(fileName, sb.ToString());
                    infoWindow.Close();
                    MessageBox.Show(
                        $"{selectedItems.Count} games exported into{Environment.NewLine}{fileName} ",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public static void Export(IList selectedItems, Database database,PgnConfiguration pgnConfiguration, Window owner)
        {
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("No games for export", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Games|*.pgn;" };
                var showDialog = saveFileDialog.ShowDialog(owner);
                var fileName = saveFileDialog.FileName;
                if (showDialog.Value && !string.IsNullOrWhiteSpace(fileName))
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    var infoWindow = new ProgressWindow
                                     {
                                         Owner = owner
                                     };

                    var sb = new StringBuilder();

                    infoWindow.SetMaxValue(selectedItems.Count);
                    infoWindow.Show();
                    var i = 0;
                    foreach (var selectedItem in selectedItems)
                    {
                        if (selectedItem is DatabaseGameSimple pgnGame)
                        {
                            sb.AppendLine(database.LoadGame(pgnGame.Id, pgnConfiguration).PgnGame.GetGame());
                            sb.AppendLine(string.Empty);
                            i++;
                            infoWindow.SetCurrentValue(i);
                        }
                    }

                    File.WriteAllText(fileName, sb.ToString());
                    infoWindow.Close();
                    MessageBox.Show(
                        $"{selectedItems.Count} games exported into{Environment.NewLine}{fileName} ",
                        "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}