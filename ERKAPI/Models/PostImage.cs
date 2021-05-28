using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostImage
    {
        public int PostImageId { get; set; }
        public int PostDataId { get; set; }
        public string Image { get; set; }

        public virtual PostData PostData { get; set; }
    }
}
