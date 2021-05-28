using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Report
    {
        public int ReportId { get; set; }
        public int? AuthorId { get; set; }
        public int? PostId { get; set; }
        public int Reason { get; set; }

        public virtual User Author { get; set; }
        public virtual Post Post { get; set; }
    }
}
