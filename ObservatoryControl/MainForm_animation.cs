﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using CPI.Plot3D;

namespace ObservatoryCenter
{
    

    /// <summary>
    ///  ANIMATION PART OF MAIN FORM
    ///  Contains variables, constants, methods
    ///  and even button handlers
    ///  i.e. all that have connection with roof animation and control
    /// </summary>
    public partial class MainForm
    {

#region Roof settings
        public double RoofDuration=30; //sec. Actual value is read from settings
        public int RoofDurationCount = 1; //Actual value is read from settings
        internal int RoofDurationInTicks; // roof duration recalculated to tickss

        //Main animation constants
        public double waitTicksBeforeCheckTolerance = 0.20; //10% - used to calculate  waitTicksBeforeCheck = RoofDuration*(1 - waitTicksBeforeCheckTolerance)
        public double maxAnimationCountsTolerance = 0.10; //10% - used to calculate    maxAnimationCounts   = RoofDuration*(1 + axAnimationCountsTolerance)

        private Point ROOF_startPos;    //roof starting coordinate, would be auto initialized
        public int ROOF_endPos = 80;   //roof ending X coordinate
        
        private double ROOF_incPos_module = 1.0; //increament speed by modulus (pixels per tick). Autocalculated
        private double ROOF_incPos; //ROOF_incPos_module with sign ("+" open, "-" close)
        private double curXCoord=0;

        private int tickCount = 0;
        private int waitTicksBeforeCheck; //would be auto set during startAnimation procudre
        private int maxAnimationCounts; //would be auto set during startAnimation procudre

        private string prev_direction = "";

#endregion roof settings

#region ////// ROOF DRAWING /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Init Roof Opening routine
        /// </summary>
        private void btnOpenRoof_Click(object sender, EventArgs e)
        {
            //calc ticks
            RoofDurationInTicks= (int)Math.Round(RoofDuration * (1000.0 / animateRoofTimer.Interval));
            waitTicksBeforeCheck = (int)Math.Floor(RoofDuration * (1000.0 / animateRoofTimer.Interval) * (1 - waitTicksBeforeCheckTolerance));
            maxAnimationCounts = (int)Math.Ceiling(RoofDuration * (1000.0 / animateRoofTimer.Interval) * (1 + maxAnimationCountsTolerance));
            ROOF_incPos_module = ROOF_endPos / (RoofDuration * (1000.0 / animateRoofTimer.Interval));

            rectRoof.Left = ROOF_startPos.X;
            ROOF_incPos = ROOF_incPos_module;
            curXCoord = rectRoof.Left;

            prev_direction = "open";

            bool RFflag=ObsControl.RoofOpen();  //start OPENING. true if was started
            if (RFflag) startAnimation();       //start animation
        }

        /// <summary>
        /// Init close roof routine
        /// </summary>
        private void btnCloseRoof_Click(object sender, EventArgs e)
        {
            //calc ticks
            waitTicksBeforeCheck = (int)Math.Floor(RoofDuration * (1000.0 / animateRoofTimer.Interval) * (1 - waitTicksBeforeCheckTolerance));
            maxAnimationCounts = (int)Math.Ceiling(RoofDuration * (1000.0 / animateRoofTimer.Interval) * (1 + maxAnimationCountsTolerance));
            ROOF_incPos_module = ROOF_endPos / (RoofDuration * (1000.0 / animateRoofTimer.Interval));

            rectRoof.Left = ROOF_startPos.X + ROOF_endPos;
            ROOF_incPos = -ROOF_incPos_module;
            curXCoord = rectRoof.Left;

            prev_direction = "close";

            bool RFflag=ObsControl.RoofClose();
            if (RFflag) startAnimation();
        }

        /// <summary>
        /// Stop roof. As for now - stops only animation
        /// </summary>
        private void btnStopRoof_Click(object sender, EventArgs e)
        {
            //
            drawStoped();
            stopAnimation();
        }

        /// <summary>
        /// Procedure to start animation
        /// </summary>
        private void startAnimation()
        {
            btnCloseRoof.Enabled = false;
            btnOpenRoof.Enabled = false;
            btnStopRoof.Enabled = true;

            animateRoofTimer.Enabled = true;

            rectRoof.BackColor = Color.WhiteSmoke;
            rectBase.BackColor = Color.WhiteSmoke;
        }

