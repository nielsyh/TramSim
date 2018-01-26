using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationProject
{
    class Station
    {
        public string stationName;
        public int stationNumber;
        public Queue<TimeSpan> remainingPassengersA = new Queue<TimeSpan>();
        public Queue<TimeSpan> remainingPassengersB = new Queue<TimeSpan>();
    }

    class SubStation : Station
    {
        public SubStation(string n, int num)
        {
            this.stationName        = n;
            this.stationNumber      = num;
        }
      
        public TimeSpan lastArrivalTrackA = TimeSpan.FromHours(6);  //maybe change to 06:00 AM
        public TimeSpan lastArrivalTrackB = TimeSpan.FromHours(6);  //maybe change to 06:00 AM
        public TimeSpan lastDepartureTrackA = TimeSpan.FromHours(6);
        public TimeSpan lastDepartureTrackB = TimeSpan.FromHours(6);
        public TimeSpan lastEtaTrackA = TimeSpan.FromHours(5);
        public TimeSpan lastEtaTrackB = TimeSpan.FromHours(5);

        public Station nextStationTrackA;
        public Station nextStationTrackB;


        public Station getNextStation(Direction d)
        {
            if (d == Direction.Uithof) return nextStationTrackA;
            if (d == Direction.UtrechtCentraal) return nextStationTrackB;
            throw new Exception("Track not found use capital A or B.");
        }

    }

    class EndStation : Station
    {
        public int numberOfTrams = 0;
        public EndStation(string stationName, int num)
        {
            this.stationName    = stationName;
            this.stationNumber  = num;
        }

        public TimeSpan lastArrival                 = TimeSpan.FromHours(0);    //maybe change to 06:00 AM
        public TimeSpan lastDeparture               = TimeSpan.FromHours(0);
        public TimeSpan nextDeparture               = TimeSpan.FromHours(0);
        public List<TimeSpan> remainingDepartures   = new List<TimeSpan>();
        public TimeSpan lastEta                     = TimeSpan.FromHours(0);
        public SubStation nextStation;

        public Station getNextStation()
        {
            return nextStation;
        }

        public Direction getDirection()
        {
            if (this.stationNumber == 1) return Direction.Uithof;
            else return Direction.UtrechtCentraal;
        }


    }
}
