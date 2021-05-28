using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class BlacklistedPost
    {
        public int UserId { get; set; }
        public int PostId { get; set; }

        public virtual Post Post { get; set; }
        public virtual User User { get; set; }
    }
}
