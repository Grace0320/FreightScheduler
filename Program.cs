using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FreightScheduler
{
    class FlightSchedule
    {
        public List<Flight> Schedule;
        /// <summary>
        /// Load a text file with the following format into schedule:
        /// Day 1: 
        /// Flight 1: Montreal airport(YUL) to Toronto(YYZ)
        /// Flight 2: Montreal(YUL) to Calgary(YYC)
        /// Flight 3: Montreal(YUL) to Vancouver(YVR)
        /// Day 2:
        /// Flight 4: Montreal airport(YUL) to Toronto(YYZ)
        /// Flight 5: Montreal(YUL) to Calgary(YYC)
        /// Flight 6: Montreal(YUL) to Vancouver(YVR)
        /// </summary>
        /// <param name="filename">filepath to text file containing flight schedule</param>
        /// <returns>0 for success, 1 for failure</returns>
        public int Load(string filepath)
        {
            // check for file existence
            if (!File.Exists(filepath))
            {
                Console.WriteLine("Flight schedule file does not exist.");
                return 1;
            }

            string[] lines = File.ReadAllLines(filepath);
            if (lines.Length == 0)
            {
                Console.WriteLine("Flight schedule file is empty.");
                return 1;
            }

            Schedule = new List<Flight>();
            int day = 0;
            foreach (string line in lines)
            {
                //get position of first colon
                int colonIdx = line.IndexOf(':');
                if (colonIdx < 0)
                {
                    Console.WriteLine("Flight schedule file has wrong format.");
                    return 1;
                }

                if (line.StartsWith("Day "))
                {
                    string dayNum = line.Substring(4, colonIdx - 4);
                    day = Int32.Parse(dayNum);
                }
                else if (line.StartsWith("Flight "))
                {
                    // if a day wasn't provided, can't load the schedule.
                    if (day == 0)
                    {
                        Console.WriteLine("Flight schedule file does not exist.");
                        return 1;
                    }

                    Flight newFlight = new Flight();
                    newFlight.Day = day;
                    // Note: 2 ways to go here, string ops or regex.
                    // String ops usually outperform regex except in very complicated cases.
                    // Regex makes for cleaner code. 
                    // Assumption: flight schedules are very large due to being planned well in advance.
                    // I opted for performance with string ops.

                    // Get flight number
                    string flight = line.Substring(7, colonIdx - 7);
                    newFlight.FlightNum = Int32.Parse(flight);

                    // looking for instances of brackets to determine departure/arrival cities
                    // first set of brackets contains departure, second contains arrival
                    int firstOpenBracket = line.IndexOf('(');
                    int secondOpenBracket = line.IndexOf('(', firstOpenBracket + 1);
                    newFlight.Departure = line.Substring(firstOpenBracket + 1, line.IndexOf(')', firstOpenBracket) - firstOpenBracket - 1);
                    newFlight.Destination = line.Substring(secondOpenBracket + 1, line.IndexOf(')', secondOpenBracket) - secondOpenBracket - 1);

                    //add to schedule.
                    Schedule.Add(newFlight);

                }
            }

            return 0;
        }


        /// <summary>
        /// Display the flight schedule in the following format on the console:
        /// Flight: 1, departure: YUL, arrival: YYZ, day: 1
        /// ...
        /// Flight: 6, departure: <departure_city>, arrival: <arrival_city>, day: x
        /// </summary>
        public void Display()
        {
            foreach (Flight fl in Schedule)
            {
                Console.WriteLine("Flight: {0}, departure: {1}, arrival: {2}, day: {3}", fl.FlightNum, fl.Departure, fl.Destination, fl.Day);
            }
        }

        /// <summary>
        /// Find the next available flight with room on it for another package
        /// </summary>
        /// <param name="destination">Desired destination</param>
        /// <returns>Next flight with room, if available, if not, null.</returns>
        public Flight GetNextAvailableFlight(string destination)
        {
            foreach (Flight fl in Schedule)
            {
                if (fl.Destination == destination && fl.HasRoom())
                    return fl;

            }

            return null;
        }
    }

    class Flight
    {
        private const int CAPACITY = 20;
        public int FlightNum = -1;
        public string Departure = null;
        public string Destination = null;
        public int Day = -1;
        public int ScheduledOrderNum = 0;

        public bool HasRoom()
        {
            return ScheduledOrderNum < CAPACITY;
        }


    }

    class Order
    {
        public string orderID = null;
        public string departure = "YUL";  //this exercise has all orders leaving from Montreal
        public string destination = null;
        public Flight schedFlight = null; //use null to denote unscheduled
    }

    class OrderScheduler
    {
        private FlightSchedule flightSched;
        private Dictionary<string, Order> orderDict;
        public OrderScheduler(FlightSchedule _flightSched, Dictionary<string, Order> _orderDict)
        {
            flightSched = _flightSched;
            orderDict = _orderDict;
        }

        /// <summary>
        /// Attempt to schedule all orders in orderDict onto flights in flightSched
        /// </summary>
        public void Schedule()
        {
            //orders are stored in priority order, so just iterate through them
            foreach (string orderID in orderDict.Keys)
            {
                Flight fl = flightSched.GetNextAvailableFlight(orderDict[orderID].destination);
                if (fl != null)
                {
                    orderDict[orderID].schedFlight = fl;
                    fl.ScheduledOrderNum++;
                }
            }
        }

        public void Display()
        {
            foreach (string orderID in orderDict.Keys)
            {
                Order order = orderDict[orderID];
                if (order.schedFlight == null)  //unscheduled
                {
                    Console.WriteLine("order: {0}, flightNumber: not scheduled", orderID);
                }
                else
                {
                    Console.WriteLine("order: {0}, flightNumber: {1}, departure: {2}, arrival: {3}, day: {4}",
                        orderID, order.schedFlight.FlightNum, order.departure, order.destination, order.schedFlight.Day);
                }
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            FlightSchedule flightSched = new FlightSchedule();
            if (flightSched.Load(args[0]) == 0)
            {
                flightSched.Display();

                // get order schedule json
                string json = File.ReadAllText(args[1]);
                Dictionary<string, Order> orders = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json);

                OrderScheduler orderScheduler = new OrderScheduler(flightSched, orders);
                orderScheduler.Schedule();
                orderScheduler.Display();
            }
        }
    }
}
