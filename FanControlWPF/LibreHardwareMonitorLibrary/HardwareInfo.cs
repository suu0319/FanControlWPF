using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitorLibrary.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareInfo
    {
        public string ParentHardwareName { get; private set; }
        public ISensor PercentageSensor { get; private set; }
        public ISensor RPMSensor { get; private set; }
        public string Name { get; private set; }
        public HardwareDeviceType DeviceType { get; private set; }
        public int RPM { get; private set; }
        public int Percentage { get; private set; }
        public int TargetPercentage { get; private set; }
        public Action<HardwareInfo> DataUpdate { get; set; }
        public bool Controlable { get; set; } = true;
        public bool Calibrated { get; set; } = false;
        
        /// <summary>
        /// returns Current HardwareInfo and bool for fan is calibrated
        /// </summary>
        public Action<HardwareInfo, bool> Calibrating { get; set; }

        private const int TEN_SECOND = 10000;

        public HardwareInfo(string name, ISensor sensor)
        {
            Name = name;
            ParentHardwareName = sensor.Hardware.Name;
            if (sensor.Control != null)
                PercentageSensor = sensor;
            else
                RPMSensor = sensor;
            DeviceType = (HardwareDeviceType)sensor.Hardware.HardwareType;

            StartGetData();
        }

        /// <summary>
        /// Add the second sensor into this fan control
        /// </summary>
        /// <param name="sensor"></param>
        public void SetSensor(ISensor sensor)
        {
            if (sensor.Control != null)
            {
                if (PercentageSensor == null)
                    PercentageSensor = sensor;
                else
                    Console.WriteLine($"{Name} set sensor error, detects 2 PercentageSensor");
            }
            else
            {
                if (RPMSensor == null)
                    RPMSensor = sensor;
                else
                    Console.WriteLine($"{Name} set sensor error, detects 2 RPMSensor");
            }
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
                        DataUpdate?.Invoke(this);
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

            Calibrated = false;
            Task.Run(() => 
            {
                CalibrateCallback();
            });

            SetPercentage(100);
            Thread.Sleep(TEN_SECOND);
            int initiateRPM = (int)RPMSensor.Value;
            SetPercentage(10);
            Thread.Sleep(TEN_SECOND);
            Controlable = RPMSensor.Value / initiateRPM < 0.4;

            if (Controlable && DeviceType == HardwareDeviceType.SuperIO)
                SetPercentage(30);
            else if (!Controlable)
                SetAllDataZero();

            Thread.Sleep(TEN_SECOND);

            Calibrated = true;
            Calibrating?.Invoke(this, Calibrated);
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
