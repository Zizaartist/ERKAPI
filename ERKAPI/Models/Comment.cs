using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Comment
    {
        [JsonIgnore]
        public int CommentId { get; set; }
        [JsonIgnore]
        public int? AuthorId { get; set; }
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Text { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual User Author { get; set; }
        [JsonIgnore]
        public virtual Post Post { get; set; }
    }
}
