using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LiorWavProject
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Enter a path of the algorithm output");
            string filePath = Console.ReadLine();

            if (File.Exists(filePath))
            {
                if (filePath.EndsWith(".txt"))
                {
                    // get the list of countries with the continents from the resource file
                    var countriesInfo = GetCountryList();

                    string[] lines = null;
                    try
                    {
                        // open the input file
                        lines = File.ReadAllLines(filePath);
                    }
                    catch
                    {
                        Console.WriteLine("Error: unable to open the file {0}", filePath);
                    }

                    if(lines != null)
                    {
                        // create a list which holds the output countries
                        List<Country> countriesOutput = new List<Country>();
                        foreach (var line in lines.Where(l=> !(l.Equals(string.Empty))))
                        {
                            // check if the line is valid from the algorithm output
                            if (line.Contains("@Country: ") && line.Contains("Hits: "))
                            {
                                // extract country name and hits from the line
                                string countryName = GetCountryName(line);
                                uint countryHits = GetCountryHits(line);
                                try
                                {
                                    // find the country's continent
                                    var country = countriesInfo.First(c => c.Name.Equals(countryName, StringComparison.InvariantCultureIgnoreCase));
                                    // update the number of hits
                                    country.Hits = countryHits;
                                    // add the country to the output
                                    countriesOutput.Add(country);
                                }
                                catch
                                {
                                    // in case the continent wasn't found in the resource file
                                    countriesOutput.Add(new Country(countryName, countryHits, "Unkown"));
                                }
                            }
                        }
                        if (countriesOutput.Count > 0)
                        {
                            // creates the json output file
                            PrintResults(countriesOutput);
                        }
                        else
                        {
                            Console.WriteLine("Input file is empty or in a bad format");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: the input has to be a .txt file");
                }
            }
            else
            {
                Console.WriteLine("Error: file doesn't exist in the given path");
            }
        }

        /// <summary>
        /// Prints results to the console in the formatted style and create output .json file with the results
        /// </summary>
        /// <param name="countries">list of countries</param>
        private static void PrintResults(List<Country> countries)
        {
            // calculate the continent with most hits
            var maxContinent = GetContinentWithMostHits(countries);
            // create new output variable to be serialized to a json output file
            // Results holds the countries order by descending hits
            // MostSuccessfulContinent holds the continent with most hits and the total hits
            var output = new
            {
                Results = countries.OrderByDescending(country => country.Hits),
                MostSuccessfulContinent = maxContinent
            };

            string outputSource = "output.json";
            try
            {
                // write the results to a json file after serializing the output object
                using (StreamWriter file = File.CreateText(outputSource))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, output);
                    Console.WriteLine("{0} has been successfully created", outputSource);
                }
            }
            catch
            {
                Console.WriteLine("Error: unable to create the output file '{0}'", outputSource);
            }

        }

        /// <summary>
        /// Returns the continent with the most hits of the song 
        /// </summary>
        /// <param name="countries">list of countries</param>
        /// <returns>An anonymous object that holds the continent name and total number of hits</returns>
        private static object GetContinentWithMostHits(List<Country> countries)
        {
            // get the maximum number of hits in a continent
            uint maxHitsPerContinent = (uint)(from country in countries
                                       group country by country.Continent
                                       into grp
                                       select grp.Sum(c => c.Hits)).Max();
            // get the continent that has the most number of hits
            var maxContinent = from country in countries
                               group country by country.Continent
                               into grp
                               let totalHits = grp.Sum(c => c.Hits)
                               where totalHits >= maxHitsPerContinent
                               select new { Continent = grp.Key, TotalHits = totalHits };
            return maxContinent;
        }

        /// <summary>
        /// Extracts <CountryName> from a line "@Country: <CountryName> Predicted Hits: <Hits>"
        /// </summary>
        /// <param name="line">line in the above format</param>
        /// <returns>Country name</returns>
        private static string GetCountryName(string line)
        {
            int prefix = ("@Country: ").Length;
            int length = line.IndexOf(" Predicted") - prefix;
            return length > 0 ? line.Substring(prefix, length) : "Wrong Input Format";
        }

        /// <summary>
        /// Extracts <Hits> from a line "@Country: <CountryName> Predicted Hits: <Hits>"
        /// </summary>
        /// <param name="line">line in the above format</param>
        /// <returns>number of hits</returns>
        private static uint GetCountryHits(string line)
        {
            uint numOfHits = 0;
            int prefix = line.IndexOf("Hits: ") + ("Hits: ").Length;
            string hits = line.Substring(prefix);

            try
            {
                numOfHits = uint.Parse(hits, System.Globalization.NumberStyles.AllowThousands,
                             System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                numOfHits = 0;
            }

            return numOfHits;
        }


        /// <summary>
        /// Returns a list of country objects with their continent
        /// </summary>
        /// <returns>List of country objects intialized</returns>
        private static List<Country> GetCountryList()
        {
            List<Country> list = new List<Country>();
            // parse the json resource file
            var countriesAtRegions = ParseJsonResource(Properties.Resources.CountriesAtRegions);
            if (countriesAtRegions != null)
            {
                foreach (var region in countriesAtRegions)
                {
                    if (region.Value != null)
                    {
                        foreach (var country in region.Value)
                        {
                            // add a new country object with the name and the region
                            list.Add(new Country(country, 0, region.Key));
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Parse the json resource file with the list of countries and their continent
        /// </summary>
        /// <param name="json">Json resource file</param>
        /// <returns>Dictionary with a continent and the list of countries in it</returns>
        private static Dictionary<String, List<string>> ParseJsonResource(byte[] json)
        {
            Dictionary<String, List<string>> values = null;
            try
            {
                // deserialize the resource file
                var reader = new StreamReader(new MemoryStream(json), Encoding.Default);
                values = new JsonSerializer().Deserialize<Dictionary<string, List<string>>>(new JsonTextReader(reader));
            }
            catch
            {
                Console.WriteLine("Unable to deserialize the input resource file 'CountriesAtRegions.json'");
            }
            return values;
        }

    }
}
