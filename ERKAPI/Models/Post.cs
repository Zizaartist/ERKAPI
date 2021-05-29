using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Post
    {
        public Post()
        {
            BlacklistedPostEntities = new HashSet<BlacklistedPost>();
            UsersWhoBlacklisted = new HashSet<User>();
            Comments = new HashSet<Comment>();
            Reposts = new HashSet<Post>();
            Opinions = new HashSet<Opinion>();
            Reports = new HashSet<Report>();
        }

        public int PostId { get; set; }
        public int? AuthorId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? RepostId { get; set; }
        public int Likes { get; set; }
        public int Dislikes { get; set; }

        public virtual User Author { get; set; }
        public virtual Post Repost { get; set; }
        public virtual PostData PostData { get; set; }
        [JsonIgnore]
        public virtual ICollection<BlacklistedPost> BlacklistedPostEntities { get; set; }
        [JsonIgnore]
        public virtual ICollection<User> UsersWhoBlacklisted { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        [JsonIgnore]
        public virtual ICollection<Post> Reposts { get; set; }
        [JsonIgnore]
        public virtual ICollection<Opinion> Opinions { get; set; }
        [JsonIgnore]
        public virtual ICollection<Report> Reports { get; set; }

        [JsonIgnore]
        [NotMapped]
        public bool IsOriginalPost { get; set; } = true;
        public bool ShouldSerializeLikes() => IsOriginalPost;
        public bool ShouldSerializeDislikes() => IsOriginalPost;
        public bool ShouldSerializeCreatedDate() => IsOriginalPost;
        public bool ShouldSerializeComments() => Comments.Any();
    }
}
