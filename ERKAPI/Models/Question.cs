using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Models
{
    public class Question
    {
        [JsonIgnore]
        public int QuestionId { get; set; }
        [JsonIgnore]
        public int AuthorId { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Text { get; set; }

        [JsonIgnore]
        public virtual User Author { get; set; }
    }
}
