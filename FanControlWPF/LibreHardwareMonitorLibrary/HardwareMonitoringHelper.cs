using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareMonitoringHelper
    {
        private static readonly Lazy<HardwareMonitoringHelper> lazy = new Lazy<HardwareMonitoringHelper>(() => new HardwareMonitoringHelper());
        private readonly Computer _computer;

        public Computer Computer
        {
            get => _computer;
        }

        private readonly UpdateVisitor _updateVisitor = new UpdateVisitor();

        public UpdateVisitor UpdateVisitor
        {
            get => _updateVisitor;
        }

        private const int DATA_PULLING_RATE = 200;
        private List<HardwareInfoGroup> _hardwareInfoGroups = new List<HardwareInfoGroup>();
        private bool _startGetData = false;
        private int _initCount = 0;
        public Action<HardwareInfo, bool> Calibrating { get; set; }
        public bool IsCalibrated { get; set; } = false;

        public event Action Calibrated;
        public string MotherboardName { get; private set; }
        public static HardwareMonitoringHelper Instance => lazy.Value;

        public HardwareMonitoringHelper()
        {
            _computer = GetComputer();
            OnlyGetMotherboardName();
            StartGetData();
            GetSensors();
        }

        private void OnlyGetMotherboardName()
        {
            if (_computer != null)
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    GetMotherboardName(hardware);
                }
            }
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

            foreach (IHardware hardware in _computer?.Hardware)
            {
                GetMotherboardName(hardware);

                List<HardwareInfo> hardwareInfos = new List<HardwareInfo>();
                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        CheckSensorAndCreateHardwareInfo(hardwareInfos, sensor);
                    }
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    CheckSensorAndCreateHardwareInfo(hardwareInfos, sensor);
                }

                _hardwareInfoGroups.Add(new HardwareInfoGroup(hardwareInfos));
            }
        }

        private void GetMotherboardName(IHardware hardware)
        {
            if (hardware.HardwareType == HardwareType.Motherboard)
                MotherboardName = hardware.Name;
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

        private void CheckSensorAndCreateHardwareInfo(List<HardwareInfo> hardwareInfos, ISensor sensor)
        {
            if (sensor.Name.Contains("Fan"))
            {
                int index = hardwareInfos.FindIndex(x => x.Name == sensor.Name);
                if (index != -1)
                {
                    hardwareInfos[index].Calibrating += CalibrateInvoke;
                    hardwareInfos[index].SetSensor(sensor);
                }
                else
                {
                    string name = sensor.Name;
                    HardwareInfo hardwareInfo = new HardwareInfo(name, sensor);
                    hardwareInfos.Add(hardwareInfo);
                }
            }
        }

        private void CalibrateInvoke(HardwareInfo hardwareInfo, bool isCalibrating)
        {
            Calibrating?.Invoke(hardwareInfo, isCalibrating);
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
