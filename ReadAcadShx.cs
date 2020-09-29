using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
//using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;

namespace ConsoleApp1
{
    class ReadAcadShx
    {
        static void Main(string[] args)
        {
            var tr = new TextReader2();
            tr.Read(@"D:\BridgeDesigner\Main\x64\Release\bin\ACAD\Fonts\宋体big.shx");
        }
    }
}
 
 
 
namespace ConsoleApp1
{
    public class Point
    {
        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
        public Point()
        {
            this.X = 0;
            this.Y = 0;
        }
        public double X;
        public double Y;
    }

    public enum FontType
    {
        Shapes,
        BigFont,
        Unifont,
    }
    struct StringCode
    {
        public short begin;
        public short end;
    }

    public class TextReader2
    {

        public float Height { set; get; } = 50.0f;
        public float LineSpace { set; get; } = 10.0f;
        public FontType FontType;
        public void Read(string path)
        {
            using (var s = File.OpenRead(path))
            {
                this.Read(s);
            }
        }

        private void Read(Stream stream)
        {
            stream.Position = 0;
            var success = this.TryReadBinary(stream);
        }

        public string Header { get; private set; }



        private bool TryReadBinary(Stream stream)
        {
            long length = stream.Length;
            if (length < 32)
            {
                throw new Exception("Incomplete file");
            }

            addDic.Clear();


            var reader = new BinaryReader(stream);

            byte[] headerss = new byte[1000];
            int i = 0;
            do
            {
                headerss[i] = reader.ReadByte();
                i++;
            } while (headerss[i - 1] != 0x1A);

            var str = System.Text.Encoding.ASCII.GetString(headerss, 0, i - 3).Split();
            Header = str[0];
            string type = str[1];
            string version = str[2];



            System.Diagnostics.Debug.WriteLine(Header + "\r" + type + "\r" + version);


            switch (type)
            {
                case "shapes":
                    FontType = FontType.Shapes;
                    ReadShapes(reader);
                    break;
                case "bigfont":

                    FontType = FontType.BigFont;
                    ReadBigFont(reader);
                    break;
                case "unifont":
                    FontType = FontType.Unifont;
                    ReadUnicode(reader);
                    break;
                default:
                    break;
            }


            return true;
        }

        private void ReadShapes(BinaryReader reader)
        {



            for (int i = 0; i < 1; i++)
            {
                var list = reader.ReadBytes(4);
                var code = ReadStringCode(list);
            }

            int count = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            System.Diagnostics.Debug.WriteLine($"{count}");


            List<ShapesIndex> indexes = new List<ShapesIndex>();

            ShapesImformatiom info;
            for (int i = 0; i < count; i++)
            {
                var list = reader.ReadBytes(4);
                var index = ReadShapesIndex(list);

                indexes.Add(index);

            }



            foreach (var index in indexes)
            {
                addDic[index.code] = reader.ReadBytes(index.length);

            }


            for (int i = 0; i < indexes.Count; i++)
            {
                var index = indexes[i];
                //reader.BaseStream.Position = index.address;
                var list = addDic[index.code];
                //Debug.Assert(list.Count() == index.length);
                if (index.code == 0)
                {
                    info = ReadShapesImformation(list);
                }
                else
                {

                }
            }
        }

        private void ReadUnicode(BinaryReader reader)
        {




            int count = BitConverter.ToInt32(reader.ReadBytes(4), 0);
            int length = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            System.Diagnostics.Debug.WriteLine($"{count}");


            List<UnicodeIndex> indexes = new List<UnicodeIndex>();

            UnifontImformatiom info = ReadUnifontImformatiom(reader.ReadBytes(length));



            for (int i = 0; i < count - 1; i++)
            {
                var list = reader.ReadBytes(4);

                var index = ReadUnicodeIndex(list);
                indexes.Add(index);
                addDic[index.code] = reader.ReadBytes(index.length);

            }

        }

        private float Up = 15;
        private float Down = 0;
        private Dictionary<int, byte[]> addDic = new Dictionary<int, byte[]>();
        //private Dictionary<int, List<List<Point>>> dic = new Dictionary<int, List<List<Point>>>();


        public IEnumerable<int> GetList()
        {
            foreach (var item in addDic.Keys)
            {
                if (item != 0)
                {
                    yield return item;
                }
            }
        }

