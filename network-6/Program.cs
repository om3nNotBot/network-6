using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace WeatherTask
{
    public class WeatherRootObject
    {
        public Location? location { get; set; }
        public Current? current { get; set; }
    }

    public class Location
    {
        public string name { get; set; } = "";
        public string country { get; set; } = "";
    }

    public class Current
    {
        public double temp_c { get; set; }
        public double feelslike_c { get; set; }
        public int humidity { get; set; }
        public double wind_kph { get; set; }
        public int is_day { get; set; }
        public Condition? condition { get; set; }
    }

    public class Condition
    {
        public string text { get; set; } = "";
        public int code { get; set; }
    }

    public class WeatherService
    {
        
        private readonly string apiKey =
            Environment.GetEnvironmentVariable("WEATHER_API_KEY") ?? "b3bbee4d1af748ad83b195422262710";

        private readonly string url = "https://api.weatherapi.com/v1/current.json";

        private readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public async Task<(WeatherRootObject? Data, string? Error)> GetWeatherData(string city)
        {
            try
            {
                var requestUrl = $"{url}?key={apiKey}&q={Uri.EscapeDataString(city)}&lang=az";

                var response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string error = response.StatusCode switch
                    {
                        HttpStatusCode.BadRequest => "Şəhər tapılmadı. Adı düzgün yazdığınızdan əmin olun.",
                        HttpStatusCode.Unauthorized => "API açarı yanlışdır.",
                        HttpStatusCode.Forbidden => "API açarının icazəsi yoxdur və ya limit bitib.",
                        _ => $"HTTP xətası: {(int)response.StatusCode} ({response.StatusCode})"
                    };
                    return (null, error);
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<WeatherRootObject>(jsonString);

                return (data, null);
            }
            catch (TaskCanceledException)
            {
                return (null, "Sorğu vaxtı bitdi (timeout). Yenidən cəhd edin.");
            }
            catch (HttpRequestException)
            {
                return (null, "İnternet bağlantısı yoxdur və ya server əlçatan deyil.");
            }
            catch (Exception ex)
            {
                return (null, $"Gözlənilməyən xəta: {ex.Message}");
            }
        }
    }

    internal class Program
    {
        static string GetWeatherEmoji(Condition? condition, int isDay)
        {
            if (condition == null) return "🌍";

            return condition.code switch
            {
                1000 => isDay == 1 ? "☀️" : "🌙",
                1003 => "⛅",
                1006 or 1009 => "☁️",
                1030 or 1135 or 1147 => "🌫️",
                1087 or 1273 or 1276 or 1279 or 1282 => "⛈️",
                1066 or 1114 or 1117 or 1210 or 1213 or 1216 or 1219 or 1222 or 1225
                    or 1255 or 1258 or 1237 or 1261 or 1264 => "❄️",
                1063 or 1150 or 1153 or 1168 or 1171 or 1180 or 1183 or 1186 or 1189
                    or 1192 or 1195 or 1198 or 1201 or 1240 or 1243 or 1246 => "🌧️",
                _ => "🌤️"
            };
        }

        static string GetTempColorEmoji(double temp)
        {
            if (temp <= 0) return "🥶";
            if (temp < 15) return "🧥";
            if (temp < 28) return "😊";
            return "🥵";
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═════════════════════════════════════════════╗");
            Console.WriteLine("║      🌍  HAVA MƏLUMATI SİSTEMİ  🌍          ║");
            Console.WriteLine("╠═════════════════════════════════════════════╣");
            Console.WriteLine("║  Dünyanın istənilən şəhərinin hava şəraiti  ║");
            Console.WriteLine("║  Çıxış üçün  exit  yazın                    ║");
            Console.WriteLine("╚═════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintLine(string icon, string label, string value, ConsoleColor color)
        {
            Console.Write($"  {icon}  {label,-18}");
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        static void PrintWeather(WeatherRootObject data)
        {
            var loc = data.location!;
            var cur = data.current!;
            string emoji = GetWeatherEmoji(cur.condition, cur.is_day);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ┌─────────────────────────────────────────┐");
            Console.WriteLine($"   📍 {loc.name}, {loc.country}");
            Console.WriteLine("  └─────────────────────────────────────────┘");
            Console.ResetColor();

            PrintLine(emoji, "Hava şəraiti:", cur.condition?.text ?? "Məlumat yoxdur", ConsoleColor.Yellow);
            PrintLine("🌡️", "Temperatur:", $"{cur.temp_c} °C {GetTempColorEmoji(cur.temp_c)}", ConsoleColor.Red);
            PrintLine("🤔", "Hiss edilən:", $"{cur.feelslike_c} °C", ConsoleColor.Magenta);
            PrintLine("💧", "Rütubət:", $"{cur.humidity} %", ConsoleColor.Blue);
            PrintLine("💨", "Küləyin sürəti:", $"{cur.wind_kph} km/saat", ConsoleColor.Cyan);
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            WeatherService weatherService = new WeatherService();

            PrintHeader();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("🔎 Şəhər adını daxil edin: ");
                Console.ResetColor();

                string? city = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(city))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("⚠️  Zəhmət olmasa şəhər adı yazın!\n");
                    Console.ResetColor();
                    continue;
                }

                if (city.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n👋 Proqram dayandırıldı. Sağ olun!");
                    Console.ResetColor();
                    break;
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("⏳ Məlumat yüklənir...");
                Console.ResetColor();

                var (weatherData, error) = await weatherService.GetWeatherData(city);

                if (weatherData?.location != null && weatherData.current != null)
                {
                    PrintWeather(weatherData);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ [Xəta]: {error ?? "Məlumat əldə edilə bilmədi!"}");
                    Console.ResetColor();
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\n" + new string('─', 47) + "\n");
                Console.ResetColor();
            }
        }
    }
}