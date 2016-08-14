using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MashAttack
{
    class Mash
    {
        public long down;
        public long up;
        public long total;
        public int sequence;

        public Mash()
        {
            down = 0;
            up = 0;
            total = 0;
            sequence = 1;
        }

        public Mash(long start, long release, long next, int newnum)
        {
            down = release - start;
            up = next - release;
            total = next - start;
            sequence = newnum;
        }

        public void Set(long newDown, long newUp, long newTotal)
        {
            down = newDown;
            up = newUp;
            total = newTotal;
        }
    }
}
