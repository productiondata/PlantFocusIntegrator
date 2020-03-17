using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantFocus_Integrator.Models
{
    public class IntegrationItem
    {
        public int iID { get; set; }
        public string Description { get; set; }
        public int PLCid { get; set; }
        public DateTime dtLastCheck { get; set; }
        public string GetString { get; set; }
        public string MostRecentSentValue { get; set; }
        public string MostRecentSentDateTime { get; set; }
    }
}
