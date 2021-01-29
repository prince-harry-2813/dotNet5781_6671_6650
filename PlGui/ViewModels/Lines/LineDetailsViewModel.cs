﻿using BL.BLApi;
using PlGui.StaticClasses;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using BL.BO;

namespace PlGui.ViewModels.Lines
{
    public class LineDetailsViewModel : BindableBase
    {

        #region Properties Declaraion

        private BL.BO.Line line;
        /// <summary>
        /// Hold Bus data 
        /// </summary>
        public BL.BO.Line Line
        {
            get
            {
                return line;
            }
            set
            {
                SetProperty(ref line, value);
            }
        }
        //return Bl.GetBus(1234456 /*TODO: Implement here bus licence Number from the user control sender */)

        private PropertyDetails selectedItem;
        public PropertyDetails SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
            }
        }

        private ObservableCollection<PropertyDetails> lbItemSource;

        public ObservableCollection<PropertyDetails> LbItemSource
        {
            get => lbItemSource;
            set
            {
                SetProperty(ref lbItemSource, value);
            }
        }

        private ObservableCollection<PropertyDetails> busStopsCollection;

        public ObservableCollection<PropertyDetails> BusStopsCollection
        {
            get => busStopsCollection;
            set
            {
                SetProperty(ref busStopsCollection, value);
            }
        }
        
        public int LicenseNumber { get; set; }
        #region Private Members

        private int lineId;
        //private BackgroundWorker insertingSecondListWorker;

        #endregion

        #endregion

        #region Service Deceleration

        private IRegionManager regionManager;

        public IBL Bl { get; set; }

        #endregion

        #region Command deceleration

        public ICommand BusDetailsButtonCommand { get; set; }

        #endregion

        public LineDetailsViewModel(IRegionManager manager, IBL bl)
        {
            #region Service Initialization

            regionManager = manager;
            Bl = bl;

            #endregion

            #region Command Implementation

            BusDetailsButtonCommand = new DelegateCommand<string>(LineDetailsButton);

            #endregion

            #region Properties Implementation

            InsertBusPropertiesToCollection(Line);
            SelectedItem = LbItemSource.FirstOrDefault();

            #endregion
        }

        #region Command Implementation

        private void LineDetailsButton(string commandParameter)
        {
            switch (commandParameter)
            {
                case "Edit":
                    var param = new NavigationParameters();
                    param.Add("Line", Line);
                    regionManager.RequestNavigate(StringNames.MainRegion, StringNames.AddLine, param);
                    break;
                case "Remove":

                    break;
            }
        }

        #endregion

        #region Interface Implementation

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>
        /// passing Parameters to the window 
        /// </summary>
        /// <param name="navigationContext"></param>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Initialize Interface 
            Bl = (IBL)navigationContext.Parameters.Where(pair => pair.Key == StringNames.BL).FirstOrDefault().Value;

            // Initialize View object 
            Line = (BL.BO.Line)navigationContext.Parameters.Where(pair => pair.Key == "Line").FirstOrDefault().Value;
        }

        #endregion

        #region Private Method

        private void InsertBusStopCollection(Line line)
        {
        //    insertingSecondListWorker = new BackgroundWorker();
        //    insertingSecondListWorker.WorkerSupportsCancellation = true;
        //    insertingSecondListWorker.WorkerReportsProgress = true;
        //    insertingSecondListWorker.DoWork += (sender, args) =>
                foreach (var item in line.Stations)
                {
                    
                }
        }

        private void InsertBusPropertiesToCollection(BL.BO.Line line)
        {
            LbItemSource.Clear();
            foreach (PropertyInfo VARIABLE in line.GetType().GetProperties())
            {
                LbItemSource.Add(new PropertyDetails()
                {
                    PropertyType = VARIABLE.PropertyType,
                    PropertyName = VARIABLE.Name,
                    Propertyvalue = VARIABLE.GetConstantValue().ToString()
                });
            }
        }

        private void InsertCollectionToBus()
        {
            foreach (var VARIABLE in Line.GetType().GetProperties())
            {
                var property = LbItemSource.Where(details => details.PropertyName == VARIABLE.Name);

                VARIABLE.SetValue(Line, property.GetEnumerator().Current.Propertyvalue);
            }
        }

        #endregion
    }

    /// <summary>
    ///  Nested Class helper 
    /// </summary>
    public class PropertyDetails : BindableBase
    {
        public Type PropertyType { get; set; }

        private string propertyName;

        public string PropertyName
        {
            get => propertyName;
            set
            {
                SetProperty(ref propertyName, value);
            }
        }

        private string propertyValue;

        public string Propertyvalue
        {
            get => propertyValue;
            set
            {
                SetProperty(ref propertyValue, value);
            }
        }
    }
}

