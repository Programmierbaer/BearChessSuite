using System;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows;
using System.Threading;


namespace SpeechTest
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum State
        {
            Idle = 0,
            Accepting = 1,
            Off = 2,
        }

        private State RecogState = State.Off;
        private SpeechRecognitionEngine srecog;
        private SpeechSynthesizer synth = null;
        private int Hypothesized = 0;
        private int Recognized = 0;

        public MainWindow()
        {
            InitializeComponent();
            //var speechRecognizer = new SpeechRecognizer();
             this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           // return;
            InitializeRecognizerSynthesizer();

            if (SelectInputDevice())
            {
                LoadDictationGrammar();
                btnStart.IsEnabled = true;
                ReadAloud("Speech Engine Ready for Input");
            }
        }

        private void InitializeRecognizerSynthesizer()
        {
            var readOnlyCollection = SpeechRecognitionEngine.InstalledRecognizers();
            //var selectedRecognizer = (from o in readOnlyCollection
            //                          where o.Culture.Equals(Thread.CurrentThread.CurrentCulture)
            //                          select o).FirstOrDefault();
            var selectedRecognizer = (from o in readOnlyCollection
                where o.Culture.Name.Contains("en")
                select o).FirstOrDefault();
            srecog = new SpeechRecognitionEngine(selectedRecognizer);
            srecog.AudioStateChanged += new EventHandler<AudioStateChangedEventArgs>(recognizer_AudioStateChanged);
            srecog.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(recognizer_SpeechHypothesized);
            srecog.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);

            synth = new SpeechSynthesizer();
        }

        private bool SelectInputDevice()
        {
            bool proceedLoading = true;
            if (IsOscompatible())
            {
                try
                {
                    srecog.SetInputToDefaultAudioDevice();
                }
                catch
                {
                    proceedLoading = false;
                }
            }
            else
                ThreadPool.QueueUserWorkItem(InitSpeechRecogniser);
            return proceedLoading;
        }

        private bool IsOscompatible()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            if (osInfo.Version > new Version("6.0"))
                return true;
            else
                return false;
        }

        private void InitSpeechRecogniser(object o)
        {
            srecog.SetInputToDefaultAudioDevice();
        }

        private void LoadDictationGrammar()
        {
            //GrammarBuilder grammarBuilder = new GrammarBuilder();
            //grammarBuilder.Append(new Choices("Neues","Spiel"));
            //grammarBuilder.Append(new Choices("kurze","Rochade"));
            //grammarBuilder.Append(new Choices("lange","Rochade"));
            //Grammar commandGrammar = new Grammar(grammarBuilder);
            //commandGrammar.Name = "main command grammar";
            //srecog.LoadGrammar(commandGrammar);

            DictationGrammar dictationGrammar = new DictationGrammar();
            dictationGrammar.Name = "dictation";
            srecog.LoadGrammar(dictationGrammar);
        }

        private void recognizer_AudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
            switch (e.AudioState)
            {
                case AudioState.Speech:
                    lStatus.Content = "Listening";
                    break;
                case AudioState.Silence:
                    lStatus.Content = "Idle";
                    break;
                case AudioState.Stopped:
                    lStatus.Content = "Stopped";
                    break;
            }
        }

        private void recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Hypothesized++;
            tHypothesized.Text = "Hypothesized: " + Hypothesized.ToString();
        }
        private void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Recognized++;
            tRecognized.Text = "Recognized: " + Recognized.ToString();

            if (RecogState == State.Off)
                return;
            float accuracy = (float)e.Result.Confidence;
            string phrase = e.Result.Text;
            {
                if (phrase == "Diktat ende")
                {
                    RecogState = State.Off;
                    srecog.RecognizeAsyncStop();
                    ReadAloud("Diktat beendet");
                    return;
                }
                TextBox1.AppendText(" " + e.Result.Text);
            }
        }

        public void ReadAloud(string speakText)
        {
            try
            {
                srecog.RecognizeAsyncCancel();
                synth.SpeakAsync(speakText);
            }
            catch { }
        }


        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            switch (RecogState)
            {
                case State.Off:
                    RecogState = State.Accepting;
                    btnStart.Content = "Stop";
                    srecog.RecognizeAsync(RecognizeMode.Multiple);
                    break;
                case State.Accepting:
                    RecogState = State.Off;
                    btnStart.Content = "Start";
                    srecog.RecognizeAsyncStop();
                    break;
            }
        }
    }
}
