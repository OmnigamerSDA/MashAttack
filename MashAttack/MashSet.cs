using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MashAttack
{
    class MashSet
    {
        public int count;
        public double totalTime;
        public long fastest;
        public long slowest;
        public double median;
        public double downTotal;
        public double upTotal;

        public List<Mash> mashes;

        public MashSet()
        {
            count = 0;
            totalTime = 0;
            fastest = 99999;
            slowest = 1;
            median = 0;
            downTotal = 0;
            upTotal = 0;

            mashes = new List<Mash>();
        }

        public void AddMash(long start, long release, long next)
        {
            long down = release - start;
            long up = next - release;
            long total = next - start;

            Mash myMash = new Mash(down, up, total,count);
            mashes.Add(myMash);

            count++;
            totalTime += total;
            downTotal += down;
            upTotal += up;

            UpdateBests(total);
        }

        private void UpdateBests(long total)
        {
            if (total < fastest)
                fastest = total;
            
            if (total > slowest)
                slowest = total;
        }

        public long GetMash(int index)
        {
            return mashes.ElementAtOrDefault(index).total;
        }
        private int CompareMash(Mash mashA, Mash mashB)
        {
            if (mashA == null)
            {
                if (mashB == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (mashB == null)
                    return 1;
                else
                {
                    if (mashA.total > mashB.total)
                        return 1;
                    else if (mashA.total == mashB.total)
                        return 0;
                    else
                        return -1;
                }
            }
        }

        public long GetMedian()
        {
            mashes.Sort(CompareMash);

            return mashes[count / 2].total;
        }
    }
}
