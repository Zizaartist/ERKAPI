using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace ERKAPI.Models
{
    public partial class Diaspora
    {
        public int DiasporaId { get; set; }
        public string Name { get; set; }
        public string Info { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool ShowInfo = false;

        public bool ShouldSerializeInfo() => ShowInfo;
    }
}
