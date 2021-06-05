using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostData
    {
        public PostData()
        {
            PostImages = new HashSet<PostImage>();
        }

        [JsonIgnore]
        public int PostDataId { get; set; }
        [JsonIgnore]
        public int PostId { get; set; }
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Text { get; set; }

        [JsonIgnore]
        public virtual Post Post { get; set; }
        public virtual ICollection<PostImage> PostImages { get; set; }

        [NotMapped]
        public bool HasHiddenText { get; set; } = false;


        public bool ShouldSerializePostImages() => PostImages.Any();
        public bool ShouldSerializeHasHiddenText() => HasHiddenText;
    }
}
