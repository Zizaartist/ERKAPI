using ERKAPI.Models.EnumModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Report
    {
        [JsonIgnore]
        public int ReportId { get; set; }
        [JsonIgnore]
        public int? AuthorId { get; set; }
        public int PostId { get; set; }
        [Required]
        public ReportReason Reason { get; set; }

        [JsonIgnore]
        public virtual User Author { get; set; }
        [JsonIgnore]
        public virtual Post Post { get; set; }
    }
}
