using ERKAPI.Models.EnumModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostMedia
    {
        [JsonIgnore]
        public int PostMediaId { get; set; }
        [JsonIgnore]
        public int PostDataId { get; set; }
        public string Path { get; set; }
        public MediaType MediaType { get; set; }

        [JsonIgnore]
        public virtual PostData PostData { get; set; }
    }
}
