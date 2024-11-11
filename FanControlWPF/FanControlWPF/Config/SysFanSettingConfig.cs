using System.Collections.Generic;
using LibreHardwareMonitorLibrary;

namespace FanControlWPF.Config;

public class SystemFanControlConfig
{
    public bool IsCalibrated { get; set; }
    public List<HardwareInfoConfig> HardwareInfos { get; set; }
}

public class HardwareInfoConfig
{
    public string Name { get; set; }
    public bool Controlable { get; set; }
    public bool Calibrated { get; set; }
    
    public HardwareInfoConfig() { }
    
    public HardwareInfoConfig(HardwareInfo infoConfig)
    {
        Name = infoConfig.Name;
        Controlable = infoConfig.Controlable;
        Calibrated = infoConfig.Calibrated;
    }
}
