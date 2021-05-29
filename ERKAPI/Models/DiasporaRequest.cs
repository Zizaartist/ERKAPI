using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ERKAPI.Models
{
    public partial class DiasporaRequest
    {
        public int DiasporaRequestId { get; set; }
        [Required]
        [StringLength(250, MinimumLength = 2)]
        public string Name { get; set; }
        [Required]
        [StringLength(2000, MinimumLength = 2)]
        public string Info { get; set; }
        public int? RequesterId { get; set; }

        public virtual User Requester { get; set; }
    }
}
