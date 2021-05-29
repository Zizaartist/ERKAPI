using ERKAPI.Models.EnumModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Report
    {
        public int ReportId { get; set; }
        public int? AuthorId { get; set; }
        public int? PostId { get; set; }
        public ReportReason Reason { get; set; }

        public virtual User Author { get; set; }
        public virtual Post Post { get; set; }
    }
}
