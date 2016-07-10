using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using NAudio.Wave;
using Newtonsoft.Json;

namespace LiorWavProject
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Enter a path of a .wav file song");
            string filePath = Console.ReadLine();

            if (File.Exists(filePath))
            {
                if (filePath.EndsWith(".wav"))
                {
                    // get the list of countries to iterate
                    var countries = GetCountryList();

                    WaveFileReader wavInput = null;
                    try
                    {
                        // open the input file
                        wavInput = new WaveFileReader(filePath);
                    }
                    catch
                    {
                        Console.WriteLine("Error: unable to open the file {0}", filePath);
                    }

                    if(wavInput != null)
                    {
                        // if we are able to read from the wav file
                        if (wavInput.CanRead)
                        {
                            // calculate hits by secret algorithm for each country
                            countries.ForEach(country => CalculateCountryHits(country, wavInput));
                            PrintResults(countries);
                        }
                        else
                        {
                            Console.WriteLine("Error: unable to read from the file {0}", filePath);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error: song has to be a .wav file");
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
                }
            }
            catch
            {
                Console.WriteLine("Error: unable to create the output file '{0}'", outputSource);
            }

            // print results to console 
            countries.ForEach(c => Console.WriteLine(c));
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
        /// Calculates the number of hits per country for the wavFile input song
        /// </summary>
        /// <param name="country">Country</param>
        /// <param name="wavFile">Song</param>
        private static void CalculateCountryHits(Country country, WaveFileReader wavFile)
        {
            Console.Write("Working on {0}...",country.Name);
            try
            {
                // revert back to position 0 of the wav file
                wavFile.Position = 0;
                // initialize the buffer size to read
                int bufferSize = wavFile.WaveFormat.AverageBytesPerSecond != 0 ? wavFile.WaveFormat.AverageBytesPerSecond : 1024;
                byte[] buffer = new byte[bufferSize];
                // get the country preference
                byte[] countryPreferenceBytes = Encoding.ASCII.GetBytes(country.Name);
                // go over the song and calculate the number of hits
                while (wavFile.Position < wavFile.Length)
                {
                    wavFile.Read(buffer, 0, bufferSize);
                    for (int i = 0; i < bufferSize; i++)
                    {
                        // if the country "likes" the song increase the hits
                        if (buffer[i] == countryPreferenceBytes[i % countryPreferenceBytes.Length])
                        {
                            country.Hits++;
                        }
                    }
                }
                Console.WriteLine(" Done!");
            }
            catch
            {
                country.Hits = 0;
                Console.WriteLine(" Failed!");  
            }            
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
