using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;
using ServiceStack.Text;
using ServiceStack.Text.Json;
namespace GeetestCrack
{
    public class Geek
    {
        private JsonObject config = null;
        private string referer = "";
        private string userAgent = "[your user agent]";
        Random rnd = new Random();
        public string challenge
        {
            get
            {
                if (config != null)
                    return config.Get<string>("challenge");
                else
                    return "";
            }
            private set 
            {
                if (config == null)
                    config = new JsonObject();
                config["challenge"] = value;
            }
        }
        private string gt 
        {
            get 
            {
                if (config != null)
                    return config.Get<string>("gt");
                else
                    return "";
            }
        }
        public Geek(string gt = "", string referer = "")
        {
            if (!string.IsNullOrEmpty(gt))
                config["gt"] = gt;
            if(!string.IsNullOrEmpty(referer))
                this.referer = referer;
            GetConfig();
        }
        public Geek(Uri uri)
        {
            string source = "";
            HttpWebRequest hreq = (HttpWebRequest)HttpWebRequest.Create(uri);
            HttpWebResponse hres = (HttpWebResponse)hreq.GetResponse();
            using (StreamReader sr = new StreamReader(hres.GetResponseStream()))
                source = sr.ReadToEnd();
            hres.Close();
            var obj = JsonObject.Parse(source);
            gt = obj.Get<string>("gt");
            this.referer = uri.Host;
            challenge = obj.Get<string>("challenge");
            GetConfig();
        }

        public void GetConfig()
        {
            if (string.IsNullOrEmpty(gt))
                return;
            var getUrl = string.Format("http://api.geetest.com/get.php?gt={0}&challenge={1}&product=embed&offline=false", gt, challenge);
            Uri uri = new Uri(getUrl);
            HttpWebRequest hreq = (HttpWebRequest)HttpWebRequest.Create(uri);
            hreq.Timeout = 10000;
            hreq.Referer = this.referer;
            hreq.CookieContainer = new CookieContainer();
            HttpWebResponse hres = (HttpWebResponse)hreq.GetResponse();
            var cookies = hreq.CookieContainer.GetCookieHeader(uri);
            string js = "";
            using (StreamReader sr = new StreamReader(hres.GetResponseStream()))
                js = sr.ReadToEnd();
            hres.Close();

            var sIndex = js.IndexOf("new Geetest({");
            if (sIndex < 1)
                return;
            var eIndex = js.IndexOf("},true)", sIndex);
            if (eIndex < sIndex)
                return;
            var json = js.Substring(sIndex + 12, eIndex - sIndex - 11);
            
            config = JsonObject.Parse(json);
            config["cookie"] = cookies;
            config["initchallenge"] = config["challenge"];
            //Console.WriteLine("config: " +  config.Dump());
        }

        public bool RefreshConfig()
        {
            if (string.IsNullOrEmpty(gt))
                return false;
            var refreshUrl = string.Format("http://api.geetest.com/refresh.php?gt={0}&challenge={1}&callback=cb", gt, challenge);
            Uri uri = new Uri(refreshUrl);
            string js = "";
            try
            {
                HttpWebRequest hreq = (HttpWebRequest)HttpWebRequest.Create(uri);
                hreq.Timeout = 10000;
                hreq.Referer = this.referer;
                hreq.CookieContainer = new CookieContainer();
                var cookies = config.Get("cookie");
                hreq.CookieContainer.SetCookies(uri, cookies);
                HttpWebResponse hres = (HttpWebResponse)hreq.GetResponse();
                using (StreamReader sr = new StreamReader(hres.GetResponseStream()))
                    js = sr.ReadToEnd();
                hres.Close();
            }
            catch(Exception ex)
            {
                //Console.WriteLine("refresh error: " + ex.Message);
                challenge = config["initchallenge"];
            }

            if (string.IsNullOrEmpty(js))
                return false;
            var json = js.Substring(3, js.Length - 4);

            var newParams = JsonObject.Parse(json);
            config["ypos"] = newParams["ypos"];
            config["challenge"] = newParams["challenge"];
            config["bg"] = newParams["bg"];
            config["fullbg"] = newParams["fullbg"];
            config["slice"] = newParams["slice"];
            //Console.WriteLine("config refreshed: " + config.Dump());
            return true;
        }

