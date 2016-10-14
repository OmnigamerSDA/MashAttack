using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MashAttack
{
    public class SerialComms
    {
        //Host commands
        public const byte START = 0xAA;
        public const byte RESET = 0xFF;
        public const byte RESEND = 0x88;

        //uC responses
        public const byte SPINUP = 0x11;
        public const byte INITIATED = 0x22;
        public const byte DOWNTIME2 = 0x44;
        public const byte UPTIME2 = 0x55;
        public const byte DOWNTIME = 0x66;
        public const byte UPTIME = 0x77;
        public const byte FINISHED = 0x33;

        SerialPort _datPort;
        int playerNum = 0;
        MainWindow main;

        MashSet mashes;
        MashSet mashes2;

        long downval, downval2;

        int period = 1;

        public Action<String> StatusDelegate;
        public Action<MashSet, MashSet> UpdateDelegate;
        public Action<Brush> BarDelegate;
        public Action CountdownDelegate;
        public Action<int> MashDelegate;

        public SerialComms(string port, int rate, int newperiod, Action<String> mydelegate)
        {
            _datPort = new SerialPort(port,rate);

            period = newperiod;
            StatusDelegate = mydelegate;
            //main = parent;

            mashes = new MashSet();
            mashes2 = new MashSet();

            if (!_datPort.IsOpen)//Open, if not already
                _datPort.Open();

            if (_datPort == null || !_datPort.IsOpen)
            {
                StatusDelegate("Something went wrong.");
                return;
            }

            _datPort.DtrEnable = false;
            _datPort.RtsEnable = false;


            _datPort.ReceivedBytesThreshold = 3;
            _datPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);

            StatusDelegate(String.Format("Connected to {0}",port));

            return;
        }

        void DataReceived(object sender, EventArgs e)
        {
            if (_datPort == null || !_datPort.IsOpen) return;
            byte[] readBuffer = new byte[3];
            int readCount = 0;

            // Try to read some data from the COM port and append it to our localBuffer.
            // If there's an IOException then the device has been disconnected.
            readCount = _datPort.BytesToRead;
            while (readCount > 2)
            {

                try
                {
                    //Status(String.Format("{0}", readCount));
                    _datPort.Read(readBuffer, 0, 3); //Read in 3 bytes from the com port
                    //Status(String.Format("Mash {0}: {1:d} {2:d}", mashes.count, readBuffer[1], readBuffer[2]));
                }
                catch (IOException)
                {
                    StatusDelegate("Something went wrong.");
                    return;
                }

                ParseResponse(readBuffer);

                readCount = _datPort.BytesToRead; //update read count in case multiple comms occurred
            }
        }

        public bool Command(byte opcode)
        {
            if (_datPort == null || !_datPort.IsOpen) return false;
            byte[] inputs = new byte[1];

            inputs[0] = opcode;

            _datPort.Write(inputs, 0, 1);//Write out control data to Arduino
            StatusDelegate(String.Format("Wrote command: {0:x}", opcode));
            return true;
        }

        private bool ParseResponse(byte[] response)
        {
            switch (response[0])
            {
                case SPINUP:
                    StatusDelegate("Device Ready.\n");
                    StatusDelegate(String.Format("Seconds: {0}    Config: {1:x}", response[1], response[2]));
                    //countdown = 9;
                    BarDelegate(Brushes.Yellow);
                    mashes = new MashSet();
                    mashes2 = new MashSet();
                    //isDown = true;
                    return true;
                case INITIATED:
                    StatusDelegate("First mash started.");
                    CountdownDelegate();
                    return true;
                case DOWNTIME:
                    //Status("Down Received.");
                    downval = (long)(response[1] + (response[2] * 256));
                    return true;
                case UPTIME:
                    //Status("Up Received.");
                    CaptureMash(response[1], response[2]);
                    return true;
                case DOWNTIME2:
                    //Status("Down2 Received.");
                    downval2 = (long)(response[1] + (response[2] * 256));
                    return true;
                case UPTIME2:
                    //Status("Up2 Received.");
                    CaptureMash2(response[1], response[2]);
                    return true;
                case FINISHED:
                    StatusDelegate("Session finished.\n");
                    UpdateDelegate(mashes, mashes2);
                    //main.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    //               new Action(delegate ()
                    //               {
                    //                   main.UpdateStats(mashes, mashes2);
                    //               }));
                    return true;
                default:
                    StatusDelegate("Device Comms Error. \n");
                    return false;
            }
        }

        private void CaptureMash(byte v1, byte v2)
        {

            long upval = (long)(v1 + (v2 * 256));
            mashes.AddMash(downval / period, upval / period);
            StatusDelegate(String.Format("Added Mash {0}: {1} {2}", mashes.count - 1, downval, upval));
            MashDelegate(mashes.count + mashes2.count);
        }

        private void CaptureMash2(byte v1, byte v2)
        {

            long upval = (long)(v1 + (v2 * 256));
            mashes2.AddMash(downval2 / period, upval / period);
            StatusDelegate(String.Format("Added Mash2 {0}: {1} {2}", mashes2.count - 1, downval2, upval));
            MashDelegate(mashes.count + mashes2.count);
        }
    }
}
