using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationProject
{
    class Event
    {
        public const int SAFETY_MEASURE = 40;
        public TimeSpan eventTime { get; set; }
        public Tram tram;
        
        public virtual void handleEvent() {
            //implement in subclass
        }
    }

    class expectedArrivalEndStation : Event
    {
        EndStation arrivalStation;
        public expectedArrivalEndStation(TimeSpan eta, EndStation arrivalStation, Tram tram)
        {
            this.eventTime = eta;
            this.arrivalStation = arrivalStation;
            this.tram = tram;
        }

        public override void handleEvent()
        {
            TimeSpan arrivalTime;

            if(tram.departures.Count() != 0)
            {
                //remove from track if next departure is in more than 30 mins
                if(tram.departures.Peek().eventTime.TotalMinutes > eventTime.TotalMinutes + 30)
                {
                    expectedArrivalEndStation resumeWork = new expectedArrivalEndStation(TimeSpan.FromMinutes(tram.departures.Peek().eventTime.TotalMinutes - 3), arrivalStation, tram);
                    Simulation.priorityQueue.Enqueue(resumeWork);
                }
                else
                {
                    if (arrivalStation.numberOfTrams >= 2)
                    {
                        arrivalTime = TimeSpan.FromSeconds(arrivalStation.nextDeparture.TotalSeconds + SAFETY_MEASURE);
                    }
                    else
                    {
                        arrivalTime = TimeSpan.FromSeconds(Math.Max(arrivalStation.lastDeparture.TotalSeconds + SAFETY_MEASURE, eventTime.TotalSeconds));
                    }

                    //generate doorblock
                    if (Simulation.doorBlockEnabled)
                    {
                        int rand = Simulation.random.Next(0, 100);
                        if (100 - Simulation.probabilityX < rand)
                        {
                            arrivalTime = TimeSpan.FromSeconds(arrivalTime.TotalSeconds + 60);
                        }
                    }

                    arrivalEndStation arrival = new arrivalEndStation(arrivalTime, arrivalStation, tram);
                    Simulation.priorityQueue.Enqueue(arrival);
                }
            }
        }
    }
    /*
     * CURRENT ENDSTATION RULES
     * 
     * - MAX 2 TRAMS AT ENDSTATION
     * - THEY CAN ARRIVE 40S IF A STOP IS FREE
     * - 40S SAFETY IS APPLIED AT NEXT SUBSTATION
     * - TRAMS CANNOT LEAVE AT SAME TIME, BUT 40 SECOND AFTER EACHOTHER
     */

    class arrivalEndStation : Event
    {
        EndStation arrivalStation;
        
        public arrivalEndStation(TimeSpan arrivalTime, EndStation arrivalStation, Tram tram)
        {
            this.eventTime = arrivalTime;
            this.arrivalStation = arrivalStation;
            this.tram = tram;
        }


        public override void handleEvent()
        {
            TimeSpan departureTime;

            //Console.WriteLine("Tram arrived at End stop: " + arrivalStation.stationName);

            //set endStation occupation to +1
            arrivalStation.numberOfTrams ++;

            tram.passengersOut(tram.getPassengers());   //Let all passengers out
            tram.direction = arrivalStation.getDirection(); //Turn tram around

            TimeSpan desiredDeparture = TimeSpan.FromMinutes(Math.Max(eventTime.TotalMinutes + 3, tram.departures.Peek().eventTime.TotalMinutes));  //Check how late we would want to leave

            if(Math.Abs(arrivalStation.nextDeparture.TotalSeconds - desiredDeparture.TotalSeconds) < SAFETY_MEASURE)
            {
                departureTime = TimeSpan.FromSeconds(arrivalStation.nextDeparture.TotalSeconds + SAFETY_MEASURE);
            }
            else
            {
                departureTime = desiredDeparture;
            }

            //save this arrivaltime as next lastArrival
            arrivalStation.lastArrival = eventTime;

            DepartureEndStation actualDeparture = new DepartureEndStation(departureTime, arrivalStation, tram);
            arrivalStation.remainingDepartures.Add(departureTime);

            //Dequeue this tram's next departure & update average delay
            Simulation.updateAverageDepartureDelay(actualDeparture.eventTime, tram.departures.Dequeue().eventTime, arrivalStation);

            Simulation.priorityQueue.Enqueue(actualDeparture);

        }
    }

    class DepartureEndStation : Event
    {
        public EndStation departureStation;
        public DepartureEndStation(TimeSpan departTime, EndStation departureStation, Tram tram)
        {
            this.eventTime = departTime;
            this.tram = tram;
            this.departureStation = departureStation;
        }

        //eventHandler for DepartureEndStationEvent
        public override void handleEvent() {

            departureStation.numberOfTrams--;

            Queue<TimeSpan> pIn = Simulation.getPassengersIn(departureStation, tram.direction, departureStation.lastDeparture, eventTime);

            if(tram.direction == Direction.Uithof)
            {
                for (int i = pIn.Count(); i > 0; i--) departureStation.remainingPassengersA.Enqueue(pIn.Dequeue());
                departureStation.remainingPassengersA = tram.passengersIn(departureStation.remainingPassengersA, eventTime);
            }
            else
            {
                for (int i = pIn.Count(); i > 0; i--) departureStation.remainingPassengersB.Enqueue(pIn.Dequeue());
                departureStation.remainingPassengersB = tram.passengersIn(departureStation.remainingPassengersB, eventTime);
            }

            departureStation.lastDeparture = eventTime;
            departureStation.remainingDepartures.Remove(eventTime);

            if (departureStation.numberOfTrams > 0) {
                departureStation.remainingDepartures = departureStation.remainingDepartures.OrderBy(x => x.TotalSeconds).ToList();
                departureStation.nextDeparture = departureStation.remainingDepartures[0];
            }
            else
            {
                departureStation.nextDeparture = TimeSpan.FromHours(0);
            }

            //Get traveltime
            int estimatedTravelTime = Simulation.getTravelTime(departureStation, tram.direction);
            TimeSpan etaNextStation = eventTime + TimeSpan.FromSeconds(estimatedTravelTime);

            //Check if not faster than last tram
            if (etaNextStation < departureStation.lastEta) {
                etaNextStation = TimeSpan.FromSeconds(departureStation.lastEta.TotalSeconds + 1);
            }
            departureStation.lastEta = etaNextStation;
      
            //gen. eta event & enqueue to eventQueue.
            expectedArrivalSubtStation etaEvent = new expectedArrivalSubtStation(etaNextStation, departureStation.nextStation, tram);
            Simulation.priorityQueue.Enqueue(etaEvent);

            //Console.WriteLine(String.Format("Event: dep.end | time: {0,-8} station: {1,-18} tramID: {2,-2} | pRemained: {3,-4} pIn: {4,-4}",eventTime, departureStation.stationName, tram.id, remainPassengers, inPassengers) + " | lastDeparture: "+ lastdep + " remTrams: " + departureStation.numberOfTrams + " nextDeparture: " + departureStation.nextDeparture );
        }
    }

    class expectedArrivalSubtStation : Event
    {
        SubStation arrivalStation;
        public expectedArrivalSubtStation(TimeSpan eta, SubStation arrivalStation, Tram tram)
        {
            this.eventTime = eta;
            this.arrivalStation = arrivalStation;
            this.tram = tram;
        }

        public override void handleEvent() {
            //when was last departure at arrival station? 40 secs safety measure.
            TimeSpan lastDeparture;
            double s = 0;
            if (tram.direction == Direction.Uithof) { lastDeparture = arrivalStation.lastDepartureTrackA; }
            else                                    { lastDeparture = arrivalStation.lastDepartureTrackB; }

            //Calc. actual arrival time inc. 40 seconds safety measure
            s = (eventTime.TotalSeconds - (lastDeparture.TotalSeconds + SAFETY_MEASURE));
            TimeSpan arrivalTime;
            if (s > 0)
            {
                arrivalTime = eventTime; //normal eta                
            }
            else //arrive to soon so delay
            {
                arrivalTime = TimeSpan.FromSeconds(eventTime.TotalSeconds + s);
            }

            //generate doorblock
            if (Simulation.doorBlockEnabled) {
                int rand = Simulation.random.Next(0, 100);
                if(100 - Simulation.probabilityX < rand)
                {
                    arrivalTime = TimeSpan.FromSeconds(arrivalTime.TotalSeconds + 60);
                }
            }

            //gen arrival
            arrivalSubStation arrival = new arrivalSubStation(arrivalTime, arrivalStation, tram);
            Simulation.priorityQueue.Enqueue(arrival);
        }

    }

    class arrivalSubStation : Event
    {
        SubStation arrivalStation;
        public arrivalSubStation(TimeSpan eta, SubStation arrivalStation, Tram tram)
        {
            this.eventTime = eta;
            this.arrivalStation = arrivalStation;
            this.tram = tram;
        }

        public override void handleEvent() {
            //generate doorblock

            //generate passengers out
            int pOut = Simulation.getPassengersOut(arrivalStation, tram.direction, eventTime, tram.getPassengers());
            tram.passengersOut(pOut);
            int pStay = tram.getPassengers(); int pInTotal;

            //generate passengers in = last arrival t/m this arrival
            TimeSpan lastDeparture;
            TimeSpan lastArrival;
            if (tram.direction == Direction.Uithof) {
                lastDeparture = arrivalStation.lastDepartureTrackA;
                lastArrival = arrivalStation.lastArrivalTrackA;
            }
            else {
                lastDeparture = arrivalStation.lastDepartureTrackB;
                lastArrival = arrivalStation.lastArrivalTrackB;
            }

            //gen. passengers between lastArrival and current eventTime TODO
            Queue<TimeSpan> pIn = Simulation.getPassengersIn(arrivalStation, tram.direction, lastDeparture, eventTime);
            

            if (tram.direction == Direction.Uithof)
            {
                pInTotal = Math.Min(tram.getOpenSeats(), pIn.Count + arrivalStation.remainingPassengersA.Count());
                for (int i = pIn.Count(); i > 0; i--) arrivalStation.remainingPassengersA.Enqueue(pIn.Dequeue());
                arrivalStation.remainingPassengersA = tram.passengersIn(arrivalStation.remainingPassengersA, eventTime);
            }
            else
            {
                pInTotal = Math.Min(tram.getOpenSeats(), pIn.Count + arrivalStation.remainingPassengersB.Count());
                for (int i = pIn.Count(); i > 0; i--) arrivalStation.remainingPassengersB.Enqueue(pIn.Dequeue());
                arrivalStation.remainingPassengersB = tram.passengersIn(arrivalStation.remainingPassengersB, eventTime);
            }

            //Console.WriteLine(String.Format("Station: {0,-25} Time: {1,0}, pInTram: {2,0} pRemained: {3,0}, pInSim: {4,0} | pRemain: {5,0}", arrivalStation.stationName, eventTime, tempTramC, passRem, pInSim, arrivalStation.remainingPassengers.Count()));

            //gen. dwellingtime
            double dwellingTime = Simulation.getDwellingTime(pInTotal, pOut, pStay);
            TimeSpan departureAt = eventTime + TimeSpan.FromSeconds((int)dwellingTime);

            //save this arrivaltime as next lastArrival
            if (tram.direction == Direction.Uithof){ arrivalStation.lastArrivalTrackA = eventTime;}
            else{ arrivalStation.lastArrivalTrackB = eventTime; }

            //gen. eta event & enqueue to eventQueue.
            departureSubStation etaEvent = new departureSubStation(departureAt, arrivalStation, tram);
            Simulation.priorityQueue.Enqueue(etaEvent);
        }
    }

    class departureSubStation : Event
    {
        SubStation departureStation;
        public departureSubStation(TimeSpan departTime, SubStation departureSation, Tram tram)
        {
            this.eventTime = departTime;
            this.tram = tram;
            this.departureStation = departureSation;
        }
        //eventHandler for DepartureEndStationEvent
        public override void handleEvent()
        {
            int estimatedTravelTime = Simulation.getTravelTime(departureStation, tram.direction);
            TimeSpan etaNextStation = TimeSpan.FromSeconds(eventTime.TotalSeconds + estimatedTravelTime);

            //set track A last departure time
            if (tram.direction == Direction.Uithof) {
                departureStation.lastDepartureTrackA = eventTime;
                //Check if not faster than last tram
                if (etaNextStation < departureStation.lastEtaTrackA)
                {
                    etaNextStation = TimeSpan.FromSeconds(departureStation.lastEtaTrackA.TotalSeconds + 1);
                }
                departureStation.lastEtaTrackA = etaNextStation;
            }
            //set Track B last departure time
            if (tram.direction == Direction.UtrechtCentraal) {
                departureStation.lastDepartureTrackB = eventTime;
                //Check if not faster than last tram
                if (etaNextStation < departureStation.lastEtaTrackB)
                {
                    etaNextStation = TimeSpan.FromSeconds(departureStation.lastEtaTrackB.TotalSeconds + 1);
                }
                departureStation.lastEtaTrackB = etaNextStation;
            }

            // Push new event to queue. checks if end or sub Station
            if (departureStation.getNextStation(tram.direction) is SubStation)
            {
                // subStation event
                // gen. eta event & enqueue to eventQueue.
                expectedArrivalSubtStation etaEvent = new expectedArrivalSubtStation(etaNextStation, (SubStation)departureStation.getNextStation(tram.direction), tram);
                Simulation.priorityQueue.Enqueue(etaEvent);
            }
            else if (departureStation.getNextStation(tram.direction) is EndStation)
            {
                // endStation event
                // gen. eta event & enqueue to eventQueue.
                expectedArrivalEndStation etaEvent = new expectedArrivalEndStation(etaNextStation, (EndStation)departureStation.getNextStation(tram.direction), tram);
                Simulation.priorityQueue.Enqueue(etaEvent);
            }
        }
    }

    class PriorityQueue<T>
    {
        // The items and priorities.
        List<Event> EventList = new List<Event>();

        // Return the number of items in the queue.
        public int NumItems
        {
            get
            {
                return EventList.Count;
            }
        }

        // Add an item to the queue.
        public void Enqueue(Event newEvent)
        {
            EventList.Add(newEvent);
            EventList = EventList.OrderBy(x => x.eventTime).ToList();
        }

        // Remove the item with the largest priority from the queue.
        public Event Dequeue()
        {
            Event tmp = EventList[0];
            EventList.RemoveAt(0);
            return tmp;
        }
        public Event Peek()
        {
            return EventList[0];
        }
    }

}