        private void ReadBigFont(BinaryReader reader)
        {

            int oneByte = reader.ReadInt16();
            int count = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            int change = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            System.Diagnostics.Debug.WriteLine($"{oneByte}\r{count}\r{change}");

            for (int i = 0; i < change; i++)
            {
                var list = reader.ReadBytes(4);
                var code = ReadStringCode(list);
            }

            List<BigFontIndex> indexes = new List<BigFontIndex>();

            BigFontImformatiom info;
            for (int i = 0; i < count; i++)
            {
                var list = reader.ReadBytes(8);
                var index = ReadBigFontIndex(list);
                if (index.code == 0 && index.length == 0 && index.address == 0)
                {
                }
                else
                {
                    indexes.Add(index);
                }

            }
            foreach (var index in indexes)
            {
                reader.BaseStream.Position = index.address;
                addDic[index.code] = reader.ReadBytes(index.length);
            }

            for (int i = 0; i < indexes.Count; i++)
            {
                var index = indexes[i];
                //reader.BaseStream.Position = index.address;
                var list = addDic[index.code];
                //Debug.Assert(list.Count() == index.length);
                if (index.code == 0)
                {
                    info = ReadBigFontImformation(list);
                }
                else
                {

                }
            }

        }


        private StringCode ReadStringCode(byte[] bytes)
        {
            StringCode code = new StringCode();
            code.begin = BitConverter.ToInt16(bytes, 0);
            code.end = BitConverter.ToInt16(bytes, 2);
            return code;
        }



        private double ToRightRadius(double a)
        {
            while (a < 0)
            {
                a += 2 * Math.PI;
            }
            while (a > 2 * Math.PI)
            {
                a -= 2 * Math.PI;
            }
            return a;
        }


        public List<List<Point>> ReadCode(int code)
        {
            //Debug.WriteLine(BitConverter.ToString(addDic[code],0));
            float up = Up;
            float down = Down;
            if ((ushort)code <= 0xff)
            {
                //up *= 1.2f;
                //down *= 1.2f;
            }
            return ReadPoint(code, addDic[code], Up, Up, Height / up, Height / up, 0, 0);
            //return ReadPoint(addDic[code], Up, Up, 1, 1, 0, 0);
        }

