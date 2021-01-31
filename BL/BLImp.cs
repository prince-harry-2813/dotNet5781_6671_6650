﻿using BL.BLApi;
using BL.BO;
using DalApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace BL
{
    internal class BLImp : IBL
    {

        private static IDAL iDal = DalApi.DalFactory.GetIDAL();

        #region IBL Bus Implementation

        /// <summary>
        /// Add New bus and sign unsigned properties appropriately 
        /// </summary>
        /// <param name="bus"></param>
        public void AddBus(Bus bus)
        {
            if (bus == null)
                throw new ArgumentNullException("Bus to Add is Null");

            if (bus.LicenseNum == 0 || bus.LicenseNum == null)
                throw new BadBusIdException("License number not initialized ", null);

            bus.FuelStatus = (bus.FuelStatus != null) ? bus.FuelStatus : 1200;
            bus.isActive = true;
            bus.LastTreatmentDate = (bus.LastTreatmentDate != DateTime.MinValue) ? bus.LastTreatmentDate : DateTime.Now;
            bus.TotalKM = (bus.TotalKM != null) ? bus.TotalKM : 0;
            bus.KmOnLastTreatment = (bus.KmOnLastTreatment != null) ? bus.KmOnLastTreatment : 0;
            bus.Status = (bus.FuelStatus != 0 && DateTime.Now.Subtract(bus.LastTreatmentDate).Days < 365 &&
                          bus.TotalKM - (int)bus.KmOnLastTreatment < 20000 && bus.isActive)
                ? BusStatusEnum.Ok
                : BusStatusEnum.Not_Available;



            try // checks if there is any bus return from DS by the license number 
            {

                DO.Bus busToAdd = new DO.Bus();
                bus.CopyPropertiesTo(busToAdd);
                iDal.AddBus(busToAdd);
            }
            catch (Exception)
            {
                throw new BadBusIdException("Bus With the same license number is already exist", null);
            }

        }

        /// <summary>
        /// Copy bus to DO property and send it do DAL to mark as not active
        /// </summary>
        /// <param name="bus"></param>
        public void DeleteBus(Bus bus)
        {
            if (bus == null)
            {
                throw new NullReferenceException("Bus to delete is Null");
            }

            DO.Bus busToDelete = new DO.Bus();
            bus.CopyPropertiesTo(busToDelete);
            iDal.DeleteBus(busToDelete.LicenseNum);
        }

        public IEnumerable<Bus> GetAllBuses()
        {
            // TODO :  check if this solution is good enough
            foreach (var VARIABLE in iDal.GetAllBuses())
            {
                //if (VARIABLE.isActive)  //deleted because DAL does this check    // Ignore deleted bus  

                yield return (Bus)VARIABLE.CopyPropertiesToNew(typeof(BO.Bus));

            }

            #region Seconed Solution

            //List<Bus> busesToreturn = new List<Bus>();
            //IEnumerable<DO.Bus> busesToCopy = iDal.GetAllBuses();
            //foreach (var bus in busesToCopy)
            //{
            //    busesToreturn.Add((Bus) bus.CopyPropertiesToNew(typeof(BO.Bus)));
            //}

            //return busesToreturn;

            #endregion
        }

        public Bus GetBus(int licenseNum)
        {
            if (licenseNum == null || licenseNum == 0)
                throw new NullReferenceException("license number is null or not initialized");

            if (licenseNum < 0)
                throw new BadBusIdException("Bus license number can't be negative",
                    new ArgumentException("Bus license number can't be negative"));

            if (licenseNum <= 999999 || licenseNum >= 100000000)
                throw new BadBusIdException("Bus license number is too large or too small",
                    new ArgumentException("Bus license number is too large or too small"));

            var busToCopy = iDal.GetBus(licenseNum);
            return (Bus)busToCopy.CopyPropertiesToNew(typeof(Bus));
        }

        public IEnumerable<Bus> GetBusBy(Predicate<Bus> predicate)
        {
            return null;
        }

        public void UpdateBus(Bus bus)
        {
            if (bus == null)
                throw new NullReferenceException("Bus is Null ");

            var busToUpdate = new DO.Bus();
            bus.CopyPropertiesTo(busToUpdate);
            iDal.UpdateBus(busToUpdate);
        }

        #endregion

        #region Line Implementation

        /// <summary>
        /// Add new line to system, also creating new lines stations and connect them, and add basic information for all dependencies
        /// </summary>
        /// <param name="line">
        /// </param>
        public void AddLine(Line line)
        {
            if (line == null)
                throw new NullReferenceException("Line to add is Null please try again");

            if (line.FirstStation == null || line.FirstStation.Station.Code == 0)
                throw new BadBusStopIDException("First Station Id not added or not exist ", new ArgumentException());

            if (line.LastStation == null || line.LastStation.Station.Code == 0)
                throw new BadBusStopIDException("First Station Id not added or not exist ", new ArgumentException());

            var station = iDal.GetStation(line.FirstStation.Station.Code);
            if (station == null)
                throw new BadBusStopIDException("First Bus stop not exist in the system", null);

            var station2 = iDal.GetStation(line.LastStation.Station.Code);
            if (station2 == null)
                throw new BadBusStopIDException("Last Bus stop not exist in the system", null);

            var FirstStID = line.FirstStation.Station.Code; //1st station code to insert
            var LastStID = line.LastStation.Station.Code;//2nd station code to insert

            //adding new line-stations to system  
            try
            {
                iDal.AddLineStation(new DO.LineStation()
                {
                    LineId = line.Id,
                    LineStationIndex = 0,
                    isActive = true,
                    StationId = FirstStID,
                    NextStation = LastStID
                });
                iDal.AddLineStation(new DO.LineStation()
                {
                    LineId = line.Id,
                    LineStationIndex = 1,
                    isActive = true,
                    StationId = LastStID,
                    PrevStation = FirstStID
                });
                //check if there is already an adjacent stations
                var adjSta = iDal.GetAllAdjacentStationsBy(a => a.Station1 == FirstStID && a.Station2 == FirstStID);
                if (adjSta.Count() == 0)
                {
                    iDal.AddAdjacentStations(new DO.AdjacentStations()
                    {
                        isActive = true,
                        Station1 = FirstStID,
                        Station2 = LastStID,
                        Distance = CalculateDistance(line.FirstStation.Station, line.LastStation.Station),
                        PairId = iDal.GetAllAdjacentStationsBy(l => l.isActive || !l.isActive).Count() + 1,
                        Time = CalculateTime(CalculateDistance(line.FirstStation.Station, line.LastStation.Station))
                    });

                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Couldn't add First and last Stations, Check inner exception", e);
            }

            try
            {
                iDal.AddLine(new DO.Line()
                {
                    LastStation = LastStID,
                    Area = (DO.Area)line.Area,
                    Code = line.Code,
                    FirstStation = FirstStID,
                    Id = line.Id,
                    isActive = true
                });
            }
            catch (Exception e)
            {
                throw new BadLineIdException("Couldn't add Line to System, check inner exception", e);
            }
        }

        /// <summary>
        /// update exist system line, adding or removing some line-stations, calculating the line trip duration 
        /// </summary>
        /// <param name="line"></param>
        public void UpdateLine(Line line)
        {
            if (line == null)

                throw new NullReferenceException("Line is Null ");
            //clean the system to update line-stations
            foreach (var item in iDal.GetAllLinesStationBy(l => l.isActive && l.LineId == line.Id))
            {
                iDal.DeleteLineStation(line.Id, item.StationId);
            }
            //update the line- stations
            foreach (var item in line.Stations)
            {
                var DOLineStation = new DO.LineStation()
                {
                    LineId = line.Id,
                    LineStationIndex = item.LineStationIndex,
                    isActive = true,
                    StationId = item.Station.Code,
                    NextStation = item.LineStationIndex == line.Stations.Count() - 1 ? 0 : line.Stations.ElementAt(item.LineStationIndex + 1).Station.Code,
                    PrevStation = item.LineStationIndex > 0 ? line.Stations.ElementAt(item.LineStationIndex - 1).Station.Code : 0,
                };
                try
                {
                    iDal.AddLineStation(DOLineStation);
                }
                catch (Exception e)
                {
                    throw new BadBusStopIDException("Couldn't update or add, check inner Exceptions details", e);
                }
            }
            var lineToUpdate = new DO.Line()
            {
                LastStation = line.LastStation.Station.Code,
                Area = (DO.Area)line.Area,
                Code = line.Code,
                FirstStation = line.FirstStation.Station.Code,
                Id = line.Id,
                isActive = true
            };
            try
            {
                iDal.UpdateLine(lineToUpdate);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Couldn't update line, Check inner Exception details", e);
            }
        }

        public void DeleteLine(Line line)
        {
            if (line == null)
            {
                throw new NullReferenceException("Line to delete is Null");
            }
            DO.Line lineToDelete = new DO.Line()
            {
                LastStation = line.LastStation.Station.Code,
                FirstStation = line.FirstStation.Station.Code,
                isActive = line.IsActive,
                Id = line.Id,
                Code = line.Code,
                Area = (DO.Area)line.Area
            };
            try
            {
                iDal.DeleteLine(lineToDelete.Id);
                //clean the system list of line stations 
                foreach (var item in iDal.GetAllLinesStationBy(l => l.isActive && l.LineId == line.Id))
                {
                    iDal.DeleteLineStation(line.Id, item.StationId);
                }
            }
            catch (Exception e)
            {
                throw new KeyNotFoundException("Couldn't complete operation, check inner exception", e);
            }
        }

        public Line GetLine(int lineId)
        {
            if (lineId == null || lineId == 0)
                throw new NullReferenceException("Line id is null or not initialized");

            if (lineId < 0)
                throw new BadBusIdException("Line id can't be negative",
                    new ArgumentException("Line id can't be negative"));

            var DalLine = iDal.GetLine(lineId);
            Line line = new Line()
            {
                FirstStation = new LineStation(),
                LastStation = new LineStation(),
                Stations = new List<LineStation>(),
                Area = (BO.Area)DalLine.Area,
                Code = DalLine.Code,
                Id = DalLine.Id,
                IsActive = true
            };
            var DOLineStations = iDal.GetAllLinesStationBy(l => l.isActive && l.LineId == lineId).OrderBy(l => l.LineStationIndex);
            foreach (var item in DOLineStations)
            {
                var BOLS = new LineStation()
                {
                    LineId = line.Id,
                    LineStationIndex = item.LineStationIndex,
                    Station = GetStation(item.StationId),
                    PrevStation = item.LineStationIndex - 1 < 0 ? 0 : DOLineStations.ElementAt(item.LineStationIndex - 1).StationId,
                    NextStation = item.LineStationIndex + 1 == DOLineStations.Count() ? 0 : DOLineStations.ElementAt(item.LineStationIndex + 1).StationId,
                    isActive = true
                };
                if (BOLS.LineStationIndex == 0)
                    BOLS.CopyPropertiesTo(line.FirstStation);
                if (BOLS.LineStationIndex == DOLineStations.Count() - 1)
                    BOLS.CopyPropertiesTo(line.LastStation);
                line.Stations.Append(BOLS);

            }

            return line;
        }

        public IEnumerable<Line> GetAllLines()
        {
            foreach (var VARIABLE in iDal.GetAllLines())
            {
                var line = new Line()
                {
                    Id = VARIABLE.Id,
                    Code = VARIABLE.Code,
                    Area = (Area)VARIABLE.Area,
                    IsActive = VARIABLE.isActive,
                    LastStation = GetLineStation(VARIABLE.Id, VARIABLE.LastStation),
                    FirstStation = GetLineStation(VARIABLE.Id, VARIABLE.FirstStation),

                    Stations = from LS in GetAllLinesStationBy(l => l.isActive && l.LineId == VARIABLE.Id)
                               orderby LS.LineStationIndex
                               select LS,

                };
                yield return line;
            }
        }

        public IEnumerable<Line> GetLinesBy(Predicate<BO.Line> predicate)
        {
            foreach (var item in iDal.GetAllLinesBy(l => l.isActive || !l.isActive))
            {

                BO.Line line = (Line)item.CopyPropertiesToNew(typeof(Line));
                if (predicate(line))
                    yield return line;

            }
        }


        #endregion

        #region Bus Stop Implementation
        public void AddStation(Station station)
        {
            station.isActive = true;

            if (station.Name.Length == 0)
            {
                station.Name = "Example " + station.Code.ToString();
            }
            if (station.Code == 0)
            {
                throw new BadBusStopIDException("bus stop number can't be 0", new ArgumentException());
            }
            if (station.Longitude < 34.3 || station.Longitude > 35.5)
            {
                station.Longitude = double.Parse((new Random(DateTime.Now.Millisecond).NextDouble() * 1.2 + 34.3).ToString().Substring(0, 8));
            }
            if (station.Latitude <= 31 || station.Latitude >= 33.3)
            {
                station.Latitude = double.Parse((new Random(DateTime.Now.Millisecond).NextDouble() * 2.3 + 31).ToString().Substring(0, 8));
            }
            try
            {
                iDal.AddStation((DO.Station)station.CopyPropertiesToNew(typeof(DO.Station)));
            }
            catch (Exception e)
            {

                throw new ArgumentException("check details", e);
            }
            Console.WriteLine("wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww");
        }

        public void UpdateStation(Station station)
        {
            DO.Station DOstation = new DO.Station();
            station.CopyPropertiesTo(DOstation);
            iDal.UpdateStation(DOstation);
        }

        public void DeleteStation(Station station)
        {
            DO.Station DOstation = new DO.Station();
            station.CopyPropertiesTo(DOstation);
            iDal.DeleteStation(DOstation.Code);
        }

        public Station GetStation(int lineId)
        {
            Station station = new Station();
            iDal.GetStation(lineId).CopyPropertiesTo(station);
            return station;
        }

        public IEnumerable<Station> GetAllStations()
        {
            foreach (var VARIABLE in iDal.GetAllStation())
            {
                yield return (Station)VARIABLE.CopyPropertiesToNew(typeof(Station));
            }
        }

        public IEnumerable<Station> GetStationBy(Predicate<Station> predicate)
        {
            throw new NotImplementedException();
        }



        #endregion

        #region User Simulation

        event Action<TimeSpan> clockObserver = null;
        private DispatcherTimer simulationTimer = new DispatcherTimer();
        internal volatile bool Cancel;

        /// <summary>
        /// Start simulator stop watch and update it according 
        /// </summary>
        /// <param name="startTime">TIME TO START  </param>
        /// <param name="Rate"> Hz per minute</param>
        /// <param name="updateTime">Action</param>
        public void StartSimulator(TimeSpan startTime, int rate, Action<TimeSpan> updateTime)
        {
            Cancel = false;
            clockObserver = updateTime;
            TimeSpan simulatorTime = new TimeSpan(TimeSpan.FromSeconds(startTime.TotalSeconds).Days,
                TimeSpan.FromSeconds(startTime.TotalSeconds).Hours,
                TimeSpan.FromSeconds(startTime.TotalSeconds).Minutes
                , TimeSpan.FromSeconds(startTime.TotalSeconds).Seconds
                , TimeSpan.FromSeconds(startTime.TotalSeconds).Milliseconds);

            simulationTimer.Interval = new TimeSpan(0, 0, 0, 0, (1000 / (rate * (10 / 6))));
            //rideOperation.interval = simulationTimer.Interval.Milliseconds;
            simulationTimer.Tick += (sender, args) =>
            {
                if (Cancel)
                {
                    clockObserver = null;
                    simulationTimer.Stop();
                    return;
                }

                simulatorTime += TimeSpan.FromSeconds(1);
                updateTime.Invoke(simulatorTime);
                //rideOperation.UpdateSimualtionTime(simulatorTime);
                Debug.Print(simulatorTime.ToString());
            };
            simulationTimer.Start();

        }





        #endregion

        #region Line Station

        public LineStation GetLineStation(int lineId, int stationCode)
        {
            LineStation station = new LineStation();
            iDal.GetLineStation(lineId, stationCode).CopyPropertiesTo(station);
            station.Station = (BO.Station)iDal.GetStation(stationCode).CopyPropertiesToNew(typeof(BO.Station));
            return station;
        }

        public IEnumerable<LineStation> GetAllLinesStation()
        {
            foreach (var item in iDal.GetAllLinesStation())
            {
                yield return (LineStation)item.CopyPropertiesToNew(typeof(BO.LineStation));
            }
        }

        public IEnumerable<LineStation> GetAllLinesStationBy(Predicate<BO.LineStation> predicate)
        {
            foreach (var item in iDal.GetAllLinesStationBy(l => l.isActive || !l.isActive))
            {
                LineStation lineStation = new LineStation();
                item.CopyPropertiesTo(lineStation);

                lineStation.Station = new Station();
                lineStation.Station = GetStation(item.StationId);
                lineStation.Station.Lines = new List<Line>();
                if (predicate(lineStation))
                {
                    yield return lineStation;
                }
            }
        }

        public void AddLineStation(LineStation lineStation)
        {
            iDal.AddLineStation((DO.LineStation)lineStation.CopyPropertiesToNew(typeof(DO.LineStation)));
        }

        public void UpdateLineStation(LineStation lineStation)
        {
            iDal.UpdateLineStation((DO.LineStation)lineStation.CopyPropertiesToNew(typeof(DO.LineStation)));
        }

        public void UpdateLineStation(int lineId, int stationCode, Action<LineStation> update)
        {
            var a = (DO.LineStation)iDal.GetAllLinesStationBy(station => station.LineId == lineId && station.StationId == stationCode).FirstOrDefault();
            if (!(a is null))
            {
                LineStation boLineStation = (LineStation)a.CopyPropertiesToNew(typeof(LineStation));
                update(boLineStation);
                boLineStation.CopyPropertiesTo(a);
                iDal.UpdateLineStation(a);
            }
        }

        #endregion

        #region Ride Operation

        public void StopSimulator()
        {
            Cancel = true;
        }

        private RideOperation rideOperation = RideOperation.Instance;

        public void SetStationPanel(int station, Action<LineTiming> updateBus)
        {
            if (station == -1)
            {
                //TODO: Shut down
            }

            rideOperation.StartSimulation();
        }


        #endregion
        #region Utilities

        /// <summary>
        /// calculate the distance between previous station to current
        /// This uses the Haversine formula to calculate the short distance between tow coordinates on sphere surface  
        /// </summary>
        /// <param name="other"> previous or other station </param>
        /// <returns>Short distance in meters </returns>
        public double CalculateDistance(Station st1, Station st2)
        {
            double earthRadius = 6371e3;
            double l1 = st1.Latitude * (Math.PI / 180);
            double l2 = st2.Latitude * (Math.PI / 180);
            double l1_2 = (st2.Latitude - st1.Latitude) * (Math.PI / 180);
            double lo_1 = (st2.Longitude - st1.Longitude) * (Math.PI / 180);
            double a = (Math.Sin(l1_2 / 2) * Math.Sin(l1_2 / 2)) +
                       Math.Cos(l1) * Math.Cos(l2) *
                       (Math.Sin(lo_1 / 2) * Math.Sin(lo_1 / 2));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var Distance = (earthRadius * c);
            return Distance;
        }



        private TimeSpan CalculateTime(Double Distance)
        {
            TimeSpan time = TimeSpan.Zero;
            double rideDistance = 0.0 + Distance / 1000;
            time = TimeSpan.FromMinutes((rideDistance / 28.0d) * 60);
            time += TimeSpan.FromMinutes(2);
            return time;
        }

        #endregion
    }

    public class RideOperation
    {

        private static RideOperation instance;

        public static RideOperation Instance
        {
            get => instance;
            set
            {
                if (Instance == null)
                {
                    value = new RideOperation();
                }

                instance = value;
            }
        }

        public int interval;
        private event EventHandler<LineTiming> updatebusPrivate;
        private int staionID;
        List<LineTrip> linesTrips = new List<LineTrip>();
        private IDAL idal;
        private IBL bl;
        private BackgroundWorker getLineStaionworker = new BackgroundWorker();
        private TimeSpan simulationTime;
        List<BusOnTrip> busesOnTrips = new List<BusOnTrip>();

        public RideOperation()
        {
            idal = DalFactory.GetIDAL();
            this.bl = bl;

            if (getLineStaionworker.IsBusy)
            {
                getLineStaionworker.CancelAsync();
            }

            #region Rides Operation Worker Initialization

            getLineStaionworker.WorkerReportsProgress = true;
            getLineStaionworker.WorkerReportsProgress = true;
            getLineStaionworker.DoWork += (sender, args) =>
            {
                int i = 0;
                foreach (var item in idal.GetAllLinesTripBy(trip => trip.isActive))
                {
                    if (getLineStaionworker.CancellationPending)
                        break;

                    getLineStaionworker.ReportProgress(i, item.CopyPropertiesToNew(typeof(LineTrip)));
                    i++;
                    if (i == 99)
                        i = 90;
                }
            };
            getLineStaionworker.ProgressChanged += (sender, args) => { linesTrips.Add((LineTrip)args.UserState); };

            #endregion

            getLineStaionworker.RunWorkerCompleted += (sender, args) =>
            {
                linesTrips.Sort((trip, lineTrip) => trip.StartAt.CompareTo(lineTrip.StartAt));

                int i = 0;
                foreach (var VARIABLE in linesTrips)
                {
                    //Thread.SpinWait((int)VARIABLE.Frequency.TotalSeconds);
                    busesOnTrips.Add(new BusOnTrip()
                    {
                        ActualTakeOff = simulationTime,
                        Id = i,
                        LineId = VARIABLE.LineId,
                        isActive = true,
                        LicenseNum = idal.GetAllBusesBy(bus => bus.Status == DO.BusStatusEnum.Ok).FirstOrDefault()
                            .LicenseNum,
                        //NextStationAt = idal.GetAdjacentStations()
                    });

                    if (simulationTime.Subtract(VARIABLE.StartAt).TotalSeconds > 0)
                    {
                        busesOnTrips.Add(new BusOnTrip()
                        {

                        });
                    }

                    i++;
                }
            };

            getLineStaionworker.RunWorkerAsync();
        }

        public void StartSimulation()
        {
            foreach (var item in linesTrips)
            {
                //   for
                Task.Factory.StartNew(() =>
                {
                    LineTiming lineTiming = new LineTiming()
                    {
                        // LastStation = (LineStation)idal.GetStation(idal.GetLineStation(item.LineId).LastStation)
                        //   .CopyPropertiesToNew(typeof(Station))
                        //,ArrivingTime = 
                    };
                });
            }



     
        }
    }
}

