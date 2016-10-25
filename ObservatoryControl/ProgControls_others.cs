﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ObservatoryCenter
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Cartes du Ciel class
    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Cartes Du Ciel class
    /// </summary>
    public class CdC_ExternatApplication : ExternalApplication
    {
        public Int32 ServerPort = 3292; //port to connect socket server

        public CdC_ExternatApplication() : base()
        { }

        public string ConnectTelescope()
        {
            string output = SocketServerClass.ConnectToServerAndSendMessage(IPAddress.Parse("127.0.0.1"), ServerPort, "CONNECTTELESCOPE\r\n", out Error);
            ErrorSt = output;
            //Error = 0;
            if (Error < 0)
            {
                Logging.AddLog("Telescope connection error in CdC", LogLevel.Important, Highlight.Error);
            }
            else
            {
                Logging.AddLog("Telescope connected in CdC", LogLevel.Activity);
            }
            return output;
        }
    }




    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // PHD Broker class
    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// PHD Broker class
    /// </summary>
    public class PHDBroker_ExternatApplication : ExternalApplication
    {
        public PHDBroker_ExternatApplication() : base()
        { }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // CCDAP class
    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// CCDAP class
    /// </summary>
    public class CCDAP_ExternatApplication : ExternalApplication
    {
        public CCDAP_ExternatApplication() : base()
        { }
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Focusmax class
    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Focusmax class
    /// </summary>
    public class FocusMax_ExternatApplication : ExternalApplication
    {
        public FocusMax_ExternatApplication() : base()
        { }
    }

}