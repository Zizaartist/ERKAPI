using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostData
    {
        public PostData()
        {
            PostImages = new HashSet<PostImage>();
        }

        public int PostDataId { get; set; }
        public int PostId { get; set; }
        public string Text { get; set; }

        public virtual Post Post { get; set; }
        public virtual ICollection<PostImage> PostImages { get; set; }
    }
}
