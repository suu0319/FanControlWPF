using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareMonitoringHelper
    {
        private static readonly Lazy<HardwareMonitoringHelper> lazy = new Lazy<HardwareMonitoringHelper>(() => new HardwareMonitoringHelper());
        private readonly Computer _computer;
        private readonly UpdateVisitor _updateVisitor = new UpdateVisitor();
        private const int DATA_PULLING_RATE = 200;
        private List<HardwareInfoGroup> _hardwareInfoGroups = new List<HardwareInfoGroup>();
        private bool _startGetData = false;
        private int _initCount = 0;
        public bool IsCalibrated { get; set; } = false;
        public event Action Calibrated;
        public static HardwareMonitoringHelper Instance => lazy.Value;

        public HardwareMonitoringHelper()
        {
            _computer = GetComputer();
            StartGetData();
            GetSensors();
        }

        private Computer GetComputer()
        {
            try
            {
                Computer computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true,
                };

                computer.Open();
                computer.Accept(_updateVisitor);

                return computer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LibreHardware GetComputer has failed, return null, Ex: {ex}");
                return null;
            }
        }

         private void GetSensors()
        {
            _hardwareInfoGroups = new List<HardwareInfoGroup>();

            foreach (IHardware hardware in _computer.Hardware)
            {
                List<HardwareInfo> hardwareInfos = new List<HardwareInfo>();
                List<ISensor> controls;
                List<ISensor> fans;
                
                foreach (var subHardware in hardware.SubHardware)
                {
                    controls = subHardware.Sensors
                        .Where(x => x.SensorType == SensorType.Control && x.Name.Contains("Fan")).ToList();
                    fans = subHardware.Sensors.
                        Where(x => x.SensorType == SensorType.Fan && x.Name.Contains("Fan")).ToList();
                    
                    foreach (var fan in fans)
                    {
                        var control = controls.Find(x => x.Name == fan.Name);

                        if (control != null)
                        {
                            CheckSensorAndCreateHardwareInfo(hardwareInfos, fan, control);
                        }
                        else
                        {
                            CheckSensorAndCreateHardwareInfo(hardwareInfos, fan);
                        }
                    }
                }

                if (hardware.Sensors != null && hardware.Sensors.Length > 0)
                {
                    controls = hardware.Sensors
                        .Where(x => x.SensorType == SensorType.Control && x.Name.Contains("GPU Fan")).ToList();
                    fans = hardware.Sensors
                        .Where(x => x.SensorType == SensorType.Fan && x.Name.Contains("GPU")).ToList();
                
                    for (var i = 0; i < fans.Count; i++)
                    {
                        if (controls.Count > i)
                        {
                            CheckSensorAndCreateHardwareInfo(hardwareInfos, fans[i], controls[i]);
                        }
                        else
                        {
                            CheckSensorAndCreateHardwareInfo(hardwareInfos, fans[i]);
                        }
                    }
                }
                
                _hardwareInfoGroups.Add(new HardwareInfoGroup(hardwareInfos));
            }
        }

        public void RunCalibration()
        {
            IsCalibrated = false;
            foreach (HardwareInfoGroup group in _hardwareInfoGroups)
            {
                foreach (HardwareInfo hardwareInfo in group.HardwareInfos)
                {
                    Task.Run(() =>
                    {
                        hardwareInfo.Calibrate();
                    });
                    Thread.Sleep(300); //for threading issue
                }
            }

            CheckIfCalibrationIsDone();
        }

        private void CheckIfCalibrationIsDone()
        {
            bool calibrationIsFinished = false;
            Task.Run(() =>
            {
                while (!calibrationIsFinished)
                {
                    calibrationIsFinished = true;
                    foreach (HardwareInfoGroup group in _hardwareInfoGroups)
                    {
                        foreach (HardwareInfo hardwareInfo in group.HardwareInfos)
                        {
                            calibrationIsFinished &= hardwareInfo.Calibrated;
                        }
                    }
                }
                IsCalibrated = true;
                Calibrated?.Invoke();
            });
        }

        private void CheckSensorAndCreateHardwareInfo(List<HardwareInfo> hardwareInfos, ISensor rpmSensor, ISensor percentageSensor = null)
        {
            var name = rpmSensor.Name;
            var hardwareInfo = new HardwareInfo(name, rpmSensor, percentageSensor);
            
            hardwareInfos.Add(hardwareInfo);
        }

        private void StartGetData()
        {
            _startGetData = true;
            Task.Run(() =>
            {
                while (_startGetData)
                {
                    if (_computer != null)
                    {
                        _updateVisitor.VisitComputer(_computer);
                        Thread.Sleep(DATA_PULLING_RATE);
                    }
                }
            });
        }

        public List<HardwareInfoGroup> GetHardwareInfoGroups()
        {
            return _hardwareInfoGroups;
        }

        /// <summary>
        /// Close api and set fan back to default
        /// </summary>
        public void Close()
        {
            _startGetData = false;
            foreach (HardwareInfoGroup hardwareInfoGroup in _hardwareInfoGroups)
            {
                foreach (HardwareInfo hardwareInfo in hardwareInfoGroup.HardwareInfos)
                {
                    hardwareInfo.SetDefault();
                }
            }

            _computer?.Close();
        }
    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            try
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    subHardware.Update();
                    subHardware.Accept(this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System Traverse Error: {ex.Message}");
            }
        }

        public void VisitSensor(ISensor sensor)
        {
            sensor.Accept(this);
        }

        public void VisitParameter(IParameter parameter)
        {
            parameter.Accept(this);
        }
    }
}
