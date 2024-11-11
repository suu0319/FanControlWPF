using System.Collections.Generic;

namespace LibreHardwareMonitorLibrary
{
    public class HardwareInfoGroup
    {
        public List<HardwareInfo> HardwareInfos { get; private set; }

        public HardwareInfoGroup(List<HardwareInfo> hardwareInfos)
        {
            HardwareInfos = hardwareInfos;
        }
    }
}
