using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;


namespace SimulationProject
{
    class Program
    {
        static TrackDefault d;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter number of simulations: ");
            int runs  = int.Parse(Console.ReadLine());

            Console.WriteLine("enable doorblock? (y/n)");
            string option = Console.ReadLine();
            if (option == "y" || option == "y")
            {
                Simulation.doorBlockEnabled = false;
                Console.WriteLine("Give probabilityX, X should be 1, 3, 5 or 10");
                Simulation.probabilityX = int.Parse(Console.ReadLine());
            }

            Console.WriteLine("Simulation started...");
            Console.WriteLine();

           // Console.WriteLine("Avg.Wtime;Max.Wtime;AvgDDC;MaxDDC;CritC;AvgDDU;MaxDDU;CritU");

            for (int i = 0; i < runs; i++)
            {
                Program a = new Program();
                d =  a.InitDefaultTrack();
                a.initScheme();
                
                while (Simulation.priorityQueue.NumItems > 0)
                {
                    Simulation.priorityQueue.Dequeue().handleEvent();
                }
                Simulation.saveRemainingPassengers(d);
               // Console.WriteLine(Simulation.averageWaitingTime + ";" + Simulation.maxWaitingTime + ";" + Simulation.averageDelayTimeC + ";" + Simulation.maxDepartureDelayC + ";" + ((100/Simulation.numberOfTramsC)*Simulation.numOfCriticalDelayedtramsC)  + ";" + Simulation.averageDelayTimeU + ";" + Simulation.maxDepartureDelayU + ";" + ((100 / Simulation.numberOfTramsU) * Simulation.numOfCriticalDelayedtramsU));
               // resetSimulation();
                
            }

            Console.WriteLine("Result of " + runs + " simulation days");
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine("Measure     |         Result         |   Average ");
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine("avg W       : " + Simulation.averageWaitingTime);
            Console.WriteLine("#passengers : " + Simulation.numberOfPassengers + "               | " + Simulation.numberOfPassengers / runs);
            Console.WriteLine("Remaining # : " + Simulation.totalRemainingPassengers + "            | " + Simulation.totalRemainingPassengers / runs);
            Console.WriteLine("Max W       : " + Simulation.maxWaitingTime);
            Console.WriteLine("Avg DC      : " + Simulation.averageDelayTimeC);
            Console.WriteLine("Avg DU      : " + Simulation.averageDelayTimeU);
            Console.WriteLine("Max DC      : " + Simulation.maxDepartureDelayC);
            Console.WriteLine("Max DU      : " + Simulation.maxDepartureDelayU);

            Console.ReadLine();
        }

        public void initScheme() {    
            
            var filePath = "data/Schemas/schema_a.csv";
            string[][] rates = File.ReadLines(filePath).Select(s => s.Replace(',', '.').Split(";".ToArray())).ToArray().ToArray();
            
            //get num of trams
            for(int i = 1; i < rates.Length; i++)
            {   if(rates[i][0] != "")
                {
                    Simulation.priorityQueue.Enqueue(initTram(i, 1, rates));
                }
                       
            }

        }

        public static void resetSimulation() {
            
            Simulation.averageWaitingTime   = 0;            
            Simulation.numberOfPassengers   = 0;

            Simulation.averageDelayTimeC    = 0;
            Simulation.maxDepartureDelayC   = 0;
            Simulation.numberOfTramsC       = 0;
            Simulation.numOfCriticalDelayedtramsC = 0;

            Simulation.averageDelayTimeU    = 0;
            Simulation.numberOfTramsU       = 0;
            Simulation.maxDepartureDelayU   = 0;
            Simulation.numOfCriticalDelayedtramsU = 0;

            Simulation.maxWaitingTime       = 0;
        }

        public expectedArrivalEndStation initTram(int i, int j, string[][] rates) {
            if (String.IsNullOrWhiteSpace(rates[i][j]))
            {
                return initTram(i, (j + 1), rates);
            }
            else
            {
                Tram t = new Tram(i);

                //get all departures
                for (int k = j; k < rates[0].Length; k++)
                {
                    if (String.IsNullOrWhiteSpace(rates[i][k]))
                    {
                        //nothing..
                    }
                    else
                    {
                        string str = rates[i][k];
                        int h = int.Parse(str.Substring(0, 2));
                        int m = int.Parse(str.Substring(3, 2));
                        int s = int.Parse(str.Substring(6, 2));
                        TimeSpan tmpTimespan = (new TimeSpan(0, h, m, s, 0));
                        if (rates[0][k] == "Vertrektijd CS")
                        {
                            t.departures.Enqueue(new DepartureEndStation(tmpTimespan,  d.centraal, t));
                        }
                        else if (rates[0][k] == "Vertrektijd P&R")
                        {
                            t.departures.Enqueue(new DepartureEndStation(tmpTimespan, d.uithof, t));
                        }
                    }
                }
                //return event
                TimeSpan desiredArrival = TimeSpan.FromMinutes(t.departures.Peek().eventTime.TotalMinutes - 3);
                expectedArrivalEndStation a = new expectedArrivalEndStation(desiredArrival, t.departures.Peek().departureStation, t);
                return a;
            }            
        }

        public TrackDefault InitDefaultTrack() {

            TrackDefault d = new TrackDefault();
            d.centraal.nextStation = d.vaartscherijn;

            d.vaartscherijn.nextStationTrackB = d.centraal;
            d.vaartscherijn.nextStationTrackA = d.galgenwaard;

            d.galgenwaard.nextStationTrackB = d.vaartscherijn;
            d.galgenwaard.nextStationTrackA = d.krommerijn;

            d.krommerijn.nextStationTrackB = d.galgenwaard;
            d.krommerijn.nextStationTrackA = d.padualaan;

            d.padualaan.nextStationTrackB = d.krommerijn;
            d.padualaan.nextStationTrackA = d.heidelberglaan;

            d.heidelberglaan.nextStationTrackB = d.padualaan;
            d.heidelberglaan.nextStationTrackA = d.umc;

            d.umc.nextStationTrackB = d.heidelberglaan;
            d.umc.nextStationTrackA = d.wkz;

            d.wkz.nextStationTrackB = d.umc;
            d.wkz.nextStationTrackA = d.uithof;

            d.uithof.nextStation = d.wkz;
            return d;
        }
    }
}
