using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Comment
    {
        public int CommentId { get; set; }
        public int? AuthorId { get; set; }
        public string Text { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual User Author { get; set; }
        public virtual Post Post { get; set; }
    }
}
