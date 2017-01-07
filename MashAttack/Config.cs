using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MashAttack
{
    static class Config
    {
        static List<string> snesbuttons = new List<string> { "B", "Y", "A", "X", "L", "R" };
        static List<string> nesbuttons = new List<string> { "A", "B", "Select", "Start"};
        static List<string> genbuttons = new List<string> { "B", "C", "A", "Start"};
        static List<string> arcbuttons = new List<string> { "Left", "Right" };

        public static readonly Dictionary<string, byte> inputs = new Dictionary<string, byte> { { "SNES", 0x40 }, { "NES", 0x20 }, { "GEN", 0x10 }, { "ARC", 0x00 } };
        public static readonly Dictionary<string, List<string>> buttons = new Dictionary<string, List<string>> { { "SNES", snesbuttons }, { "NES", nesbuttons }, { "GEN", genbuttons }, { "ARC", arcbuttons } };

        public static byte GetCode(string input, int button1)
        {
            byte val1 = 0x01;

            return (byte)(inputs[input] | (val1 << button1));
        }

        public static byte GetCode(string input, int button1, int button2)
        {
            byte val1 = 0x01;
            byte val2 = 0x01;

            return (byte)(inputs[input] | (val1 << button1) | (val2 << button2) | 0x80);
        }
    }
}
