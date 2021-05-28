using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class DiasporaRequest
    {
        public int DiasporaRequestId { get; set; }
        public string Name { get; set; }
        public string Info { get; set; }
        public int? RequesterId { get; set; }

        public virtual User Requester { get; set; }
    }
}