        /// <summary>
        /// Procedure to stop animation
        /// </summary>
        private void stopAnimation()
        {
            animateRoofTimer.Enabled = false;

            rectRoof.BackColor = Color.LightSeaGreen;
            rectBase.BackColor = Color.Turquoise;

            tickCount = 0;
        }

        /// <summary>
        /// Roof opening/closing main tick
        /// </summary>
        private void animateRoof_Tick(object sender, EventArgs e)
        {
            tickCount++;
            curXCoord += ROOF_incPos;
            rectRoof.Location = new Point((int)Math.Round(curXCoord), rectRoof.Top);
                
            //Was enough time passed to start cheking actual roof status?
            if (tickCount > waitTicksBeforeCheck)
            {
                //Roof was opened/closed?
                if (((ROOF_incPos > 0) && ObsControl.objDome.ShutterStatus != ShutterState.shutterOpen) ||
                    ((ROOF_incPos < 0) && ObsControl.objDome.ShutterStatus != ShutterState.shutterClosed))

                {
                //Not yet

                    //check - if this is too long?
                    if (tickCount < RoofDurationInTicks)
                    {
                        //normal duration wasn't reached, do nothing - continue animation
                    }
                    else if (tickCount < maxAnimationCounts)
                    {
                        //restart animation
                        if (ROOF_incPos > 0){
                            btnOpenRoof_Click(null, null);
                        }else{
                            btnCloseRoof_Click(null, null);
                        }
                    }
                    else
                    {
                        //signal error
                        drawStoped();
                        if (ROOF_incPos > 0){
                            AlarmRoofMoving("open");
                        }else{
                            AlarmRoofMoving("clos");
                        }
                    }
                }
                else
                {
                //finished - closed/opened
                    //calc and save statistic
                    TimeSpan SinceStartPassed = DateTime.Now.Subtract(ObsControl.RoofRoutine_StartTime);
                    ObsControl.curRoofRoutineDuration_Seconds = (int)Math.Round(SinceStartPassed.TotalSeconds, 0);
                    RoofDuration = (RoofDuration * RoofDurationCount + ObsControl.curRoofRoutineDuration_Seconds) / (RoofDurationCount+1); //calc new roof duration
                    Properties.Settings.Default.RoofDuration = Convert.ToDecimal(RoofDuration);
                    Properties.Settings.Default.RoofDurationMeasurementsCount = RoofDurationCount+1;
                    Properties.Settings.Default.Save();

                    if (ROOF_incPos > 0)
                    {
                        drawOpened();
                        Logging.AddLog("Roof was opened in " + ObsControl.curRoofRoutineDuration_Seconds + " sec");
                    }
                    else
                    {
                        drawClosed();
                        Logging.AddLog("Roof was closed in " + ObsControl.curRoofRoutineDuration_Seconds + " sec");
                    }
                    stopAnimation();
                }
                
            }
            /*else
            {
                //close animation
                if (rectRoof.Left < ROOF_startPos.X + ROOF_incPos_module * 2)
                {
                    rectRoof.Left = ROOF_startPos.X;
                    stopAnimation();
                } 
            } */
        }

        private void drawOpened()
        {
            rectRoof.Left = ROOF_startPos.X + ROOF_endPos;
            btnCloseRoof.Enabled = true;
            btnOpenRoof.Enabled = false;
            btnStopRoof.Enabled = false;
        }
        private void drawClosed()
        {
            rectRoof.Left = ROOF_startPos.X;
            btnCloseRoof.Enabled = false;
            btnOpenRoof.Enabled = true;
            btnStopRoof.Enabled = false;
        }
        private void drawStoped()
        {
            ShutterState curShutState = ObsControl.objDome.ShutterStatus;
            if ((curShutState == ShutterState.shutterOpening) || (curShutState == ShutterState.shutterClosing))
            {
                rectRoof.Left = ROOF_startPos.X + Convert.ToInt16(Math.Round((double)ROOF_endPos / 2));
                if (prev_direction == "open")
                {
                    btnCloseRoof.Enabled = true;
                    btnOpenRoof.Enabled = false;
                    btnStopRoof.Enabled = false;
                }
                else if (prev_direction == "close")
                {
                    btnCloseRoof.Enabled = false;
                    btnOpenRoof.Enabled = true;
                    btnStopRoof.Enabled = false;
                }
            }
            else if (curShutState == ShutterState.shutterOpen)
            {
                drawOpened();
            }
            else if (curShutState == ShutterState.shutterClosed)
            {
                drawClosed();
            }
        }



