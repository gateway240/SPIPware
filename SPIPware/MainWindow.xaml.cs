﻿using System;
using System.Windows;
using SPIPware.Communication;
using SPIPware.Util;
using System.ComponentModel;
using System.Windows.Controls.Ribbon;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace SPIPware
{
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        Machine machine = Machine.Instance;
        CameraControl camera = CameraControl.Instance;
        CycleControl cycle = new CycleControl();


        GrblSettingsWindow settingsWindow = new GrblSettingsWindow();
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            InitializeComponent();

            cycle.StatusUpdate += UpdateCycleStatus;
            updateSerialPortComboBox(PeripheralSerialPortSelect);
            updateSerialPortComboBox(SerialPortSelect);

            Task task = new Task(() => camera.StartVimba());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
            //camera.StartVimba();
            Task task2 = new Task(() => UpdateCheck.CheckForUpdate());
            task2.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task2.Start();

            machine.ConnectionStateChanged += Machine_ConnectionStateChanged;

            machine.NonFatalException += Machine_NonFatalException;
            machine.Info += Machine_Info;
            machine.LineReceived += Machine_LineReceived;
            machine.LineReceived += settingsWindow.LineReceived;
            machine.StatusReceived += Machine_StatusReceived;
            machine.LineSent += Machine_LineSent;

            machine.PositionUpdateReceived += Machine_PositionUpdateReceived;
            machine.StatusChanged += Machine_StatusChanged;
            machine.DistanceModeChanged += Machine_DistanceModeChanged;
            machine.UnitChanged += Machine_UnitChanged;
            machine.PlaneChanged += Machine_PlaneChanged;
            machine.BufferStateChanged += Machine_BufferStateChanged;
            machine.OperatingModeChanged += Machine_OperatingMode_Changed;
            //machine.FileChanged += Machine_FileChanged;
            //machine.FilePositionChanged += Machine_FilePositionChanged;
            //machine.ProbeFinished += Machine_ProbeFinished;
            machine.OverrideChanged += Machine_OverrideChanged;
            machine.PinStateChanged += Machine_PinStateChanged;

            Machine_OperatingMode_Changed();
            Machine_PositionUpdateReceived();

            Properties.Settings.Default.SettingChanging += Default_SettingChanging;
            FileRuntimeTimer.Tick += FileRuntimeTimer_Tick;

            machine.ProbeFinished += Machine_ProbeFinished_UserOutput;

            settingsWindow.SendLine += machine.SendLine;

            CheckBoxUseExpressions_Changed(null, null);
            //updatePlateCheckboxes();
            
            //cameraControl.m_CameraList = m_CameraList;
            camera.m_PictureBox = m_PictureBox;

        }

        public Vector3 LastProbePosMachine { get; set; }
        public Vector3 LastProbePosWork { get; set; }

        

        private void Machine_ProbeFinished_UserOutput(Vector3 position, bool success)
        {
            LastProbePosMachine = machine.LastProbePosMachine;
            LastProbePosWork = machine.LastProbePosWork;

            RaisePropertyChanged("LastProbePosMachine");
            RaisePropertyChanged("LastProbePosWork");
        }
        static void ExceptionHandler(Task task)
        {
            var exception = task.Exception;
            Console.WriteLine(exception);
        }
        private void UnhandledException(object sender, UnhandledExceptionEventArgs ea)
        {
            Exception e = (Exception)ea.ExceptionObject;

            string info = "Unhandled Exception:\r\nMessage:\r\n";
            info += e.Message;
            info += "\r\nStackTrace:\r\n";
            info += e.StackTrace;
            info += "\r\nToString():\r\n";
            info += e.ToString();

            MessageBox.Show(info);
            Console.WriteLine(info);

            try
            {
                System.IO.File.WriteAllText("SPIPware_Crash_Log.txt", info);
            }
            catch { }

            Environment.Exit(1);
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            if (e.SettingName.Equals("JogFeed") ||
                e.SettingName.Equals("JogDistance") ||
                e.SettingName.Equals("ProbeFeed") ||
                e.SettingName.Equals("ProbeSafeHeight") ||
                e.SettingName.Equals("ProbeMinimumHeight") ||
                e.SettingName.Equals("ProbeMaxDepth") ||
                e.SettingName.Equals("SplitSegmentLength") ||
                e.SettingName.Equals("ViewportArcSplit") ||
                e.SettingName.Equals("ArcToLineSegmentLength") ||
                e.SettingName.Equals("ProbeXAxisWeight") ||
                e.SettingName.Equals("ConsoleFadeTime"))
            {
                if (((double)e.NewValue) <= 0)
                    e.Cancel = true;
            }

            if (e.SettingName.Equals("SerialPortBaud") ||
                e.SettingName.Equals("StatusPollInterval") ||
                e.SettingName.Equals("ControllerBufferSize"))
            {
                if (((int)e.NewValue) <= 0)
                    e.Cancel = true;
            }
        }

        private string parseStatus(bool status)
        {
           return status ? "Running" : "Not Running";
        }
    
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        public string Version
        {
            get
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version}";
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    string file = files[0];

                    if (file.EndsWith(".hmap"))
                    {
                        if (machine.Mode == Machine.OperatingMode.Probe)
                            return;

                        //OpenHeightMap(file);
                    }
                    else
                    {
                        if (machine.Mode == Machine.OperatingMode.SendFile)
                            return;

                        try
                        {
                            machine.SetFile(System.IO.File.ReadAllLines(file));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    string file = files[0];

                    if (file.EndsWith(".hmap"))
                    {
                        if (machine.Mode != Machine.OperatingMode.Probe)
                        {
                            e.Effects = DragDropEffects.Copy;
                            return;
                        }
                    }
                    else
                    {
                        if (machine.Mode != Machine.OperatingMode.SendFile)
                        {
                            e.Effects = DragDropEffects.Copy;
                            return;
                        }
                    }
                }
            }

            e.Effects = DragDropEffects.None;
        }

        private void viewport_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                machine.FeedHold();
                e.Handled = true;
            }
        }

        private void ButtonSaveTLOPos_Click(object sender, RoutedEventArgs e)
        {
            if (machine.Mode != Machine.OperatingMode.Manual)
                return;

            double Z = (Properties.Settings.Default.TLSUseActualPos) ? machine.MachinePosition.Z : LastProbePosMachine.Z;

            Properties.Settings.Default.ToolLengthSetterPos = Z;
        }

        private void ButtonApplyTLO_Click(object sender, RoutedEventArgs e)
        {
            if (machine.Mode != Machine.OperatingMode.Manual)
                return;

            double Z = (Properties.Settings.Default.TLSUseActualPos) ? machine.MachinePosition.Z : LastProbePosMachine.Z;

            double delta = Z - Properties.Settings.Default.ToolLengthSetterPos;

            machine.SendLine($"G43.1 Z{delta.ToString(Constants.DecimalOutputFormat)}");
        }

        private void ButtonClearTLO_Click(object sender, RoutedEventArgs e)
        {
            if (machine.Mode != Machine.OperatingMode.Manual)
                return;

            machine.SendLine("G49");
        }

        private void cbSelectAll_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void btnCameraSettingsReload_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => camera.forceSettingsReload());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }

        private void cameraSettingsCB_DropDownOpened(object sender, EventArgs e)
        {
            updateCameraSettingsOptions();
        }

        private void cameraSettingsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Task task = new Task(() => camera.loadCameraSettings());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonStartTimeLapse_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => startTimeLapse());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonStopTimeLapse_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => stopTimeLapse());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonStopCycle_Click(object sender, RoutedEventArgs e)
        {
            StopCycle();
        }
        private void ButtonSaveExperiment_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => SaveSettingsToFile());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonSaveExperimentDefaults_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => SaveDefaults());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonLoadExperimentDefaults_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => LoadDefaults());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonSaveAsExperiment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = openSaveDialog();
            SaveAsSettingsToFile(dialog);
        }
        private void ButtonLoadExperiment_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => LoadSettingsFromFile());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonCameraDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => camera.ShutdownVimba());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ButtonCameraConnect_Click(object sender, RoutedEventArgs e)
        {
            Task task = new Task(() => camera.StartVimba());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }
        private void ListBoxHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void Ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
