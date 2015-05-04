﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;



namespace ObservatoryCenter
{
    public partial class MainForm : Form
    {
        
        public ObservatoryControls ObsControl;

        /// <summary>
        /// Link to preferences form + functions for loading parameters
        /// </summary>
        public SettingsForm SetForm;

        Color OnColor = Color.DarkSeaGreen;
        Color OffColor = Color.Tomato;

        public SocketServerClass SocketServer;

        /// <summary>
        /// For logging window
        /// </summary>
        private bool AutoScrollLogFlag = true;
        private Int32 caretPos = 0;
        public Int32 MAX_LOG_LINES = 500;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            ObsControl = new ObservatoryControls(this);
            SetForm = new SettingsForm(this);
        }

        /// <summary>
        /// Main form load event - startup actions take place here
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            //Connect Devices, which are general adapters (no need to power or control something)
            ObsControl.connectSwitch = true;
            ObsControl.connectDome = true; ;

            //Update visual interface statuses
            UpdateStatusbarASCOMStatus();
            UpdatePowerButtonsStatus();

            //init graphic elements
            ROOF_startPos = rectRoof.Location;
            //Update visual Roof Status
            UpdateRoofPicture();

            //Start tcp server
            SocketServer = new SocketServerClass(this);
            toolStripStatus_Connection.Text = "CONNECTIONS: 0";
            if (true)
            {
                backgroundWorker_SocketServer.RunWorkerAsync();
                toolStripStatus_Connection.ForeColor = Color.Black;
            }
            else
            {
                toolStripStatus_Connection.ForeColor = Color.Gray;
            }
            
            //init vars
            DrawTelescopeV(panelTelescopeV);
        }


        /// <summary>
        /// Main timer tick
        /// </summary>
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            UpdateCCDCameraFieldsStatus();
            UpdateGuiderFieldsStatus();
            UpdatePowerButtonsStatus();
            UpdateStatusbarASCOMStatus();
            UpdateTelescopeStatus();
        }


