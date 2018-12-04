﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static SPIPware.Communication.PeripheralControl;

namespace SPIPware.Communication
{
    class CycleControl
    {
        public delegate void CycleUpdate();
        public event CycleUpdate StatusUpdate;

        CameraControl camera = CameraControl.Instance;
        Machine machine = Machine.Instance;
        PeripheralControl peripheral = Instance;
        private List<int> imagePositions = new List<int>();
        public bool runningCycle = false;
        public bool firstRun = true;
        private decimal targetLocation = 0;
        bool foundPlate = false;
        private bool cycleStatus = false;

        public List<int> ImagePositions { get => imagePositions; set => imagePositions = value; }
        public bool CycleStatus { get => cycleStatus; set => cycleStatus = value; }

        private int posIndex = 0;

        private bool IsNextIndex(int index)
        {
            return index <= imagePositions.Count;
        }
        public void UpdatePositionList(List<CheckBox> checkBoxes)
        {
            ImagePositions = new List<int>();
            for (var i = 0; i < checkBoxes.Count; i++)
            {
                if (checkBoxes[i].IsChecked == true)
                {
                    ImagePositions.Add(i);
                }
            }
            //ImagePositions.ForEach((position) => Console.Write(position + ","));
            //Console.WriteLine();
        }
        public void Start()
        {
            //if (machine.Connected && peripheral.Connected)
            {
                if (runningCycle) Stop();


                runningCycle = true;
                CycleStatus = runningCycle;
                StatusUpdate.Raise(this, EventArgs.Empty);

                camera.loadCameraSettings();
                posIndex = 0;
                //currentIndex = 0;
                
                //bool isPlateFound = FindCheckedBox(currentIndex, true);
                if (!ImagePositions.Any()) return;

                if (firstRun)
                {
                    machine.SendLine("$H");
                    peripheral.SetLight(Peripheral.GrowLight, false, false);
                    //Properties.Settings.Default.CurrentPlate = 1;
                    firstRun = false;
                }
                else
                {
                    peripheral.SetLight(Peripheral.Backlight, true);
                    peripheral.SetLight(Peripheral.GrowLight, false, false);
                    //Thread.Sleep(300);
                    targetLocation = machine.sendMotionCommand(imagePositions[posIndex]);
                }
            }
            //else
            //{
            //    Machine_NonFatalException("Machine not connected");
            //}


        }
        public void End()
        {
            runningCycle = false;
            CycleStatus = runningCycle;
            StatusUpdate.Raise(this, EventArgs.Empty);

            //currentIndex = 0;
            posIndex = 0;
            //machine.sendMotionCommand(0);
            machine.SendLine("$H");
            peripheral.SetLight(Peripheral.Backlight, false);
            peripheral.SetLight(Peripheral.GrowLight, true, !peripheral.IsNightTime());
        }
        public void Stop()
        {
            End();
            if (machine.Connected)
            {
                machine.SoftReset();
            }
        }

        public void Check()
        {

            if (runningCycle)
            {
                if (machine.Status == "Home")
                {
                    //cameraControl.home = true;
                    //cameraControl.firstRun = false;
                    peripheral.SetLight(Peripheral.Backlight, true);
                    //Thread.Sleep(300);
                    targetLocation = machine.sendMotionCommand(imagePositions[posIndex]);
                }

                else if (machine.WorkPosition.X == (double)targetLocation && machine.Status == "Idle")
                {
                    //Console.WriteLine("Current Index" + currentIndex);
                    peripheral.SetLight(Peripheral.Backlight, true);
                    camera.CapSaveImage();
                    if (Properties.Settings.Default.CurrentPlate < Properties.Settings.Default.TotalPlates)
                    {
                        Properties.Settings.Default.CurrentPlate++;
                    }
                    else
                    {
                        Properties.Settings.Default.CurrentPlate = 1;
                    }

                    //Console.WriteLine("Found Plate: " + foundPlate);
                    //foundPlate = FindCheckedBox(currentIndex, false);
                    foundPlate = IsNextIndex(posIndex);
                    if (foundPlate)
                    {
                        posIndex++;
                        targetLocation = machine.sendMotionCommand(imagePositions[posIndex]);
                    }
                    else End();

                }

            }
            else
            {
                return;
            }
        }
    }
}
