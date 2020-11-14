﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

namespace dotNet5781_02_6671_6650
{
    class Program
    {
        #region properties
        /// <summary>
        /// initial collection of bus line
        /// </summary>
        internal static LinesCollection systemCollection = new LinesCollection();

        public static Random Random = new Random();

        /// <summary>
        /// Boolean to exit out of the program 
        /// </summary>
        public static bool Flag { get; set; } = true;

        /// <summary>
        /// User option selector
        /// </summary>
        private static int option = -1;
        #endregion

        #region operations
        /// <summary>
        /// Method to add stop to bus if th user want to add it with explicit parameters  
        /// </summary>
        /// <param name="line">Line code</param>
        /// <param name="stop">Station code</param>
        public static void AddStopToLineExplicit(int line, int stop)
        {
            double[] location = new double[2];
            Console.WriteLine("Enter latitude");
            double.TryParse(Console.ReadLine(), out location[0]);
            Console.WriteLine("Enter longitude");
            double.TryParse(Console.ReadLine(), out location[1]);
            Console.WriteLine("Enter area");
            systemCollection[line].AddStop(new BusStop(stop, location[0], location[1], "" + Console.ReadLine()));
        }
        /// <summary>
        /// Use to register new line to main collection of line's
        /// </summary>
        private static void RegisterNewBus()
        {
            Console.WriteLine("Please enter the new line number");
            int[] input = new int[3];
            int.TryParse(Console.ReadLine(), out input[0]);
            Console.WriteLine("Please enter first station Id");
            int.TryParse(Console.ReadLine(), out input[1]);
            Console.WriteLine("Please enter last station Id");
            int.TryParse(Console.ReadLine(), out input[2]);
            var newBus = new BusLine(input[0], input[1], input[2]);
            systemCollection.Add(newBus);
        }

        /// <summary>
        /// method to adding stop to line.
        /// this method check if the new station is already exist, assign it (with the same details) to this line 
        /// </summary>
        private static void AddStopToLine(int line)
        {
            Console.WriteLine($"Please enter the station number");
            int.TryParse(Console.ReadLine(), out int stopCode);
            foreach (BusLine item in systemCollection)
            {
                if (item.IsExist(stopCode))
                {
                    systemCollection[line].AddStop(item.LineStations.FirstOrDefault(stop => stop.StationCode == (stopCode)));
                    return;
                }
            }
            Console.WriteLine("Do you want to assign the properties of this station? choose no if you want to generate it randomly (Y/N)");
            switch ("" + Console.ReadLine().ToUpper())
            {
                case "YES":
                case "Y":
                    AddStopToLineExplicit(line, stopCode);
                    return;
                default:
                    break;
            }
            systemCollection[line].AddStop(new BusStop(stopCode));
        }
        /// <summary>
        /// the method 
        /// </summary>
        private static void RemoveLine()
        {
            Console.WriteLine("Please enter the line number");
            int[] input = new int[2];
            int.TryParse(Console.ReadLine(), out input[0]);
            systemCollection.Remove(systemCollection[input[0]]);
        }
        /// <summary>
        /// 
        /// </summary>
        private static void RemoveStopfromLine()
        {
            Console.WriteLine("Please enter the line number");
            int[] input = new int[2];
            int.TryParse(Console.ReadLine(), out input[0]);
            Console.WriteLine("Please enter station number");
            int.TryParse(Console.ReadLine(), out input[1]);
            systemCollection[input[0]].RemoveStop(input[1]);
        }
        /// <summary>
        /// 
        /// </summary>
        private static void SearchLines()
        {
            Console.WriteLine("Please enter the station number");
            int.TryParse(Console.ReadLine(), out int input);
            foreach (BusLine item in systemCollection)
            {
                if (item.IsExist(input))
                {
                    Console.WriteLine($"Line number : {item.LineKey}.");
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private static void FindRoute()
        {
            Console.WriteLine("Please enter your current station number");
            int[] stop = new int[2];
            int.TryParse(Console.ReadLine(), out stop[0]);
            Console.WriteLine("Please enter your destination station number");
            int.TryParse(Console.ReadLine(), out stop[1]);
            LinesCollection driveOption = new LinesCollection();
            foreach (BusLine item in systemCollection)
            {
                if (item.IsExist(stop[0]) && item.IsExist(stop[1]) && item.LineStations.FindIndex(l => l.StationCode == stop[0]) < item.LineStations.FindIndex(l => l.StationCode == stop[1]))
                {
                    driveOption.Add(item);
                }
            }
            Console.WriteLine($"The fastest best option to arrive your destination:");
            driveOption.SorterLines();
            foreach (BusLine item in driveOption)
            {
                
                Console.WriteLine($"Line number: {item.LineKey}, time of ride on this line: {item.CalculateRideTime(item.LineStations.Find(b => b.StationCode == stop[0]), item.LineStations.Find(b => b.StationCode == stop[1]))}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private static void PrintLinesInfo()
        {
            String output = "\n";
            systemCollection.SorterLines();
            foreach (BusLine item in systemCollection)
            {
                output += "Line number: " +item.LineKey.ToString()+"\n";
            }
            Console.WriteLine(output);
        }
        /// <summary>
        /// Go over every bus in system, using union method to get all stations number one time.
        /// Then for every line check if is have a stop in that station. 
        /// </summary>
        private static void PrintStaionInfo()
        {
            IEnumerable<BusStop> stations = new List<BusStop>();
            String output = "These are all active stations in the system: \n";
            foreach (BusLine item in systemCollection)
            {
                stations = stations.Union(item.LineStations);
            }
            foreach (BusStop stop in stations)
            {
                output += $"This is the lines that have a stop at station number {stop.StationCode}:\n";
                foreach (BusLine item in systemCollection)
                {
                    if (item.IsExist(stop.StationCode))
                    {
                        output += $"- {item.LineKey}\n";
                    }
                }
            }
            Console.WriteLine(output);
        }
        #endregion

        static void Main(string[] args)
        {
            for (int i = 0; i < 20; i++)
            {
                systemCollection.Add(new BusLine(Random.Next(999), Random.Next(999999), Random.Next(999999)));
            }
            foreach (BusLine item in systemCollection)
            {
                for (int i = 0; i < 3; i++)
                item.AddStop(new BusStop(Random.Next(999999)), item.LineStations.Count - 2);
            }
            do
            {
                Console.WriteLine(@"Hello

Please choose an operation that you'll like to operate:

                1 : Register new line
                2 : Add stop with detailed or random location to exist line
                3 : Remove line from system
                4 : Remove stop from line route 
                5 : Search line by station code
                6 : Show route from a station to destination 
                7 : Info: print all bus line in system
                8 : Info: print all stations with their lines
 
                0 : Exit  ");
                int.TryParse(Console.ReadLine(), out option);
                switch (option)
                {
                    case 1:
                        RegisterNewBus();
                        break;
                    case 2:
                        Console.WriteLine("Please enter the line number");
                        AddStopToLine(int.Parse(Console.ReadLine()));
                        break;
                    case 3:
                        RemoveLine();
                        break;
                    case 4:
                        RemoveStopfromLine();
                        break;
                    case 5:
                        SearchLines();
                        break;
                    case 6:
                        FindRoute();
                        break;
                    case 7:
                        PrintLinesInfo();
                        break;
                    case 8:
                        PrintStaionInfo();
                        break;

                    case 0:
                        Console.WriteLine("Good Bye");
                        Console.ReadKey();
                        Flag = false;
                        break;
                    default:

                        Console.WriteLine("Wrong input, please try again");

                        break;
                }

            } while (Flag);
        }
    }
}
