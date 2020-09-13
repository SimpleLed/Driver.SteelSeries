using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HidSharp;

using Newtonsoft.Json;
using SimpleLed;
using SteelSeries.GameSenseSDK;

using DeviceTypes = SimpleLed.DeviceTypes;

namespace SteelSeriesSLSProvider
{
    public class SteelSeriesDriver : ISimpleLed
    {

        public T GetConfig<T>() where T : SLSConfigData
        {
            //TODO throw new NotImplementedException();
            return null;
        }

        public void PutConfig<T>(T config) where T : SLSConfigData
        {
            //TODO throw new NotImplementedException();
        }
        public class SteelSeriesHIDDevice
        {
            public int PID { get; set; }
            public string Name { get; set; }
            public int NumberOfLeds { get; set; }
            public string DeviceClass { get; set; }
            public Bitmap Image { get; set; }
            public SteelSeriesHIDDevice(int pid, string name, string deviceClass, int leds,string image)
            {
                PID = pid;
                Name = name;
                DeviceClass = deviceClass;
                NumberOfLeds = leds;

                Bitmap arctis5;

                Assembly myAssembly = Assembly.GetExecutingAssembly();
                using (Stream myStream = myAssembly.GetManifestResourceStream("SteelSeriesSLSProvider."+image+".png"))
                {
                    if (myStream != null)
                    {
                        Image = (Bitmap) System.Drawing.Image.FromStream(myStream);
                    }
                }
            }
        }

        private static List<SteelSeriesHIDDevice> HIDDevices = new List<SteelSeriesHIDDevice>
        {
            new SteelSeriesHIDDevice( 0x1250 , "SteelSeries Arctis 5 Game", DeviceTypes.Headset ,1,"arctis5"),
            new SteelSeriesHIDDevice( 0x1251 , "SteelSeries Arctis 5 Game - Dota 2 edition", DeviceTypes.Headset ,1,"arctis5"),
            new SteelSeriesHIDDevice( 0x1252 , "SteelSeries Arctis Pro Game", DeviceTypes.Headset ,1,"arctis5"),
            new SteelSeriesHIDDevice(0x1260, "Arctis 7 Game", DeviceTypes.Headset, 0,"arctis7"),
            new SteelSeriesHIDDevice(0x1290, "Arctis Pro Wireless", DeviceTypes.Headset, 0,"arctispro"),
            new SteelSeriesHIDDevice(0x1294, "Arctis Pro Wireless Game", DeviceTypes.Headset, 0,"arctispro"),
            new SteelSeriesHIDDevice(0x12A8, "Arctis 5 Game - PUBG edition", DeviceTypes.Headset, 1,"arctis5"),
            new SteelSeriesHIDDevice(0x12AA, "Arctis 5 Game - 2018 edition", DeviceTypes.Headset, 1,"arctis5"),
            new SteelSeriesHIDDevice(0x12AD, "Arctis 7 Game - 2018 edition", DeviceTypes.Headset, 1,"arctis7"),
            
            new SteelSeriesHIDDevice(0x1618, "APEX 7 TKL", DeviceTypes.Keyboard, 84,"apex7tkl"),

            new SteelSeriesHIDDevice(0x1824, "Rival 3",DeviceTypes.Mouse,3,"rival3")
        };

        public SteelSeries.GameSenseSDK.GameSenseSDK GameSenseSdk = new GameSenseSDK();
        public SteelSeriesDriver()
        {
            // SteelSeriesSDK.Initialize();
            GameSenseSdk.init("SSGESLS", "SimpleLed", 0);
        }
        private const int VENDOR_ID = 0x1038;


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public static int[] RemapKeyboard = new int[122];
        public void Configure(DriverDetails driverDetails)
        {
            int[,] mappy = new int[22, 6];
            int startx = 0;
            int starty = 0;
            int x = 0;
            int y = 0;
            for (int i = 0; i < 122; i++)
            {
                Debug.WriteLine($"{x},{y}={i}");
                mappy[x, y] = i;

                y--;
                x++;
                if (y < 0)
                {
                    starty++;
                    if (starty > 5)
                    {
                        starty = 5;
                        startx++;
                    }

                    x = startx;
                    y = starty;
                }


            }

            int ct = 0;
            for (int yy = 0; yy < 6; yy++)
            {
                for (int xx = 0; xx < 22; xx++)
                {
                    RemapKeyboard[ct] = mappy[xx, yy];
                }
            }
        }

