using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LiorWavProject
{
    [JsonObject]
    public class Country
    {
        public string Name { get; set; }
        public uint Hits { get; set; }
        [JsonIgnore]
        public string Continent { get; set; }

        public Country(string name,uint hits,string continent)
        {
            Name = name;
            Hits = hits;
            Continent = continent;
        }
        public override string ToString()
        {
            return string.Format("@Country: {0} Predicted Hits: {1:n0}", Name, Hits);
        }

    }
}