        // shapes unifont 嵌套改变位置
        private Point endpoint;
        private List<List<Point>> ReadPoint(int dataCode, byte[] drawbuf, float height, float width, float srx = 1, float sry = 1, float basex = 0, float basey = 0)
        {
            StringBuilder shp = null;
#if DEBUG
            shp = new StringBuilder($"{dataCode} ");
#endif


            List<List<Point>> polys = new List<List<Point>>();

            List<Point> points = new List<Point>();

            Point p1 = new Point(basex, basey);
            SByte convexity = 0;
            float xps = 0, yps = 0;
            float nx, ny, xbl = 1;
            bool m_draw = false;

            bool isSevenNext = false;


            var center = new Point();
            var c = drawbuf.Count();
            points = new List<Point>();

            if (FontType == FontType.Shapes)
            {
                points.Add(p1);
                m_draw = true;
            }

            var startR = 0.0d;


            Action drawArc = () =>
            {
                float dis = (float)Math.Sqrt(xps * xps + yps * yps);

                if (convexity > 0)
                {
                    //  sin(x + 2co) - sin(x) = -y;
                    //  cos(x + 2co) - cos(x) = -x;

                    double co = Math.PI - Math.Atan(127.0f / convexity) * 2;
                    double rrr = dis / 2 / Math.Sin(co);


                    var temp = yps / rrr / Math.Sin(co) / 2;
                    if (temp < -1)
                    {
                        temp = -1;
                    }
                    if (temp > 1)
                    {
                        temp = 1;
                    }

                    var t1 = ToRightRadius(Math.Acos(temp) - co);
                    var t2 = ToRightRadius(-Math.Acos(temp) - co);
                    temp = -xps / rrr / Math.Sin(co) / 2;
                    if (temp < -1)
                    {
                        temp = -1;
                    }
                    if (temp > 1)
                    {
                        temp = 1;
                    }

                    var t3 = ToRightRadius(Math.Asin(temp) - co);
                    var t4 = ToRightRadius(Math.PI - Math.Asin(temp) - co);


                    if (Math.Abs(t1 - t3) < 1e-4)
                    {
                        startR = t1;
                    }
                    else if (Math.Abs(t1 - t4) < 1e-4)
                    {
                        startR = t1;
                    }
                    else if (Math.Abs(t2 - t3) < 1e-4)
                    {
                        startR = t2;
                    }
                    else
                    {
                        if (Math.Abs(t2 - t4) > 1e-4)
                        {
                            throw new Exception();
                        }
                        startR = t2;
                    }

                    center = p1;
                    center.X += -rrr * Math.Cos(startR) * srx;
                    center.Y += -rrr * Math.Sin(startR) * sry;


                    for (double k = startR; k <= startR + co * 2; k += Math.PI / 180 / 4)
                    {
                        var xps1 = (float)(Math.Cos(k) * rrr);
                        var yps1 = (float)(Math.Sin(k) * rrr);
                        nx = (xps1 * xbl * srx);
                        ny = yps1 * sry;
                        var p = new Point(center.X + nx, center.Y + ny);
                        points.Add(p);
                    }


                }
                else
                {
                    //  sin(x) - sin(x - 2co) = -y;
                    //  cos(x) - cos(x - 2co) = -x;

                    double co = Math.PI - Math.Atan(127.0f / -convexity) * 2;
                    double rrr = dis / 2 / Math.Sin(co);
                    var temp = -yps / rrr / Math.Sin(co) / 2;
                    if (temp < -1)
                    {
                        temp = -1;
                    }
                    if (temp > 1)
                    {
                        temp = 1;
                    }

                    var t1 = ToRightRadius(Math.Acos(temp) + co);
                    var t2 = ToRightRadius(-Math.Acos(temp) + co);
                    temp = xps / rrr / Math.Sin(co) / 2;
                    if (temp < -1)
                    {
                        temp = -1;
                    }
                    if (temp > 1)
                    {
                        temp = 1;
                    }

                    var t3 = ToRightRadius(Math.Asin(temp) + co);
                    var t4 = ToRightRadius(Math.PI - Math.Asin(temp) + co);

                    //var t1 = ToRightRadius(Math.Acos(-yps / rrr / Math.Sin(co) / 2) + co);
                    //var t2 = ToRightRadius(-Math.Acos(-yps / rrr / Math.Sin(co) / 2) + co);
                    //var t3 = ToRightRadius(Math.Asin(xps / rrr / Math.Sin(co) / 2) + co);
                    //var t4 = ToRightRadius(Math.PI - Math.Asin(xps / rrr / Math.Sin(co) / 2) + co);


                    if (Math.Abs(t1 - t3) < 1e-4)
                    {
                        startR = t1;
                    }
                    else if (Math.Abs(t1 - t4) < 1e-4)
                    {
                        startR = t1;
                    }
                    else if (Math.Abs(t2 - t3) < 1e-4)
                    {
                        startR = t2;
                    }
                    else
                    {
                        if (Math.Abs(t2 - t4) > 1e-4)
                        {
                            throw new Exception();
                        }
                        startR = t2;
                    }

                    center = p1;
                    center.X += -rrr * Math.Cos(startR) * srx;
                    center.Y += -rrr * Math.Sin(startR) * sry;


                    for (double k = startR; k >= startR - co * 2; k -= Math.PI / 180 / 4)
                    {
                        var xps1 = (float)(Math.Cos(k) * rrr);
                        var yps1 = (float)(Math.Sin(k) * rrr);
                        nx = (xps1 * xbl * srx);
                        ny = yps1 * sry;
                        var p = new Point(center.X + nx, center.Y + ny);
                        points.Add(p);
                    }
                }



                nx = (xps * xbl * srx);
                ny = yps * sry;
                p1 = new Point(p1.X + nx, p1.Y + ny);
                points.Add(p1);
            };


            List<Point> pop = new List<Point>();//栈

            for (int i = 0; i < c; i++)
            {
                byte cmd = drawbuf[i];

                if (i == 0)
                {
                    while (drawbuf[i] != 0)
                    {

                        i++;

                    }
                    continue;

                }



                if (cmd <= 0xf && isSevenNext)
                {
                    isSevenNext = false;
                    shp?.Append($"{cmd},");
                    continue;
                }


                switch (cmd)
                {
                    case 0://结束 
                        shp?.Append("0");
                        if (i != c - 1)
                        {
                            shp?.Append(",");
                        }
                        break;
                    case 1://落笔
                        m_draw = true;
                        points.Add(p1);
                        shp?.Append("1,");
                        break;
                    case 2://抬笔
                        m_draw = false;
                        if (points != null && points.Count > 1)
                        {
                            polys.Add(points);
                        }
                        points = new List<Point>();
                        shp?.Append("2,");
                        break;
                    case 3://除矢量
                        i++;
                        srx /= (drawbuf[i]);
                        sry /= (drawbuf[i]);
                        shp?.Append($"3,{drawbuf[i]}");
                        break;
                    case 4://乘矢量
                        i++;
                        srx *= (drawbuf[i]);
                        sry *= (drawbuf[i]);
                        shp?.Append($"4,{drawbuf[i]})");
                        break;
                    case 5://入栈
                        pop.Add(p1);
                        shp?.Append("5,");
                        break;
                    case 6://出栈
                        if (pop.Count != 0)
                        {
                            if (points != null && points.Count > 1)
                            {
                                polys.Add(points);
                            }

                            p1 = pop.Last();
                            pop.Remove(p1);
                            points = new List<Point>();
                            if (m_draw)
                            {
                                points.Add(p1);
                            }
                            //if (m_draw)
                            //{
                            //    points.Add(p1);
                            //}
                        }
                        shp?.Append("6,");
                        break;
                    case 7://嵌套
                        i++;
                        shp?.Append("7,");
                        if (FontType == FontType.BigFont)
                        {
                            if (drawbuf[i] == 0)
                            {
                                i++;
                                int code = BitConverter.ToInt16(new byte[] { drawbuf[i], drawbuf[i + 1] }, 0);
                                i += 5;
                                var orgx = drawbuf[i - 3];
                                var orgy = drawbuf[i - 2];
                                var wt = drawbuf[i - 1];
                                var ht = drawbuf[i];
                                if (shp != null)
                                {
                                    int code1 = BitConverter.ToInt16(new byte[] { drawbuf[i + 1], drawbuf[i] }, 0);
                                    shp.Append($"0,0{code1:X4},{orgx},{orgy},{wt},{ht}),");
                                }
                                if (addDic.ContainsKey(code))
                                {
                                    var sx = (float)wt / (width * xbl) * srx;
                                    var sy = (float)ht / (height) * sry;

                                    polys.AddRange(ReadPoint(code, addDic[code], ht, wt, sx, sy, (float)(orgx * srx), (float)(orgy * sry)));
                                    //var s = BitConverter.ToString(new byte[] { drawbuf[i - 5], drawbuf[i - 4]});

                                }
                            }
                            else if (drawbuf[i] == 2)
                            {
                                shp?.Append("2,");
                                p1 = new Point(basex + width * xbl * srx, basey);
                            }
                            else
                            {
                                isSevenNext = true;
                                m_draw = false;
                                i--;
                            }


                        }
                        else if (FontType == FontType.Shapes)
                        {
                            if (points != null && points.Count > 1)
                            {
                                polys.Add(points);
                            }
                            points = new List<Point>();
                            int code = BitConverter.ToInt16(new byte[] { drawbuf[i], 0 }, 0);
                            shp?.Append($"0,0{drawbuf[i]:X},");
                            if (addDic.ContainsKey(code))
                            {
                                polys.AddRange(ReadPoint(code, addDic[code], height, width, srx, sry, (float)p1.X, (float)p1.Y));
                                p1 = endpoint;
                            }
                        }
                        else if (FontType == FontType.Unifont)
                        {
                            if (points != null && points.Count > 1)
                            {
                                polys.Add(points);
                            }
                            points = new List<Point>();
                            int code = BitConverter.ToInt16(new byte[] { drawbuf[i + 1], drawbuf[i] }, 0);
                            i++;
                            if (shp != null)
                            {
                                int code1 = BitConverter.ToInt16(new byte[] { drawbuf[i], drawbuf[i + 1] }, 0);
                                shp.Append($",0{code1:X4}");
                            }
                            if (addDic.ContainsKey(code))
                            {
                                polys.AddRange(ReadPoint(code, addDic[code], height, width, srx, sry, (float)p1.X, (float)p1.Y));
                                p1 = endpoint;
                            }
                        }
                        break;
                    case 8://xy位移
                        i++;
                        xps = (SByte)(drawbuf[i]);
                        i++;
                        yps = (SByte)(drawbuf[i]); ;
                        nx = (xps * xbl * srx);
                        ny = yps * sry;
                        p1 = new Point(p1.X + nx, p1.Y + ny);
                        if (m_draw) points.Add(p1);
                        shp?.Append($"8,({xps},{yps}),");
                        //Debug.WriteLine(p1);
                        break;
                    case 9://连续xy位移 (0,0)结束
                        i++;
                        xps = (SByte)(drawbuf[i]); ;
                        i++;
                        yps = (SByte)(drawbuf[i]); ;
                        shp?.Append($"9,({xps},{yps}),");
                        while (xps != 0 || yps != 0)
                        {
                            nx = (xps * xbl * srx);
                            ny = yps * sry;
                            p1 = new Point(p1.X + nx, p1.Y + ny);
                            if (m_draw) points.Add(p1);
                            i++;
                            xps = (SByte)(drawbuf[i]);
                            i++;
                            yps = (SByte)(drawbuf[i]);
                            shp?.Append($"({xps},{yps}),");
                        }
                        //Debug.WriteLine(p1);
                        break;
                    case 10://2字节八分圆
                        i++;
                        Byte r = drawbuf[i];
                        int lr = r;
                        i++;
                        var cmd1 = (SByte)(drawbuf[i]);

                        int or = cmd1 & 0x07;
                        int rr = (cmd1 & 0x77);
                        rr >>= 4;

                        center = p1;
                        var rad = Math.PI;

                        startR = rr * 0.25 * rad;


                        center.X += -lr * Math.Cos(startR) * srx;
                        center.Y += -lr * Math.Sin(startR) * sry;

                        switch (or)
                        {
                            case 0:
                                rad = rad * 2;

                                break;
                            case 1:
                                rad = rad * 0.25;

                                break;
                            case 2:
                                rad = rad * 0.5;

                                break;
                            case 3:
                                rad = rad * 0.75;

                                break;
                            case 4:
                                rad = rad * 1;

                                break;
                            case 5:
                                rad = rad * 1.25;

                                break;
                            case 6:
                                rad = rad * 1.5;

                                break;
                            case 7:
                                rad = rad * 1.75;

                                break;
                            default:
                                break;
                        }
                        shp?.Append($"10,({r},{cmd1}),");

                        if (cmd1 > 0)
                        {
                            for (double k = startR; k <= rad + startR; k += Math.PI / 180 / 4)
                            {
                                xps = (float)Math.Cos(k) * r;
                                yps = (float)Math.Sin(k) * r;
                                nx = (xps * xbl * srx);
                                ny = yps * sry;
                                p1 = new Point(center.X + nx, center.Y + ny);
                                points.Add(p1);
                            }
                        }
                        else
                        {
                            for (double k = startR; k >= startR - rad; k -= Math.PI / 180 / 4)
                            {
                                xps = (float)Math.Cos(k) * r;
                                yps = (float)Math.Sin(k) * r;
                                nx = (xps * xbl * srx);
                                ny = yps * sry;
                                p1 = new Point(center.X + nx, center.Y + ny);
                                points.Add(p1);
                            }
                        }

                        break;
                    case 11://5字节八分圆
                        i++;
                        int start_offset = drawbuf[i];
                        i++;
                        int end_offset = drawbuf[i];
                        i++;
                        var hr = drawbuf[i];
                        i++;
                        lr = drawbuf[i];

                        lr = hr * 256 + lr;


                        i++;
                        cmd1 = (SByte)drawbuf[i];

                        rad = Math.PI;
                        or = cmd1 & 0x07;
                        rr = (cmd1 & 0x77);
                        center = p1;


                        rr >>= 4;

                        startR = rr * 0.25 * rad;


                        if (end_offset != 0)
                        {
                            or = (or - 1 + 8) % 8;

                        }
                        switch (or)
                        {
                            case 0:
                                rad = rad * 2;

                                break;
                            case 1:
                                rad = rad * 0.25;

                                break;
                            case 2:
                                rad = rad * 0.5;

                                break;
                            case 3:
                                rad = rad * 0.75;

                                break;
                            case 4:
                                rad = rad * 1;

                                break;
                            case 5:
                                rad = rad * 1.25;

                                break;
                            case 6:
                                rad = rad * 1.5;

                                break;
                            case 7:
                                rad = rad * 1.75;
                                break;
                            default:
                                break;
                        }

                        shp?.Append($"11,({start_offset},{end_offset},{hr},{lr},{cmd1}),");

                        if (cmd1 >= 0)
                        {
                            var endR = startR + rad;

                            startR += Math.PI * start_offset * 45 / 256 / 180;
                            endR += Math.PI * end_offset * 45 / 256 / 180;

                            center.X += -lr * Math.Cos(startR) * srx;
                            center.Y += -lr * Math.Sin(startR) * sry;


                            startR = ToRightRadius(startR);
                            endR = ToRightRadius(endR);

                            while (startR > endR)
                            {
                                startR = startR - Math.PI * 2;
                            }

                            for (double k = startR; k <= endR; k += Math.PI / 180 / 4)
                            {
                                xps = (float)Math.Cos(k) * lr;
                                yps = (float)Math.Sin(k) * lr;
                                nx = (xps * xbl * srx);
                                ny = yps * sry;
                                p1 = new Point(center.X + nx, center.Y + ny);
                                points.Add(p1);
                            }

                        }
                        else
                        {
                            var endR = startR - rad;

                            startR -= Math.PI * start_offset * 45 / 256 / 180;
                            endR -= Math.PI * end_offset * 45 / 256 / 180;

                            center.X += -lr * Math.Cos(startR) * srx;
                            center.Y += -lr * Math.Sin(startR) * sry;


                            startR = ToRightRadius(startR);
                            endR = ToRightRadius(endR);

                            while (startR < endR)
                            {
                                startR = startR + Math.PI * 2;
                            }

                            for (double k = startR; k >= endR; k -= Math.PI / 180 / 4)
                            {
                                xps = (float)Math.Cos(k) * lr;
                                yps = (float)Math.Sin(k) * lr;
                                nx = (xps * xbl * srx);
                                ny = yps * sry;
                                p1 = new Point(center.X + nx, center.Y + ny);
                                points.Add(p1);
                            }

                        }

                        break;
                    case 12://三字节凸度圆
                        //Debug.WriteLine(BitConverter.ToString(BitConverter.GetBytes(unchecked((short)(last))), 0));
                        i++;
                        xps = (SByte)(drawbuf[i]);
                        i++;
                        yps = (SByte)(drawbuf[i]);
                        i++;
                        convexity = (SByte)(drawbuf[i]);
                        shp?.Append($"12,({xps},{yps},{convexity}),");
                        drawArc();

                        break;
                    case 13://连续三字节凸度圆 (0,0)结束

                        //Debug.WriteLine(BitConverter.ToString(BitConverter.GetBytes(last), 0));
                        i++;
                        xps = (SByte)(drawbuf[i]);
                        i++;
                        yps = (SByte)(drawbuf[i]);
                        shp?.Append($"13,({xps},{yps},");
                        while (xps != 0 || yps != 0)
                        {
                            i++;
                            convexity = (SByte)(drawbuf[i]); ;
                            drawArc();
                            shp?.Append($",{convexity})");
                            i++;
                            xps = (SByte)(drawbuf[i]);
                            i++;
                            yps = (SByte)(drawbuf[i]);
                            shp?.Append($",({xps},{yps}");
                        }
                        shp?.Append("),");
                        break;
                    case 14://双向文字
                            // 水平位置,跳过
                        i++;
                        switch (drawbuf[i])
                        {
                            case 3:
                            case 4:
                                i++;
                                break;
                            case 5:
                            case 6:
                                break;
                            case 7:
                                i += 2;
                                break;
                            case 8:
                                i += 2;
                                break;
                            case 9:
                                i++;
                                xps = (SByte)(drawbuf[i]); ;
                                i++;
                                yps = (SByte)(drawbuf[i]); ;
                                while (xps != 0 || yps != 0)
                                {
                                    nx = (xps * xbl * srx);
                                    ny = yps * sry;
                                    i++;
                                    xps = (SByte)(drawbuf[i]); ;
                                    i++;
                                    yps = (SByte)(drawbuf[i]); ;
                                }
                                break;
                            case 10:
                                i += 2;
                                break;
                            case 11:
                                i += 5;
                                break;
                            case 12:
                                i += 3;
                                break;
                            case 13:
                                i++;
                                xps = (SByte)(drawbuf[i]);
                                i++;
                                yps = (SByte)(drawbuf[i]);

                                while (xps != 0 || yps != 0)
                                {
                                    i++;
                                    convexity = (SByte)(drawbuf[i]); ;
                                    i++;
                                    xps = (SByte)(drawbuf[i]); ;
                                    i++;
                                    yps = (SByte)(drawbuf[i]); ;

                                }
                                break;
                            default:
                                break;
                        }
                        break;

                    default://位移
                        if (cmd > 0x0f)
                        {
                            shp?.Append($"0{cmd:X},");
                            int opt;
                            opt = (cmd & 0x0f);
                            int ll = (cmd & 0xff);
                            ll >>= 4;

                            switch (opt)
                            {
                                case 0:
                                    xps = ll;
                                    yps = 0;
                                    break;
                                case 1:
                                    xps = ll;
                                    yps = ll * 0.5f;
                                    break;
                                case 2:
                                    xps = ll;
                                    yps = ll;
                                    break;
                                case 3:
                                    xps = ll * 0.5f;
                                    yps = ll;
                                    break;
                                case 4:
                                    xps = 0;
                                    yps = ll;
                                    break;
                                case 5:
                                    xps = -ll * 0.5f;
                                    yps = ll;
                                    break;
                                case 6:
                                    xps = -ll;
                                    yps = ll;
                                    break;
                                case 7:
                                    xps = -ll;
                                    yps = ll * 0.5f;
                                    break;
                                case 8:
                                    xps = -ll;
                                    yps = 0;
                                    break;
                                case 9:
                                    xps = -ll;
                                    yps = -ll * 0.5f;
                                    break;
                                case 10:
                                    xps = -ll;
                                    yps = -ll;
                                    break;
                                case 11:
                                    xps = -ll * 0.5f;
                                    yps = -ll;
                                    break;
                                case 12:
                                    xps = 0;
                                    yps = -ll;
                                    break;
                                case 13:
                                    xps = ll * 0.5f;
                                    yps = -ll;
                                    break;
                                case 14:
                                    xps = ll;
                                    yps = -ll;
                                    break;
                                case 15:
                                    xps = ll;
                                    yps = -ll * 0.5f;
                                    break;
                            }
                            nx = (xps * xbl * srx);
                            ny = yps * sry;
                            p1 = new Point(p1.X + nx, p1.Y + ny);
                            if (m_draw) points.Add(p1);
                        }
                        break;
                }

            }
            if (points != null && points.Count > 1)
            {
                polys.Add(points);
            }
            Debug.WriteLine(shp);
            endpoint = p1;
            return polys;
        }


