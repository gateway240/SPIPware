﻿using SynchronousGrab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace SPIPware
{
    partial class MainWindow
    {
        //private static VimbaHelper m_VimbaHelper = null;
       

       
        private void updateCameraSettingsOptions()
        {
            cameraSettingsCB.Items.Clear();

            DirectoryInfo d = new DirectoryInfo("./Resources/CameraSettings/");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.xml"); //Getting Text files
            //string str = "";
            foreach (FileInfo file in Files)
            {
                cameraSettingsCB.Items.Add(file);
            }


            //if (!cameraSettingsCB.Items.Contains(Properties.Settings.Default.CameraSettingsPath))
            //{
            //    cameraSettingsCB.SelectedItem = 0;
            //    cameraSettingsCB.SelectedIndex = 0;
            //}


        }
        private void cameraStatusUpdated(object sender, DataTransferEventArgs e)
        {
            TextBlock text = (TextBlock)sender;
            updateCameraStatus(text.Text);
        }
        private void disableCameraControlButtons()
        {
            btnCapture.IsEnabled = false;
            //btnCameraSettingsReload.IsEnabled = false;

        }
        private void enableCameraControlButtons()
        {
            btnCapture.IsEnabled = true;
            //btnCameraSettingsReload.IsEnabled = false;
        }
        public void updateCameraStatus(string cameraStatus)
        {
            if (string.Equals("Not Ready", cameraStatus))
            {
                cameraStatusIcon.Source = RED_IMAGE;
            }
            else
            {
                cameraStatusIcon.Source = GREEN_IMAGE;
            }
        }
        private void cbCameraOpen(object sender, EventArgs e)
        {

        }
    }
}
