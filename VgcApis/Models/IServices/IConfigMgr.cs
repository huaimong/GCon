using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VgcApis.Models.IServices
{
    public interface IConfigMgrService
    {
        long RunSpeedTest(string rawConfig);
    }
}
