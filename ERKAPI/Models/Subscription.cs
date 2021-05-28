using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Subscription
    {
        public int SubscriberId { get; set; }
        public int SubscribedToId { get; set; }

        public virtual User SubscribedTo { get; set; }
        public virtual User Subscriber { get; set; }
    }
}
