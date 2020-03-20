using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using Newtonsoft.Json;
using RestSharp;

namespace coronaapp
{
    class Program
    {
        private static List<CoronaCases> cases;
        private const string data = "data.json";
        private const string url = "https://www.worldometers.info/coronavirus/";
        private const int timeToSleep = 120000;
        private static string dataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, data);
        static async Task Main(string[] args)
        {
            cases = GetData();
            await Start();
        }

        private static void WriteData()
        {
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(cases));
        }

        private static List<CoronaCases> GetData()
        {
            if (File.Exists(dataPath))
            {
                var file = File.ReadAllText(dataPath);
                var parsed = JsonConvert.DeserializeObject<List<CoronaCases>>(file);
                return parsed;
            }

            return new List<CoronaCases>();
        }

        private static async Task Start()
        {
            var client = new RestClient(url);
            var response = await client.ExecuteAsync(new RestRequest());
            var now = DateTime.Now;
            if (response.IsSuccessful)
            {
                var content = response.Content;
                var dom = CQ.CreateDocument(content);
                var numbers = dom[".maincounter-number"].Selection.ToList();
                if (numbers.Count > 0)
                {
                    GetCounts(numbers, out var deathCount, out var caseCount);
                    Calculations(caseCount, deathCount, now);
                    cases.Add(new CoronaCases { Cases = caseCount, Deaths = deathCount, DataTime = now });
                    WriteData();
                    
                }
            }
            await Task.Delay(timeToSleep);
            await Start();
        }

        private static void GetCounts(List<IDomObject> numbers, out int deathCount, out int caseCount)
        {
            caseCount = int.Parse(Regex.Match(numbers[0].InnerHTML.Replace(",", ""), "[0-9]+").Value);
            deathCount = int.Parse(Regex.Match(numbers[1].InnerHTML.Replace(",", ""), "[0-9]+").Value);
        }

        private static void Calculations(int caseCount, int deathCount, DateTime now)
        {
            var formattedCases = caseCount.ToString("N0");
            var formattedDeaths = deathCount.ToString("N0");
            var lastCase = cases.LastOrDefault();
            if (lastCase == null)
            {
                
                Console.WriteLine($"{now} - There's no data to compare yet!\r\nTotal Cases as of now: {formattedCases} - Total Deaths as of now: {formattedDeaths}");
                return;
            }

            var difference = (int)(now - lastCase.DataTime).TotalMinutes;
            Console.WriteLine($"{now} - {caseCount - lastCase.Cases} new cases and {deathCount - lastCase.Deaths} new deaths in {difference} minutes\r\nTotal Cases as of now: {formattedCases} - Total Deaths as of now: {formattedDeaths}");

        }
    }

    

    public class CoronaCases
    {
        public DateTime DataTime { get; set; }
        public int Cases { get; set; }
        public int Deaths { get; set; }
    }
}
