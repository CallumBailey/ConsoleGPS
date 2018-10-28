using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ConsoleGPS
{
    class Program
    {
        public static JObject apiResponse;
        public static string google_app_id = "AIzaSyDYba8ZvZX5gkPWMTg7e1CFHGkkqjjIouc";
        public static string here_app_id = "UPbemsYAZ0RarUofZeGy";
        public static string here_app_code = "CIPAHdO1yuwe5A9d-qEP4g";
        public static string currentCity, currentCoords, weather;

        static void Main(string[] args)
        {
            Console.SetWindowSize(64, 30);
            IWebProxy defaultWebProxy = WebRequest.DefaultWebProxy;
            defaultWebProxy.Credentials = CredentialCache.DefaultCredentials;
            var wc = new WebClient
            {
                Proxy = defaultWebProxy
            };
            wc.Credentials = CredentialCache.DefaultNetworkCredentials;
            bool locationvalid = false;
            while (!locationvalid)
            {
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("----------------------------------------------------------------");
                Console.WriteLine("Welcome to ConsoleGPS, an application developed using the Google");
                Console.WriteLine("   Cloud Platform, and the Here.com mapping and weather API's.  ");
                Console.WriteLine("----------------------------------------------------------------");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Enter your current town/city: ");
                currentCity = Console.ReadLine();
                try
                {
                    weather = getWeather(wc);
                    locationvalid = true;
                }
                catch
                {
                    weather = "Error: Could not be found";
                }
            }
            bool end = false;
            while (!end)
            {
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("----------------------------------------------------------------");
                Console.WriteLine("Welcome to ConsoleGPS, an application developed using the Google");
                Console.WriteLine("   Cloud Platform, and the Here.com mapping and weather API's.  ");
                Console.WriteLine("----------------------------------------------------------------");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Weather in {currentCity.ToUpper()}: {weather}");
                Console.WriteLine("----------------------------------------------------------------");
                Console.WriteLine("1. GPS System\n2. Search for Nearby...\n3. Close");
                Console.Write(">");
                string menu = Console.ReadLine();
                if (menu == "1")
                {
                    Console.Clear();
                    GPSOption(wc);
                }
                else if (menu == "2")
                {
                    Console.Clear();
                    nearestSearch(wc);
                }
                else if (menu == "3")
                {
                    end = true;
                }
                else
                {
                
                }
            }
        }
        
        //GPS FUNCTION
        static void GPSOption(WebClient wc)
        {
            try
            {
                Console.WriteLine("Enter your start point, then destination, most reliable with a more precise address: ");
                Console.Write("Start at: ");
                string start = Console.ReadLine();
                Console.Write("Destination: ");
                string destination = Console.ReadLine();
                start = start.Replace(" ", "+");
                destination = destination.Replace(" ", "+");
                createBasicQuery(start, destination, wc);
                Console.WriteLine("-------------------------");
                Console.Write("Would you like to add a waypoint? <Y/N> ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    Console.Write("Waypoint: ");
                    string waypoint = Console.ReadLine();
                    waypoint = waypoint.Replace(" ", "+");
                    createWaypointQuery(start, destination, waypoint, wc);
                }
                else
                {
                    directionOption(apiResponse);
                }
            }
            catch
            {
                Console.WriteLine("There was an error during this process. Please try again.");
            }

            Console.ReadKey();
        }

        static void createBasicQuery(string start, string destination, WebClient wc)
        {
            string app_id = "AIzaSyDYba8ZvZX5gkPWMTg7e1CFHGkkqjjIouc";
            string queryUrl = "https://maps.googleapis.com/maps/api/directions/json?origin=";
            queryUrl += $"{start}&destination={destination}";
            queryUrl += $"&key={app_id}";

            runQuery(queryUrl, wc, false);
        }

        static void createWaypointQuery(string start, string destination, string waypoint, WebClient wc)
        {
            string queryUrl = "https://maps.googleapis.com/maps/api/directions/json?origin=";
            queryUrl += $"{start}&destination={destination}";
            queryUrl += $"&waypoints={waypoint}";
            queryUrl += $"&key={google_app_id}";

            runQuery(queryUrl, wc, true);
        }

        static void runQuery(string query, WebClient wc, bool waypoint)
        {
            string reply = wc.DownloadString(query);
            apiResponse = JObject.Parse(reply);
            JObject details = JObject.Parse(apiResponse["routes"][0]["legs"][0].ToString());
            JObject details2 = new JObject();
            if (waypoint) { details2 = JObject.Parse(apiResponse["routes"][0]["legs"][1].ToString()); }
            string distance = details["distance"]["text"].ToString();
            string time = details["duration"]["text"].ToString();
            string dist2, time2;
            if (waypoint)
            {
                dist2 = details2["distance"]["text"].ToString();
                time2 = details2["duration"]["text"].ToString();
                Console.WriteLine("-------------------------");
                Console.WriteLine($"Distance to Waypoint: {distance}\nTime to Waypoint: {time}\nWaypoint to Dest: {dist2}\nTime to Dest: {time2}");
                Console.WriteLine("-------------------------");
                Console.WriteLine($"Therefore the total for the trip is:");
                distance = distance.Replace(" km", ""); dist2 = dist2.Replace(" km", "");
                time = time.Replace(" mins", ""); time2 = time2.Replace(" mins", "");
                distance = (Convert.ToDouble(distance) + Convert.ToDouble(dist2)).ToString();
                time = (Convert.ToDouble(time) + Convert.ToDouble(time2)).ToString();
            }
            if (waypoint)
            {
                Console.WriteLine($"Distance: {distance} km\nTime: {time} mins");
                directionOption(apiResponse);
            }
            else
            {
                Console.WriteLine($"Distance: {distance}\nTime: {time}");
            }
        }

        static void directionOption(JObject apiResponse)
        {
            Console.WriteLine("-------------------------");
            Console.Write("Would you like to view the list of directions? <Y/N> ");
            if (Console.ReadLine().ToLower() == "y")
            {
                Console.WriteLine("Completing...");
                Thread.Sleep(2500);
                directionsToText(apiResponse);
            }
        }

        static void directionsToText(JObject apiResponse)
        {
            StreamWriter writeToFile = new StreamWriter("./directions.txt");

            for(int i = 0; i < apiResponse["routes"][0]["legs"].Count(); i++)
            {
                for(int j = 0; j < apiResponse["routes"][0]["legs"][i]["steps"].Count(); j++)
                {
                    string temp = apiResponse["routes"][0]["legs"][i]["steps"][j]["html_instructions"].ToString();
                    temp = temp.Replace("<b>", "");
                    temp = temp.Replace("</b>", "");
                    temp = temp.Replace("</div>", "");
                    if (temp.Contains("<div"))
                    {
                        int one = temp.IndexOf('<');
                        int two = temp.IndexOf('>');
                        string removed = temp.Substring(one, two - one + 1);
                        temp = temp.Replace(removed, ". ");
                    }
                    writeToFile.WriteLine($"{j+1}. {temp}");
                }
                writeToFile.WriteLine("-------------------------");
            }
            writeToFile.Close();

            StreamReader readFile = new StreamReader("./directions.txt");
            int count = File.ReadAllLines("directions.txt").Count();

            Console.WriteLine("-------------------------");
            for(int i = 0; i < count; i++)
            {
                string line = readFile.ReadLine();
                Console.WriteLine(line);
            }
            readFile.Close();

            Console.Write("View as printable? <y/n>");
            if(Console.ReadLine().ToLower() == "y")
            {
                Console.WriteLine("Loading...");
                Thread.Sleep(2500);
                Console.WriteLine("Press enter to continue...");
                writeToPdf();
            }
        }

        static void writeToPdf()
        {
            FileStream fs = new FileStream("Directions.pdf", FileMode.Create, FileAccess.Write);
            Document doc = new Document();
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();

            doc.Add(new Paragraph("ConsoleGPS Directions Print-Out\n-------------------------\n"));
            StreamReader readFile = new StreamReader("./directions.txt");
            int count = File.ReadAllLines("directions.txt").Count();
            for (int i = 0; i < count; i++)
            {
                string line = readFile.ReadLine();
                doc.Add(new Paragraph(line));
            }
            readFile.Close();

            doc.Close();

            System.Diagnostics.Process.Start("Directions.pdf");
        }
        //END

        //NEAREST LOCATION SEARCH FUNCTION
        static void nearestSearch(WebClient wc)
        {
            Console.WriteLine("Enter a search term and you will be shown the 5 nearest places\ne.g. museums, mcdonalds etc.");
            Console.Write("Search: ");
            string inputTerm = Console.ReadLine();

            string searchQuery = $"https://places.api.here.com/places/v1/discover/search?app_id={here_app_id}&app_code={here_app_code}";
            searchQuery += $"&size=5&q={inputTerm}&tf=plain&at={currentCoords}";

            string response = wc.DownloadString(searchQuery);
            JObject hereResponse = JObject.Parse(response);

            for(int i = 0; i< 5; i++)
            {
                string name = hereResponse["results"]["items"][i]["title"].ToString();
                string distance = hereResponse["results"]["items"][i]["distance"].ToString();
                Console.WriteLine($"{(Convert.ToInt16(i+1)).ToString()}. {name}, approx. {distance} metres away");
            }
            Console.WriteLine("You can use the name of a desired location in the GPS System.");
            Console.WriteLine("Please press enter to continue...");

            Console.ReadKey();
        }
        //END

        //WEATHER/COORDS FUNCTION
        static string getWeather(WebClient wc)
        {
            string weather = "";

            string query = $"https://weather.api.here.com/weather/1.0/report.json?app_id={here_app_id}&app_code={here_app_code}";
            query += $"&product=observation&name={currentCity}";

            string response = wc.DownloadString(query);
            JObject weatherResp = JObject.Parse(response);
            weather = $"{weatherResp["observations"]["location"][0]["observation"][0]["temperature"]} degrees. ";
            weather += $"{weatherResp["observations"]["location"][0]["observation"][0]["description"]}";
            currentCoords = $"{weatherResp["observations"]["location"][0]["observation"][0]["latitude"]},";
            currentCoords += $"{weatherResp["observations"]["location"][0]["observation"][0]["longitude"]}";

            return weather;
        }
        //END
    }
}