        private void AlarmRoofMoving(string MoveType)
        {
            animateRoofTimer.Enabled = false;
            System.Windows.Forms.MessageBox.Show("Roof " + (MoveType) + "ing is too long!");
        }


        /// <summary>
        /// Draw roof position whenever needed
        /// as for now called only once - form_load
        /// </summary>
        private void UpdateRoofPicture()
        {
            //Draw roof status
            if (ObsControl.objDome.Connected)
            {
                if (ObsControl.objDome.ShutterStatus == ShutterState.shutterClosed)
                {
                    drawClosed();
                }
                else if (ObsControl.objDome.ShutterStatus == ShutterState.shutterOpen)
                {
                    drawOpened();
                }
                rectRoof.BackColor = Color.LightSeaGreen;
                rectBase.BackColor = Color.Turquoise;
            }
            else
            {
                drawClosed();
                rectRoof.BackColor = Color.WhiteSmoke;
                rectBase.BackColor = Color.WhiteSmoke;
                btnOpenRoof.Enabled = false;
            }
        }
#endregion draw roof

#region ////// DRAW TELESCOPE /////////////////////||||||||||||||||||||///////////////////////////////////////////////////////////

        Int16 angelAz = 0;
        int angelAlt = -45;
        double angelAlt_raw = -45.0;

        bool PoinitingSide = true; //true - direct (west side), false - rear (east side)
        Int16 VAzAdjust = 0; //adjust for pierflip (vertical)

        public Int16 NorthAzimuthCorrection = 0;
        Rectangle TelescopeVertical, TelescopeVertical2, MountRect;
        Rectangle TelescopeHoriz, TelescopeHoriz2;
        Rectangle ObjectiveRect, ObjectiveRect_back, EyepieceRect;

        Int16 TelW, Tel2W = 0;
        Int16 TelH, Tel2H = 0;
        Int16 TelH_proj=0, Tel2H_proj=0;
        Int16 PierVSize = 0;
        Int16 EllipseR, EllipseR2 = 0;

        Point TelescopeVertical_startPos = new Point(50, 50);
        Point TelescopeHoriz_startPos = new Point(50, 50);
        Point PierV_startPos = new Point(50, 50);
        //////////////////////////////////////////////////////////////////////////////////////////////////        
        public float LatitudeGrad = 56.0F;

        int PARAM_DecAxix_Len = 50;
        int PARAM_DecAxix_Thick = 10; //unused in 3d

        int PARAM_RAAxix_Len = 50;
        int PARAM_RAAxix_Thick = 4;

        int PARAM_Telescope_Thick = 30;
        int PARAM_Telescope_Len = 100;

        public CPI.Plot3D.Point3D CameraPosition;
        public CPI.Plot3D.Point3D StartDrawingPosition;

        public float DECGrad, DECRad;
        public float HAGrad, HARad;

