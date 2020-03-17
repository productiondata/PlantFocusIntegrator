using System;

namespace PlantFocus_Integrator.Models
{
    public class IntegrationData
    {
        public int priKey { get; set; }
        public DateTime dt { get; set; }
        public int iID { get; set; }
        public string Value { get; set; }
        public string FromStoreLoc { get; set; }
        public string SiteID { get; set; }
        public string UseType { get; set; }
        public string Status { get; set; }
        public string LineType { get; set; }
        public string ItemNum { get; set; }
        public int PLCid { get; set; }
        public string FromBin { get; set; }
        public string IUL_UseType { get; set; }
        public string IUL_FromStoreLoc { get; set; }
        public string IUL_OrgID { get; set; }
        public string SetFileID { get; set; }
    }
}
