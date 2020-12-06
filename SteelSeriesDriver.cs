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
using System.Timers;
using Newtonsoft.Json;
using SimpleLed;
using SteelSeries.GameSenseSDK;

using DeviceTypes = SimpleLed.DeviceTypes;

namespace SteelSeriesSLSProvider
{
    internal static class ColorExtensions
    {
        internal static int[] ToIntArray(this LEDColor color) => new int[] { color.Red, color.Green, color.Blue };
    }

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
            public SteelSeriesDeviceType SteelSeriesDeviceType { get; set; }
            public Dictionary<LedId, SteelSeriesLedId> Mapping { get; set; }
            public SteelSeriesHIDDevice(int pid, string name, string deviceClass, int leds, string image, SteelSeriesDeviceType ssDeviceType, Dictionary<LedId, SteelSeriesLedId> map)
            {
                PID = pid;
                Name = name;
                DeviceClass = deviceClass;
                NumberOfLeds = leds;
                SteelSeriesDeviceType = ssDeviceType;
                Mapping = map;
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                using (Stream myStream = myAssembly.GetManifestResourceStream("SteelSeriesSLSProvider." + image + ".png"))
                {
                    if (myStream != null)
                    {
                        Image = (Bitmap)System.Drawing.Image.FromStream(myStream);
                    }
                }
            }
        }

        private static List<SteelSeriesHIDDevice> HIDDevices = new List<SteelSeriesHIDDevice>
        {
            new SteelSeriesHIDDevice( 0x1250 , "SteelSeries Arctis 5 Game", DeviceTypes.Headset ,2,"arctis5", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice( 0x1251 , "SteelSeries Arctis 5 Game - Dota 2 edition", DeviceTypes.Headset ,2,"arctis5", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice( 0x1252 , "SteelSeries Arctis Pro Game", DeviceTypes.Headset ,2,"arctis5", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x1260, "Arctis 7 Game", DeviceTypes.Headset, 0,"arctis7", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x1290, "Arctis Pro Wireless", DeviceTypes.Headset, 0,"arctispro", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x1294, "Arctis Pro Wireless Game", DeviceTypes.Headset, 0,"arctispro", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x12A8, "Arctis 5 Game - PUBG edition", DeviceTypes.Headset, 2,"arctis5", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x12AA, "Arctis 5 Game - 2018 edition", DeviceTypes.Headset, 2,"arctis5", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),
            new SteelSeriesHIDDevice(0x12AD, "Arctis 7 Game - 2018 edition", DeviceTypes.Headset, 2,"arctis7", SteelSeriesDeviceType.TwoZone, RGBNetGubbins.HEADSET_TWO_ZONE),

            new SteelSeriesHIDDevice(0x1618, "APEX 7 TKL", DeviceTypes.Keyboard, RGBNetGubbins.KEYBOARD_TKL_MAPPING_UK.Count,"apex7tkl", SteelSeriesDeviceType.PerKey, RGBNetGubbins.KEYBOARD_TKL_MAPPING_UK),
            new SteelSeriesHIDDevice(0x161C, "APEX 5", DeviceTypes.Keyboard, RGBNetGubbins.KEYBOARD_MAPPING_UK.Count,"apex7tkl", SteelSeriesDeviceType.PerKey, RGBNetGubbins.KEYBOARD_MAPPING_UK),
            new SteelSeriesHIDDevice(0x1612, "APEX 7", DeviceTypes.Keyboard, RGBNetGubbins.KEYBOARD_MAPPING_UK.Count,"apex7tkl", SteelSeriesDeviceType.PerKey, RGBNetGubbins.KEYBOARD_MAPPING_UK),
            new SteelSeriesHIDDevice(0x0616, "APEX M750", DeviceTypes.Keyboard, RGBNetGubbins.KEYBOARD_MAPPING_UK.Count,"apex7tkl", SteelSeriesDeviceType.PerKey, RGBNetGubbins.KEYBOARD_MAPPING_UK),

            new SteelSeriesHIDDevice(0x1824, "Rival 3",DeviceTypes.Mouse,3,"rival3", SteelSeriesDeviceType.ThreeZone, RGBNetGubbins.MOUSE_THREE_ZONE),
            new SteelSeriesHIDDevice(0x170E, "Rival 500", DeviceTypes.Mouse,2,"rival500", SteelSeriesDeviceType.Mouse, RGBNetGubbins.MOUSE_TWO_ZONE),
            new SteelSeriesHIDDevice(0x184C, "Rival 3", DeviceTypes.Mouse,3,"rival3", SteelSeriesDeviceType.ThreeZone, RGBNetGubbins.MOUSE_THREE_ZONE)
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

        public event Events.DeviceChangeEventHandler DeviceAdded;
        public event Events.DeviceChangeEventHandler DeviceRemoved;

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

            Timer tmr = new Timer(2000);
            tmr.AutoReset = true;
            tmr.Elapsed += (sender, args) =>
            {
                if (DeviceAdded != null)
                {
                    tmr.Stop();
                    GetDevices();
                }
            };

            tmr.Start();



        }

        List<SteelSeriesControlDevice> foundDevices = new List<SteelSeriesControlDevice>();
        public void GetDevices()
        {
           
            var supportedUsbs = this.GetProperties().SupportedDevices;
            var connected = SLSManager.GetSupportedDevices(supportedUsbs);

            foreach (var supportedDevice in connected)
            {

                AddDevice(supportedDevice.HID.Value);

            }
        }

        public class SteelSeriesLedData : ControlDevice.LEDData
        {
            public int KeyCode { get; set; }
            public string ZoneName { get; set; }
        }

        public void Push(ControlDevice controlDevice)
        {
            SteelSeriesControlDevice cs = (SteelSeriesControlDevice) controlDevice;

            Dictionary<string, int[]> mdata = new Dictionary<string, int[]>();
            foreach (ControlDevice.LedUnit controlDeviceLeD in controlDevice.LEDs)
            {
                mdata.Add(((SteelSeriesLedData)controlDeviceLeD.Data).ZoneName, controlDeviceLeD.Color.ToIntArray());
            }

            GameSenseSdk.UpdateLeds(cs.SSDeviceType.GetAPIName(), mdata);
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
                Id = Guid.Parse("996534cc-d81f-4fbb-b49e-7bfd6449dae9"),
                Author = "mad ninja",
                Blurb = "Work in progress driver for Steel Series devices",
                CurrentVersion = new ReleaseNumber(0, 0, 0, 10),
                GitHubLink = "https://github.com/SimpleLed/Driver.SteelSeries",
                IsPublicRelease = false,
                SupportedDevices = HIDDevices.Select(x => new USBDevice()
                {
                    DeviceName = x.Name,
                    DeviceType = x.DeviceClass,
                    VID = VENDOR_ID,
                    HID = x.PID,
                    ManufacturerName = "Steel Series",

                }).ToList()

            };
        }
        public string Name() => "Steel Series Driver";
        public void InterestedUSBChange(int VID, int PID, bool connected)
        {
            Debug.WriteLine(VID + "_" + PID + "_" + connected);
            if (connected)
            {
                AddDevice(PID);
            }
            else
            {
                if (foundDevices.Any(x => x.HID == PID))
                {
                    SteelSeriesControlDevice dev = foundDevices.First(x => x.HID == PID);
                    DeviceRemoved?.Invoke(this, new Events.DeviceChangeEventArgs(dev));
                    foundDevices.Remove(dev);
                }
            }
        }

        public void AddDevice(int PID)
        {
            var supportedDevice = HIDDevices.First(x => x.PID == PID);

            SteelSeriesControlDevice device = new SteelSeriesControlDevice
            {
                LEDs = new ControlDevice.LedUnit[supportedDevice.NumberOfLeds],
                Driver = this,
                Name = supportedDevice.Name,
                DeviceType = supportedDevice.DeviceClass,
                ProductImage = supportedDevice.Image,
                HID = PID,
                SSDeviceType = supportedDevice.SteelSeriesDeviceType
            };

            List<KeyValuePair<LedId, SteelSeriesLedId>> mp = supportedDevice.Mapping.ToList();
            for (int i = 0; i < supportedDevice.NumberOfLeds; i++)
            {

                int ln = i;
                int kc = 2;
                SteelSeriesLedId tp = SteelSeriesLedId.ZoneOne;
                if (i < mp.Count)
                {
                    ln = (int)mp[i].Key;
                    kc = (int) mp[i].Value;
                    tp = mp[i].Value;
                }

                device.LEDs[i] =
                    new ControlDevice.LedUnit
                    {
                        LEDName = "LED " + i,
                        Data = new SteelSeriesLedData
                        {
                            LEDNumber = ln,
                            KeyCode = kc,
                            ZoneName = tp.GetAPIName()
                        }
                    };
            }

            DeviceAdded?.Invoke(this, new Events.DeviceChangeEventArgs(device));

            foundDevices.Add(device);
        }

        public event EventHandler DeviceRescanRequired;

    }

    public class SteelSeriesControlDevice : ControlDevice
    {
        public int HID { get; set; }
        public SteelSeriesDeviceType SSDeviceType { get; set; }
    }

    public static class RGBNetGubbins
    {


        internal static readonly Dictionary<LedId, SteelSeriesLedId> KEYBOARD_MAPPING_UK = new Dictionary<LedId, SteelSeriesLedId>
        {
            { LedId.Logo, SteelSeriesLedId.Logo },
            { LedId.Keyboard_Escape, SteelSeriesLedId.Escape },
            { LedId.Keyboard_F1, SteelSeriesLedId.F1 },
            { LedId.Keyboard_F2, SteelSeriesLedId.F2 },
            { LedId.Keyboard_F3, SteelSeriesLedId.F3 },
            { LedId.Keyboard_F4, SteelSeriesLedId.F4 },
            { LedId.Keyboard_F5, SteelSeriesLedId.F5 },
            { LedId.Keyboard_F6, SteelSeriesLedId.F6 },
            { LedId.Keyboard_F7, SteelSeriesLedId.F7 },
            { LedId.Keyboard_F8, SteelSeriesLedId.F8 },
            { LedId.Keyboard_F9, SteelSeriesLedId.F9 },
            { LedId.Keyboard_F10, SteelSeriesLedId.F10 },
            { LedId.Keyboard_F11, SteelSeriesLedId.F11 },
            { LedId.Keyboard_GraveAccentAndTilde, SteelSeriesLedId.Backqoute },
            { LedId.Keyboard_1, SteelSeriesLedId.Keyboard1 },
            { LedId.Keyboard_2, SteelSeriesLedId.Keyboard2 },
            { LedId.Keyboard_3, SteelSeriesLedId.Keyboard3 },
            { LedId.Keyboard_4, SteelSeriesLedId.Keyboard4 },
            { LedId.Keyboard_5, SteelSeriesLedId.Keyboard5 },
            { LedId.Keyboard_6, SteelSeriesLedId.Keyboard6 },
            { LedId.Keyboard_7, SteelSeriesLedId.Keyboard7 },
            { LedId.Keyboard_8, SteelSeriesLedId.Keyboard8 },
            { LedId.Keyboard_9, SteelSeriesLedId.Keyboard9 },
            { LedId.Keyboard_0, SteelSeriesLedId.Keyboard0 },
            { LedId.Keyboard_MinusAndUnderscore, SteelSeriesLedId.Dash },
            { LedId.Keyboard_Tab, SteelSeriesLedId.Tab },
            { LedId.Keyboard_Q, SteelSeriesLedId.Q },
            { LedId.Keyboard_W, SteelSeriesLedId.W },
            { LedId.Keyboard_E, SteelSeriesLedId.E },
            { LedId.Keyboard_R, SteelSeriesLedId.R },
            { LedId.Keyboard_T, SteelSeriesLedId.T },
            { LedId.Keyboard_Y, SteelSeriesLedId.Y },
            { LedId.Keyboard_U, SteelSeriesLedId.U },
            { LedId.Keyboard_I, SteelSeriesLedId.I },
            { LedId.Keyboard_O, SteelSeriesLedId.O },
            { LedId.Keyboard_P, SteelSeriesLedId.P },
            { LedId.Keyboard_BracketLeft, SteelSeriesLedId.LBracket },
            { LedId.Keyboard_CapsLock, SteelSeriesLedId.Caps },
            { LedId.Keyboard_A, SteelSeriesLedId.A },
            { LedId.Keyboard_S, SteelSeriesLedId.S },
            { LedId.Keyboard_D, SteelSeriesLedId.D },
            { LedId.Keyboard_F, SteelSeriesLedId.F },
            { LedId.Keyboard_G, SteelSeriesLedId.G },
            { LedId.Keyboard_H, SteelSeriesLedId.H },
            { LedId.Keyboard_J, SteelSeriesLedId.J },
            { LedId.Keyboard_K, SteelSeriesLedId.K },
            { LedId.Keyboard_L, SteelSeriesLedId.L },
            { LedId.Keyboard_SemicolonAndColon, SteelSeriesLedId.Semicolon },
            { LedId.Keyboard_ApostropheAndDoubleQuote, SteelSeriesLedId.Quote },
            { LedId.Keyboard_LeftShift, SteelSeriesLedId.LShift },
            { LedId.Keyboard_NonUsTilde, SteelSeriesLedId.Pound },
            { LedId.Keyboard_Z, SteelSeriesLedId.Z },
            { LedId.Keyboard_X, SteelSeriesLedId.X },
            { LedId.Keyboard_C, SteelSeriesLedId.C },
            { LedId.Keyboard_V, SteelSeriesLedId.V },
            { LedId.Keyboard_B, SteelSeriesLedId.B },
            { LedId.Keyboard_N, SteelSeriesLedId.N },
            { LedId.Keyboard_M, SteelSeriesLedId.M },
            { LedId.Keyboard_CommaAndLessThan, SteelSeriesLedId.Comma },
            { LedId.Keyboard_PeriodAndBiggerThan, SteelSeriesLedId.Period },
            { LedId.Keyboard_SlashAndQuestionMark, SteelSeriesLedId.Slash },
            { LedId.Keyboard_LeftCtrl, SteelSeriesLedId.LCtrl },
            { LedId.Keyboard_LeftGui, SteelSeriesLedId.LWin },
            { LedId.Keyboard_LeftAlt, SteelSeriesLedId.LAlt },
            { LedId.Keyboard_Space, SteelSeriesLedId.Spacebar },
            { LedId.Keyboard_RightAlt, SteelSeriesLedId.RAlt },
            { LedId.Keyboard_RightGui, SteelSeriesLedId.RWin },
            { LedId.Keyboard_Application, SteelSeriesLedId.SSKey },
            { LedId.Keyboard_F12, SteelSeriesLedId.F12 },
            { LedId.Keyboard_PrintScreen, SteelSeriesLedId.PrintScreen },
            { LedId.Keyboard_ScrollLock, SteelSeriesLedId.ScrollLock },
            { LedId.Keyboard_PauseBreak, SteelSeriesLedId.Pause },
            { LedId.Keyboard_Insert, SteelSeriesLedId.Insert },
            { LedId.Keyboard_Home, SteelSeriesLedId.Home },
            { LedId.Keyboard_PageUp, SteelSeriesLedId.PageUp },
            { LedId.Keyboard_BracketRight, SteelSeriesLedId.RBracket },
            { LedId.Keyboard_Backslash, SteelSeriesLedId.Backslash },
            { LedId.Keyboard_Enter, SteelSeriesLedId.Return },
            { LedId.Keyboard_EqualsAndPlus, SteelSeriesLedId.Equal },
            { LedId.Keyboard_Backspace, SteelSeriesLedId.Backspace },
            { LedId.Keyboard_Delete, SteelSeriesLedId.Delete },
            { LedId.Keyboard_End, SteelSeriesLedId.End },
            { LedId.Keyboard_PageDown, SteelSeriesLedId.PageDown },
            { LedId.Keyboard_RightShift, SteelSeriesLedId.RShift },
            { LedId.Keyboard_RightCtrl, SteelSeriesLedId.RCtrl },
            { LedId.Keyboard_ArrowUp, SteelSeriesLedId.UpArrow },
            { LedId.Keyboard_ArrowLeft, SteelSeriesLedId.LeftArrow },
            { LedId.Keyboard_ArrowDown, SteelSeriesLedId.DownArrow },
            { LedId.Keyboard_ArrowRight, SteelSeriesLedId.RightArrow },
            { LedId.Keyboard_NumLock, SteelSeriesLedId.KeypadNumLock },
            { LedId.Keyboard_NumSlash, SteelSeriesLedId.KeypadDivide },
            { LedId.Keyboard_NumAsterisk, SteelSeriesLedId.KeypadTimes },
            { LedId.Keyboard_NumMinus, SteelSeriesLedId.KeypadMinus },
            { LedId.Keyboard_NumPlus, SteelSeriesLedId.KeypadPlus },
            { LedId.Keyboard_NumEnter, SteelSeriesLedId.KeypadEnter },
            { LedId.Keyboard_Num7, SteelSeriesLedId.Keypad7 },
            { LedId.Keyboard_Num8, SteelSeriesLedId.Keypad8 },
            { LedId.Keyboard_Num9, SteelSeriesLedId.Keypad9 },
            { LedId.Keyboard_Num4, SteelSeriesLedId.Keypad4 },
            { LedId.Keyboard_Num5, SteelSeriesLedId.Keypad5 },
            { LedId.Keyboard_Num6, SteelSeriesLedId.Keypad6 },
            { LedId.Keyboard_Num1, SteelSeriesLedId.Keypad1 },
            { LedId.Keyboard_Num2, SteelSeriesLedId.Keypad2 },
            { LedId.Keyboard_Num3, SteelSeriesLedId.Keypad3 },
            { LedId.Keyboard_Num0, SteelSeriesLedId.Keypad0 },
            { LedId.Keyboard_NumPeriodAndDelete, SteelSeriesLedId.KeypadPeriod }
        };

    internal static readonly Dictionary<LedId, SteelSeriesLedId> KEYBOARD_TKL_MAPPING_UK = new Dictionary<LedId, SteelSeriesLedId>
        {
            { LedId.Logo, SteelSeriesLedId.Logo },
            { LedId.Keyboard_Escape, SteelSeriesLedId.Escape },
            { LedId.Keyboard_F1, SteelSeriesLedId.F1 },
            { LedId.Keyboard_F2, SteelSeriesLedId.F2 },
            { LedId.Keyboard_F3, SteelSeriesLedId.F3 },
            { LedId.Keyboard_F4, SteelSeriesLedId.F4 },
            { LedId.Keyboard_F5, SteelSeriesLedId.F5 },
            { LedId.Keyboard_F6, SteelSeriesLedId.F6 },
            { LedId.Keyboard_F7, SteelSeriesLedId.F7 },
            { LedId.Keyboard_F8, SteelSeriesLedId.F8 },
            { LedId.Keyboard_F9, SteelSeriesLedId.F9 },
            { LedId.Keyboard_F10, SteelSeriesLedId.F10 },
            { LedId.Keyboard_F11, SteelSeriesLedId.F11 },
            { LedId.Keyboard_GraveAccentAndTilde, SteelSeriesLedId.Backqoute },
            { LedId.Keyboard_1, SteelSeriesLedId.Keyboard1 },
            { LedId.Keyboard_2, SteelSeriesLedId.Keyboard2 },
            { LedId.Keyboard_3, SteelSeriesLedId.Keyboard3 },
            { LedId.Keyboard_4, SteelSeriesLedId.Keyboard4 },
            { LedId.Keyboard_5, SteelSeriesLedId.Keyboard5 },
            { LedId.Keyboard_6, SteelSeriesLedId.Keyboard6 },
            { LedId.Keyboard_7, SteelSeriesLedId.Keyboard7 },
            { LedId.Keyboard_8, SteelSeriesLedId.Keyboard8 },
            { LedId.Keyboard_9, SteelSeriesLedId.Keyboard9 },
            { LedId.Keyboard_0, SteelSeriesLedId.Keyboard0 },
            { LedId.Keyboard_MinusAndUnderscore, SteelSeriesLedId.Dash },
            { LedId.Keyboard_Tab, SteelSeriesLedId.Tab },
            { LedId.Keyboard_Q, SteelSeriesLedId.Q },
            { LedId.Keyboard_W, SteelSeriesLedId.W },
            { LedId.Keyboard_E, SteelSeriesLedId.E },
            { LedId.Keyboard_R, SteelSeriesLedId.R },
            { LedId.Keyboard_T, SteelSeriesLedId.T },
            { LedId.Keyboard_Y, SteelSeriesLedId.Y },
            { LedId.Keyboard_U, SteelSeriesLedId.U },
            { LedId.Keyboard_I, SteelSeriesLedId.I },
            { LedId.Keyboard_O, SteelSeriesLedId.O },
            { LedId.Keyboard_P, SteelSeriesLedId.P },
            { LedId.Keyboard_BracketLeft, SteelSeriesLedId.LBracket },
            { LedId.Keyboard_CapsLock, SteelSeriesLedId.Caps },
            { LedId.Keyboard_A, SteelSeriesLedId.A },
            { LedId.Keyboard_S, SteelSeriesLedId.S },
            { LedId.Keyboard_D, SteelSeriesLedId.D },
            { LedId.Keyboard_F, SteelSeriesLedId.F },
            { LedId.Keyboard_G, SteelSeriesLedId.G },
            { LedId.Keyboard_H, SteelSeriesLedId.H },
            { LedId.Keyboard_J, SteelSeriesLedId.J },
            { LedId.Keyboard_K, SteelSeriesLedId.K },
            { LedId.Keyboard_L, SteelSeriesLedId.L },
            { LedId.Keyboard_SemicolonAndColon, SteelSeriesLedId.Semicolon },
            { LedId.Keyboard_ApostropheAndDoubleQuote, SteelSeriesLedId.Quote },
            { LedId.Keyboard_LeftShift, SteelSeriesLedId.LShift },
            { LedId.Keyboard_NonUsTilde, SteelSeriesLedId.Pound },
            { LedId.Keyboard_Z, SteelSeriesLedId.Z },
            { LedId.Keyboard_X, SteelSeriesLedId.X },
            { LedId.Keyboard_C, SteelSeriesLedId.C },
            { LedId.Keyboard_V, SteelSeriesLedId.V },
            { LedId.Keyboard_B, SteelSeriesLedId.B },
            { LedId.Keyboard_N, SteelSeriesLedId.N },
            { LedId.Keyboard_M, SteelSeriesLedId.M },
            { LedId.Keyboard_CommaAndLessThan, SteelSeriesLedId.Comma },
            { LedId.Keyboard_PeriodAndBiggerThan, SteelSeriesLedId.Period },
            { LedId.Keyboard_SlashAndQuestionMark, SteelSeriesLedId.Slash },
            { LedId.Keyboard_LeftCtrl, SteelSeriesLedId.LCtrl },
            { LedId.Keyboard_LeftGui, SteelSeriesLedId.LWin },
            { LedId.Keyboard_LeftAlt, SteelSeriesLedId.LAlt },
            { LedId.Keyboard_Space, SteelSeriesLedId.Spacebar },
            { LedId.Keyboard_RightAlt, SteelSeriesLedId.RAlt },
            { LedId.Keyboard_RightGui, SteelSeriesLedId.RWin },
            { LedId.Keyboard_Application, SteelSeriesLedId.SSKey },
            { LedId.Keyboard_F12, SteelSeriesLedId.F12 },
            { LedId.Keyboard_PrintScreen, SteelSeriesLedId.PrintScreen },
            { LedId.Keyboard_ScrollLock, SteelSeriesLedId.ScrollLock },
            { LedId.Keyboard_PauseBreak, SteelSeriesLedId.Pause },
            { LedId.Keyboard_Insert, SteelSeriesLedId.Insert },
            { LedId.Keyboard_Home, SteelSeriesLedId.Home },
            { LedId.Keyboard_PageUp, SteelSeriesLedId.PageUp },
            { LedId.Keyboard_BracketRight, SteelSeriesLedId.RBracket },
            { LedId.Keyboard_Backslash, SteelSeriesLedId.Backslash },
            { LedId.Keyboard_Enter, SteelSeriesLedId.Return },
            { LedId.Keyboard_EqualsAndPlus, SteelSeriesLedId.Equal },
            { LedId.Keyboard_Backspace, SteelSeriesLedId.Backspace },
            { LedId.Keyboard_Delete, SteelSeriesLedId.Delete },
            { LedId.Keyboard_End, SteelSeriesLedId.End },
            { LedId.Keyboard_PageDown, SteelSeriesLedId.PageDown },
            { LedId.Keyboard_RightShift, SteelSeriesLedId.RShift },
            { LedId.Keyboard_RightCtrl, SteelSeriesLedId.RCtrl },
            { LedId.Keyboard_ArrowUp, SteelSeriesLedId.UpArrow },
            { LedId.Keyboard_ArrowLeft, SteelSeriesLedId.LeftArrow },
            { LedId.Keyboard_ArrowDown, SteelSeriesLedId.DownArrow },
            { LedId.Keyboard_ArrowRight, SteelSeriesLedId.RightArrow }
        };

    internal static readonly Dictionary<LedId, SteelSeriesLedId> MOUSE_TWO_ZONE = new Dictionary<LedId, SteelSeriesLedId>
                                                               {
                                                                   {LedId.Mouse1, SteelSeriesLedId.ZoneOne},
                                                                   {LedId.Mouse2, SteelSeriesLedId.ZoneTwo}
                                                               };

    internal static readonly Dictionary<LedId, SteelSeriesLedId> MOUSE_THREE_ZONE = new Dictionary<LedId, SteelSeriesLedId>
                                                               {
            {LedId.Mouse1, SteelSeriesLedId.ZoneOne},
            {LedId.Mouse2, SteelSeriesLedId.ZoneTwo},
            {LedId.Mouse3, SteelSeriesLedId.ZoneThree}
                                                               };

    internal static readonly Dictionary<LedId, SteelSeriesLedId> MOUSE_EIGHT_ZONE = new Dictionary<LedId, SteelSeriesLedId>
                                                              {
                                                                  { LedId.Mouse1, SteelSeriesLedId.ZoneOne},
                                                                  { LedId.Mouse2, SteelSeriesLedId.ZoneTwo},
                                                                  { LedId.Mouse3, SteelSeriesLedId.ZoneThree},
                                                                  { LedId.Mouse4, SteelSeriesLedId.ZoneFour},
                                                                  { LedId.Mouse5, SteelSeriesLedId.ZoneFive},
                                                                  { LedId.Mouse6, SteelSeriesLedId.ZoneSix},
                                                                  { LedId.Mouse7, SteelSeriesLedId.ZoneSeven},
                                                                  { LedId.Mouse8, SteelSeriesLedId.ZoneEight}
                                                               };

    internal static readonly Dictionary<LedId, SteelSeriesLedId> HEADSET_TWO_ZONE = new Dictionary<LedId, SteelSeriesLedId>
        {
            {LedId.Headset1, SteelSeriesLedId.ZoneOne},
            {LedId.Headset2, SteelSeriesLedId.ZoneTwo}
        };
}
}