        public int X0;
        public int Y0;
        //////////////////////////////////////////////////////////////////////////////////////////////////        
        private void panelTelescopeV_Paint(object sender, PaintEventArgs e)
        {
            CalculateTelescopeData();
            DrawTelescopeV((Panel)sender);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////        
        private void CalculateTelescopeData()
        {
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


            if (PoinitingSide)
            {
                angelAlt = (Int16)(Math.Round(ObsControl.objTelescope.Altitude + 180));
            }
            else
            {
                angelAlt = (Int16)(Math.Round(ObsControl.objTelescope.Altitude));
            }
            //HTelescope Az corrections
            if (ObsControl.objTelescope.Azimuth < 90 || ObsControl.objTelescope.Azimuth > 270)
            {
                angelAlt = 180 - angelAlt;
            }
            angelAlt_raw = ObsControl.objTelescope.Altitude;



            /*X0 = (int)(panel1.Width / 2);
            Y0 = (int)(panel1.Height / 2);

            DECGrad = (float)udDec.Value;
            HAGrad = (float)((double)udHA_hours.Value * (360.0 / 24.0) + (double)udHA_mins.Value / 60.0);

            TelescopeBack_X_Skew = (float)((double)udSkewX.Value / 180.0 * Math.PI);

            //Camera positon
            CameraPosition = new CPI.Plot3D.Point3D(Convert.ToInt32(txtCameraPos_X.Text), Convert.ToInt32(txtCameraPos_Y.Text), Convert.ToInt32(txtCameraPos_Z.Text));
            //Start postion
            StartDrawingPosition = new CPI.Plot3D.Point3D(X0, Y0, 0);
            */
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Draw3DTelescope(PaintEventArgs e)
        {

            using (Graphics graphicsObj = e.Graphics)
            //using (Graphics graphicsObj = panel1.CreateGraphics())
            {
                graphicsObj.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                //Create graph objects
                SolidBrush Brush1 = new SolidBrush(Color.LightGray); //закраска
                SolidBrush RedBrush = new SolidBrush(Color.Red); //закраска

                Pen grayPen = new Pen(Color.LightGray, 1); //цвет линии
                Pen blackPen = new Pen(Color.Black, 3); //цвет линии

                //1. Сместить и повернуть холст
                //1.1. Сместить на центр, чтобы вращение было по точке соприкосновения с телескопом
                graphicsObj.TranslateTransform(X0, Y0);
                //1.2. Повернуть на широту
                graphicsObj.RotateTransform(LatitudeGrad);
                //1.3. Вернуть начало координат в начало
                graphicsObj.TranslateTransform(-X0, -Y0);

                //Обозначим цетр
                graphicsObj.FillEllipse(RedBrush, X0 - 2, Y0 - 2, 4, 4);

                //1. Ось RA
                Rectangle recRAAxis_el1 = new Rectangle((int)(X0 - PARAM_RAAxix_Thick / 2), (int)(Y0), (int)(PARAM_RAAxix_Thick), (int)(PARAM_RAAxix_Len));
                graphicsObj.DrawRectangle(Pens.Black, recRAAxis_el1);


                using (CPI.Plot3D.Plotter3D p = new CPI.Plot3D.Plotter3D(graphicsObj, new Pen(Color.Black, 1), CameraPosition))
                {
                    //System.Threading.Thread.Sleep(50);
                    //g.Clear(this.BackColor);

                    //Camera positon
                    p.Location = StartDrawingPosition;

                    //2. Ось DEC
                    p.PenUp();
                    p.TurnLeft(90);
                    p.TurnDown(90);

                    p.TurnRight(HAGrad); //Rotate Hour Angle

                    p.Forward(PARAM_DecAxix_Len / 2);
                    p.TurnRight(180);
                    p.PenDown();
                    p.Forward(PARAM_DecAxix_Len); //Dec axis

                    //3. Телескоп
                    p.PenUp();
                    p.TurnDown(90);
                    p.TurnRight(90);

                    p.TurnRight(DECGrad); //Rotate DEC

                    // Move to telescope start
                    p.Forward(PARAM_Telescope_Len / 2);
                    p.TurnUp(90);
                    p.Forward(PARAM_Telescope_Thick);
                    p.TurnRight(90);
                    p.Forward(PARAM_Telescope_Thick / 2);
                    p.TurnRight(90);
                    p.TurnUp(90);
                    p.PenDown();

                    Draw3DCube(p, PARAM_Telescope_Len, PARAM_Telescope_Thick, PARAM_Telescope_Thick);

                    //4. Телексоп front
                    p.TurnDown(90);
                    Draw3DRect(p, PARAM_Telescope_Thick, PARAM_Telescope_Thick, new Pen(Color.Blue));

                    //5. Телексоп строна к монтировке
                    p.PenUp();
                    p.Forward(PARAM_Telescope_Thick);
                    p.TurnUp(90);
                    p.PenDown();
                    Draw3DRect(p, PARAM_Telescope_Len, PARAM_Telescope_Thick, new Pen(Color.Silver));
                }
            }
        }

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        // 3D
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        public void Draw3DRect(Plotter3D p, float sideWidth, float sideHeight)
        {
            p.Forward(sideWidth);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideHeight);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideWidth);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideHeight);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees
        }

