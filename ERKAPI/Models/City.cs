using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Models
{
    public class City
    {
        public City() 
        {
            Users = new HashSet<User>();
        }

        public int CityId { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public int CountryId { get; set; }

        [JsonIgnore]
        public Country Country { get; set; }
        [JsonIgnore]
        public virtual ICollection<User> Users { get; set; }
    }
}
