using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Models
{
    public class Country
    {
        public Country() 
        {
            Users = new HashSet<User>();
            Cities = new HashSet<City>();
        }

        public int CountryId { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<City> Cities { get; set; }
    }
}
