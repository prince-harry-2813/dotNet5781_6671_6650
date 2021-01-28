﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BL.BLApi;
using BL.BO;
using DalApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace BL
{
    internal class BLImp : IBL
    {
        
        private IDAL iDal = DalApi.DalFactory.GetIDAL();

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
            bus.LastTreatment = (bus.LastTreatment != DateTime.MinValue) ? bus.LastTreatment : DateTime.Now;
            bus.TotalKM = (bus.TotalKM != null) ? bus.TotalKM : 0;
            bus.LastTreatmentKm = (bus.LastTreatmentKm != null) ? bus.LastTreatmentKm : 0;
            bus.Status = (bus.FuelStatus != 0 && DateTime.Now.Subtract(bus.LastTreatment).Days < 365 &&
                          bus.TotalKM - (int)bus.LastTreatmentKm < 20000 && bus.isActive)
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

        public IEnumerable<Bus> GetBusBy(Predicate<object> predicate)
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
        /// Add new line and add basic information
        /// </summary>
        /// <param name="line">
        /// </param>
       public void AddLine(Line line)
        {
            if (line == null)
                throw new NullReferenceException("Line to add is Null please try again");

            if (line.FirstStation.Station.Code == null || line.FirstStation.Station.Code == 0)
                throw new BadBusStopIDException("First Station Id not added or not exist ", new ArgumentException());

            if (line.LastStation == null || line.LastStation.Station.Code == 0)
                throw new BadBusStopIDException("First Station Id not added or not exist ", new ArgumentException());

            var station = iDal.GetStation(line.FirstStation.Station.Code);
            if (station == null)
                throw new BadBusStopIDException("First Bus stop not exist in the system", null);

           var station2 = iDal.GetStation(line.LastStation.Station.Code);
            if (station2 == null)
                throw new BadBusStopIDException("Last Bus stop not exist in the system", null);

            try
            {
                iDal.AddLine((DO.Line)line.CopyPropertiesToNew(typeof(DO.Line)));
            }
            catch (Exception e)
            {
                throw new BadLineIdException("Line with the same id is already exist", new ArgumentException());
            }
        }

        public void UpdateLine(Line line)
        {
            if (line == null)
                throw new NullReferenceException("Line is Null ");

            var lineToUpdate = new DO.Line();
            line.CopyPropertiesTo(lineToUpdate);
            iDal.UpdateLine(lineToUpdate);
        }

        void IBL.DeleteLine(Line line)
        {
            if (line == null)
            {
                throw new NullReferenceException("Line to delete is Null");
            }

            DO.Line lineToDelete = new DO.Line();
            line.CopyPropertiesTo(lineToDelete);
            iDal.DeleteLine(lineToDelete.Id);
        }

        public Line GetLine(int lineId)
        {
            if (lineId == null || lineId == 0)
                throw new NullReferenceException("Line id is null or not initialized");

            if (lineId < 0)
                throw new BadBusIdException("Line id can't be negative",
                    new ArgumentException("Line id can't be negative"));

            var lineToCopy = iDal.GetLine(lineId);
            return (Line)lineToCopy.CopyPropertiesToNew(typeof(Line));
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
                     

                };
                yield return line;
            }
        }

        public IEnumerable<Line> GetLineBy(Predicate<BO.Line> predicate)
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
       public  void AddBusStop(Station station)
        {
            station.isActive = true;

            if (station.Name.Length == 0) 
            {
                station.Name = "Exemple "+station.Code.ToString();
            }
            if (station.Code==0)
            {
                throw new BadBusStopIDException("bus stop number can't be 0", new ArgumentException());
            }
            if (station.Longitude<34.3||station.Longitude>35.5)
            {
                station.Longitude= double.Parse((new Random(DateTime.Now.Millisecond).NextDouble() * 1.2 + 34.3).ToString().Substring(0, 8));
            }
            if (station.Latitude <= 31 || station.Latitude >= 33.3)
            {
                station.Latitude = double.Parse((new Random(DateTime.Now.Millisecond).NextDouble() * 2.3 + 31).ToString().Substring(0,8));
            }
            try
            {
                iDal.AddStation((DO.Station)station.CopyPropertiesToNew(typeof(DO.Station)));
            }
            catch (Exception e)
            {

                throw new ArgumentException("check details",e);
            }
            Console.WriteLine("wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww");
        }

       public void UpdateBusStop(Station station)
        {
            DO.Station DOstation = new DO.Station();
            station.CopyPropertiesTo(DOstation);
            iDal.UpdateStation(DOstation);
        }

       public void eleteBusStop(Station station)
        {
            DO.Station DOstation = new DO.Station();
            station.CopyPropertiesTo(DOstation);
            iDal.DeleteStation(DOstation.Code);
        }

        public Station GetBusStop(int lineId)
        {
            Station station = new Station();
            iDal.GetStation(lineId).CopyPropertiesTo(station);
            return station;
        }

        public IEnumerable<Station> GetAllBusStops()
        {
            foreach (var VARIABLE in iDal.GetAllStation())
            {
                yield return (Station)VARIABLE.CopyPropertiesToNew(typeof(Station));
            }
        }

       public IEnumerable<Station> GetBusStopsBy(Predicate<Station> predicate)
        {
            throw new NotImplementedException();
        }

       public IEnumerable<Bus> GetBusBy(Predicate<Bus> predicate)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region User Simulation

        event Action<TimeSpan> clockObserver = null;
        private DispatcherTimer simulationTimer = new DispatcherTimer();
        internal volatile bool Cancel ;

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
            TimeSpan simulatorTime = new TimeSpan(TimeSpan.FromSeconds(startTime.TotalSeconds).Days , 
                TimeSpan.FromSeconds(startTime.TotalSeconds).Hours ,
                TimeSpan.FromSeconds(startTime.TotalSeconds).Minutes 
                , TimeSpan.FromSeconds(startTime.TotalSeconds).Seconds
                , TimeSpan.FromSeconds(startTime.TotalSeconds).Milliseconds);
            {
                simulationTimer.Interval = new TimeSpan(0, 0, 0, 0, (1000 / (rate * (10 / 6)) ) );
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
                    Debug.Print(simulatorTime.ToString());
                };
                simulationTimer.Start();
            };
        }

        public void StopSimulator()
        {
            Cancel = true;
        }

        public void SetStationPanel(int station, Action<LineTiming> updateBus)
        {

        }



        #endregion

        #region Line Station

        public LineStation GetLineStation(int lineId, int stationCode)
        {
            LineStation station = new LineStation();
            iDal.GetLineStation(lineId, stationCode).CopyPropertiesTo(station);
            return (LineStation)station;
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
            foreach (var item in GetAllLinesStation())
            {
                LineStation lineStation = new LineStation();
                item.CopyPropertiesTo(lineStation);
                if (predicate(lineStation))
                {
                    yield return (LineStation)lineStation;
                }
            }
        }

        public void AddLine(LineStation lineStation)
        {
            iDal.AddLine((DO.LineStation)lineStation.CopyPropertiesToNew(typeof(DO.LineStation)));
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
                LineStation boLineStation = (LineStation) a.CopyPropertiesToNew(typeof(LineStation));
                update(boLineStation);
                boLineStation.CopyPropertiesTo(a);
                iDal.UpdateLineStation(a);
            }
        }
        #endregion
    }
}

