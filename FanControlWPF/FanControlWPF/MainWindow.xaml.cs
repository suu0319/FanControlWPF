using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FanControlWPF.Config;
using LibreHardwareMonitorLibrary;

namespace FanControlWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, HardwareInfo> _hardwareInfoDict = new Dictionary<string, HardwareInfo>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly SystemFanControlConfig _config;
        
        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed!;
            HardwareMonitoringHelper.Instance.Calibrated += UpdateConfig;

            _config = ConfigManager.Instance.LoadConfig();

            if (_config.IsCalibrated)
            {
                DisplaySysFanInfo();
            }
            else
            {
                Calibrate();
            }
        }
        
        private void Calibrate(object sender, RoutedEventArgs e)
        {
            Calibrate();
        }

        private void Calibrate()
        {
            _cancellationTokenSource.Cancel();

            Dispatcher.Invoke(() =>
            {
                FanState.Text = "Calibrating";
            });
            
            HardwareMonitoringHelper.Instance.RunCalibration();
        }

        private void DisplaySysFanInfo()
        {
            Dispatcher.Invoke(() =>
            {
                FanState.Text = "Calibration Info Read Successfully";
            });
                
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            var groups = HardwareMonitoringHelper.Instance.GetHardwareInfoGroups();

            TraverseGroups(groups);

            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    GetSysFanInfo();
                }
            }, token);
        }

        #region HandleHardwareInfoGroups
        private void TraverseGroups(List<HardwareInfoGroup> groups)
        {
            foreach (var group in groups)
            {
                foreach (var info in group.HardwareInfos)
                {
                    switch (info.Name)
                    {
                        case { } str when str.Contains("CPU Fan"):
                            _hardwareInfoDict["CpuFan"] = info;
                            break;
    
                        case { } str when str.Contains("Pump Fan"):
                            _hardwareInfoDict["PumpFan"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #1"):
                            _hardwareInfoDict["Fan1"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #2"):
                            _hardwareInfoDict["Fan2"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #3"):
                            _hardwareInfoDict["Fan3"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #4"):
                            _hardwareInfoDict["Fan4"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #5"):
                            _hardwareInfoDict["Fan5"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #6"):
                            _hardwareInfoDict["Fan6"] = info;
                            break;
    
                        case { } str when str.Contains("Fan #7"):
                            _hardwareInfoDict["Fan7"] = info;
                            break;
    
                        case { } str when str.Contains("GPU Fan 1"):
                            _hardwareInfoDict["GpuFan1"] = info;
                            break;
    
                        case { } str when str.Contains("GPU Fan 2"):
                            _hardwareInfoDict["GpuFan2"] = info;
                            break;
                    }
                }
            }
        }

        private void GetSysFanInfo()
        {
            foreach (var info in _hardwareInfoDict)
            {
                switch (info.Key)
                    {
                        case "CpuFan":
                            AssignFanInfo(info.Value, CpuFanPercentage, CpuFanRPM, CpuFanPercentageNew, SetCpuFanPercentageBtn);
                            break;
    
                        case "PumpFan":
                            AssignFanInfo(info.Value, PumpFanPercentage, PumpFanRPM, PumpFanPercentageNew, SetPumpFanPercentageBtn);
                            break;
    
                        case "Fan1":
                            AssignFanInfo(info.Value, Fan1Percentage, Fan1RPM, Fan1PercentageNew, SetFan1PercentageBtn);
                            break;
    
                        case "Fan2":
                            AssignFanInfo(info.Value, Fan2Percentage, Fan2RPM, Fan2PercentageNew, SetFan2PercentageBtn);
                            break;
    
                        case "Fan3":
                            AssignFanInfo(info.Value, Fan3Percentage, Fan3RPM, Fan3PercentageNew, SetFan3PercentageBtn);
                            break;
    
                        case "Fan4":
                            AssignFanInfo(info.Value, Fan4Percentage, Fan4RPM, Fan4PercentageNew, SetFan4PercentageBtn);
                            break;
    
                        case "Fan5":
                            AssignFanInfo(info.Value, Fan5Percentage, Fan5RPM, Fan5PercentageNew, SetFan5PercentageBtn);
                            break;
    
                        case "Fan6":
                            AssignFanInfo(info.Value, Fan6Percentage, Fan6RPM, Fan6PercentageNew, SetFan6PercentageBtn);
                            break;
    
                        case "Fan7":
                            AssignFanInfo(info.Value, Fan7Percentage, Fan7RPM, Fan7PercentageNew, SetFan7PercentageBtn);
                            break;
    
                        case "GpuFan1":
                            AssignFanInfo(info.Value, GpuFan1Percentage, GpuFan1RPM, GpuFan1PercentageNew, SetGpuFan1PercentageBtn);
                            break;
    
                        case "GpuFan2":
                            AssignFanInfo(info.Value, GpuFan2Percentage, GpuFan2RPM, GpuFan2PercentageNew, SetGpuFan2PercentageBtn);
                            break;
                    }
            }
        }
        #endregion
        
        private void AssignFanInfo(HardwareInfo? info, TextBox percentage, TextBox rpm, TextBox newPercentage, Button setBtn)
        {
            var configInfo = _config.HardwareInfos.Find(x => x.Name == info.Name);

            if (configInfo != null)
            {
                Dispatcher.Invoke(() =>
                {
                    percentage.Text = info.Percentage.ToString();
                    rpm.Text = info.RPM.ToString();

                    newPercentage.IsEnabled = configInfo.Controlable;
                    setBtn.IsEnabled = configInfo.Controlable;
                });
            }
        }
        
        private void UpdateConfig()
        {
            var groups = HardwareMonitoringHelper.Instance.GetHardwareInfoGroups();

            _config.IsCalibrated = HardwareMonitoringHelper.Instance.IsCalibrated;

            foreach (var group in groups)
            {
                foreach (var info in group.HardwareInfos)
                {
                    WriteHardwareInfoToConfig(info);
                }
            }

            Dispatcher.Invoke(() =>
            {
                FanState.Text = "Calibration Finished";
            });
            
            DisplaySysFanInfo();
        }

        private void WriteHardwareInfoToConfig(HardwareInfo info)
        {
            try
            { 
                if (_config.HardwareInfos != null)
                {
                    int index = _config.HardwareInfos.FindIndex(x => x.Name == info.Name);
                    if (index != -1)
                    {
                        /*Write to exist config*/
                        _config.HardwareInfos[index].Calibrated = info.Calibrated;
                        _config.HardwareInfos[index].Controlable = info.Controlable;
                    }
                    else
                    {
                        /*Write to new config*/
                        _config.HardwareInfos.Add(new HardwareInfoConfig(info));
                    }
                }
                else
                {
                    _config.HardwareInfos = new List<HardwareInfoConfig> { new HardwareInfoConfig(info) };
                }
                
                ConfigManager.Instance.SaveConfig(_config);
            }
            catch (Exception e)
            {
                Console.WriteLine($"SystemFanControlHelper.UpdateConfig fail. ex:{e}, HardwareInfo:{info.Name}");
            }
        }

        #region SetFanPercentage
        private void SetCpuFanPercentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["CpuFan"], int.Parse(CpuFanPercentageNew.Text));
        }
        
        private void SetPumpFanPercentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["PumpFan"], int.Parse(PumpFanPercentageNew.Text));
        }
        
        private void SetFan1Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan1"], int.Parse(Fan1PercentageNew.Text));
        }

        private void SetFan2Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan2"], int.Parse(Fan2PercentageNew.Text));
        }

        private void SetFan3Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan3"], int.Parse(Fan3PercentageNew.Text));
        }

        private void SetFan4Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan4"], int.Parse(Fan4PercentageNew.Text));
        }

        private void SetFan5Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan5"], int.Parse(Fan5PercentageNew.Text));
        }

        private void SetFan6Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan6"], int.Parse(Fan6PercentageNew.Text));
        }

        private void SetFan7Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["Fan7"], int.Parse(Fan7PercentageNew.Text));
        }

        private void SetGpuFan1Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["GpuFan1"], int.Parse(GpuFan1PercentageNew.Text));
        }

        private void SetGpuFan2Percentage(object sender, RoutedEventArgs e)
        {
            SetFanPercentage(_hardwareInfoDict["GpuFan2"], int.Parse(GpuFan2PercentageNew.Text));
        }
        
        private void SetFanPercentage(HardwareInfo fan, int percentage)
        {
            percentage = percentage switch
            {
                < 0 => 0,
                > 100 => 100,
                _ => percentage
            };
            
            fan.SetPercentage(percentage);
        }
        #endregion
        
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            HardwareMonitoringHelper.Instance.Close();
        }
    }
}