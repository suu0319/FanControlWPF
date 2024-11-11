using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareInfo
    {
        public ISensor PercentageSensor { get; private set; }
        public ISensor RPMSensor { get; private set; }
        public string Name { get; private set; }
        public int RPM { get; private set; }
        public int Percentage { get; private set; }
        public int TargetPercentage { get; private set; }
        public bool Controlable { get; set; } = true;
        public bool Calibrated { get; set; } = false;
        
        /// <summary>
        /// returns Current HardwareInfo and bool for fan is calibrated
        /// </summary>
        public Action<HardwareInfo, bool> Calibrating { get; set; }

        private const int TEN_SECOND = 10000;

        public HardwareInfo(string name, ISensor rpmSensor, ISensor percentageSensor = null)
        {
            Name = name;
            PercentageSensor = percentageSensor;
            RPMSensor = rpmSensor;

            StartGetData();
        }
        
        /// <summary>
        /// Set fan speed percentage
        /// </summary>
        /// <param name="percentage"></param>
        public void SetPercentage(int percentage)
        {
            TargetPercentage = percentage;
            if (PercentageSensor != null && Controlable) 
                PercentageSensor.Control.SetSoftware(percentage);
        }

        public void StartGetData()
        {
            Task.Run(() =>
            {
                while (Controlable)
                {
                    if (RPMSensor != null && PercentageSensor != null)
                    {
                        Percentage = (int)PercentageSensor.Value;
                        RPM = (int)RPMSensor.Value;
                    }
                    Thread.Sleep(200);
                }
            });
        }

        public void SetDefault()
        {
            PercentageSensor.Control.SetDefault();
        }

        
        public void Calibrate()
        {
            if (RPMSensor == null || PercentageSensor == null)
            {
                Controlable = false;
                SetAllDataZero();
                Calibrated = true;
                return;
            }
            
            Task.Run(() => 
            {
                CalibrateCallback();
            });

            Controlable = IsMutableRPM();

            Thread.Sleep(TEN_SECOND);

            Calibrated = true;
            Calibrating?.Invoke(this, Calibrated);
        }

        private bool IsMutableRPM()
        {
            var rpms = new List<int>();

            for (var i = 100; i >= 0; i -= 10)
            {
                SetPercentage(i);
                Thread.Sleep(TEN_SECOND);
                rpms.Add((int)RPMSensor.Value / 100);
            }

            var descendingRPMs = rpms.OrderByDescending(x => x).ToList();

            return descendingRPMs.First() != 0 && descendingRPMs.First() > descendingRPMs.Last();
        }

        /// <summary>
        /// Set all data to zero when the fan is uncontrolable
        /// </summary>
        private void SetAllDataZero()
        {
            TargetPercentage = 0;
            Percentage = 0;
            RPM = 0;
        }

        private void CalibrateCallback()
        {
            while (!Calibrated)
            {
                Calibrating?.Invoke(this, Calibrated);
                Thread.Sleep(500);
            }
        }
    }
}
