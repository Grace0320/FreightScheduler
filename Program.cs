using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FreightScheduler
{
    class FlightSchedule
    {
        private const int AIRPORT_CODE_LEN = 3;
        private List<Flight> flightSchedule;

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
        /// <returns>true for success, false if load fails</returns>
        public bool Load(string filepath)
        {
            // check for file existence
            if (!File.Exists(filepath))
            {
                Console.WriteLine("Flight schedule file does not exist.");
                return false;
            }

            string[] lines = File.ReadAllLines(filepath);
            if (lines.Length == 0)
            {
                Console.WriteLine("Flight schedule file is empty.");
                return false;
            }

            flightSchedule = new List<Flight>();
            int day = 0;
            foreach (string line in lines)
            {
                //get position of first colon
                int colonIdx = line.IndexOf(':');
                if (colonIdx < 0)
                {
                    Console.WriteLine("Flight schedule file has wrong format.");
                    return false;
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
                        Console.WriteLine("Flight schedule file has wrong format.");
                        return false;
                    }

                    // Note: 2 ways to go here, string ops or regex.
                    // String ops usually outperform regex except in very complicated cases.
                    // Regex makes for cleaner code. 
                    // Assumption: flight schedules are very large due to being planned well in advance.
                    // I opted for performance with string ops.

                    // Get flight number
                    string flight = line.Substring(7, colonIdx - 7);
                    int flightNum = Int32.Parse(flight);

                    // looking for instances of brackets to determine departure/arrival cities
                    // first set of brackets contains departure, second contains arrival
                    int firstOpenBracket = line.IndexOf('(');
                    int secondOpenBracket = line.IndexOf('(', firstOpenBracket + 1);
                    string departure = line.Substring(firstOpenBracket + 1, AIRPORT_CODE_LEN);
                    string destination = line.Substring(secondOpenBracket + 1, AIRPORT_CODE_LEN);

                    //add to schedule.

                    flightSchedule.Add(new Flight(flightNum, departure, destination, day));

                }
            }

            return true;
        }


        /// <summary>
        /// Display the flight schedule in the following format on the console:
        /// Flight: 1, departure: YUL, arrival: YYZ, day: 1
        /// ...
        /// Flight: 6, departure: <departure_city>, arrival: <arrival_city>, day: x
        /// </summary>
        public void Display()
        {
            if (flightSchedule.Count == 0)
            {
                Console.WriteLine("There are no flights to display.");
                return;
            }

            foreach (Flight fl in flightSchedule)
            {
                fl.PrintToConsole();
            }
        }

        /// <summary>
        /// Find the next available flight with room on it for another package
        /// </summary>
        /// <param name="destination">Desired destination</param>
        /// <returns>Next flight with room, if available, if not, null.</returns>
        public Flight GetNextAvailableFlight(string destination)
        {
            foreach (Flight fl in flightSchedule)
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
        private int flightNum = -1;
        private string departure = null;
        private string destination = null;
        private int day = -1;
        private int scheduledOrderNum = 0;

        public string Destination
        {
            get { return destination; }
        }

        public int FlightNum
        {
            get { return flightNum; }
        }

        public int Day
        {
            get { return day; }
        }

        public Flight(int _flightNum, string _departure, string _destination, int _day)
        {
            flightNum = _flightNum;
            departure = _departure;
            destination = _destination;
            day = _day;
        }

        /// <summary>
        /// Check if this flight is under capacity
        /// </summary>
        /// <returns>true if under capacity, false if at capacity</returns>
        public bool HasRoom()
        {
            return scheduledOrderNum < CAPACITY;
        }

        /// <summary>
        /// Increments the scheduledOrders if there is capacity
        /// </summary>
        /// <returns>true if order was added, false if not</returns>
        public bool AddOrder()
        {
            if (HasRoom())
            {
                scheduledOrderNum++;
                return true;
            }

            return false;
        }

        public void PrintToConsole()
        {
            Console.WriteLine("Flight: {0}, departure: {1}, arrival: {2}, day: {3}", flightNum, departure, destination, day);
        }

    }

    class Order
    {
        private string orderID;
        private string departure = "YUL";  //this exercise has all orders leaving from Montreal, this will be the default departure
        private string destination = null;
        private Flight schedFlight = null; //use null to denote unscheduled

        public string OrderID
        {
            get { return orderID; }
            set { orderID = value; }
        }
        public string Departure
        {
            get { return departure; }
            set { departure = value; }
        }

        public string Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        public Flight SchedFlight
        {
            get { return schedFlight; }
            set { schedFlight = value; }
        }

        public void PrintToConsole()
        {
            if (SchedFlight == null)  //unscheduled
            {
                Console.WriteLine("order: {0}, flightNumber: not scheduled", orderID);
            }
            else
            {
                Console.WriteLine("order: {0}, flightNumber: {1}, departure: {2}, arrival: {3}, day: {4}",
                    orderID, schedFlight.FlightNum, departure, destination, schedFlight.Day);
            }
        }
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
                Flight fl = flightSched.GetNextAvailableFlight(orderDict[orderID].Destination);
                if (fl != null)
                {
                    orderDict[orderID].SchedFlight = fl;
                    fl.AddOrder();
                }
            }
        }

        /// <summary>
        /// Display all orders' status
        /// </summary>
        public void Display()
        {
            if (orderDict.Count == 0)
            {
                Console.WriteLine("There are no orders to display");
                return;
            }
            
            foreach (string orderID in orderDict.Keys)
            {
                orderDict[orderID].PrintToConsole();
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            FlightSchedule flightSched = new FlightSchedule();
            if (flightSched.Load(args[0]))
            {
                flightSched.Display();

                // get order schedule json
                string json = File.ReadAllText(args[1]);
                Dictionary<string, Order> orders = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json);

                //add order IDs to objects - the given json uses the order ID as a dynamic key, so it can't be deserialized into the class directly.
                foreach (string orderID in orders.Keys)
                {
                    orders[orderID].OrderID = orderID;
                }

                OrderScheduler orderScheduler = new OrderScheduler(flightSched, orders);
                orderScheduler.Schedule();
                orderScheduler.Display();

            }
        }
    }
}
