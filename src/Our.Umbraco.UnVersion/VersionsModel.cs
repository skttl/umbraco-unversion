using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Our.Umbraco.UnVersion
{
    public class VersionsModel
    {
        public Guid VersionID { get; set; }
        public DateTime VersionDate { get; set; }
        public bool Published { get; set; }
        public bool Newest { get; set; }
    }
}