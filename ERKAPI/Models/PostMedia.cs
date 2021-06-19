using ERKAPI.Models.EnumModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ERKAPI.Models
{
    public partial class PostMedia
    {
        [JsonIgnore]
        public int PostMediaId { get; set; }
        [JsonIgnore]
        public int PostDataId { get; set; }
        [Required]
        public string Path { get; set; }
        [Required]
        public string PreviewPath { get; set; }
        public MediaType MediaType { get; set; }

        [JsonIgnore]
        public virtual PostData PostData { get; set; }
    }
}