// Block with update elements
#region ///// Update visual elements (Status bar, telescope, etc) /////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Updates markers in status bar
        /// </summary>
        private void UpdateStatusbarASCOMStatus()
        {
            //SWITCH
            if (ObsControl.objSwitch != null && ObsControl.objSwitch.Connected)
            {
                toolStripStatus_Switch.ForeColor = Color.Blue;
            }
            else
            {
                toolStripStatus_Switch.ForeColor = Color.Gray;
            }

            //DOME
            if (ObsControl.objDome != null && ObsControl.objDome.Connected)
            {
                toolStripStatus_Dome.ForeColor = Color.Blue;
            }
            else
            {
                toolStripStatus_Dome.ForeColor = Color.Gray;
            }

            //TELESCOPE
            bool Tprog=(ObsControl.objTelescope != null && ObsControl.connectTelescope);
            bool Tmaxim = false;
            try
            {
                Tmaxim = (ObsControl.MaximObj.MaximApplicationObj != null && ObsControl.MaximObj.MaximApplicationObj.TelescopeConnected);
            }
            catch { Tmaxim = false; }
            bool Tcdc=false; //later organize checking
            toolStripStatus_Telescope.ToolTipText = "Control center direct connection: " + (Tprog ? "ON" : "off") + Environment.NewLine;
            toolStripStatus_Telescope.ToolTipText += "Maxim telescope connection: " + (Tmaxim ? "ON" : "off") + Environment.NewLine;
            toolStripStatus_Telescope.ToolTipText += "CdC telescope connection: " + (Tcdc ? "ON" : "off") + Environment.NewLine;

            if (Tprog && Tmaxim && Tcdc)
            {
                toolStripStatus_Telescope.ForeColor = Color.Blue;
            }
            else if (Tprog || Tmaxim || Tcdc)
            {
                toolStripStatus_Telescope.ForeColor = Color.MediumOrchid;
            }
            else
            {
                toolStripStatus_Telescope.ForeColor = Color.Gray;
            }

            //FOCUSER
            bool testFocus=false;
            try
            {
                testFocus = (ObsControl.MaximObj.MaximApplicationObj != null && ObsControl.MaximObj.MaximApplicationObj.FocuserConnected);
            }
            catch { testFocus = false; }
            if (testFocus)
            {
                toolStripStatus_Focuser.ForeColor = Color.Blue;
            }
            else
            {
                toolStripStatus_Focuser.ForeColor = Color.Gray;
            }

            //CAMERA
            bool testCamera = false;
            try
            {
                testCamera = (ObsControl.MaximObj.CCDCamera != null && ObsControl.MaximObj.CCDCamera.LinkEnabled);
            }
            catch { testCamera = false; }

            if (testCamera)
            {
                toolStripStatus_Camera.ForeColor = Color.Blue;
            }
            else
            {
                toolStripStatus_Camera.ForeColor = Color.Gray;
            }

        }

        /// <summary>
        /// Updates buttons status
        /// </summary>
        private void UpdatePowerButtonsStatus()
        {
            btnTelescopePower.BackColor = (ObsControl.MountPower ? OnColor : OffColor);
            btnCameraPower.BackColor = (ObsControl.CameraPower ? OnColor : OffColor);
            btnFocuserPower.BackColor = (ObsControl.FocusPower ? OnColor : OffColor);
            btnRoofPower.BackColor = (ObsControl.RoofPower ? OnColor : OffColor);
        }

        /// <summary>
        /// Updates CCD camera status
        /// </summary>
        private void UpdateCCDCameraFieldsStatus()
        {
            bool testCamera = false;
            try
            {
                testCamera = (ObsControl.MaximObj.CCDCamera != null && ObsControl.MaximObj.CCDCamera.LinkEnabled);
            }
            catch { testCamera = false; }

            if (testCamera)
            {
                int bin=ObsControl.MaximObj.CCDCamera.BinX;
                txtCameraBinMode.Text = Convert.ToString(bin) + "x" +Convert.ToString(bin);

                try
                {
                    var st = ObsControl.MaximObj.CCDCamera.FilterNames;
                    txtFilterName.Text = Convert.ToString(st[ObsControl.MaximObj.CCDCamera.Filter]);
                }
                catch
                {
                    txtFilterName.Text = "";
                }
                
                txtCameraTemp.Text = ObsControl.MaximObj.GetCameraTemp().ToString();
                txtCameraSetPoint.Text = ObsControl.MaximObj.CameraSetTemp.ToString();
                txtCameraCoolerPower.Text = ObsControl.MaximObj.GetCoolerPower().ToString();
                txtCameraStatus.Text = ObsControl.MaximObj.GetCameraStatus();

                txtCameraName.BackColor = OnColor;
                txtCameraTemp.BackColor = OnColor;
                txtCameraSetPoint.BackColor = OnColor;
                txtCameraCoolerPower.BackColor = OnColor;
            }
            else
            {
                txtCameraName.BackColor = OffColor;
                txtCameraTemp.BackColor = OffColor;
                txtCameraSetPoint.BackColor = OffColor;
                txtCameraCoolerPower.BackColor = OffColor;
            }
        }


        private void UpdateGuiderFieldsStatus()
        {
            bool testCamera = false;
            try
            {
                testCamera = (ObsControl.MaximObj.CCDCamera != null && ObsControl.MaximObj.CCDCamera.LinkEnabled);
            }
            catch { testCamera = false; }

            if (testCamera)
            {
                btnGuider.Text = (ObsControl.MaximObj.CCDCamera.GuiderRunning ? "Guider running" : "Guider stoped");
                btnGuider.BackColor = (ObsControl.MaximObj.CCDCamera.GuiderRunning ? OnColor : OffColor);
            }
            else
            {
            }
        }


        /// <summary>
        /// Update Telescope Fields and Draw
        /// </summary>
        private void UpdateTelescopeStatus()
        {
            if (ObsControl.objTelescope != null)
            {
                txtTelescopeAz.Enabled = true;
                txtTelescopeAlt.Enabled = true;
                txtTelescopeRA.Enabled = true;
                txtTelescopeDec.Enabled = true;
                btnPark.Enabled = true;
                btnTrack.Enabled = true;

                if (ObsControl.objTelescope.AtPark)
                {
                    //btnPark.Text = "Parked";
                    btnPark.BackColor = OffColor;
                }
                else
                {
                    //btnPark.Text = "UnParked";
                    btnPark.BackColor = OnColor;
                }

                if (ObsControl.objTelescope.Tracking)
                {
                    //btnTrack.Text = "Parked";
                    btnTrack.BackColor = OnColor;
                }
                else
                {
                    //btnTrack.Text = "UnParked";
                    btnTrack.BackColor = OffColor;
                }
                
                //update fields
                
                //txtTelescopeAz.Text = Convert.ToString(Math.Truncate(ObsControl.objTelescope.Azimuth)) + " " + Convert.ToString(Math.Truncatse(ObsControl.objTelescope.Azimuth));
                txtTelescopeAz.Text = ObsControl.ASCOMUtils.DegreesToDMS(ObsControl.objTelescope.Azimuth);
                txtTelescopeAlt.Text = ObsControl.ASCOMUtils.DegreesToDMS(ObsControl.objTelescope.Altitude);
                txtTelescopeRA.Text = ObsControl.ASCOMUtils.HoursToHMS(ObsControl.objTelescope.RightAscension);
                txtTelescopeDec.Text = ObsControl.ASCOMUtils.DegreesToDMS(ObsControl.objTelescope.Declination);

                if (ObsControl.objTelescope.SideOfPier == PierSide.pierEast)
                {
                    txtPierSide.Text = "East, looking West";
                    PoinitingSide = false;
                    VAzAdjust = 180;
                }
                else if (ObsControl.objTelescope.SideOfPier == PierSide.pierWest)
                {
                    txtPierSide.Text = "West, looking East";
                    PoinitingSide = true;
                    VAzAdjust = 0;
                }
                else
                {
                    txtPierSide.Text = "Unknown";
                }

                //Redraw
                angelAz = (Int16)(Math.Round(ObsControl.objTelescope.Azimuth) + NorthAzimuthCorrection + VAzAdjust);
                panelTelescopeV.Invalidate();

                if (PoinitingSide)
                {
                    angelAlt = (Int16)(Math.Round(ObsControl.objTelescope.Altitude+180));
                }
                else
                {
                    angelAlt = (Int16)(Math.Round(ObsControl.objTelescope.Altitude));
                }
                //HTelescope Az corrections
                if (ObsControl.objTelescope.Azimuth < 90 || ObsControl.objTelescope.Azimuth > 270)
                {
                    angelAlt = 180-angelAlt;
                }

                panelTelescopeH.Invalidate();
            }
            else
            {
                txtTelescopeAz.Enabled = false;
                txtTelescopeAlt.Enabled = false;
                txtTelescopeRA.Enabled = false;
                txtTelescopeDec.Enabled = false;

                btnPark.Enabled = false;
                btnTrack.Enabled = false;
            }

            //init variables (will not draw anyway)
            //DrawTelescopeV(panelTelescopeV);
            //DrawTelescopeH(panelTelescopeH);
        }

