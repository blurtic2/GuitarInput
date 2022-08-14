using HidSharp;
using HidSharp.Reports.Input;
using static WindowsInput.Sender;

namespace GuitarFreaksInput
{
    public class InputConverter
    {
        enum GuitarKeyList : byte
        {
            Button_1 = 0,
            Button_2 = 1,
            Button_3 = 2,
            Button_4 = 3,
            Button_5 = 4,
            Pick_Up = 5,
            Pick_Down = 6,
            Select = 7,
            Start = 8,
            Wailing_Up = 9,
            Wailing_Down = 10,
        }

        const byte INPUT_STATUS_INDEX = 7;
        const byte INPUT_STATUS_INDEX2 = 8;

        const byte STATUS_INDEX_BUTTON_1 = 0;
        const byte STATUS_INDEX_BUTTON_2 = 1;
        const byte STATUS_INDEX_BUTTON_3 = 2;
        const byte STATUS_INDEX_BUTTON_4 = 3;
        const byte STATUS_INDEX_BUTTON_5 = 4;
        const byte STATUS_INDEX_PICK_UP = 5;
        const byte STATUS_INDEX_PICK_DOWN = 6;
        const byte STATUS2_INDEX_SELECT = 0;
        const byte STATUS2_INDEX_START = 1;
        const byte STATUS2_INDEX_WAILING_UP = 2;
        const byte STATUS2_INDEX_WAILING_DOWN = 3;

        static bool[] LastGuitarKeyStatus = new bool[11];
        static bool[] CurrentGuitarKeyStatus = new bool[11];
        static bool[] PickStatus = new bool[5];
        static KeyCode[] keyCodes = new KeyCode[11] { KeyCode.KEY_Z, KeyCode.KEY_S, KeyCode.KEY_X, KeyCode.KEY_D, KeyCode.KEY_C, // Button 1~5
                                                    KeyCode.KEY_O, KeyCode.KEY_P, //Pick Up, Down
                                                    KeyCode.KEY_Q, KeyCode.KEY_W, // Select, Start
                                                    KeyCode.KEY_E, KeyCode.KEY_R};// Wailing Up ,Down


        static void ParseKeyInput(byte Info1, byte Info2, bool[] Parsed)
        {
            Parsed[0] = (Info1 & (1 << STATUS_INDEX_BUTTON_1)) != 0 ? true : false;
            Parsed[1] = (Info1 & (1 << STATUS_INDEX_BUTTON_2)) != 0 ? true : false;
            Parsed[2] = (Info1 & (1 << STATUS_INDEX_BUTTON_3)) != 0 ? true : false;
            Parsed[3] = (Info1 & (1 << STATUS_INDEX_BUTTON_4)) != 0 ? true : false;
            Parsed[4] = (Info1 & (1 << STATUS_INDEX_BUTTON_5)) != 0 ? true : false;
            Parsed[5] = (Info1 & (1 << STATUS_INDEX_PICK_UP)) != 0 ? true : false;
            Parsed[6] = (Info1 & (1 << STATUS_INDEX_PICK_DOWN)) != 0 ? true : false;
            Parsed[7] = (Info2 & (1 << STATUS2_INDEX_SELECT)) != 0 ? true : false;
            Parsed[8] = (Info2 & (1 << STATUS2_INDEX_START)) != 0 ? true : false;
            Parsed[9] = (Info2 & (1 << STATUS2_INDEX_WAILING_UP)) != 0 ? true : false;
            Parsed[10] = (Info2 & (1 << STATUS2_INDEX_WAILING_DOWN)) != 0 ? true : false;
        }

        static void EmulateKeyboardInput(bool[] Current, bool[] Last)
        {
            // send button 1~5 input only when the picking lever is activated
            for (int i = 0; i < 5; i++)
            {
                if ((Current[(int)GuitarKeyList.Pick_Up] != Last[(int)GuitarKeyList.Pick_Up]) ||
                   (Current[(int)GuitarKeyList.Pick_Down] != Last[(int)GuitarKeyList.Pick_Down]))
                {
                    if (Current[(int)GuitarKeyList.Pick_Up] || Current[(int)GuitarKeyList.Pick_Down])
                    {
                        // Pick detected
                        if (Current[i])
                        {
                            SendKeyDown(keyCodes[i]);
                            PickStatus[i] = true;
                        }
                    }
                }
            }

            // Release all buttons if picking lever is not activated
            if (!Current[(int)GuitarKeyList.Pick_Up] && !Current[(int)GuitarKeyList.Pick_Down])
            {
                for (int i = 0; i < 5; i++)
                {
                    SendKeyUp(keyCodes[i]);
                }
            }

            // handle other inputs in general way.
            for (int i = 7; i < 11; i++)
            {
                if (Current[i] != Last[i])
                {
                    if (Current[i])
                    {
                        SendKeyDown(keyCodes[i]);
                    }
                    else
                    {
                        SendKeyUp(keyCodes[i]);
                    }
                }
            }

            // update current input
            for (int i = 0; i < 11; i++)
            {
                Last[i] = Current[i];
            }
        }
        public static void Main(string[] args)
        {
            // It is not clear whether all products have the same ID
            const int GITALLER_VENDER_ID = 3727;
            const int GITALLER_PRODUCT_ID = 768;
            var Devices = FilteredDeviceList.Local.GetHidDevices(GITALLER_VENDER_ID, GITALLER_PRODUCT_ID);
            if (Devices.Count() == 0)
            {
                Console.WriteLine("No GITALLER has been found.");
                Console.ReadKey();
                return;
            }

                foreach (HidDevice device in Devices)
            {
                Console.WriteLine("Device found.");
                HidStream connection;
                HidDeviceInputReceiver inputReceiver;

                var options = new OpenConfiguration();
                options.SetOption(OpenOption.Exclusive, true);
                options.SetOption(OpenOption.Priority, OpenPriority.VeryHigh);

                device.TryOpen(options, out connection);
                var reportDescriptor = device.GetReportDescriptor();
                inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                inputReceiver.Start(connection);

                byte[] buffer = new byte[device.GetMaxInputReportLength()];
                while (true)
                {                    
                    HidSharp.Reports.Report report;
                    bool result = inputReceiver.TryRead(buffer, 0, out report);
                    bool inputChanged = false;
                    if (result)
                    {
                        byte ButtonStatus = buffer[INPUT_STATUS_INDEX];
                        byte ButtonStatus2 = buffer[INPUT_STATUS_INDEX2];
                        ParseKeyInput(ButtonStatus, ButtonStatus2, CurrentGuitarKeyStatus);
                        for(int i=0; i<CurrentGuitarKeyStatus.Length; i++)
                        {
                            if(LastGuitarKeyStatus[i] != CurrentGuitarKeyStatus[i])
                            {
                                inputChanged = true;
                                break;
                            }
                        }
                        if (inputChanged)
                        {
                            EmulateKeyboardInput(CurrentGuitarKeyStatus, LastGuitarKeyStatus);
                        }
                    }
                    inputReceiver.WaitHandle.WaitOne();
                }
            }
        }
    }
}