        public List<ControlDevice> GetDevices()
        {
            

            var devices = DeviceList.Local.GetHidDevices(VENDOR_ID);
            var result = new List<ControlDevice>();
            string output = "";
            //USBClassLibrary.USBClass usbclass = new USBClassLibrary.USBClass();
            var usbDevices = GetUSBDevices();

            Dictionary<int, string> hids = Enum.GetValues(typeof(USBHIDCodes))
                .Cast<USBHIDCodes>()
                .ToDictionary(t => (int)t, t => t.ToString());

            string json = JsonConvert.SerializeObject(hids);

            foreach (var hidDevice in devices)
            {
                try
                {
                    Bitmap img = null;
                    
                    // var r = USBClass.GetUSBDevice((uint)hidDevice.VendorID, (uint)hidDevice.ProductID);
                    var related = usbDevices.Where(t => t.VEN == "1038").ToList();
                    Debug.WriteLine(usbDevices);
                    var ssUsb = usbDevices.Where(x => x.DeviceID.Contains("VID_1038"));

                    foreach (var ui in ssUsb)
                    {
                        Console.WriteLine($"{ui.Description} - {ui.DeviceID} - {ui.VEN} - {ui.PID}");
                        output = output + $"{ui.Description} - {ui.DeviceID} - {ui.VEN} - {ui.PID}" + "\r\n";
                    }

                    int numberOfLeds = 0;
                    string deviceType = DeviceTypes.Other;
                    string name = "";

                    if (related.Any(x => x.Description == "USB Audio Device"))
                    {
                        deviceType = DeviceTypes.Headset;
                        numberOfLeds = 1;
                        name = "SteelSeries Headphones";
                    }

                    if (HIDDevices.Any(y=>y.PID==hidDevice.ProductID))
                    {
                        var hid = HIDDevices.First(x => x.PID == hidDevice.ProductID);
                        deviceType = hid.DeviceClass;
                        numberOfLeds = hid.NumberOfLeds;
                        name = hid.Name;
                        img = hid.Image;
                    }

                    int pid = hidDevice.ProductID;

                    /*
                     Enable to fake 
                    deviceType = DeviceTypes.Keyboard;
                    pid = 0x1618;
                    */

                    if (deviceType == DeviceTypes.Keyboard)
                    {
                        /*
                        if (File.Exists("SteelSeriesKBMaps\\" + pid + ".json"))
                        {
                            string js = File.ReadAllText("SteelSeriesKBMaps\\" + pid + ".json");
                            hids = JsonConvert.DeserializeObject<Dictionary<int, string>>(js);
                        }

                        numberOfLeds = hids.Count;
                        */

                        numberOfLeds = 122;
                    }


                    ControlDevice device = new ControlDevice
                    {
                        LEDs = new ControlDevice.LedUnit[numberOfLeds],
                        Driver = this,
                        Name = name,
                        DeviceType = deviceType,
                        ProductImage = img
                    };

                    if (deviceType == DeviceTypes.Keyboard)
                    {
                        int i = 0;
                        foreach (var mp in hids)
                        {
                            device.LEDs[i] =
                                new ControlDevice.LedUnit
                                {
                                    LEDName = mp.Value,
                                    Data = new SteelSeriesLedData
                                    {
                                        LEDNumber = i,
                                        KeyCode = mp.Key
                                    }
                                };

                            i++;
                        }
                    }
                    else
                    {


                        for (int i = 0; i < numberOfLeds; i++)
                        {

                            device.LEDs[i] =
                                new ControlDevice.LedUnit
                                {
                                    LEDName = "LED " + i,
                                    Data = new SteelSeriesLedData
                                    {
                                        LEDNumber = i,
                                        KeyCode = 2
                                    }
                                };
                        }
                    }

                    result.Add(device);
                }
                catch
                {
                }
            }

            File.WriteAllText("SSHardware.txt", output);
            return result;
        }