#endregion update visual elements
// end of block


// Region block with hadnling power management visual interface
#region /// POWER BUTTONS HANDLING ///////////////////////////////////////////////////////////////////////////////////////////////////
        private void btnTelescopePower_Click(object sender, EventArgs e)
        {
            //get current state
            bool SwitchState = (((Button)sender).BackColor == OnColor);
            SwitchState = !SwitchState;

            //toggle
            ObsControl.MountPower=SwitchState;

            //display new status
            ((Button)sender).BackColor = (SwitchState ? OnColor : OffColor);
        }


        private void btnRoofPower_Click(object sender, EventArgs e)
        {
            //get current state
            bool SwitchState = (((Button)sender).BackColor == OnColor);
            SwitchState = !SwitchState;

            //toggle
            ObsControl.RoofPower=SwitchState;

            //display new status
            ((Button)sender).BackColor = (SwitchState ? OnColor : OffColor);


            /////
            txtCameraName.BackColor = (SwitchState ? OnColor : OffColor);
        }

        private void btnCameraPower_Click(object sender, EventArgs e)
        {
            //get current state
            bool SwitchState = (((Button)sender).BackColor == OnColor);
            SwitchState = !SwitchState;

            //toggle
            ObsControl.CameraPower=SwitchState;

            //display new status
            ((Button)sender).BackColor = (SwitchState ? OnColor : OffColor);
        }

        private void btnFocuserPower_Click(object sender, EventArgs e)
        {
            //get current state
            bool SwitchState = (((Button)sender).BackColor == OnColor);
            SwitchState = !SwitchState;

            //toggle
            ObsControl.FocusPower=SwitchState;

            //display new status
            ((Button)sender).BackColor = (SwitchState ? OnColor : OffColor);
        }
