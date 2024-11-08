using LibreHardwareMonitorLibrary.Enums;
using System.Collections.Generic;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareInfoGroup
    {
        public string HardwareName { get; private set; }
        public HardwareDeviceType DeviceType { get; private set; }
        public List<HardwareInfo> HardwareInfos { get; private set; }

        public HardwareInfoGroup(List<HardwareInfo> hardwareInfos)
        {
            HardwareInfos = hardwareInfos;
            if (hardwareInfos != null && hardwareInfos.Count > 0)
            {
                HardwareName = hardwareInfos[0].ParentHardwareName;
                DeviceType = hardwareInfos[0].DeviceType;
            }
        }
    }
}
