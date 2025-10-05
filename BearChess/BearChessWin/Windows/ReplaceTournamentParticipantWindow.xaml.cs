using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Definitions;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessBaseLib.Definitions;
using www.SoLaNoSoft.com.BearChessDatabase;
using www.SoLaNoSoft.com.BearChessTournament;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für ReplaceTournamentParticipantWindow.xaml
    /// </summary>
    public partial class ReplaceTournamentParticipantWindow : Window
    {
        private readonly Configuration _configuration;
        private readonly Database _database;
        private readonly PgnConfiguration _pgnConfiguration;
        private  ObservableCollection<UciInfo> _uciInfos;
        private  ObservableCollection<UciInfo> _uciInfosPlayer;


        private bool _isInitialized = false;
        private readonly ResourceManager _rm;

     
        public UciInfo RemovedParticipant
        {
            get; private set;
        }
        public UciInfo AddedParticipant
        {
            get; private set;
        }

        public bool CreateNewTournament => radioButtonCreateNew.IsChecked.HasValue && radioButtonCreateNew.IsChecked.Value;

        public UciInfo[] Participants => _uciInfosPlayer.ToArray();
        
        public string GameEvent => textBoxGameEvent.Text;

        private readonly CurrentTournament _currentTournament;

        public ReplaceTournamentParticipantWindow()
        {
            InitializeComponent();
            _isInitialized = true;
            _rm = SpeechTranslator.ResourceManager;
        }

        public ReplaceTournamentParticipantWindow(IEnumerable<UciInfo> uciInfos, Configuration configuration, Database database, CurrentTournament currentTournament, bool replaceParticipant) : this()
        {
            _configuration = configuration;
            _database = database;
            _uciInfos = new ObservableCollection<UciInfo>(uciInfos.Where(u => !u.IsPlayer).OrderBy(e => e.Name).ToList());
            _uciInfosPlayer = new ObservableCollection<UciInfo>(currentTournament.Players);
            dataGridEngine.ItemsSource = _uciInfos;
            labelEngines.Content = $"{_rm.GetString("AvailableEngines")} ({_uciInfos.Count})";
            dataGridEnginePlayer.ItemsSource = _uciInfosPlayer;
           
            textBoxGameEvent.Text = currentTournament.GameEvent;
            _currentTournament = currentTournament;
           
        }
     

        public CurrentTournament GetCurrentTournament()
        {
           
            return new CurrentTournament(Participants.ToList(), _currentTournament.Deliquent, GetTimeControl(), _currentTournament.TournamentType, _currentTournament.Cycles,
                _currentTournament.TournamentSwitchColor, GameEvent, true);
        }

        public TimeControl GetTimeControl()
        {
            return _currentTournament.TimeControl;
          
        }

        
    
       

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
          
            DialogResult = true;
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        

        private void ButtonConfig_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = dataGridEnginePlayer.SelectedItem is UciInfo;
            if (selectedItem)
            {
                var uciConfigWindow = new UciConfigWindow((UciInfo)dataGridEnginePlayer.SelectedItem, false, false, false) { Owner = this };
                var showDialog = uciConfigWindow.ShowDialog();
                if (showDialog.HasValue && showDialog.Value)
                {
                    var indexOf = _uciInfosPlayer.IndexOf((UciInfo)dataGridEnginePlayer.SelectedItem);
                    _uciInfosPlayer.Remove((UciInfo)dataGridEnginePlayer.SelectedItem);
                    _uciInfosPlayer.Insert(indexOf, uciConfigWindow.GetUciInfo());
                }
            }
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var strings = textBoxFilter.Text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (strings.Length > 0)
            {
                List<UciInfo> uciInfos = new List<UciInfo>(_uciInfos);
                foreach (var s in strings)
                {
                    uciInfos.RemoveAll(r => !r.Name.ContainsCaseInsensitive(s));
                }

                dataGridEngine.ItemsSource = uciInfos.Distinct().OrderBy(u => u.Name);
                return;
            }
            dataGridEngine.ItemsSource = _uciInfos.OrderBy(u => u.Name);
        }

        private void ResetParticipants()
        {
            if (RemovedParticipant == null || AddedParticipant == null)
            {
                return;
            }

            int indexOf = _uciInfosPlayer.IndexOf(AddedParticipant);
            _uciInfosPlayer[indexOf] = RemovedParticipant;
            RemovedParticipant = null;
            AddedParticipant = null;
        }

        private void ButtonReplace_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItemOld = dataGridEnginePlayer.SelectedItem is UciInfo;
            var selectedItem = dataGridEngine.SelectedItem is UciInfo;
            if (selectedItemOld && selectedItem)
            {
                if (_uciInfosPlayer.FirstOrDefault(u => u.Id.Equals(((UciInfo)dataGridEngine.SelectedItem).Id)) != null)
                {
                    return;
                }
                ResetParticipants();
                RemovedParticipant = (UciInfo)dataGridEnginePlayer.SelectedItem;
                AddedParticipant = (UciInfo)dataGridEngine.SelectedItem;
                if (RemovedParticipant == null || AddedParticipant == null)
                {
                    RemovedParticipant = null;
                    AddedParticipant = null;
                    return;
                }
                int indexOf = _uciInfosPlayer.IndexOf(RemovedParticipant);
                _uciInfosPlayer[indexOf] = AddedParticipant;
            }
        }

        private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
        {
          ResetParticipants();
        }
    }
}
