using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics;
using MathNet.Numerics.Distributions;

namespace SimulationProject
{
     class Simulation
    {
        static public PriorityQueue<Event> priorityQueue = new PriorityQueue<Event>();

        static public double averageWaitingTime = 0;
        static public double numberOfPassengers = 0;
        static public double maxWaitingTime     = 0;

        //Central
        static public double averageDelayTimeC  = 0;
        static public double numberOfTramsC     = 0;
        static public double maxDepartureDelayC = 0;
        static public double numOfCriticalDelayedtramsC = 0;

        //Uithof
        static public double averageDelayTimeU  = 0;
        static public double numberOfTramsU     = 0;
        static public double maxDepartureDelayU = 0;
        static public double numOfCriticalDelayedtramsU = 0;

        static public double totalRemainingPassengers = 0;

        public static string[][] enteringRatesA = getRatesEnteringA();
        public static string[][] leavingRatesA  = getRatesLeavingA();
        public static string[][] enteringRatesB = getRatesEnteringB();
        public static string[][] leavingRatesB  = getRatesLeavingB();

        public static readonly int[] drivingTimesA = new int[] { 134, 243, 59, 101, 60, 86, 78, 113 };
        public static readonly int[] drivingTimesB = new int[] { 110, 78, 82, 60, 100, 59, 243, 135 };

        public static bool doorBlockEnabled = false;    //enabled or disabled, default = false
        public static int probabilityX      = 0;        //X can be 1, 3, 5 or 10

        public static Random random = new Random();

        static public void saveRemainingPassengers(TrackDefault d)
        {
            foreach(Station s in d.getStations())
            {
                totalRemainingPassengers += s.remainingPassengersA.Count();
                totalRemainingPassengers += s.remainingPassengersB.Count();
            }
        }

        static public void updateAverageWaitingTime(double waitingTime)
        {
            if (waitingTime > maxWaitingTime) maxWaitingTime = waitingTime;
            if(waitingTime > 0)
            {
                averageWaitingTime = (averageWaitingTime * numberOfPassengers + waitingTime) / (numberOfPassengers + 1);
            }
            numberOfPassengers++;
        }

        public static void updateAverageDepartureDelay(TimeSpan actualDeparture, TimeSpan scheduleDeparture, EndStation station)
        {
            double delay = (actualDeparture - scheduleDeparture).TotalSeconds;

            if (station.stationNumber == 1) {
                if (delay > maxDepartureDelayC) maxDepartureDelayC = delay;
                if (delay > 0)
                {
                    averageDelayTimeC = (averageDelayTimeC * numberOfTramsC + delay) / (numberOfTramsC + 1);
                    if(delay > 60) { numOfCriticalDelayedtramsC++; }
                }
                numberOfTramsC++;
            }
            else {
                if (delay > maxDepartureDelayU) maxDepartureDelayU = delay;
                if (delay > 0)
                {
                    averageDelayTimeU = (averageDelayTimeU * numberOfTramsU + delay) / (numberOfTramsU + 1);
                    if (delay > 60) { numOfCriticalDelayedtramsU++; }
                }
                numberOfTramsU++;
            }
        }

        //Calculate the travel time between two stops given the mean travel time. This mean is given in the assignment description
        static public int getTravelTime(Station station, Direction direction)
        {
            int mean = getAverageDrivingTime(station, direction);
            //Calculate variance (as square of std.deviation) from mean. This is according to the relation found during input analysis
            double variance = Math.Pow((0.0682 * mean + 0.759), 2);

            //Calculate shape from mean and variance
            double shape = Math.Pow(mean, 2) / variance;
            //Calculate rate from mean and shape
            double rate = shape / mean;

            return (int)Gamma.Sample(shape, rate);
        }


        public static int getAverageDrivingTime(Station station, Direction direction)
        {
            if (direction == Direction.Uithof)
            {
                //use a
                return drivingTimesA[station.stationNumber - 1];
            }
            else {
                //use b
                return drivingTimesB[9 - station.stationNumber];
            }
        }