        public void Draw3DRect(Plotter3D p, float sideWidth, float sideHeight, Pen PenColor)
        {
            p.Forward(sideWidth, PenColor);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideHeight, PenColor);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideWidth, PenColor);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees

            p.Forward(sideHeight, PenColor);  // Draw a line sideLength long
            p.TurnRight(90);        // Turn right 90 degrees
        }


        public void Draw3DCube(Plotter3D p, float XWidth, float YHeight, float ZThick)
        {
            /*            for (int i = 0; i < 4; i++)
                        {
                            //DrawSquare(p, sideLength);
                            DrawRect(p, sideLength+10, sideLength-10);
                            p.Forward(sideLength);
                            p.TurnDown(90);
                        }
            */
            Draw3DRect(p, XWidth, YHeight);

            p.Forward(XWidth);

            p.TurnDown(90);
            Draw3DRect(p, ZThick, YHeight);

            p.Forward(ZThick);
            p.TurnDown(90);
            Draw3DRect(p, XWidth, YHeight);

            p.Forward(XWidth);
            p.TurnDown(90);
            Draw3DRect(p, ZThick, YHeight);

            //and go to initial position
            p.PenUp();
            p.Forward(ZThick);  // Draw a line sideLength long
            p.TurnDown(90);
            p.PenDown();

        }
        //////////////////////////////////////////////////////////////////////

        private void DrawTelescopeV(Panel pannelV)
        {
            //init calculate size parameters
            TelW=(Int16)Math.Round(pannelV.Height*0.25);
            Tel2W = (Int16)Math.Round(TelW * 0.6);
            TelH=(Int16)Math.Round(pannelV.Width/2 * 0.9);
            Tel2H = (Int16)Math.Round(TelH * 0.15);

            PierVSize = (Int16)(TelW / 2);
            
            //3d projection
            EllipseR = (Int16)Math.Round(TelW * Math.Sin(angelAlt_raw * Math.PI / 180));
            EllipseR2 = (Int16)Math.Round(Tel2W * Math.Sin(angelAlt_raw * Math.PI / 180));
            TelH_proj = (Int16)Math.Round(TelH * Math.Cos(angelAlt_raw * Math.PI / 180));
            Tel2H_proj = (Int16)Math.Round(Tel2H * Math.Cos(angelAlt_raw * Math.PI / 180));

            //calculate pos parameters
            PierV_startPos.X = (pannelV.Width - PierVSize) / 2;
            PierV_startPos.Y = (pannelV.Height - PierVSize) / 2 - 2;

            TelescopeVertical_startPos.X = (pannelV.Width - TelH_proj) / 2;
            TelescopeVertical_startPos.Y = (pannelV.Height) / 2 + PierVSize / 2;

            //create rectangles
            MountRect = new Rectangle(PierV_startPos.X, PierV_startPos.Y, PierVSize, PierVSize);
            if (PoinitingSide)
            {
                TelescopeVertical = new Rectangle(TelescopeVertical_startPos.X + Tel2H_proj, TelescopeVertical_startPos.Y, TelH_proj - Tel2H_proj, TelW);
                TelescopeVertical2 = new Rectangle((TelescopeVertical_startPos.X), TelescopeVertical_startPos.Y + (TelW - Tel2W) / 2, Tel2H_proj, Tel2W);
                ObjectiveRect = new Rectangle(TelescopeVertical_startPos.X + TelH_proj - EllipseR / 2, TelescopeVertical_startPos.Y, EllipseR, TelW);
                ObjectiveRect_back = new Rectangle(TelescopeVertical_startPos.X + Tel2H_proj - EllipseR / 2, TelescopeVertical_startPos.Y, EllipseR, TelW);
                EyepieceRect = new Rectangle(TelescopeVertical_startPos.X - EllipseR2 / 2, TelescopeVertical_startPos.Y + (TelW - Tel2W) / 2, EllipseR2, Tel2W);
            }
            else
            {
                TelescopeVertical = new Rectangle(TelescopeVertical_startPos.X, TelescopeVertical_startPos.Y, TelH_proj - Tel2H_proj, TelW);
                TelescopeVertical2 = new Rectangle((TelescopeVertical_startPos.X + TelH_proj - Tel2H_proj), TelescopeVertical_startPos.Y + (TelW - Tel2W) / 2, Tel2H_proj, Tel2W);
                ObjectiveRect = new Rectangle(TelescopeVertical_startPos.X - EllipseR / 2, TelescopeVertical_startPos.Y, EllipseR, TelW);
                ObjectiveRect_back = new Rectangle(TelescopeVertical_startPos.X + TelH_proj - Tel2H_proj - EllipseR / 2, TelescopeVertical_startPos.Y, EllipseR, TelW);
                EyepieceRect = new Rectangle(TelescopeVertical_startPos.X + TelH_proj - EllipseR2 / 2, TelescopeVertical_startPos.Y + (TelW - Tel2W) / 2, EllipseR2, Tel2W);
            }

            //graph objects
            Graphics graphicsObj = pannelV.CreateGraphics();
            graphicsObj.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            Pen Pen1 = new Pen(Color.Blue, 1);
            Pen Pen2 = new Pen(Color.Gray, 1);
            SolidBrush Brush1 = new SolidBrush(Color.LightGray);
            SolidBrush BrushO = new SolidBrush(Color.White);
            SolidBrush Brush2 = new SolidBrush(Color.Gray);

            //the central point of the rotation
            graphicsObj.TranslateTransform(pannelV.Width / 2, pannelV.Height/2);
            //rotation procedure
            graphicsObj.RotateTransform(angelAz);
            //return transformation to start
            graphicsObj.TranslateTransform(-(pannelV.Width / 2), -(pannelV.Height / 2));

            //draw rectangles
            graphicsObj.FillEllipse(Brush2, EyepieceRect);
            graphicsObj.FillRectangle(Brush2, TelescopeVertical2);

            graphicsObj.FillEllipse(Brush1, ObjectiveRect_back);
            graphicsObj.FillRectangle(Brush1, TelescopeVertical);
            graphicsObj.FillEllipse(BrushO, ObjectiveRect);

            graphicsObj.DrawEllipse(Pen2, MountRect);
        }

        private void DrawTelescopeH(Panel pannelH)
        {
            
            //calculate pos parameters
            TelescopeHoriz_startPos.X = (pannelH.Width - TelH) / 2;
            TelescopeHoriz_startPos.Y = (pannelH.Height - TelW) / 2;

            //create rectangles
            if (PoinitingSide)
            {
                TelescopeHoriz = new Rectangle(TelescopeHoriz_startPos.X + Tel2H, TelescopeHoriz_startPos.Y, TelH - Tel2H, TelW);
                TelescopeHoriz2 = new Rectangle((TelescopeHoriz_startPos.X), TelescopeHoriz_startPos.Y + (TelW - Tel2W) / 2, Tel2H, Tel2W);
            }
            else
            {
                TelescopeHoriz = new Rectangle(TelescopeHoriz_startPos.X, TelescopeHoriz_startPos.Y, TelH - Tel2H, TelW);
                TelescopeHoriz2 = new Rectangle((TelescopeHoriz_startPos.X + TelH - Tel2H), TelescopeHoriz_startPos.Y + (TelW - Tel2W) / 2, Tel2H, Tel2W);
            }

            //graph objects
            Graphics graphicsObj = pannelH.CreateGraphics();
            Pen myPen = new Pen(Color.Red, 1);
            graphicsObj.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //the central point of the rotation
            graphicsObj.TranslateTransform(pannelH.Width / 2, pannelH.Height / 2);
            //rotation procedure
            graphicsObj.RotateTransform(angelAlt);
            //return transformation to start
            graphicsObj.TranslateTransform(-(pannelH.Width / 2), -(pannelH.Height / 2));

            //draw rectangles
            graphicsObj.DrawRectangle(myPen, TelescopeHoriz);
            graphicsObj.DrawRectangle(myPen, TelescopeHoriz2);
        }




#endregion draw telescope

    }
}
