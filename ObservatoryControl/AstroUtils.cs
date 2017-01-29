﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASCOM.Utilities;
using ASCOM.Astrometry;
using ASCOM;

namespace ObservatoryCenter
{
    public class AstroUtils
    {
        static ASCOM.Utilities.Util ASCOMUtils;

        static double longitude = 38.7133333333333;


        static AstroUtils()
        {
            ASCOMUtils = new Util();
        }

        static DateTime GetUTCTime()
        {
            return DateTime.UtcNow;
        }

        static public double GetJD()
        {
            return ASCOMUtils.JulianDate;
        }


        //Calculates Greenwich Apparent Sidereal time
        static public double NowLAST()
        {
            var nov = new ASCOM.Astrometry.NOVAS.NOVAS31();
            var ast = new ASCOM.Astrometry.AstroUtils.AstroUtils();

            var currJD = ast.JulianDateUT1(0);

            double lstNow = 0;
            var res = nov.SiderealTime(
                currJD, 0d, 0, GstType.GreenwichApparentSiderealTime, Method.EquinoxBased, Accuracy.Full, ref lstNow);

            if (res != 0) throw new InvalidValueException("Error getting Greenwich Apparent Sidereal time");

            return (lstNow + longitude / 15);
        }

        //Calculates Greenwich Mean Sidereal time
        static public double NowLMST()
        {
            var nov = new ASCOM.Astrometry.NOVAS.NOVAS31();
            var ast = new ASCOM.Astrometry.AstroUtils.AstroUtils();

            var currJD = ast.JulianDateUT1(0);

            double lstNow = 0;
            var res = nov.SiderealTime(
                currJD, 0d, 0, GstType.GreenwichMeanSiderealTime, Method.EquinoxBased, Accuracy.Full, ref lstNow);

            if (res != 0) throw new InvalidValueException("Error getting Greenwich Mean Sidereal time");

            return (lstNow + longitude / 15);
        }

        static public string GetSideralTimeSt()
        {
            double stm = NowLMST();
            int h = (int) Math.Truncate(stm);
            int m = (int)Math.Truncate((stm - h) * 60);
            int s = (int)Math.Truncate((stm - h - m/60) * 3600 );

            return h + ":" + m + ":" + s;
        }

        static public double GetSideralTime()
        {

            DateTime dateTime = DateTime.Now;

            double julianDate = 367 * dateTime.Year -
                (int)((7.0 / 4.0) * (dateTime.Year +
                (int)((dateTime.Month + 9.0) / 12.0))) +
                (int)((275.0 * dateTime.Month) / 9.0) +
                dateTime.Day - 730531.5; ;

            double julianCenturies = julianDate / 36525.0;

            // Sidereal Time
            double siderealTimeHours = 6.6974 + 2400.0513 * julianCenturies;

            double siderealTimeUT = siderealTimeHours +
                (366.2422 / 365.2422) * (double)dateTime.TimeOfDay.TotalHours;

            double siderealTime = siderealTimeUT * 15 + longitude;

            return siderealTime;
        }


        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;

        /*!
            * \brief Calculates the sun light.
            *
            * CalcSunPosition calculates the suns "position" based on a
            * given date and time in local time, latitude and longitude
            * expressed in decimal degrees. It is based on the method
            * found here:
            * http://www.astro.uio.no/~bgranslo/aares/calculate.html
            * The calculation is only satisfiably correct for dates in
            * the range March 1 1900 to February 28 2100.
            * \param dateTime Time and date in local time.
            * \param latitude Latitude expressed in decimal degrees.
            * \param longitude Longitude expressed in decimal degrees.
            */
        public static void CalculateSunPosition(DateTime dateTime, double latitude, double longitude)
        {
            // Convert to UTC
            dateTime = dateTime.ToUniversalTime();

            // Number of days from J2000.0.
            double julianDate = 367 * dateTime.Year -
                (int)((7.0 / 4.0) * (dateTime.Year +
                (int)((dateTime.Month + 9.0) / 12.0))) +
                (int)((275.0 * dateTime.Month) / 9.0) +
                dateTime.Day - 730531.5;

            double julianCenturies = julianDate / 36525.0;

            // Sidereal Time
            double siderealTimeHours = 6.6974 + 2400.0513 * julianCenturies;

            double siderealTimeUT = siderealTimeHours +
                (366.2422 / 365.2422) * (double)dateTime.TimeOfDay.TotalHours;

            double siderealTime = siderealTimeUT * 15 + longitude;

            // Refine to number of days (fractional) to specific time.
            julianDate += (double)dateTime.TimeOfDay.TotalHours / 24.0;
            julianCenturies = julianDate / 36525.0;

            // Solar Coordinates
            double meanLongitude = CorrectAngle(Deg2Rad *
                (280.466 + 36000.77 * julianCenturies));

            double meanAnomaly = CorrectAngle(Deg2Rad *
                (357.529 + 35999.05 * julianCenturies));

            double equationOfCenter = Deg2Rad * ((1.915 - 0.005 * julianCenturies) *
                Math.Sin(meanAnomaly) + 0.02 * Math.Sin(2 * meanAnomaly));

            double elipticalLongitude =
                CorrectAngle(meanLongitude + equationOfCenter);

            double obliquity = (23.439 - 0.013 * julianCenturies) * Deg2Rad;

            // Right Ascension
            double rightAscension = Math.Atan2(
                Math.Cos(obliquity) * Math.Sin(elipticalLongitude),
                Math.Cos(elipticalLongitude));

            double declination = Math.Asin(
                Math.Sin(rightAscension) * Math.Sin(obliquity));

            // Horizontal Coordinates
            double hourAngle = CorrectAngle(siderealTime * Deg2Rad) - rightAscension;

            if (hourAngle > Math.PI)
            {
                hourAngle -= 2 * Math.PI;
            }

            double altitude = Math.Asin(Math.Sin(latitude * Deg2Rad) *
                Math.Sin(declination) + Math.Cos(latitude * Deg2Rad) *
                Math.Cos(declination) * Math.Cos(hourAngle));

            // Nominator and denominator for calculating Azimuth
            // angle. Needed to test which quadrant the angle is in.
            double aziNom = -Math.Sin(hourAngle);
            double aziDenom =
                Math.Tan(declination) * Math.Cos(latitude * Deg2Rad) -
                Math.Sin(latitude * Deg2Rad) * Math.Cos(hourAngle);

            double azimuth = Math.Atan(aziNom / aziDenom);

            if (aziDenom < 0) // In 2nd or 3rd quadrant
            {
                azimuth += Math.PI;
            }
            else if (aziNom < 0) // In 4th quadrant
            {
                azimuth += 2 * Math.PI;
            }

            // Altitude
            Console.WriteLine("Altitude: " + altitude * Rad2Deg);

            // Azimut
            Console.WriteLine("Azimuth: " + azimuth * Rad2Deg);
        }

        /*!
        * \brief Corrects an angle.
        *
        * \param angleInRadians An angle expressed in radians.
        * \return An angle in the range 0 to 2*PI.
        */
        private static double CorrectAngle(double angleInRadians)
        {
            if (angleInRadians < 0)
            {
                return 2 * Math.PI - (Math.Abs(angleInRadians) % (2 * Math.PI));
            }
            else if (angleInRadians > 2 * Math.PI)
            {
                return angleInRadians % (2 * Math.PI);
            }
            else
            {
                return angleInRadians;
            }
        }
    }
}