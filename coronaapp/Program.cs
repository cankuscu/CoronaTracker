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
        private static List<CoronaCases> cases = new List<CoronaCases>();
        private const string data = "data.json";
        private const string url = "https://www.worldometers.info/coronavirus/";
        private static string dataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, data);
        static async Task Main(string[] args)
        {
            GetData();
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
                cases = parsed;
                return cases;
            }

            cases = new List<CoronaCases>();

            return cases;
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
                    var casess = numbers[0].InnerHTML.ToString();
                    var caseCount = int.Parse(Regex.Match(casess.Replace(",", ""), "[0-9]+").Value);
                    var deaths = numbers[1].InnerHTML.ToString();
                    var deathCount = int.Parse(Regex.Match(deaths.Replace(",", ""), "[0-9]+").Value);
                    Calculations(caseCount, deathCount);
                    //var deaths = numbers[1]["span"];
                    cases.Add(new CoronaCases { Cases = caseCount, Deaths = deathCount, DataTime = DateTime.Now });
                    WriteData();
                    await Task.Delay(120000);
                    await Start();
                }
            }

        }

        private static void Calculations(int caseCount, int deathCount)
        {
            var lastCase = cases.LastOrDefault();
            if (lastCase == null)
            {
                Console.WriteLine($"{DateTime.Now} - There's no data to compare yet! Total Cases as of now: {caseCount} - Total Deaths as of now: {deathCount}");
                return;
            }

            var difference = (DateTime.Now - lastCase.DataTime).TotalMinutes;
            Console.WriteLine($"{DateTime.Now} - {caseCount - lastCase.Cases} new cases and {deathCount - lastCase.Deaths} new deaths in {difference} minutes \r\nTotal Cases as of now: {caseCount} - Total Deaths as of now: {deathCount}");

        }
    }

    public class CoronaCases
    {
        public DateTime DataTime { get; set; }
        public int Cases { get; set; }
        public int Deaths { get; set; }
    }
}