        //Calculate the amount of passengers entering the tram at a given station, given the station, last tram arrival at that station of a previous tram and the current time
        static public Queue<TimeSpan> getPassengersIn(Station station, Direction direction, TimeSpan lastArrival, TimeSpan now)
        {
            Queue<TimeSpan> passengers = new Queue<TimeSpan>();
            if ((station.stationNumber == 1 && direction == Direction.UtrechtCentraal) || (station.stationNumber == 9 && direction == Direction.Uithof)) { return passengers; } //empty queue }

            if (lastArrival.Hours < 6)
            {
                lastArrival = TimeSpan.FromHours(6);
            }
            int firstInterval = (int)(lastArrival.TotalMinutes - 360)/15;
            int lastInterval = (int)(now.TotalMinutes - 360) / 15;
            double rate;

            for (int n = firstInterval; n <= lastInterval; n++)
            {
                if (direction == Direction.Uithof) { rate = double.Parse(enteringRatesA[n + 1][station.stationNumber - 1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo); }
                else { rate = double.Parse(enteringRatesB[n + 1][9 - station.stationNumber], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo); }

                double minutesInInterval = Math.Min(now.TotalMinutes, (n + 1) * 15 + 360) - (Math.Max(lastArrival.TotalMinutes, (n * 15) + 360));

                //Get the amount of passengers using the rate and time in this current interval
                if (rate > 0 && minutesInInterval > 0)
                {
                    int pii = Poisson.Sample(rate * minutesInInterval);
                    for (int i = 0; i < pii; i++)
                    {
                        passengers.Enqueue(TimeSpan.FromMinutes(
                                                    (Math.Max(lastArrival.TotalMinutes, (n * 15) + 360)) + minutesInInterval / 2));
                    }
                }
            }
                return passengers;            

        }

        static public int getPassengersOut(Station station, Direction direction, TimeSpan now, int passengersInTram)
        {
            double fraction;

            //Get the rate dependant on the current moment
            if(now.Hours > 7 && now.Hours <= 9)            
                fraction = getLeaveFrac(2, station, direction);
            
            if(now.Hours > 16 && now.Hours <= 18)             
                fraction = getLeaveFrac(3, station, direction);
            
            else          
                fraction = getLeaveFrac(1, station, direction);            

            return passengersInTram * (int)fraction;
        }

        //Read from leaving passenger csv given position
        static public double getLeaveFrac(int interval, Station station, Direction direction)
        {
            if (direction == Direction.Uithof) return double.Parse(leavingRatesA[interval][station.stationNumber - 1], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo); //Get the rate from the CS to Uithof
            else return double.Parse(leavingRatesB[interval][9 - station.stationNumber], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
        }

        //Get the dwelling time at a given station using the given formula and the amount of passengers leaving/entering/staying in the tram
        static public double getDwellingTime(int passengerIn, int passengerOut, int passengerStay)
        {
            double d_one = 12.5 + 0.22* passengerIn + 0.13 * passengerOut;                                      //Formula given, parameter for uncrowded stops
            double d_two =  2.3 * Math.Pow(10,-5) * Math.Pow(passengerStay,2) * (passengerIn + passengerOut);   //Formula given, parameter for crowded stops

            double d_linear_combi = ((1 - ((double)passengerStay /Tram.MAX_OCCUPANCY)) * d_one) + (((double)passengerStay / Tram.MAX_OCCUPANCY) * d_two);    // (passengerStay/420), the occupancy rate of this tram, is an indication for how crowded the tram is. We take a linear combination of both parameter values.
            double mean = d_linear_combi;                                                                 //Formula given
            double rate = 2 / mean;                                                                       //From definition of gamma distribution with shape (k=2) and rate parameter

            //return mean;
            int sampleWait = (int)Gamma.Sample(2, rate);
            return Math.Max(sampleWait, (mean*0.8));
        }

        static public string[][] getRatesEnteringB()
        {
            var filePath = "data/Rates/PRtoCS/CSV_entering_pr2cs.csv";
            //var filePath = "data/Validation_Rates/PRtoCS/val_3.csv";

            string[][] rates = File.ReadLines(filePath).Select(s => s.Replace(',', '.').Split(";".ToArray())).ToArray().ToArray();
            return rates;
        }
        static public string[][] getRatesLeavingB()
        {
            var filePath = "data/Rates/PRtoCS/CSV_leaving_pr2cs.csv";

            string[][] rates = File.ReadLines(filePath).Select(s => s.Replace(',', '.').Split(";".ToArray())).ToArray().ToArray();
            return rates;
        }
        static public string[][] getRatesEnteringA()
        {
            var filePath = "data/Rates/CStoPR/CSV_entering_cs2pr.csv";
           // var filePath = "data/Validation_Rates/CStoPR/val_3.csv";

            string[][] rates = File.ReadLines(filePath).Select(s => s.Replace(',', '.').Split(";".ToArray())).ToArray().ToArray();
            return rates;
        }
        static public string[][] getRatesLeavingA()
        {
            var filePath = "data/Rates/CStoPR/CSV_leaving_cs2pr.csv";

            string[][] rates = File.ReadLines(filePath).Select(s => s.Replace(',', '.').Split(";".ToArray())).ToArray().ToArray();
            return rates;
        }

    }
}
