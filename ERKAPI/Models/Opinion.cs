using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Opinion
    {
        public bool LikeDislike { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }

        public virtual Post Post { get; set; }
        public virtual User User { get; set; }
    }
}
