/**
Copyright (c) 2017, CoinPayments.net
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the CoinPayments.net nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL CoinPayments.net BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/

using Database;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace libCoinPaymentsNET
{
    public class CoinPayments
    {
        private static string s_privkey 
        {
            get
            {
                return ConfigJson.Config["coinpayment_privkey"];
            }
        }
        private static string s_pubkey
        {
            get
            {
                return ConfigJson.Config["coinpayment_pubkey"];
            }
        }
        private static readonly Encoding encoding = Encoding.UTF8;
        private static readonly string default_coin = "BTC";

        public CoinPayments()
        {
        }

        public static async Task<JSONNode> GetCallbackAddress(string urlCallback, string currency = null)
        {
            if (currency == null) currency = default_coin;
            return await CallAPI("get_callback_address", new SortedList<string, string>()
            {
                { "currency", currency },
                { "ipn_url", urlCallback }
            });
        }

        public static async Task<JSONNode> CallAPI(string cmd, SortedList<string, string> parms = null)
        {
            if (parms == null)
            {
                parms = new SortedList<string, string>();
            }
            parms["version"] = "1";
            parms["key"] = s_pubkey;
            parms["cmd"] = cmd;

            string post_data = "";
            foreach (KeyValuePair<string, string> parm in parms)
            {
                if (post_data.Length > 0) { post_data += "&"; }
                post_data += parm.Key + "=" + Uri.EscapeDataString(parm.Value);
            }

            byte[] keyBytes = encoding.GetBytes(s_privkey);
            byte[] postBytes = encoding.GetBytes(post_data);
            var hmacsha512 = new System.Security.Cryptography.HMACSHA512(keyBytes);
            string hmac = BitConverter.ToString(hmacsha512.ComputeHash(postBytes)).Replace("-", string.Empty);

            // do the post:
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cl = new WebClient();
            cl.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            cl.Headers.Add("HMAC", hmac);
            cl.Encoding = encoding;

            JSONNode ret = null;
            try
            {
                string resp = await cl.UploadStringTaskAsync(new Uri("https://www.coinpayments.net/api.php"), post_data);
                ret = JSON.Parse(resp);
            }
            catch (WebException e)
            {
                ret = new JSONObject();
                ret["error"] = "Exception while contacting CoinPayments.net: " + e.Message;
            }
            catch (Exception e)
            {
                ret = new JSONObject();
                ret["error"] = "Unknown exception: " + e.Message;
            }

            return ret;
        }
    }
}