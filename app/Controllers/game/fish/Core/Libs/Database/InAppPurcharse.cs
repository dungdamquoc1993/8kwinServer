using BanCa.Redis;
using BanCa.Sql;
using Entites.General;
using MySqlProcess.Genneral;
using Nancy.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PAYHandler
{
    public class Product
    {
        public string PakageId { get; set; }
        public string Price { get; set; }
        public long Cash { get; set; }
        public float Rate { get; set; }

        public static byte RegisterType
        {
            get { return (byte)'P'; }
        }
        public static byte[] Serialize(object data)
        {
            Product ene = (Product)data;
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(ene.PakageId);
                    writer.Write(ene.Price);
                    writer.Write(ene.Cash);
                    writer.Write(ene.Rate);
                }
                return m.ToArray();
            }
        }

        public static Product Desserialize(byte[] data)
        {
            Product result = new Product();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    result.PakageId = reader.ReadString();
                    result.Price = reader.ReadString();
                    result.Cash = reader.ReadInt64();
                    result.Rate = reader.ReadSingle();
                }
            }
            return result;
        }
    }

    public class GiftCodeHandle
    {
        public class GiftcodeResponse
        {
            public byte errorCode;
            public int id;
            public long amount;
            public string time_created;
            public string signature;
            public string msg;
        }

        public static async Task<SimpleJSON.JSONNode> GiftCode(User user, string GiftCode)
        {
            var msg = new SimpleJSON.JSONObject();
            msg["ok"] = false;
            long amount;
            byte error = 0;
            if (GiftCode.Length < 15)
            {
                var tup = await MySqlPayment.GiftCode(GiftCode, user.UserId, string.IsNullOrEmpty(user.PhoneNumber) ? user.Username : user.PhoneNumber);
                string err = tup.Item1;
                amount = tup.Item2;
                error = tup.Item3;
                //Logger.Info("Gift code: " + GiftCode + " uname " + user.Username + " phone " + user.PhoneNumber + " amount " + amount + " err " + err);
                //response.ErrorCode = error;
                if (error == 0)
                {
                    //MySqlPayment.AddOrSubCash(user.UserId, amount, user.Peer.Platform, "GIFT_CODE", out current_cash, out error);
                    var res_cash = await RedisManager.IncEpicCash(user.UserId, amount, user.Platform, "GIFT_CODE", TransType.GIFT_CODE);
                    if (res_cash >= 0)
                    {
                        user.Cash = res_cash;

                        ////insert thong ke tien tang qua vqmm
                        //DateTime dateTime = DateTime.UtcNow;
                        //string date_now = dateTime.ToString("yyyy-MM-dd");
                        //string sql = "INSERT INTO `game_analytics` (`date_current`,`platform`,`index_type`,`total`,`position`) " +
                        //    "VALUES ('{0}','{1}','{2}','{3}','{4}') " +
                        //    "ON DUPLICATE KEY UPDATE total=total+{5};";
                        //sql = string.Format(sql, date_now, user.Platform, "EPIC", amount, 13, amount);
                        //MySqlCommon.ExecuteNonQuery(sql);
                        ////end thong ke

                        SqlLogger.LogGiftCode(user.UserId, user.Cash, amount, GiftCode);

                        //string message = "Quý khách đã được cộng {0} Roll";
                        msg["ok"] = true;
                        msg["amount"] = amount;
                        msg["cash"] = res_cash;

                        //RedisManager.LogCashIn(user.UserId, amount, CashType.GiftCode);
                        MySqlProcess.Genneral.MySqlUser.SaveCashToDb(user.UserId, res_cash);
                    }
                    else
                    {
                        //response.ErrorMsg = LanguageManager.Instance.Message("system_maintain", language_code);
                        msg["err"] = 111;
                    }
                }
                else
                {
                    if (error == 2)
                    {
                        //"Bạn đã sử dụng Gift Code trong sự kiện này";
                        msg["err"] = 112;
                    }
                    else
                    {
                        //"Gift Code đã được sử dụng hoặc không tồn tại";
                        msg["err"] = 113;
                    }
                }
            }
            else
            {
                //"Gift Code đã được sử dụng hoặc không tồn tại";
                msg["err"] = 113;

                //////
                //string url = "http://api.chiengame.com/update_giftcode.php?";
                //string access_key = "su81ka03ee65mn83yt90";
                //string secret_key = "B2C0A9P-F64G5-4CL36-B2H8E-UX8502M64E";

                //string signature = BitConverter.ToString(Util.HmacSHA256(access_key + GiftCode + "app_shopby", secret_key)).Replace("-", "").ToLower(); ;
                //string myParameters = "code=" + GiftCode + "&signature=" + signature;

                //string result = "";
                //try
                //{
                //    result = new WebClient().DownloadString(url + myParameters);
                //} catch(Exception ex)
                //{
                //    Logger.Error("Error in GiftCode: " + ex.ToString());
                //    msg["err"] = 114;
                //    return msg;
                //}

                //JavaScriptSerializer serializer = new JavaScriptSerializer();

                //var res = serializer.Deserialize<GiftcodeResponse>(result);

                //if (res.errorCode == 0)
                //{
                //    var res_cash = RedisManager.IncEpicCash(user.UserId, res.amount, user.Platform, "APP_GIFT_CODE");
                //    if (res_cash >= 0)
                //    {
                //        amount = res.amount;
                //        user.Cash = res_cash;
                //        //string message = "Quý khách đã được cộng {0} Roll";

                //        //MySqlPayment.ChargingHistory(trans_id, user.UserId, user.Username, seri, carpin, res.amount, user.Cash, telco, response.ErrorCode, res.msg, "", "", res.request_id);
                //        string insert = "INSERT INTO `charging_histories`(user_id,username,seri,number_card,amount,current_cash,telco,status,platform,device_id,description,create_time,is_sum) " +
                //            "VALUES({0},'{1}','{2}','{3}',{4},{5},'{6}',{7},'{8}','{9}','{10}','{11}',{12})";
                //        insert = string.Format(insert, user.UserId, user.Username, GiftCode, GiftCode, amount, res_cash, "SBY", res.errorCode, user.Platform, user.DeviceId, res.msg, res.time_created, 0);
                //        string rs_gift = MySqlCommon.ExecuteNonQuery(insert);
                //        string sql = "UPDATE `users` SET `vip_point` = `vip_point`+{0} WHERE `user_id` = {1}; ";
                //        sql = string.Format(sql, amount, user.UserId);
                //        string rs = MySqlCommon.ExecuteNonQuery(sql);

                //        msg["ok"] = true;
                //        msg["amount"] = amount;
                //        msg["cash"] = res_cash;
                //        RedisManager.LogCashIn(user.UserId, amount, CashType.GiftCode);
                //        MySqlProcess.Genneral.MySqlUser.SaveCashToDb(user.UserId, res_cash);
                //    }
                //    else
                //    {
                //        msg["err"] = 115;
                //    }

                //}
                //else
                //{
                //    msg["err"] = res.errorCode;
                //}

            }

            return msg;
        }
    }

    public class InAppPurcharse
    {
        private static Product[] apple, google;

        public static Product[] GetItemInApp(string platform = "android", string version = "1.0.0")
        {
            if (platform == "iphone")
            {
                if (apple == null)
                {
                    apple = new Product[] {
                        new Product() { PakageId = "com.vn.epicjackpot_22k", Price = "22.000", Cash = 22000 },
                        new Product() { PakageId = "com.vn.epicjackpot_45k", Price = "45.000", Cash = 45000 },
                        new Product() { PakageId = "com.vn.epicjackpot_69k", Price = "69.000", Cash = 69000 },
                        new Product() { PakageId = "com.vn.epicjackpot_109k", Price = "109.000", Cash = 109000 },
                        new Product() { PakageId = "com.vn.epicjackpot_199k", Price = "199.000", Cash = 199000 },
                        new Product() { PakageId = "com.vn.epicjackpot_299k", Price = "299.000", Cash = 299000 },
                        new Product() { PakageId = "com.vn.epicjackpot_399k", Price = "399.000", Cash = 399000 },
                        new Product() { PakageId = "com.vn.epicjackpot_499k", Price = "499.000", Cash = 499000 },
                        new Product() { PakageId = "com.vn.epicjackpot_999k", Price = "999.000", Cash = 999000 }
                    };
                }
                return apple;
            }
            else
            {
                if (google == null)
                {
                    google = new Product[] {
                        new Product() { PakageId = "epicjackpot20k", Price = "20.000", Cash = 20000 },
                        new Product() { PakageId = "epicjackpot50k", Price = "50.000", Cash = 50000 },
                        new Product() { PakageId = "epicjackpot100k", Price = "100.000", Cash = 100000 },
                        new Product() { PakageId = "epicjackpot200k", Price = "200.000", Cash = 200000 },
                        new Product() { PakageId = "epicjackpot500k", Price = "500.000", Cash = 500000 },
                        new Product() { PakageId = "epicjackpot1m", Price = "1.000.000", Cash = 1000000 }
                    };
                }
                return google;
            }
        }

        public static long GetPriceofPackageId(string package_id, string platform = "android", string version = "1.0.0")
        {
            long price = 0;

            Product[] product = GetItemInApp(platform, version).Where(p => p.PakageId == package_id).ToArray();
            if (product.Length > 0 && product.First() != null)
                price = product.First().Cash;
            return price;
        }
    }

    public class PAY4_Handler
    {
        public static async Task<Tuple<int, long>> OnUserOperationRequest(User user, string SignedData, string Signature, string appId, long versionCode)
        {
            var amount = 0L;
            try
            {
                try
                {
                    await MySqlUser.IAPLogs(user.UserId, Signature, SignedData, user.Platform, appId);
                }
                catch (Exception ex)
                {
                    Logger.Info("Insert Pay4 infor to database is error " + ex.ToString());
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();

                googlepay pay = null;

                try
                {
                    pay = serializer.Deserialize<googlepay>(SignedData);
                    if (pay == null || string.IsNullOrEmpty(pay.orderId))
                    {
                        //response.ErrorMsg = LanguageManager.Instance.Message("iap_order_code_billing_invalid", lang_code);//"Mã yêu cầu không hợp lệ";
                        //user.Peer.SendOperationResponse(response, sendParameters);
                        return new Tuple<int, long>(1, amount);
                    }
                }
                catch (Exception ex)
                {
                    //response.ErrorMsg = LanguageManager.Instance.Message("iap_order_code_billing_invalid", lang_code);//"Mã yêu cầu không hợp lệ";
                    //user.Peer.SendOperationResponse(response, sendParameters);
                    Logger.Info("Exception JavaScriptSerializer Pay4 " + ex.ToString());
                    return new Tuple<int, long>(2, amount);
                }

                string sql = "SELECT count(order_id) FROM app_billing where order_id ='{0}'";

                sql = string.Format(sql, pay.orderId);


                int count = 0;
                int.TryParse(await MySqlCommon.ExecuteScalar(sql), out count);
                if (count > 0)
                {
                    //response.ErrorMsg = LanguageManager.Instance.Message("iap_billing_existed", lang_code);//"Mã giao dịch đã tồn tại";
                    //user.Peer.SendOperationResponse(response, sendParameters);
                    return new Tuple<int, long>(3, amount);
                }
                string public_key = await GetPublicKey(appId);
                if (!Verify(SignedData, Signature, public_key))
                {
                    //log.InfoFormat("{0} - Verify FALSE", TAG);
                    //response.ErrorMsg = LanguageManager.Instance.Message("system_maintain", lang_code);//"Yêu cầu không hợp lệ";
                    //user.Peer.SendOperationResponse(response, sendParameters);
                    return new Tuple<int, long>(4, amount);
                }
                var Products = await SqlLogger.GetIapItem(appId, versionCode);
                var item = default(SimpleJSON.JSONNode);
                for (int i = 0, n = Products.Count; i < n; i++)
                {
                    var p = Products[i].AsObject;
                    string productId = p["productId"];
                    if (pay.productId.Equals(productId))
                    {
                        item = p;
                        break;
                    }
                }
                if (item == null)
                {
                    //response.ErrorMsg = LanguageManager.Instance.Message("system_maintain", lang_code); //"Yêu cầu không hợp lệ";
                    //user.Peer.SendOperationResponse(response, sendParameters);
                    return new Tuple<int, long>(5, amount);
                }
                //response.ErrorMsg = "Chức năng đang bảo trì";
                //user.Peer.SendOperationResponse(response, sendParameters);
                //return;
                long cash = item["cash"].AsLong;
                var res = await RedisManager.IncEpicCash(user.UserId, cash, user.Platform, "GOOGLE_BILLING", TransType.GOOGLE_BILLING);

                if (res >= 0)
                {
                    user.Cash = res;

                    int trust = 0;
                    int.TryParse((cash / 10000).ToString(), out trust);

                    //RedisManager.LogCashIn(user.UserId, cash, CashType.IapGoogle);
                    amount = cash;
                    try
                    {
                        user.VipPoint += (int)cash;
                        string sql1 = "UPDATE `users` SET `vip_point` = {0},trust=trust+" + trust + " WHERE `user_id` = {1}; ";

                        sql1 = string.Format(sql1, user.VipPoint, user.UserId);
                        MySqlCommon.ExecuteNonQuery(sql1);

                        user.Trust += trust;
                    }
                    catch (Exception ex)
                    {
                        Logger.Info("Exception: " + ex.ToString());
                    }
                }
                else
                {
                    //response.ErrorMsg = LanguageManager.Instance.Message("system_maintain", lang_code);//"Hệ thống đang bảo trì, xin vui lòng thử lại";
                    //user.Peer.SendOperationResponse(response, sendParameters);
                    return new Tuple<int, long>(6, amount);
                }

                string query = "INSERT INTO `app_billing` (`user_id`, `product`, `amount`, `current_cash`, `telco`, `order_id`, `platform`, `device_id`, `create_time`,`country`,`version`)";
                query += "VALUES ({0}, '{1}', {2}, {3}, 'Google', '{4}', '{5}', '{6}', now(), '{7}', '{8}' ); ";
                query = string.Format(query, user.UserId, pay.productId, cash, user.Cash, pay.orderId, user.Platform, user.DeviceId, user.Language, appId);
                MySqlCommon.ExecuteNonQuery(query);


                DateTime dateTime = DateTime.UtcNow;
                dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime).ToLocalTime();
                string curDate = dateTime.ToString("yyyy-MM-dd");
                Dictionary<string, string> pu = await MySqlUser.ReadOneData("users_top", "user_id,total_price_in", "user_id,total_price_in", "user_id=" + user.UserId + " AND date_current='" + curDate + "'");

                //int is_pu = 0;
                //if (pu.Count == 0)
                //{
                //    is_pu = 1;
                //}
                //else
                //{
                //    if (int.Parse(pu["total_price_in"]) == 0)
                //    {
                //        is_pu = 1;
                //    }
                //}
                //if (is_pu == 1)
                //{
                //    string npu = "INSERT INTO `game_analytics`(date_current,platform,index_type,total,position) VALUES('" + curDate + "','" + user.Platform + "','NPU',1,5) ON DUPLICATE KEY UPDATE total=total+1";
                //    MySqlCommon.ExecuteNonQuery(npu);
                //}
                return new Tuple<int, long>(0, amount);
            }
            catch (Exception ex)
            {
                Logger.Log("iap google error: " + ex.ToString());
                return new Tuple<int, long>(6, amount);
            }
        }

        public static bool Verify(String signedData, String base64Signature, String publicKey)
        {
            // By default the result is false
            bool result = false;
            try
            {
                RSACryptoServiceProvider provider = CryptoServiceProviderFromPublicKeyInfo(Convert.FromBase64String(publicKey));

                byte[] signature = Convert.FromBase64String(base64Signature);
                SHA1Managed sha = new SHA1Managed();
                byte[] data = Encoding.UTF8.GetBytes(signedData);

                result = provider.VerifyData(data, sha, signature);

            }
            catch (Exception e)
            {
                Logger.Error("IAP verify error: " + e.ToString());
            }
            return result;
        }

        private static bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
        static byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
        public static RSACryptoServiceProvider CryptoServiceProviderFromPublicKeyInfo(byte[] x509key)
        {
            byte[] seq = new byte[15];
            int x509size;

            if (x509key == null || x509key.Length == 0)
                return null;

            x509size = x509key.Length;

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            MemoryStream mem = new MemoryStream(x509key);
            BinaryReader binr = new BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;

            try
            {
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)	//data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                seq = binr.ReadBytes(15);		//read the Sequence OID
                if (!CompareBytearrays(seq, SeqOID))	//make sure Sequence for OID is correct
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8103)	//data read as little endian order (actual data order for Bit String is 03 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8203)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                bt = binr.ReadByte();
                if (bt != 0x00)		//expect null byte next
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)	//data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                byte lowbyte = 0x00;
                byte highbyte = 0x00;

                if (twobytes == 0x8102)	//data read as little endian order (actual data order for Integer is 02 81)
                    lowbyte = binr.ReadByte();	// read next bytes which is bytes in modulus
                else if (twobytes == 0x8202)
                {
                    highbyte = binr.ReadByte();	//advance 2 bytes
                    lowbyte = binr.ReadByte();
                }
                else
                    return null;
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                int modsize = BitConverter.ToInt32(modint, 0);

                int firstbyte = binr.PeekChar();
                if (firstbyte == 0x00)
                {	//if first byte (highest order) of modulus is zero, don't include it
                    binr.ReadByte();	//skip this null byte
                    modsize -= 1;	//reduce modulus buffer size by 1
                }

                byte[] modulus = binr.ReadBytes(modsize);	//read the modulus bytes

                if (binr.ReadByte() != 0x02)			//expect an Integer for the exponent data
                    return null;
                int expbytes = (int)binr.ReadByte();		// should only need one byte for actual exponent data (for all useful values)
                byte[] exponent = binr.ReadBytes(expbytes);


                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSAParameters RSAKeyInfo = new RSAParameters();
                RSAKeyInfo.Modulus = modulus;
                RSAKeyInfo.Exponent = exponent;
                RSA.ImportParameters(RSAKeyInfo);

                return RSA;
            }
            finally
            {
                binr.Close();
            }
        }

        public static async Task<string> GetPublicKey(string appId)
        {
            var cacheKey = "bc_google_key_" + appId;
            var redis = RedisManager.GetRedis();
            var cache = await redis.StringGetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cache))
            {
                return cache;
            }

            string res = await SqlLogger.GetIapGooglePublicKey(appId);
            await redis.StringSetAsync(cacheKey, res);
            await redis.KeyExpireAsync(cacheKey, DateTime.Now.AddDays(1));

            return res;
        }
    }
    public class googlepay
    {
        public string orderId { get; set; }
        public string productId { get; set; }
        public string purchaseToken { get; set; }
    }
}