        public Point GetEndPonit()
        {
            //Debug.WriteLine("p" + endpoint);
            return endpoint;
        }


        private ShapesIndex ReadShapesIndex(byte[] list)
        {
            ShapesIndex index = new ShapesIndex();
            index.code = BitConverter.ToInt16(new byte[] { list[0], list[1] }, 0);
            index.length = BitConverter.ToInt16(list, 2);

            return index;
        }

        private UnicodeIndex ReadUnicodeIndex(byte[] list)
        {
            UnicodeIndex index = new UnicodeIndex();
            index.code = BitConverter.ToInt16(new byte[] { list[0], list[1] }, 0);
            index.length = BitConverter.ToInt16(list, 2);

            return index;
        }

        private BigFontIndex ReadBigFontIndex(byte[] list)
        {
            BigFontIndex index = new BigFontIndex();
            index.code = BitConverter.ToInt16(new byte[] { list[1], list[0] }, 0);
            index.length = BitConverter.ToInt16(list, 2);
            index.address = BitConverter.ToInt32(list, 4);

            return index;
        }

        private BigFontImformatiom ReadBigFontImformation(byte[] list)
        {
            BigFontImformatiom info = new BigFontImformatiom();

            if (list.Count() == 0)
            {
                return info;
            }
            int count = list.Count();
            byte[] bytes = new byte[0x1000];
            int i = 0;
            byte b = list[0];
            while (b != 0)
            {
                bytes[i] = b;
                i++;
                if (i > count)
                {
                    break;
                }
                b = list[i];
            }
            info.header = System.Text.Encoding.Default.GetString(bytes, 0, i);
            info.baseUpHeight = list[++i];
            while (info.baseUpHeight == 0)
            {
                info.baseUpHeight = list[++i];
            }
            info.baseDownHeight = list[++i];
            info.ShowType = (FontEnableType)list[++i];

            Up = info.baseUpHeight;
            Down = info.baseDownHeight;

            System.Diagnostics.Debug.WriteLine($"{info.header}\rUp:{info.baseUpHeight }\rDown:{info.baseDownHeight}\r{info.ShowType}");
            return info;
        }
        private ShapesImformatiom ReadShapesImformation(byte[] list)
        {
            ShapesImformatiom info = new ShapesImformatiom();

            if (list.Count() == 0)
            {
                return info;
            }
            int count = list.Count();
            byte[] bytes = new byte[0x1000];
            int i = 0;
            byte b = list[0];
            while (b != 0)
            {
                bytes[i] = b;
                i++;
                if (i > count)
                {
                    break;
                }
                b = list[i];
            }

            info.header = System.Text.Encoding.Default.GetString(bytes, 0, i);
            info.baseUpHeight = list[++i];
            while (info.baseUpHeight == 0)
            {
                info.baseUpHeight = list[++i];
            }
            info.baseDownHeight = list[++i];


            Up = info.baseUpHeight;
            Down = info.baseDownHeight;


            System.Diagnostics.Debug.WriteLine($"{info.header}\rUp:{info.baseUpHeight }\rDown:{info.baseDownHeight}");
            return info;
        }

