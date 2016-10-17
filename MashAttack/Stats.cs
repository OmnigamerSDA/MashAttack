using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MashAttack
{
    class Stats
    {
        readonly string BASE = ".\\Stats\\";

        public long totaltime = 0;
        public long totalcount = 0;
        public long totalup = 0;
        public long totaldown = 0;
        public long totalsessions = 0;
        public long totalscore = 0;
        public long totalmedian = 0;

        public Stats()
        {
            totaltime = 0;
            totalcount = 0;
            totalup = 0;
            totaldown = 0;
            totalsessions = 0;
            totalscore = 0;
            totalmedian = 0;
        }
        public Stats(long newtotal, long newcount, long newup, long newdown, long newsessions, long newscore, long newmedian)
        {
            totaltime = newtotal;
            totalcount = newcount;
            totalup = newup;
            totaldown = newdown;
            totalsessions = newsessions;
            totalscore = newscore;
            totalmedian = newmedian;
        }

        public void UpdateAll(bool onebutton, string player, string mode, string input)
        {
            Stats tempStats = new Stats();
            if (onebutton)
            {
                tempStats.LoadFile(BASE+"OneButton\\", "global.txt");
                tempStats += this;
                tempStats.SaveFile(BASE+"OneButton\\", "global.txt");

                tempStats.LoadFile(BASE + "OneButton\\", input + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "OneButton\\", input + ".txt");

                tempStats.LoadFile(BASE + "OneButton\\", mode + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "OneButton\\", mode + ".txt");

                tempStats.LoadFile(BASE + "OneButton\\" + player + "\\", player + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "OneButton\\" + player + "\\", player + ".txt");

                tempStats.LoadFile(BASE + "OneButton\\" + player + "\\", mode + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "OneButton\\" + player + "\\", mode + ".txt");
            }
            else
            {
                tempStats.LoadFile(BASE + "TwoButton\\", "global.txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "TwoButton\\", "global.txt");

                tempStats.LoadFile(BASE + "TwoButton\\", input + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "TwoButton\\", input + ".txt");

                tempStats.LoadFile(BASE + "TwoButton\\", mode + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "TwoButton\\", mode + ".txt");

                tempStats.LoadFile(BASE + "TwoButton\\" + player + "\\", player + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "TwoButton\\" + player + "\\", player + ".txt");

                tempStats.LoadFile(BASE + "TwoButton\\" + player + "\\", mode + ".txt");
                tempStats += this;
                tempStats.SaveFile(BASE + "TwoButton\\" + player + "\\", mode + ".txt");
            }
        }

        private void LoadFile(string path, string filename)
        {
            Directory.CreateDirectory(path);
            if (File.Exists(path+filename))
            {
                StreamReader sr = new StreamReader(path+filename);

                totaltime = Convert.ToInt64(sr.ReadLine());
                totalcount = Convert.ToInt64(sr.ReadLine());
                totalup = Convert.ToInt64(sr.ReadLine());
                totaldown = Convert.ToInt64(sr.ReadLine());
                totalsessions = Convert.ToInt64(sr.ReadLine());
                totalscore = Convert.ToInt64(sr.ReadLine());
                totalmedian = Convert.ToInt64(sr.ReadLine());
                sr.Close();

                //return new Stats(newtotal, newcount, newup, newdown, newsessions, newscore);
            }
            else
            {
                totaltime = 0;
                totalcount = 0;
                totalup = 0;
                totaldown = 0;
                totalsessions = 0;
                totalscore = 0;
                totalmedian = 0;
            }
        }

        public Stats GetGlobal(bool onebutton)
        {
            Stats temp = new Stats();
            if (onebutton)
                temp.LoadFile(BASE + "OneButton\\", "global.txt");
            else
                temp.LoadFile(BASE + "TwoButton\\", "global.txt");

            return temp;
        }

        public Stats GetPlayerData(bool onebutton, string player, string mode)
        {
            Stats temp = new Stats();
            if (onebutton)
                temp.LoadFile(BASE + "OneButton\\" + player + "\\", mode + ".txt");
            else
                temp.LoadFile(BASE + "TwoButton\\" + player + "\\", mode + ".txt");

            return temp;
        }

        private void SaveFile(string path, string filename)
        {
            Directory.CreateDirectory(path);
            StreamWriter sw = new StreamWriter(path+filename,false);
            sw.WriteLine(String.Format("{0}", totaltime));
            sw.WriteLine(String.Format("{0}", totalcount));
            sw.WriteLine(String.Format("{0}", totalup));
            sw.WriteLine(String.Format("{0}", totaldown));
            sw.WriteLine(String.Format("{0}", totalsessions));
            sw.WriteLine(String.Format("{0}", totalscore));
            sw.WriteLine(String.Format("{0}", totalmedian));
            sw.Close();

        }

        public static Stats operator +(Stats s1, Stats s2)
        {
            return new MashAttack.Stats(s1.totaltime + s2.totaltime, s1.totalcount + s2.totalcount, s1.totalup + s2.totalup, s1.totaldown + s2.totaldown, s1.totalsessions + s2.totalsessions, s1.totalscore + s2.totalscore, s1.totalmedian+s2.totalmedian);
        }
    }
}