        public JsonObject GetValidate()
        {
            // load images
            var imgUrlBase = string.Format("http://{0}", config.Get<string[]>("staticservers")[0]);
            Image bg = LoadImage(imgUrlBase + config.Get<string>("bg"));
            Image full = LoadImage(imgUrlBase + config.Get<string>("fullbg"));
            int ypos = config.Get<int>("ypos") + 3;
            bg = AlignImage(bg, ypos);
            full = AlignImage(full, ypos);
            
            // get position
            int xpos = GetPositionX(bg, full);
            config["xpos"] = xpos.ToString();
            var actions = GetActions(xpos);
            //Console.WriteLine("xpos: " + xpos);
            // try actions
            var cookies = config.Get<string>("cookie");
            foreach(var action in actions)
            {
                xpos = action.Get<int>("pos");
                //Console.WriteLine("try pos: " + xpos);
                string response = GetResponseString(xpos, challenge);
                int passTime = action.Get<int>("passtime");
                string actString = action.Get<string>("action");
                int imgLoadTime = rnd.Next(0, 200) + 50;
                Thread.Sleep(passTime); // wait
                var ajaxUrl = string.Format("{0}ajax.php?gt={1}&challenge={2}&imgload={3}&passtime={4}&userresponse={5}&a={6}&callback=cb",
                    config.Get<String>("apiserver"),
                    gt,
                    challenge,
                    imgLoadTime,
                    passTime,
                    response,
                    actString
                    );
                
                var uri = new Uri(ajaxUrl);
                var hreq = (HttpWebRequest)HttpWebRequest.Create(uri);
                hreq.Timeout = 30000;
                hreq.Referer = referer;
                hreq.UserAgent = userAgent;
                hreq.CookieContainer = new CookieContainer();
                
                hreq.CookieContainer.SetCookies(uri, cookies);
                var hres = (HttpWebResponse)hreq.GetResponse();
                var js = "";
                using (StreamReader sr = new StreamReader(hres.GetResponseStream()))
                    js = sr.ReadToEnd();
                hres.Close();
                var json = js.Substring(3, js.Length - 4);
                //Console.WriteLine(json);
                var result = JsonObject.Parse(json);
                result["challenge"] = challenge;
                if (result.Get<int>("success") == 1)
                {
                    // success
                    return result;
                }
                else if (result.Get("message") == "abuse")
                {
                    // return abuse ,refresh config and try again
                    return result;
                }
                else if (result.Get("message") == "forbidden")
                {
                    // try next action
                }
            }
            // failed
            return null;
        }
        private List<JsonObject> GetActions(int xpos)
        {
            var acts = new List<JsonObject>();

            for (var i = 0; i < 4; i++)
            {
                JsonObject act = new JsonObject();
                act["pos"] = xpos.ToString();
                var action = generate(xpos);
                act["action"] = encrypt(action);
                int pt = 0;
                foreach (var a in action)
                {
                    pt += a[2];
                }
                act["passtime"] = pt.ToString();
                acts.Add(act);
            }
            return acts;

        }

        private List<int[]> generate(int xpos)
        {
            var sx = rnd.Next(15, 30);
            var sy = rnd.Next(15, 30);
            var arr = new List<int[]>();
            arr.Add(new int[] { sx, sy, 0 });
            var maxCount = 100; // max len 100
            double x = 0;
            double lx = xpos - x;
            while (Math.Abs(lx) > 0.8 && maxCount-- > 0)
            {
                var rn = rnd.NextDouble();

                var dx = rn * lx * 0.6;
                if (Math.Abs(dx) < 0.5)
                    continue;
                var dt = rnd.NextDouble() *  (rn * 80 + 50)+ 10;

                rn = rnd.NextDouble();
                double dy = 0;
                if (rn < 0.2 && dx > 10) // 
                {
                    dy = rn * 20.0;
                    if (rn < 0.05)
                        dy = -rn * 80;
                }

                x += dx;
                arr.Add(new int[] { (int)(dx + 0.5), (int)(dy + 0.5), (int)(dt + 0.5) });
                lx = xpos - x;
            }
            var dtlast = 500.0 * rnd.NextDouble() + 100.0;
            arr.Add(new int[] { 0, 0, (int)(dtlast) });
            return arr;
        }