        private UnifontImformatiom ReadUnifontImformatiom(byte[] list)
        {
            UnifontImformatiom info = new UnifontImformatiom();

            if (list.Count() == 0)
            {
                return info;
            }
            int count = list.Count();
            byte[] bytes = new byte[0x1000];
            int i = 0;
            byte b = list[0];
            while (b != 0)
            {
                bytes[i] = b;
                i++;
                if (i > count)
                {
                    break;
                }
                b = list[i];
            }

            info.header = System.Text.Encoding.Default.GetString(bytes, 0, i);
            info.baseUpHeight = list[++i];
            while (info.baseUpHeight == 0)
            {
                info.baseUpHeight = list[++i];
            }
            info.baseDownHeight = list[++i];
            info.showType = (FontEnableType)list[++i];
            info.isUnicode = list[++i];
            info.isEnable = list[++i];
            var zero = list[++i];

            Up = info.baseUpHeight;
            Down = info.baseDownHeight;

            System.Diagnostics.Debug.WriteLine($"{info.header}\rUp:{info.baseUpHeight }\rDown:{info.baseDownHeight}\r{info.showType}\r{info.isUnicode}\r{info.isEnable}");
            return info;
        }

    }
    public struct UnifontImformatiom
    {
        public string header;
        public byte baseUpHeight;
        public byte baseDownHeight;
        public FontEnableType showType;
        public byte isUnicode;
        public byte isEnable;

    }
    public struct ShapesImformatiom
    {
        public string header;
        public byte baseUpHeight;
        public byte baseDownHeight;

    }

    public struct BigFontImformatiom
    {
        public string header;
        public byte baseUpHeight;
        public byte baseDownHeight;

        public FontEnableType ShowType { get; internal set; }
    }

    public enum FontEnableType
    {
        HorizontalOnly = 0,
        VerticalOnly,
        DoubleEable,
    }

    public struct BigFontIndex
    {
        public Int16 code;
        public Int16 length;
        public int address;
    }

    public struct ShapesIndex
    {
        public Int16 code;
        public Int16 length;
    }
    public struct UnicodeIndex
    {
        public int code;
        public Int16 length;
    }
}