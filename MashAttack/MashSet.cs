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

        public void AddMash(long down, long up)
        {
            long total = down+up;

            Mash myMash = new Mash();
            myMash.Set(down, up, total, count);
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
            if (count != 0)
                return mashes.ElementAtOrDefault(index).total;
            else
                return 1;
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
            if (count != 0)
            {
                mashes.Sort(CompareMash);

                long myVal = mashes[count / 2].total;

                mashes.Sort(Reorder);

                return myVal;
            }
            else
                return 1;
        }

        private int Reorder(Mash mashA, Mash mashB)
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
                    if (mashA.sequence > mashB.sequence)
                        return 1;
                    else if (mashA.sequence == mashB.sequence)
                        return 0;
                    else
                        return -1;
                }
            }
        }
    }
}