        private List<int[]> diff(List<int[]> arr)
        {
            List<int[]> b = new List<int[]>();
            for (var c = 0; c < arr.Count - 1; c++)
            {
                int[] e = new int[3];
                e[0] = (arr[c + 1][0] - arr[c][0]);
                e[1] = (arr[c + 1][1] - arr[c][1]);
                e[2] = (arr[c + 1][2] - arr[c][2]);
                if (e[0] == 0 && e[1] == 0 && e[2] == 0)
                    continue;
                b.Add(e);
            }
            return b;
        }
        private string encode(int n)
        {
            const string b = "()*,-./0123456789:?@ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqr";
            var c = b.Length;
            char d = (char)0;
            var e = Math.Abs(n);
            var f = e / c;
            if (f >= c)
                f = c - 1;
            if (f != 0)
            {
                d = b[f];
                e %= c;
            }
            var g = "";
            if (n < 0)
                g += "!";
            if (d != 0)
                g += "$";
            
            return g + (d == 0 ? "": d.ToString()) + b[e];
        }
        private char replace(int[] a2)
        {
            var b = new int[][] { new int[] { 1, 0 }, new int[] { 2, 0 }, new int[] { 1, -1 }, 
                new int[] { 1, 1 }, new int[] { 0, 1 }, new int[] { 0, -1 }, 
                new int[] { 3, 0 }, new int[] { 2, -1 }, new int[] { 2, 1 } };
            var c = "stuvwxyz~";
            for (var d = 0; d < b.Length; d++)
                if (a2[0] == b[d][0] && a2[1] == b[d][1])
                    return c[d];
            return '\0';
        }
        private string encrypt(List<int[]> action)
        {
            var d = action;// diff(action);
            string dx = "", dy = "", dt = "";
            for(var j=0; j<d.Count; j++)
            {
                var b = replace(d[j]);
                if(b != 0){
                    dy += b.ToString();
                }
                else
                {
                    dx += (encode(d[j][0]));
                    dy += (encode(d[j][1]));
                }
                dt += (encode(d[j][2]));
            }
          return  dx + "!!" + dy + "!!" + dt;
        }

