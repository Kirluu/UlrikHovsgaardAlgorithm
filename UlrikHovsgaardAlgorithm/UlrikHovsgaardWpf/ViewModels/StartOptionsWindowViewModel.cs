﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Parsing;

namespace UlrikHovsgaardWpf.ViewModels
{
    public delegate void DcrGraphLoaded(DcrGraph graph);

    public delegate void LogLoaded(Log log);

    public delegate void AlphabetSizeSelected(int alphabetSize);

    public class StartOptionsWindowViewModel : CloseableViewModel, INotifyPropertyChanged
    {
        public event DcrGraphLoaded DcrGraphLoaded;
        public event LogLoaded LogLoaded;
        public event AlphabetSizeSelected AlphabetSizeSelected;


        #region Fields

        private const string OwnFileSelected = "Select your own file";
        private const string HospitalLog = "Hospital workflow log";
        private const string BpiChallenge2015 = "BPI Challenge 2015";
        private const string BpiChallenge2014 = "BPI Challenge 2014";

        #endregion

        #region Properties

        private ObservableCollection<string> _logChoices;
        private string _logChosen;
        private string _alphabetSize;

        public ObservableCollection<string> LogChoices { get { return _logChoices; } set { _logChoices = value; OnPropertyChanged(); } }
        public string LogChosen { get { return _logChosen; } set { _logChosen = value; OnPropertyChanged(); OnPropertyChanged("AddLogButtonName"); } }
        public string AlphabetSize { get { return _alphabetSize; } set { _alphabetSize = value; OnPropertyChanged(); } }

        public string AddLogButtonName
        {
            get
            {
                if (LogChosen == OwnFileSelected) return "Select log...";
                return "Load log";
            }
        }



        #region Command properties

        private ICommand _logChosenConfirmedCommand;
        private ICommand _alphabetSizeChosenConfirmedCommand;
        private ICommand _dcrGraphChosenConfirmedCommand;

        public ICommand LogChosenConfirmedCommand { get { return _logChosenConfirmedCommand; } set { _logChosenConfirmedCommand = value; OnPropertyChanged(); } }
        public ICommand AlphabetSizeChosenConfirmedCommand { get { return _alphabetSizeChosenConfirmedCommand; } set { _alphabetSizeChosenConfirmedCommand = value; OnPropertyChanged(); } }
        public ICommand DcrGraphChosenConfirmedCommand { get { return _dcrGraphChosenConfirmedCommand; } set { _dcrGraphChosenConfirmedCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public StartOptionsWindowViewModel()
        {
            LogChoices = new ObservableCollection<string> { OwnFileSelected, HospitalLog, BpiChallenge2014, BpiChallenge2015 };
            AlphabetSize = "";
            LogChosenConfirmedCommand = new ButtonActionCommand(LogChosenConfirmed);
            AlphabetSizeChosenConfirmedCommand = new ButtonActionCommand(AlphabetSizeChosenConfirmed);
            DcrGraphChosenConfirmedCommand = new ButtonActionCommand(DcrGraphChosenConfirmed);
        }


        #region Event handling methods

        private void LogChosenConfirmed()
        {
            switch (LogChosen)
            {
                case OwnFileSelected:
                    var filePath = BrowseLog();
                    if (filePath != null)
                    {
                        // TODO: Attempt parsing and fire event if success
                        MessageBox.Show(filePath);
                        // Close view
                        OnClosingRequest();
                    }
                    break;
                case HospitalLog:
                    break;
                case BpiChallenge2014:
                    // TODO: Remove or find other log - this one is actually CSV format - shouldn't need to support
                    break;
                case BpiChallenge2015:
                    try
                    {
                        var log = XmlParser.ParseLog(Properties.Resources.BPIC15_1_xes);
                        // Fire event
                        LogLoaded?.Invoke(log);
                        // Close view
                        OnClosingRequest();
                    }
                    catch
                    {
                        MessageBox.Show("An error occured when trying to parse the log", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                default:
                    MessageBox.Show("Unexpected choice occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void AlphabetSizeChosenConfirmed()
        {
            if (string.IsNullOrEmpty(AlphabetSize))
                MessageBox.Show("Please enter an integer value.");
            int amount;
            if (int.TryParse(AlphabetSize, out amount))
            {
                if (amount < 0 || amount > 26) // A-Z
                {
                    MessageBox.Show("Please enter a value in the interval [0-26].", "Whoops!");
                    return;
                }
                // Fire event
                AlphabetSizeSelected?.Invoke(amount);
                // Close view
                OnClosingRequest();
            }
            else
            {
                MessageBox.Show("Value is not an integer. Please enter a valid integer value.", "Error");
            }
        }

        private void DcrGraphChosenConfirmed()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a graph XML-file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK) // They selected a file
            {
                var filePath = dialog.FileName;
                var xml = File.ReadAllText(filePath);

                var graphFromXml = XmlParser.ParseDcrGraph(xml);

                // Fire event
                DcrGraphLoaded?.Invoke(graphFromXml);
                // Close view
                OnClosingRequest();
            }
        }

        #endregion

        private string BrowseLog()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a log file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}