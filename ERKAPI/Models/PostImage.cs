using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostImage
    {
        [JsonIgnore]
        public int PostImageId { get; set; }
        [JsonIgnore]
        public int PostDataId { get; set; }
        public string Image { get; set; }

        [JsonIgnore]
        public virtual PostData PostData { get; set; }
    }
}
