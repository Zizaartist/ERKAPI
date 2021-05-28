using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Post
    {
        public Post()
        {
            BlacklistedPosts = new HashSet<BlacklistedPost>();
            Comments = new HashSet<Comment>();
            InverseRepost = new HashSet<Post>();
            Opinions = new HashSet<Opinion>();
            Reports = new HashSet<Report>();
        }

        public int PostId { get; set; }
        public int? AuthorId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? RepostId { get; set; }

        public virtual User Author { get; set; }
        public virtual Post Repost { get; set; }
        public virtual PostData PostDatum { get; set; }
        public virtual ICollection<BlacklistedPost> BlacklistedPosts { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Post> InverseRepost { get; set; }
        public virtual ICollection<Opinion> Opinions { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
    }
}
