using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BanCa.Sql;
using Database;
using SimpleJSON;

namespace BanCa.Libs
{
    public class EpicApi
    {
        public static async Task<string> Get(string uri)
        {
            Logger.Info("GET: " + uri);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            //request.AllowAutoRedirect = true;
            //request.KeepAlive = false;
            //request.MaximumAutomaticRedirections = 512;
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36";
            //request.Headers["upgrade-insecure-requests"] = "1";
            //request.Headers["accept-language"] = "en-US,en;q=0.9,vi;q=0.8";
            //request.Headers["accept-encoding"] = "gzip, deflate, br";
            //request.Headers["sec-fetch-mode"] = "navigate";
            //request.Headers["sec-fetch-site"] = "none";
            //request.Headers["sec-fetch-user"] = "?1";
            //request.Headers["cache-control"] = "max-age=0";
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var responseText = await reader.ReadToEndAsync();
                Logger.Info("GET response: " + responseText);
                return responseText;
            }
        }

        public static async Task<string> Get(string shortURL, string param, string token)
        {
            try
            {
                var responseText = "";
                var encoding = ASCIIEncoding.UTF8;
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(shortURL + ((param != "") ? "?" + param : ""));
                Logger.Info("GET: " + req.RequestUri.ToString());
                req.Method = "GET";
                req.Timeout = 30000;
                //req.KeepAlive = true;
                req.ContentType = "application/json";
                req.Accept = "application/json";
                req.Headers["X-Api-Key"] = token;
                HttpWebResponse response = (HttpWebResponse)await req.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                    {
                        responseText = await reader.ReadToEndAsync();
                    }
                }
                response.Close();
                Logger.Info("GET response: " + responseText);
                return responseText;
            }
            catch (Exception e)
            {
                Logger.Info("GET error: " + shortURL + " > " + param + " > " + token + " > " + e.ToString());
                return null;
            }
        }

        public static async Task<string> PostFormUrlencoded(string uri, string postData)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }

            var response = (HttpWebResponse)await request.GetResponseAsync();

            var responseString = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
            return responseString;
        }

        public static async Task<string> PostJson(string uri, string json)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), uri))
                {
                    request.Headers.TryAddWithoutValidation("content-type", "application/json");

                    request.Content = new StringContent(json);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static async Task<string> PostVipRik(string uri, string json)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), uri))
                {
                    request.Headers.TryAddWithoutValidation("content-type", "application/json");

                    request.Content = new StringContent(json);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        #region sms verification
        private const string appId = "V4Bq68kD1POoXbHL1ssp5wBRSfPt5ZeI";
        private const string accessToken = "D0Ca3m0c51aWtc_NT-GIJUGZ8WfUOoEm";
        public static string SmsMsgTemplate = "Ma xac thuc cua ban la {pin_code}";
        public static async Task<JSONNode> SendOtpSms(string phone, string msgTemplate = null)
        {
            if (string.IsNullOrEmpty(msgTemplate)) msgTemplate = SmsMsgTemplate;
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.speedsms.vn/index.php/pin/create"))
                {
                    var stringbuilder = new StringBuilder();
                    stringbuilder.Append("{\"to\": \"");
                    stringbuilder.Append(phone);
                    stringbuilder.Append("\", \"content\": \"");
                    stringbuilder.Append(msgTemplate);
                    stringbuilder.Append("\", \"app_id\": \"");
                    stringbuilder.Append(appId);
                    stringbuilder.Append("\"}");
                    var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(accessToken + ":"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64authorization);
                    request.Content = new StringContent(stringbuilder.ToString(), Encoding.UTF8, "application/json");
                    request.Method = HttpMethod.Post;
                    var response = await httpClient.SendAsync(request);
                    //Console.WriteLine(response.ToString());
                    var responseString = await response.Content.ReadAsStringAsync();
                    return JSON.Parse(responseString);
                }
            }
        }

        public static async Task<JSONNode> VerifyPin(string phone, string otp)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.speedsms.vn/index.php/pin/verify"))
                {
                    var stringbuilder = new StringBuilder();
                    stringbuilder.Append("{\"phone\": \"");
                    stringbuilder.Append(phone);
                    stringbuilder.Append("\", \"pin_code\": \"");
                    stringbuilder.Append(otp);
                    stringbuilder.Append("\", \"app_id\": \"");
                    stringbuilder.Append(appId);
                    stringbuilder.Append("\"}");
                    var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(accessToken + ":"));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64authorization);
                    request.Content = new StringContent(stringbuilder.ToString(), Encoding.UTF8, "application/json");
                    request.Method = HttpMethod.Post;
                    var response = await httpClient.SendAsync(request);
                    //Console.WriteLine(response.ToString());
                    var responseString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseString);
                    return JSON.Parse(responseString);
                }
            }
        }

        public static async Task<JSONNode> SendSms(string phone, string content)
        {
            const string apiKey = "CB991EC380FA2608B12FBB04A901FD";
            const string secretKey = "00A58E01101465763DC821682DB392";
            const string smsType = "2";
            var url = "http://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_get?Phone={0}&Content={1}&ApiKey={2}&SecretKey={3}&SmsType={4}&Brandname=Verify";
            url = string.Format(url, phone, Nancy.Helpers.HttpUtility.UrlEncode(content), apiKey, secretKey, smsType);
            Logger.Info("Sending sms api: " + url);
            var res = await Get(url);
            Logger.Info("Send sms result: " + res);

            // Sending sms api: http://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_get?Phone=0356071114&Content=test+sms&ApiKey=CB991EC380FA2608B12FBB04A901FD&SecretKey=00A58E01101465763DC821682DB392&SmsType=4
            // Send sms result: { "CodeResult":"100","CountRegenerate":0,"SMSID":"96e86b87-0996-4dd6-8108-70122c8efcd3156"}
            return JSON.Parse(res);
        }
        #endregion

        #region Gachthe
        public static async Task<JSONNode> CardIn(string network, string cardCode, string cardSeri, int cardValue, string urlCallBack, string transId)
        {
            string url = "http://gachthe.vn/API/NapThe?APIKey={0}&Network={1}&CardCode={2}&CardSeri={3}&CardValue={4}&URLCallback={5}&TrxID={6}";
            const string apiKey = "540ecd9b-0ed0-4a44-900d-b27d7dc09b96";
            url = string.Format(url, apiKey, network, cardCode, cardSeri, cardValue, Nancy.Helpers.HttpUtility.UrlEncode(urlCallBack), transId);
            Logger.Info("Sending cashin api: " + url);
            var res = await Get(url);
            Logger.Info("Send cashin result: " + res);

            //{ Code: 1, Message: "Đã nhận thẻ" }
            return JSON.Parse(res);
        }
        #endregion

        #region MoMo
        public const string MoMoSecretKey = "hYk9QJQs%w+k-Yyk=ScXb95M5Y!SKZ6D";
        public static async Task<JSONNode> GetMoMoAcc()
        {
            var url = "http://139.180.206.12:6001/BankAPI/getInfo";
            string requestTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string type = "2";
            string authKey = Md5(requestTime + "|" + type + "|" + MoMoSecretKey);

            // res
            //{
            //    "errorCode":"0",
            //"errorDescription":"Success",
            //"destinationInfo":"ew0KInBodddazY4NzM5MCIsDQoibmFtZSI6Ik5
            //ndXnhu4VuIENoaeG6v24gVGjhuq9uZyINCn0\u003d"
            //}

            var data = new JSONObject();
            data["requestTime"] = requestTime;
            data["type"] = type;
            data["authKey"] = authKey;
            var json = data.ToString();
            Logger.Log("GetMoMoAcc post " + json);
            string res = await PostJson(url, json);
            //string res = "{\"errorCode\":\"0\",\"errorDescription\":\"Success\",\"destinationInfo\":\"ew0KInBob25lIjoiMDkyNTQ3NzA2MCIsDQoibmFtZSI6Ik5HVVlFTiBIQUkgSEEiDQp9\"}";
            Logger.Log("GetMoMoAcc response: " + res);
            var response = JSON.Parse(res);
            if (response.HasKey("destinationInfo"))
            {
                var di = response["destinationInfo"];
                if (di != null)
                    return JSON.Parse(Base64Decode(response["destinationInfo"].Value));
            }
            return null;
        }
        #endregion

        #region xxeng
        public static async Task<JSONNode> xxengCashIn(string nickname, long changeCash)
        {
            string hash = Md5(nickname + changeCash + "gamebai#66@88");
            //var url = string.Format("https://backend.xxeng.vip/api_backend?c=8797&nn={0}&mn={1}&h={2}", nickname, changeCash, hash);
            var xxengHost = ConfigJson.Config["xxeng-backend"].Value;
            var url = string.Format("{3}/api_backend?c=8797&nn={0}&mn={1}&h={2}", nickname, changeCash, hash, xxengHost);
        
            return JSON.Parse(await Get(url));
        }
        public static async Task<JSONNode> xxengLogIn(string username, string password, string platform)
        {
            var xxengHost = ConfigJson.Config["xxeng-host"].Value;
             //var url = string.Format("https://portal.xxeng.vip/api?c=3&un={0}&pw={1}&pf={2}&at=", username, password, platform);
            var url = string.Format("{3}/api?c=3&un={0}&pw={1}&pf={2}&at=", username, password, platform, xxengHost);
            return JSON.Parse(await Get(url));
        }
        public static async Task<JSONNode> thinhLogIn(string username, string password, string platform)
        {
            var thinhHost = ConfigJson.Config["thinh-host"].Value;
             //var url = string.Format("https://portal.xxeng.vip/api?c=3&un={0}&pw={1}&pf={2}&at=", username, password, platform);
            var url = string.Format("{3}/api?c=3&un={0}&pw={1}&pf={2}&at=", username, password, platform, thinhHost);
            return JSON.Parse(await Get(url));
        }
        public static async Task<JSONNode> viprikCashIn(string username, long ccash)
        {
            var data = new JSONObject();
            data["username"] = username;
            data["ccash"] = ccash;
            var json = data.ToString();
            Logger.Log("viprikCashIn post " + json);
            string res = await PostVipRik("https://ws.king86.club/shootfishcashin/", json);

            return JSON.Parse(res);
        }
        public static async Task<JSONNode> viprikCashOut(string username, long ccash)
        {
            var data = new JSONObject();
            data["username"] = username;
            data["ccashOut"] = ccash;
            var json = data.ToString();
            Logger.Log("viprikCashIn post " + json);
            string res = await PostVipRik("https://ws.king86.club/shootfishcashin/", json);

            return JSON.Parse(res);
        }
        #endregion

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string GetHashSha256(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        public static string Md5(string sInput)
        {
            var algorithmType = default(HashAlgorithm);
            var enCoder = new ASCIIEncoding();
            byte[] valueByteArr = enCoder.GetBytes(sInput);
            byte[] hashArray = null;
            // Encrypt Input string 
            algorithmType = new MD5CryptoServiceProvider();
            hashArray = algorithmType.ComputeHash(valueByteArr);
            //Convert byte hash to HEX
            var sb = new StringBuilder();
            foreach (byte b in hashArray)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
