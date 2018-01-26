using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationProject
{
    enum Direction
    {
        Uithof,
        UtrechtCentraal
    }

    class Tram
    {
        public const int    MAX_OCCUPANCY = 420;
        private int         passengers;
        public Direction    direction;
        public int id;
        public Queue<DepartureEndStation> departures;

        public Tram(int id, int startPassengers = 0)
        {
            this.passengers = startPassengers;
            this.id = id;
            this.departures = new Queue<DepartureEndStation>();
        }

       public int getPassengers()
        {
            return passengers;
        }

        public Queue<TimeSpan> passengersIn(Queue<TimeSpan> pIn, TimeSpan now)
        {
            //use queue, return remaining time's, and update avg waiting time
            while (getOpenSeats() > 0 && (pIn.Count() > 0))
            {
                passengers++;
                Simulation.updateAverageWaitingTime(now.TotalSeconds - pIn.Dequeue().TotalSeconds);
            }
            return pIn;
        }

        public int passengersOut(int pOut)
        {
            int t = passengers - pOut;
            if(t < 0)
            {
                passengers = 0;
                return t;
            }
            passengers = t;
            return passengers;
        }

        public int getOpenSeats() {
            return MAX_OCCUPANCY - passengers;
        }
    }
}