#endregion Power button handling
// End of block with power buttons handling

// Block with autorun procedures 
#region /// Autorun procedures ////////////////////////////////////////////////////
        private void btnStartAll_Click(object sender, EventArgs e)
        {
            ObsControl.StartUpObservatory();
        }

        private void btnBeforeImaging_Click(object sender, EventArgs e)
        {
            /// Prepare Imaging run scenario
            //ObsControl.?

        }

        /// <summary>
        /// Run maxim and connect it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMaximStart_Click(object sender, EventArgs e)
        {
            ObsControl.StartMaximDLroutines();
        }
#endregion Autorun procedures
//End of autorun procedures block


        private void btnSettings_Click(object sender, EventArgs e)
        {
            SetForm.ShowDialog();
        }

        /// <summary>
        /// Used to load all prameters during startup
        /// </summary>
        public void LoadParams()
        {
            Logging.AddLog("Loading saved parameters",3);
            try
            {
                ObsControl.MaximDLPath = Properties.Settings.Default.MaximDLPath;
                ObsControl.CCDAPPath = Properties.Settings.Default.CCDAPPath;
                ObsControl.PlanetariumPath = Properties.Settings.Default.CartesPath;

                ObsControl.POWER_MOUNT_PORT = Convert.ToByte(Properties.Settings.Default.SwitchMountPort);
                ObsControl.POWER_CAMERA_PORT = Convert.ToByte(Properties.Settings.Default.SwitchCameraPort);
                ObsControl.POWER_FOCUSER_PORT = Convert.ToByte(Properties.Settings.Default.SwitchFocuserPort);
                ObsControl.POWER_ROOFPOWER_PORT = Convert.ToByte(Properties.Settings.Default.SwitchRoofPowerPort);
                ObsControl.POWER_ROOFSWITCH_PORT = Convert.ToByte(Properties.Settings.Default.SwitchRoofSwitchPort);

                RoofDuration = Convert.ToInt16(Properties.Settings.Default.RoofDuration);
                RoofDurationCount = Convert.ToInt16(Properties.Settings.Default.RoofDurationMeasurementsCount);
            }
            catch (Exception ex)
            {
                StackTrace st = new StackTrace(ex, true);
                StackFrame[] frames = st.GetFrames();
                string messstr = "";

                // Iterate over the frames extracting the information you need
                foreach (StackFrame frame in frames)
                {
                    messstr += String.Format("{0}:{1}({2},{3})", frame.GetFileName(), frame.GetMethod().Name, frame.GetFileLineNumber(), frame.GetFileColumnNumber());
                }

                string FullMessage = "Error loading params. ";
                FullMessage += "IOException source: " + ex.Data + " | " + ex.Message + " | " + messstr;

                Logging.AddLog(FullMessage,1,Highlight.Error);
            }
            Logging.AddLog("Loading saved parameters end", 3);


        }


        /// <summary>
        /// Thread for socket server
        /// </summary>
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            SocketServer.ListenSocket();
        }

        /// <summary>
        /// Timer to work with log information (save it, display, etc)
        /// </summary>
        private void logRefreshTimer_Tick(object sender, EventArgs e)
        {
            string LogAppend = Logging.DumpToString(1);
            txtLog.AppendText(LogAppend);

            Logging.DumpToFile(Logging.DEBUG_LEVEL);
        }
        

#region //// Telescope routines //////////////////////////////////////

        private void btnConnectTelescope_Click(object sender, EventArgs e)
        {
            if (btnConnectTelescope.Text == "Connect")
            {
                ObsControl.connectTelescope = true;
                btnConnectTelescope.Text = "Diconnect";
                btnConnectTelescope.BackColor = OnColor;
            }
            else
            {
                ObsControl.connectTelescope = false;
                btnConnectTelescope.Text = "Connect";
                btnConnectTelescope.BackColor = OffColor;
                btnTrack.BackColor = SystemColors.Control;
                btnPark.BackColor = SystemColors.Control;
            }
        }

#endregion Telescope routines




    }
}
