using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wistu.Lib.ClassifyModel
{
    public static class ClassifyHelper
    {
        public static Point FileName2Point(string name)
        {
            name = name.Trim();
            name = Path.GetFullPath(name);
            name = Path.GetFileNameWithoutExtension(name);
            if (name.Contains("#"))
            {
                name = name.Split('#', StringSplitOptions.RemoveEmptyEntries)[0];
            }
            var pts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var p = new Point(int.Parse(pts[1]), int.Parse(pts[0]));
            return p;
        }
    }
}