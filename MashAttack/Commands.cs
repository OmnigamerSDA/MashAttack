using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MashAttack
{
    static class Commands
    {
        //Host commands
        public const byte START = 0xAA;
        public const byte RESET = 0xFF;
        public const byte RESEND = 0x88;

        //uC responses
        public const byte SPINUP = 0x11;
        public const byte INITIATED = 0x22;
        public const byte DOWNTIME = 0x66;
        public const byte UPTIME = 0x77;
        public const byte FINISHED = 0x33;
    }
}
