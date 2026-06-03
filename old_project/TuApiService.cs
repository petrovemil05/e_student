using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace e_student
{
    public class TuApiService
    {
        private HttpClient client;
        private CookieContainer cookies;

        public TuApiService()
        {
            cookies = new CookieContainer();

            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                UseCookies = true,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            client = new HttpClient(handler);

            // Default browser-like headers
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 OPR/131.0.0.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            // Optional sec-ch and fetch headers (may help)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua",
                "\"Chromium\";v=\"147\", \"Not=A?Brand\";v=\"24\", \"Opera\";v=\"131\"");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
        }

        public async Task<string> GetHtmlAsync(string fnum, string egn)
        {
            const string url = "https://e-university.tu-sofia.bg/ETUS/studenti/index.php";

            // 1. LOGIN
            var loginBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("fnum", fnum),
                new KeyValuePair<string, string>("egn", egn)
            });

            var loginResponse = await client.PostAsync(url, loginBody);

            loginResponse.EnsureSuccessStatusCode();

            string loginHtml = await loginResponse.Content.ReadAsStringAsync();

            // 2. FETCH DATA
            var dataBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("deistvie", "1")
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = dataBody
            };

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}