        public class SteelSeriesLedData : ControlDevice.LEDData
        {
            public int KeyCode { get; set; }
        }

        public void Push(ControlDevice controlDevice)
        {
            
            GameSensePayloadPeripheryColorEventJSON payload = new GameSensePayloadPeripheryColorEventJSON();

            GameSenseSdk.setupEvent(payload);
            switch (controlDevice.DeviceType)
            {
                case DeviceTypes.Headset:
                    LEDColor color = controlDevice.LEDs.First().Color;
                    GameSenseSdk.setHeadsetColor((byte)color.Red, (byte)color.Green, (byte)color.Blue, payload);
                    GameSenseSdk.sendFullColorRequest(payload);
                    break;

                case DeviceTypes.Keyboard:

                    List<byte> hids = new List<byte>();
                    List<Tuple<byte, byte, byte>> colors = new List<Tuple<byte, byte, byte>>();
                    foreach (var controlDeviceLeD in controlDevice.LEDs)
                    {
                        LEDColor clr = controlDeviceLeD.Color;
                        hids.Add((byte)((SteelSeriesLedData)controlDeviceLeD.Data).KeyCode);
                        colors.Add(new Tuple<byte, byte, byte>((byte)clr.Red, (byte)clr.Blue, (byte)clr.Green));
                    }

                    GameSenseSdk.setKeyboardColors(hids, colors, payload);
                    break;

                case DeviceTypes.Mouse:
                    GameSenseSdk.setMouseScrollWheelColor((byte)controlDevice.LEDs[0].Color.Red, (byte)controlDevice.LEDs[0].Color.Green, (byte)controlDevice.LEDs[0].Color.Blue, payload);
                    GameSenseSdk.setMouseColor((byte)controlDevice.LEDs[1].Color.Red, (byte)controlDevice.LEDs[1].Color.Green, (byte)controlDevice.LEDs[1].Color.Blue, payload);
                    GameSenseSdk.setMouseLogoColor((byte)controlDevice.LEDs[2].Color.Red, (byte)controlDevice.LEDs[2].Color.Green, (byte)controlDevice.LEDs[2].Color.Blue, payload);
                    
                    GameSenseSdk.sendFullColorRequest(payload);
                    break;

            }
            
            
        }

        public void Pull(ControlDevice controlDevice)
        {
            //throw new NotImplementedException();
        }

        public DriverProperties GetProperties()
        {
            return new DriverProperties
            {
                SupportsPush = true,
                IsSource = false,
                SupportsPull = false,
                Id = Guid.Parse("b9440d02-8ca3-4e35-a9a3-88b024cc0e2d")
            };
        }

        public string Name() => "Steel Series Driver";

        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                collection = searcher.Get();

            foreach (ManagementBaseObject device in collection)
            {
                //USB\VID_1038&PID_12AA&MI_00\7&2662CC34&0&0000
                var dvc = (new USBDeviceInfo(
                    (string)device.GetPropertyValue("DeviceID"),
                    (string)device.GetPropertyValue("PNPDeviceID"),
                    (string)device.GetPropertyValue("Description")
                ));


                var parts = dvc.DeviceID.Split('\\');
                dvc.Root = parts[0];
                if (dvc.DeviceID.Contains("VEN_") || dvc.DeviceID.Contains("VID_"))
                {
                    var things = parts[1].Split('&');

                    dvc.VEN = things[0].Split('_').Last();
                    dvc.PID = things[1].Split('_').Last();
                }

                devices.Add(dvc);
            }

            collection.Dispose();
            return devices;
        }

        class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
            public string VEN { get; set; }
            public string PID { get; set; }
            public string Root { get; set; }
        }
    }
}
