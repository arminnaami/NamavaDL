using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace Namavaa
{
    struct Quality
    {
        public string Resolution;
        public string Uri;
    }
    class Video
    {
        public string Url;
        public string Key;
        public long Length = 0;
    }
    class Namavaa
    {
        private static HttpClient client = new HttpClient();
        public async Task Decrypt(string key,string input, string output)
        {

            var response = await client.GetAsync(key);
            byte[] encryptionKey = await response.Content.ReadAsByteArrayAsync();

            var outputFile = output;
            using (FileStream outputFileStream = new FileStream(outputFile, FileMode.Create))
            {
                byte[] encryptionIV = new byte[16];
                using (FileStream inputFileStream = new FileStream(input, FileMode.Open))
                {
                    using (var aes = new AesManaged { Key = encryptionKey, IV = encryptionIV, Mode = CipherMode.CBC })
                    using (var encryptor = aes.CreateDecryptor())
                    using (var cryptoStream = new CryptoStream(inputFileStream, encryptor, CryptoStreamMode.Read))
                    {
                        await cryptoStream.CopyToAsync(outputFileStream);
                    }
                }
            }
            File.Delete(input);
        }
        public async Task<List<Quality>> ParseStream(string stream)
        {
            try
            {
                
                var response = await client.GetAsync(stream);
                var responseFromServer = await response.Content.ReadAsStringAsync();
                if (responseFromServer.StartsWith("#EXTM3U"))
                {
                    return ParseString(responseFromServer);
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }
        public static List<Quality> ParseString(string response)
        {
            List < Quality > qualities= new List<Quality>();
            string[] q = response.Split(new string[] { "#EXT-X-STREAM-INF" }, StringSplitOptions.None);
            for(int i = 0; i < q.Length; i++)
            {
                string line = q[i];
                if (line == string.Empty || line.Contains("#EXTM3U"))
                    continue;
                string[] r = line.Split(new string[] { "\r\n" },StringSplitOptions.None);
                if (r.Length < 2)
                    continue;
                string info = r[0];
                string uri = r[1];
                if (!Regex.IsMatch(info, "RESOLUTION=(.*)"))
                    continue;
                Match m = Regex.Match(info, "RESOLUTION=(.*)");
                string resolution = m.Groups[1].Value;
                qualities.Add(new Quality() { Resolution = resolution,Uri = uri });
            }
            return qualities;
        }
        public async Task< Video >  ParseVideo(string videoUrl)
        {
            try
            {
                long finalOffset = 0;
                var response = await client.GetAsync(videoUrl);
                var responseFromServer = await response.Content.ReadAsStringAsync();
                if (responseFromServer.StartsWith("#EXTM3U"))
                {
                    if (Regex.IsMatch(responseFromServer, "#EXT-X-KEY:METHOD=AES-128,URI=\"(.*)\""))
                    {
                        string key = Regex.Match(responseFromServer, "#EXT-X-KEY:METHOD=AES-128,URI=\"(.*)\"").Groups[1].Value;
                        string[] junks = responseFromServer.Split(new string[] { "\r\n" },StringSplitOptions.None);
                        string url = "";
                        for(int i = 0; i < junks.Length; i++)
                        {
                            if (junks[i].StartsWith("range"))
                            {
                                string line = junks[i];
                                if(Regex.IsMatch(line, "range/(.*)/(.*)/"))
                                {
                                    string low = Regex.Match(line, "range/(.*)/(.*)/").Groups[1].Value;
                                    long l = long.Parse(low);
                                    string high = Regex.Match(line, "range/(.*)/(.*)/").Groups[2].Value;
                                    long h = long.Parse(high);
                                    finalOffset = l + h;
                                    url = line;
                                    
                                }
                            }
                        }
                        url = Regex.Replace(url, "range/.*/.*/", string.Format("range/{0}/{1}/", 0, finalOffset));
                        return new Video() { Url = url,Key=key ,Length = finalOffset};
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