        private string GetResponseString(int posx, string challenge)
        {
            var ct = challenge.Substring(32);
            if (ct.Length < 2)
                return "";
            int [] d = new int[ct.Length];
            for (var e = 0; e < ct.Length; e++)
            {
                var f = ct[e];
                if (f > 57)
                    d[e] = f - 87;
                else
                    d[e] = f - 48;
            }
            var c = 36 * d[0] + d[1];
            var g = posx + c;
            ct = challenge.Substring(0, 32);
            var i = new List<List<char>>(5);
            for(var ii =0; ii<5; ii++)
            {
                i.Add(new List<char>());
            }
            Dictionary<char, int> j = new Dictionary<char, int>();
            int k = 0;
            foreach (var h in ct)
            {
                if (!j.Keys.Contains(h) || j[h] != 1)
                {
                    j[h] = 1;
                    i[k].Add(h);
                    k++;
                    k %= 5;
                }

            }
            int n = g, o = 4;
            var p = "";
            var q = new int[] { 1, 2, 5, 10, 50 }.ToList(); ;
            Random rnd = new Random();
            while (n > 0)
            {
                if (n - q[o] >= 0)
                {
                    int m = rnd.Next(0, i[o].Count);
                    p += i[o][m];
                    n -= q[o];
                }
                else
                {
                    i.RemoveAt(o);
                    q.RemoveAt(o);
                    o--;
                }
            }
            return p;
        }
        private Image LoadImage(string url)
        {
            Image image;
            HttpWebRequest hreq = (HttpWebRequest)HttpWebRequest.Create(url);
            HttpWebResponse hres = (HttpWebResponse)hreq.GetResponse();
            using (var stream = hres.GetResponseStream())
            {
                image = Image.FromStream(stream);
            }
            hres.Close();
            return image;
        }
        private Image AlignImage(Image img, int ypos = 0, int height = 52)
        {
            const int width = 260;
            Bitmap bmp = new Bitmap(width, height);
            var pos = new int[] {157, 145, 265, 277,181, 169, 241, 253, 109, 97, 289, 301, 85, 73, 25, 37, 13, 1, 121, 133, 61, 49, 217, 229, 205, 193,
                145, 157, 277, 265, 169, 181, 253, 241, 97, 109, 301, 289, 73, 85, 37, 25, 1, 13, 133, 121, 49, 61, 229, 217, 193, 205};
            int dx = 0, sy = 58, dy = 0;
            var g = Graphics.FromImage(bmp);
            for (var i = 0; i < pos.Length; i++) {
                g.DrawImage(img, new Rectangle(dx, dy - ypos, 10, 58), new Rectangle(pos[i], sy, 10, 58), GraphicsUnit.Pixel);
                dx += 10;
                if (dx == width)
                {
                    dx = 0;
                    dy = 58;
                    sy = 0;
                }
            }
            g.Dispose();
            return bmp;
        }
        private int GetPositionX(Image imgBg, Image imgFullBg, Image imgSlice = null)
        {
            var bg = new Bitmap(imgBg);
            var full = new Bitmap(imgFullBg);
            Rectangle rect = new Rectangle(0, 0, bg.Width, bg.Height);
            const int bytesCount = 4;
            var bgData = bg.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var fullData = full.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int xpos = -1;
            unsafe
            {
                byte * pBg =(byte *)bgData.Scan0;
                byte * pFull = (byte *)fullData.Scan0;
                //sub 2 images
                for (var i = 0; i < bgData.Stride * bgData.Height; i+=4)
                {
                    pBg[i] = (byte)Math.Abs((int)pBg[i] - pFull[i]);
                    pBg[i + 1] = (byte)Math.Abs((int)pBg[i + 1] - pFull[i + 1]);
                    pBg[i + 2] = (byte)Math.Abs((int)pBg[i + 2] - pFull[i + 2]);
                }
                var w = bgData.Width;
                // Roberts edge detect and calculate histgram
                int[] histgram = new int[w];
                int[] histSum = new int[w];
                for (var y = 0; y < bgData.Height - 1; y++)
                {
                    for (var x = 0; x < w - 1; x++)
                    {
                        var i00 = (x + y * w);
                        var i11 = (i00 + w + 1) * bytesCount;
                        var i01 = (i00 + 1) * bytesCount;
                        var i10 = (i00 + w) * bytesCount;
                        i00 *= bytesCount;
                        pFull[i00] = (byte)(Math.Abs(pBg[i00] - pBg[i11]) + Math.Abs(pBg[i01] - pBg[i10])); // b
                        pFull[i00 + 1] = (byte)(Math.Abs(pBg[i00 + 1] - pBg[i11 + 1]) + Math.Abs(pBg[i01 + 1] - pBg[i10 + 1])); // g
                        pFull[i00 + 2] = (byte)(Math.Abs(pBg[i00 + 2] - pBg[i11 + 2]) + Math.Abs(pBg[i01 + 2] - pBg[i10 + 2])); // r
                        histgram[x] += pFull[i00] + pFull[i00 + 1] + pFull[i00 + 2];
                    }
                }
                // find xpos
                int ww = 48, maxValue = -1;
                for (var i = 0; i < ww; i++)
                    histSum[0] += histgram[i];
                for (var x = 1; x < w - ww; x++)
                {
                    histSum[x] = histSum[x - 1] + histgram[x + ww - 1] - histgram[x - 1];
                    if (histSum[x] > maxValue)
                    {
                        xpos = x;
                        maxValue = histSum[x];
                    }
                }
            } // exit unsafe
            bg.UnlockBits(bgData);
            full.UnlockBits(fullData);
            //offset 6 pixels
            return xpos - 6; 
        }
    }
}