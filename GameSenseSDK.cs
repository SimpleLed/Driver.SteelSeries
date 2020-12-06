// GameSenseSDK C# beta by brainbug89 is licensed under CC BY-NC-SA 4.0

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SimpleLed;
using SteelSeriesSLSProvider.Model;

namespace SteelSeries.GameSenseSDK
{

    public class SSECorePropsJSON
    {
        public String address { get; set; }
        public String encrypted_address { get; set; }
    }

    public class GameSensePayloadLISPHandlerJSON
    {
        public String game { get; set; }
        public String golisp { get; set; }
    }

    public class GameSensePayloadHeartbeatJSON
    {
        public String game { get; set; }
    }

    public class GameSensePayloadGameDataJSON
    {
        public String game { get; set; }
        public String game_display_name { get; set; }
        public byte icon_color_id { get; set; }
    }

    public class GameSensePayloadPeripheryColorEventJSON
    {
        public String game { get; set; }
        public String Event { get; set; }
        public String data { get; set; }
    }

    public class GameSenseSDK
    {
        private String COREPROPS_JSON_PATH = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/SteelSeries/SteelSeries Engine 3/coreProps.json";
        private String sseGameName = "SSGESSL";
        private String sseGameDisplayname = "SimpleLED Driver";

     

        private const string EVENT_NAME = "UPDATELEDS";
        private static readonly string HANDLER = $@"(define (getZone x)
  (case x
    {string.Join(Environment.NewLine, Enum.GetValues(typeof(SteelSeriesLedId))
            .Cast<SteelSeriesLedId>()
            .Select(x => x.GetAPIName())
            .Select(ledId => $"    ((\"{ledId}\") {ledId}:)"))}
  ))
(handler ""{EVENT_NAME}""
  (lambda (data)
    (let* ((device (value: data))
           (zones (zones: data))
           (colors (colors: data)))
      (on-device device show-on-zones: colors (map (lambda (x) (getZone x)) zones)))))
(add-event-per-key-zone-use ""{EVENT_NAME}"" ""all"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-1-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-2-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-3-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-4-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-5-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-6-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-7-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-8-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-12-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-17-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-24-zone"")
(add-event-zone-use-with-specifier ""{EVENT_NAME}"" ""all"" ""rgb-103-zone"")";

        private static String sseAddress = "";
        private Game _game;
        public void init(String sseGameName, String sseGameDisplayname, byte iconColorID)
        {
            _game = new Game(sseGameName, sseGameDisplayname);
            _event = new Event(_game, EVENT_NAME);
            Console.WriteLine("Loading Props: "+ COREPROPS_JSON_PATH);
            if (!File.Exists(COREPROPS_JSON_PATH))
                throw new FileNotFoundException($"Core Props file could not be found at \"{COREPROPS_JSON_PATH}\"");

            // read %PROGRAMDATA%/SteelSeries/SteelSeries Engine 3/coreProps.json
            SSECorePropsJSON coreProps = JsonConvert.DeserializeObject<SSECorePropsJSON>(File.ReadAllText(@COREPROPS_JSON_PATH));
            sseAddress = coreProps.address;

            // setup "game" meda data
            this.sseGameName = sseGameName;
            this.sseGameDisplayname = sseGameDisplayname;
            setupGame(iconColorID);

            // setup golisp handler
            setupLISPHandlers();
        }

        public void setupEvent(GameSensePayloadPeripheryColorEventJSON payload)
        {
            payload.game = sseGameName;
            payload.Event = "COLOR";
            payload.data = "{";
        }

        public void setPeripheryColor(byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("periph", red, green, blue, payload);
        }

        public void setMouseColor(byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("mouse", red, green, blue, payload);
        }

        public void setMouseScrollWheelColor(byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("mousewheel", red, green, blue, payload);
        }

        public void setMouseLogoColor(byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("mouselogo", red, green, blue, payload);
        }



        public void setHeadsetColor(byte red, byte green, byte blue, byte red2, byte green2, byte blue2, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("rgb-2-zone", red, green, blue, payload);
        }

        public void setHeadse2tColor(byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            sendColor("headset", red, green, blue, payload);
        }

        public void sendColor(String deviceType, byte red, byte green, byte blue, GameSensePayloadPeripheryColorEventJSON payload)
        {
            payload.data += "\"" + deviceType + "\":{\"color\": [" + red + ", " + green + ", " + blue + "]},";
        }

        public void setMousepadColor(List<Tuple<byte, byte, byte>> colors, GameSensePayloadPeripheryColorEventJSON payload)
        {
            List<string> zones = new List<string>(new string[] { "mpone", "mptwo", "mpthree", "mpfour", "mpfive", "mpsix", "mpseven", "mpeight", "mpnine", "mpten", "mpeleven", "mptwelve" });
            if (colors.Count == 2)
            {
                payload.data += "\"mousepadtwozone\":{";

                for (int i = 0; i < 2; i++)
                {
                    payload.data += "\"" + zones[i] + "\": [" + colors[i].Item1 + ", " + colors[i].Item2 + ", " + colors[i].Item3 + "],";
                }
                payload.data = payload.data.TrimEnd(',');
                payload.data += "},";
            }
            else if (colors.Count == 12)
            {
                payload.data += "\"mousepad\":{";
                payload.data += "\"colors\":[";
                foreach (Tuple<byte, byte, byte> color in colors)
                {
                    payload.data += "[" + color.Item1 + ", " + color.Item2 + ", " + color.Item3 + "],";
                }
                payload.data = payload.data.TrimEnd(',');
                payload.data += "]},";
            }
        }

        public void setKeyboardColors(List<byte> hids, List<Tuple<byte, byte, byte>> colors, GameSensePayloadPeripheryColorEventJSON payload)
        {
            payload.data += "\"keyboard\":{";
            payload.data += "\"hids\":";
            payload.data += JsonConvert.SerializeObject(hids);
            payload.data += ",";
            payload.data += "\"colors\":[";
            foreach (Tuple<byte, byte, byte> color in colors)
            {
                payload.data += "[" + color.Item1 + ", " + color.Item2 + ", " + color.Item3 + "],";
            }
            // JSON doesn't allow trailing commas
            payload.data = payload.data.TrimEnd(',');
            payload.data += "]";
            payload.data += "},";
        }



        public void sendFullColorRequest(GameSensePayloadPeripheryColorEventJSON payload)
        {
            payload.data = payload.data.TrimEnd(',');
            payload.data += "}";

            // sending POST request
            String json = JsonConvert.SerializeObject(payload);
            sendPostRequest("http://" + sseAddress + "/game_event", json);
        }

        public void sendRawRequest(string json)
        {
            sendPostRequest("http://" + sseAddress + "/game_event", json);
        }

        public void sendHeartbeat()
        {
            GameSensePayloadHeartbeatJSON payload = new GameSensePayloadHeartbeatJSON();
            payload.game = sseGameName;
            // sending POST request
            String json = JsonConvert.SerializeObject(payload);
            sendPostRequest("http://" + sseAddress + "/game_heartbeat", json);
        }

        internal string _baseUrl => "http://" + sseAddress;
        public void sendStop()
        {
            GameSensePayloadPeripheryColorEventJSON payload = new GameSensePayloadPeripheryColorEventJSON();
            payload.game = sseGameName;
            payload.Event = "STOP";
            // sending POST request
            String json = JsonConvert.SerializeObject(payload);
            sendPostRequest("http://" + sseAddress + "/game_event", json);
        }


        private static readonly HttpClient _client = new HttpClient();
        private string PostJson(string urlSuffix, object o)
        {
            string payload = JsonConvert.SerializeObject(o);
            return _client.PostAsync(_baseUrl + urlSuffix, new StringContent(payload, Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync().Result;
        }


        internal void UpdateLeds(string device, Dictionary<string, int[]> data)
        {
            _event.Data.Clear();
            _event.Data.Add("value", device);
            _event.Data.Add("colors", data.Values.ToList());
            _event.Data.Add("zones", data.Keys.ToList());

            TriggerEvent(_event);
        }

        private Event _event=null;
        private string TriggerEvent(Event e) => PostJson("/game_event", e);
        private string RegisterGoLispHandler(GoLispHandler handler) => PostJson("/load_golisp_handlers", handler);
        private string RegisterEvent(Event e) => PostJson("/register_game_event", e);
        private string UnregisterEvent(Event e) => PostJson("/remove_game_event", e);
        private string RegisterGame(Game game) => PostJson("/game_metadata", game);
        private string UnregisterGame(Game game) => PostJson("/remove_game", game);
        private string StopGame(Game game) => PostJson("/stop_game", game);
        private string SendHeartbeat(Game game) => PostJson("/game_heartbeat", game);

        private void setupLISPHandlers()
        {
            RegisterGoLispHandler(new GoLispHandler(_game, HANDLER));
        }

        private void setupGame(byte iconColorID)
        {
            GameSensePayloadGameDataJSON payload = new GameSensePayloadGameDataJSON();
            payload.game = sseGameName;
            payload.game_display_name = sseGameDisplayname;
            payload.icon_color_id = iconColorID;
            // sending POST request
            String json = JsonConvert.SerializeObject(payload);
            sendPostRequest("http://" + sseAddress + "/game_metadata", json);
        }

        private void sendPostRequest(String address, String payload)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest) WebRequest.Create(address);
                httpWebRequest.ReadWriteTimeout = 30;
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(payload);
                }

                // sending POST request
                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    streamReader.ReadToEnd();
                }
            }
            catch
            {
            }
        }

    }

}