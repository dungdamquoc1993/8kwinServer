using BanCa.Redis;
using BanCa.Sql;
using Entites;
using Entites.Cms;
using Entites.General;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MySqlProcess.Genneral
{
    public class MySqlUser
    {
        static List<string> IPLocks = new List<string>() { };

        public static void SaveCashToDb(long userId, long cash)
        {
            try
            {
                string query = "UPDATE `users` SET `cash` = {0} WHERE `user_id` = {1};";
                query = string.Format(query, cash, userId);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SaveCashToDb " + ex.ToString());
            }
        }

        public static void SavePhoneToDb(long userId, string phone)
        {
            try
            {
                string query = "UPDATE `users` SET `phone_number` = {0} WHERE `user_id` = {1};";
                query = string.Format(query, phone, userId);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SavePhoneToDb " + ex.ToString());
            }
        }

        public static void SaveVerifySmsLoginStateToDb(long userId, bool on)
        {
            try
            {
                string query = "UPDATE `users` SET `verify_login` = {0} WHERE `user_id` = {1};";
                query = string.Format(query, on ? 1 : 0, userId);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SaveVerifySmsLoginStateToDb " + ex.ToString());
            }
        }

        public static bool ChangePassword(long userId, string newPass)
        {
            try
            {
                newPass = MySqlCommon.md5(newPass);
                string query = "UPDATE `users` SET `password`='{0}' WHERE `user_id`={1};";
                query = string.Format(query, newPass, userId);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in save cash sql " + ex.ToString());
            }
            return false;
        }

        public static async Task<response_base> Register(string username, string password, string platform, string device_id, string ip, string language = "en")
        {
            response_base res = new response_base() { error = 1, message = "" };
            if (!ValidString(password))
            {
                res.message = "Mật khẩu phải là chuỗi (không chứa ký tự đặc biệt) có độ dài từ 4-32 ký tự";
                return res;
            }
            if (!ValidString(username))
            {
                res.message = "Tài khoản phải là chuỗi (không chứa ký tự đặc biệt) có độ dài từ 4-32 ký tự";
                return res;
            }
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_Register", con))
                    {
                        password = MySqlCommon.md5(password);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_username", username);
                        cmd.Parameters.AddWithValue("@_password", password);
                        cmd.Parameters.AddWithValue("@_platform", platform);
                        cmd.Parameters.AddWithValue("@_avatar", "");
                        cmd.Parameters.AddWithValue("@_device_id", device_id);
                        cmd.Parameters.AddWithValue("@_ip", ip);
                        cmd.Parameters.AddWithValue("@_language", language);
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            await reader.ReadAsync();
                            res.error = short.Parse(reader["error"].ToString());
                            res.message = reader["msg"].ToString();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                res.error = 1;
                var ms = ex.ToString();
                Logger.Error("Error Register " + ms);
                res.message = ms;
            }

            return res;
        }

        public static async Task<User> Login(string username, string password)
        {
            User res = new User() { error = 1, message = "" };
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand("SP_Login", con))
                    {
                        //Logger.Info("Login " + username + " " + password);
                        password = MySqlCommon.md5(password);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@_username", username);
                        cmd.Parameters.AddWithValue("@_password", password);
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader) await cmd.ExecuteReaderAsync();
                        await reader.ReadAsync();
                        int first_login = 0;
                        string platform = "";

                        if (reader.HasRows)
                        {
                            res.error = short.Parse(reader["error"].ToString());
                            if (res.error == 0)
                            {
                                res.Username = username;
                                res.Password = password;
                                res.Nickname = reader["nickname"].ToString();
                                res.Cp = reader["cp"].ToString();
                                res.Cash = long.Parse(reader["cash"].ToString());
                                res.CashSilver = long.Parse(reader["cash_silver"].ToString());
                                res.CashSafe = long.Parse(reader["cash_safe"].ToString());
                                res.VipId = byte.Parse(reader["vip_id"].ToString());
                                res.UserId = long.Parse(reader["user_id"].ToString());
                                res.DeviceId = reader["device_id"].ToString();

                                res.Avatar = reader["avatar"].ToString();
                                if (string.IsNullOrEmpty(res.Avatar))
                                {
                                    res.Avatar = "0";

                                }
                                res.PhoneNumber = reader["phone_number"].ToString();
                                res.Platform = platform = reader["platform"].ToString();
                                res.VipPoint = int.Parse(reader["vip_point"].ToString());

                                res.IsExChange = reader["block"].ToString() == "0" ? (byte)1 : (byte)0;
                                try
                                {
                                    res.TimeLogin = reader.GetString("time_login");
                                    if (string.IsNullOrEmpty(res.TimeLogin))
                                    {
                                        res.TimeLogin = "";
                                        first_login = 1;
                                    }
                                }
                                catch (System.Data.SqlTypes.SqlNullValueException)
                                {
                                    res.TimeLogin = "";
                                    first_login = 1;
                                }
                                res.TotalFriend = int.Parse(reader["total_friend"].ToString());
                                res.Gender = reader["gender"].ToString();
                                res.Age = string.IsNullOrEmpty(reader["age"].ToString()) ? 0 : int.Parse(reader["age"].ToString());
                                res.Married = reader["marries"].ToString();
                                res.Level = int.Parse(reader["level"].ToString());
                                res.Like = int.Parse(reader["like"].ToString());
                                res.Games = reader["game"].ToString();
                                res.Description = reader["description"].ToString();
                                res.UrlFacebook = reader["url_facebook"].ToString();
                                res.UrlTwitter = reader["url_twitter"].ToString();
                                res.Language = reader["language"].ToString();
                                res.PublicProfile = int.Parse(reader["publicprofile"].ToString());
                                res.Trust = int.Parse(reader["trust"].ToString());
                                res.TimestampRegister = reader.GetDateTime("time_register");
                                res.VerifyLogin = int.Parse(reader["verify_login"].ToString()) != 0;

                            }
                            res.message = reader["msg"].ToString();
                        }
                        else
                        {
                            res.error = 3;
                            //res.message = "Mật khẩu không đúng";
                        }
                        reader.Close();
                        //if (first_login == 1 && platform != "BOT")
                        //{
                        //    DateTime dateTime = DateTime.UtcNow;
                        //    dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime).ToLocalTime();
                        //    string curDate = dateTime.ToString("yyyy-MM-dd");
                        //    string NAU = "INSERT INTO game_analytics(date_current,platform,index_type,total) VALUES('" + curDate + "','" + platform + "','NAU',1)";
                        //    NAU += " ON DUPLICATE KEY UPDATE total=total+1";

                        //    MySqlCommon.ExecuteNonQuery(NAU);
                        //}
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                res.error = 1;
                var ms = ex.ToString();
                Logger.Error("Error Login " + username + " msg " + ms);
                res.message = ms;
                return res;
            }
        }

        public static async Task<LoginFbResponse> LoginFacebook(string facebook_id, string username, string nickname, string password,
            string platform, string avatar, string device_id, string ip, int cNumberOfAcc, int maxAccount, string language = "en")
        {
            LoginFbResponse res = new LoginFbResponse() { error = 1, message = "" };
            res.NewAcc = false;
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    /*
                    using (MySqlCommand cmd = new MySqlCommand("SP_LoginFacebook", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        password = MySqlCommon.md5(password);
                        cmd.Parameters.AddWithValue("@_facebook_id", facebook_id);
                        cmd.Parameters.AddWithValue("@_username", username);
                        cmd.Parameters.AddWithValue("@_nickname", nickname);
                        cmd.Parameters.AddWithValue("@_password", password);
                        cmd.Parameters.AddWithValue("@_platform", platform);
                        cmd.Parameters.AddWithValue("@_avatar", avatar);
                        cmd.Parameters.AddWithValue("@_device_id", device_id);
                        cmd.Parameters.AddWithValue("@_ip", ip);
                        cmd.Parameters.AddWithValue("@_language", language);
                        con.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();
                        reader.Read();
                        res.error = short.Parse(reader["error"].ToString());
                        res.message = reader["msg"].ToString();
                        if (res.error == 0)
                            res.Username = reader["username"].ToString();
                    }
                    */

                    string sql = "SELECT user_id,active,block,username,password,platform,time_login FROM users where facebook_id=@facebook_id LIMIT 0,1";
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        password = MySqlCommon.md5(password);
                        cmd.Parameters.AddWithValue("@facebook_id", facebook_id);
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            int _active = int.Parse(reader["active"].ToString());
                            int _block = int.Parse(reader["block"].ToString());
                            string _password = reader["password"].ToString();
                            string _username = reader["username"].ToString();
                            //if (_block == 0)
                            //{
                            if (_active == 1)
                            {
                                if (_password == password)
                                {
                                    res.error = 0;
                                    res.message = "Success";
                                    res.Username = reader["username"].ToString();
                                }
                                else
                                {
                                    res.error = 3;
                                    res.message = "Account not match";
                                }
                            }
                            else
                            {
                                res.error = 2;
                                res.message = "Account is not actived";
                            }
                            //}
                            //else
                            //{
                            //    res.error = 2;
                            //    res.message = "Account is banned";
                            //}
                        }
                        else if (cNumberOfAcc >= maxAccount)
                        {
                            res.error = -11;
                        }
                        else if (!await RedisManager.CanRegisterInDay(device_id))
                        {
                            res.error = -15;
                        }
                        else
                        {
                            string new_account = "INSERT INTO users(username, nickname, password, platform, facebook_id,device_id, avatar, type, ip,language, time_register) VALUES(";
                            new_account += "'" + username + "',";
                            new_account += "'" + nickname + "',";
                            new_account += "'" + password + "',";
                            new_account += "'" + platform + "',";
                            new_account += "'" + facebook_id + "',";
                            new_account += "'" + device_id + "',";
                            new_account += "'" + avatar + "',";
                            new_account += "1,";
                            new_account += "'" + ip + "',";
                            new_account += "'" + language + "',";
                            new_account += "NOW())";

                            await SqlLogger.executeNonQueryAsync(new_account);

                            res.error = 0;
                            res.Username = username;
                            res.NewAcc = true;
                            RedisManager.IncRegisterInDay(device_id);
                            //if (platform != "BOT")
                            //{
                            //    DateTime dateTime = DateTime.UtcNow;
                            //    dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime).ToLocalTime();
                            //    string curDate = dateTime.ToString("yyyy-MM-dd");

                            //    string NRU = "INSERT INTO game_analytics(date_current,platform,index_type,total) VALUES('" + curDate + "','" + platform + "','NRU',1)";
                            //    NRU += " ON DUPLICATE KEY UPDATE total=total+1";
                            //    MySqlCommon.ExecuteNonQuery(NRU);
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var ms = ex.ToString();
                Logger.Error("Error LoginFacebook " + ms);
                res.message = ms;
            }

            return res;
        }

        public static async Task<LoginFbResponse> LoginByDevice(string username, string password, string nickname, string platform, string device_id, string ip, int cNumberOfAcc, int maxAccount, string language = "en")
        {
            LoginFbResponse res = new LoginFbResponse() { error = 1, message = "" };
            res.NewAcc = false;
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    /*
                    using (MySqlCommand cmd = new MySqlCommand("SP_LoginByDevice", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        password = MySqlCommon.md5(password);
                        cmd.Parameters.AddWithValue("@_username", username);
                        cmd.Parameters.AddWithValue("@_password", password);
                        cmd.Parameters.AddWithValue("@_platform", platform);
                        cmd.Parameters.AddWithValue("@_device_id", device_id);
                        cmd.Parameters.AddWithValue("@_ip", ip);
                        cmd.Parameters.AddWithValue("@_language", language);
                        con.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();
                        reader.Read();
                        res.error = short.Parse(reader["error"].ToString());
                        res.message = reader["msg"].ToString();
                        if (res.error == 0)
                            res.Username = reader["username"].ToString();
                    }
                    */

                    string sql = "SELECT user_id,active,block,username,password,platform,time_login FROM users where device_id=@device_id and type = 2 LIMIT 0,1";
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        password = MySqlCommon.md5(password);
                        cmd.Parameters.AddWithValue("@device_id", device_id);
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        res.Username = "";
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            int _active = int.Parse(reader["active"].ToString());
                            int _block = int.Parse(reader["block"].ToString());
                            string _password = reader["password"].ToString();
                            string _username = reader["username"].ToString();
                            //if (_block == 0)
                            //{
                            if (_active == 1)
                            {
                                if (_password == password)
                                {
                                    res.error = 0;
                                    res.message = "Success";

                                    res.Username = reader["username"].ToString();
                                }
                                else
                                {
                                    res.error = 3;
                                    res.message = "Account not match";
                                }
                            }
                            else
                            {
                                res.error = 2;
                                res.message = "Account is not actived";
                            }
                            //}
                            //else
                            //{
                            //    res.error = 2;
                            //    res.message = "Account is banned";
                            //}
                        }
                        else if (cNumberOfAcc >= maxAccount)
                        {
                            res.error = -11;
                        }
                        else if (!await RedisManager.CanRegisterInDay(device_id))
                        {
                            res.error = -15;
                        }
                        else
                        {
                            string new_account = "INSERT INTO users(username, password, nickname, cash, platform, device_id, type, ip, language, time_register,avatar) VALUES(";

                            int rand = new Random().Next(7, 100);

                            new_account += "'" + username + "',";
                            new_account += "'" + password + "',";
                            new_account += "'" + nickname + "',";
                            new_account += "'0',";
                            new_account += "'" + platform + "',";
                            new_account += "'" + device_id + "',";
                            new_account += "2,";
                            new_account += "'" + ip + "',";
                            new_account += "'" + language + "',";
                            new_account += "NOW(),";
                            new_account += "'" + rand.ToString() + "');";

                            await SqlLogger.executeNonQueryAsync(new_account);
                            RedisManager.IncRegisterInDay(device_id);
                            res.error = 0;
                            res.Username = username;
                            res.NewAcc = true;
                            //if (platform != "BOT")
                            //{
                            //    DateTime dateTime = DateTime.UtcNow;
                            //    dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime).ToLocalTime();
                            //    string curDate = dateTime.ToString("yyyy-MM-dd");
                            //    string NRU = "INSERT INTO game_analytics(date_current,platform,index_type,total) VALUES('" + curDate + "','" + platform + "','NRU',1)";
                            //    NRU += " ON DUPLICATE KEY UPDATE total=total+1";
                            //    MySqlCommon.ExecuteNonQuery(NRU);
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var ms = ex.ToString();
                Logger.Error("Error LoginByDevice " + ms);
                res.message = ms;
            }

            return res;
        }

        public static async Task<long> GetUserCash(long user_id)
        {
            try
            {
                string sql = "SELECT `cash` FROM `users` WHERE `user_id` = {0} LIMIT 1;";
                sql = string.Format(sql, user_id);
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader) await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            return long.Parse(reader["cash"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetUserCash(long user_id):" + ex.ToString());
            }
            return -1;
        }

        public static async Task<User> GetUserInfo(long user_id)
        {
            User res = new User() { error = 1, message = "" };

            try
            {
                string sql = "SELECT * FROM users WHERE user_id = {0} LIMIT 1;";
                sql = string.Format(sql, user_id);
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            res.error = 0;
                            res.Username = reader["username"].ToString();
                            res.Password = reader["password"].ToString();
                            res.Nickname = reader["nickname"].ToString();
                            res.Cash = long.Parse(reader["cash"].ToString());
                            //res.CashSilver = long.Parse(reader["cash_silver"].ToString());
                            //res.CashSafe = long.Parse(reader["cash_safe"].ToString());
                            res.DeviceId = reader["device_id"].ToString();
                            res.VipId = byte.Parse(reader["vip_id"].ToString());
                            res.UserId = long.Parse(reader["user_id"].ToString());
                            res.Avatar = reader["avatar"].ToString();
                            if (string.IsNullOrEmpty(res.Avatar))
                            {
                                res.Avatar = "0";
                            }
                            res.PhoneNumber = reader["phone_number"].ToString();
                            res.Platform = reader["platform"].ToString();
                            res.VipPoint = int.Parse(reader["vip_point"].ToString());

                            //res.IsExChange = reader["block"].ToString() == "0" ? (byte)1 : (byte)0;
                            res.TimeLogin = reader["time_login"].ToString();
                            res.TotalFriend = int.Parse(reader["total_friend"].ToString());
                            res.Gender = reader["gender"].ToString();
                            res.Age = string.IsNullOrEmpty(reader["age"].ToString()) ? 0 : int.Parse(reader["age"].ToString());
                            res.Married = reader["marries"].ToString();
                            res.Level = int.Parse(reader["level"].ToString());
                            res.Like = int.Parse(reader["like"].ToString());
                            res.Games = reader["game"].ToString();
                            res.Description = reader["description"].ToString();
                            //res.UrlFacebook = reader["url_facebook"].ToString();
                            //res.UrlTwitter = reader["url_twitter"].ToString();
                            res.Language = reader["language"].ToString();
                            //res.PublicProfile = int.Parse(reader["publicprofile"].ToString());
                            //res.Trust = int.Parse(reader["trust"].ToString());
                            res.VerifyLogin = int.Parse(reader["verify_login"].ToString()) != 0;
                        }
                        else
                        {
                            res.message = "Tài khoản không tồn tại. Xin vui lòng thử lại";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetUserInfo(long user_id):" + ex.ToString());
            }
            return res;
        }

        public static async Task<User> GetUserInfo(string username)
        {
            User res = new User() { error = 1, message = "" };

            try
            {
                string sql = "SELECT `username`,`password`,`nickname`,`cash`,`user_id`,`avatar`,`phone_number`,`platform` FROM users WHERE username = '{0}' LIMIT 1;";
                sql = string.Format(sql, username);
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            res.error = 0;
                            res.Username = reader["username"].ToString();
                            res.Password = reader["password"].ToString();
                            res.Nickname = reader["nickname"].ToString();
                            res.Cash = long.Parse(reader["cash"].ToString());
                            //res.CashSilver = long.Parse(reader["cash_silver"].ToString());
                            //res.CashSafe = long.Parse(reader["cash_safe"].ToString());
                            //res.VipId = byte.Parse(reader["vip_id"].ToString());
                            res.UserId = long.Parse(reader["user_id"].ToString());
                            res.Avatar = reader["avatar"].ToString();
                            if (string.IsNullOrEmpty(res.Avatar))
                            {
                                res.Avatar = "0";
                            }
                            res.PhoneNumber = reader["phone_number"].ToString();
                            res.Platform = reader["platform"].ToString();
                            //res.VipPoint = int.Parse(reader["vip_point"].ToString());

                            //res.IsExChange = reader["block"].ToString() == "0" ? (byte)1 : (byte)0;
                            //res.TimeLogin = reader["time_login"].ToString();
                            //res.TotalFriend = int.Parse(reader["total_friend"].ToString());
                            //res.Gender = reader["gender"].ToString();
                            //res.Age = string.IsNullOrEmpty(reader["age"].ToString()) ? 0 : int.Parse(reader["age"].ToString());
                            //res.Married = reader["marries"].ToString();
                            //res.Level = int.Parse(reader["level"].ToString());
                            //res.Like = int.Parse(reader["like"].ToString());
                            //res.Games = reader["game"].ToString();
                            //res.Description = reader["description"].ToString();
                            //res.UrlFacebook = reader["url_facebook"].ToString();
                            //res.UrlTwitter = reader["url_twitter"].ToString();
                            //res.Language = reader["language"].ToString();
                            //res.PublicProfile = int.Parse(reader["publicprofile"].ToString());
                            //res.Trust = int.Parse(reader["trust"].ToString());
                        }
                        else
                        {
                            res.message = "Tài khoản không tồn tại. Xin vui lòng thử lại";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GetUserInfo(string username):" + ex.ToString());
            }
            return res;
        }

        public static void SaveDeviceId(long userId, string deviceId)
        {
            try
            {
                string query = "UPDATE `users` SET `device_id`='{0}' WHERE `user_id`={1};";
                query = string.Format(query, deviceId, userId);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SaveDeviceId sql " + ex.ToString());
            }
        }

        public static void SaveAppId(long userId, string appId, int vcode, string cp)
        {
            try
            {
                DateTime dateTime = DateTime.UtcNow;
                string date_now = dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                string query = @"UPDATE `users` SET `time_login`='{0}', `appId`='{1}', `vcode`={2} WHERE `user_id`={3};
UPDATE `users` SET `cp`='{4}' WHERE `user_id`={3} AND (`cp` IS NULL OR `cp` = '');";
                query = string.Format(query, date_now, appId, vcode, userId, cp);
                MySqlProcess.Genneral.MySqlCommon.ExecuteNonQuery(query);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SaveAppId sql " + ex.ToString());
            }
        }

        public static async Task<response_base> UpdateProfile(long user_id, string phone, string avatar)
        {
            response_base res = new response_base() { error = 1, message = "" };
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(avatar))
            {
                res.error = 1;
                return res;
            }

            try
            {
                string sql = (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(avatar)) ? string.Format("UPDATE `users` SET `phone_number`='{0}',`avatar`='{1}' WHERE `user_id`={2} LIMIT 1;", phone, avatar, user_id) :
                    (!string.IsNullOrEmpty(phone) ? string.Format("UPDATE `users` SET `phone_number`='{0}' WHERE `user_id`={1} LIMIT 1;", phone, user_id) :
                    string.Format("UPDATE `users` SET `avatar`='{0}' WHERE `user_id`={1} LIMIT 1;", avatar, user_id));
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        //Logger.Info("Update profile cmd: " + cmd.ToString());
                        int result = await cmd.ExecuteNonQueryAsync();
                        //Logger.Info("Update profile result: " + result);
                    }
                }

                res.error = 0;
                //res.message = "Cập nhật thông tin thành công.";
            }
            catch (System.Exception ex)
            {
                res.error = 2;
                //res.message = ex.ToString();//"Hệ thống lỗi, xin vui lòng thử lại";
                Logger.Error("Error in update profile: " + ex.ToString());
            }

            return res;
        }

        public static async Task<response_base> UpdateNickname(long user_id, string nickname)
        {
            response_base res = new response_base() { error = 1, message = "" };
            if (string.IsNullOrEmpty(nickname) || nickname.Length < 6 || nickname.Length > 20)
            {
                res.error = 1;
                return res;
            }

            try
            {
                string sql = string.Format("UPDATE `users` SET `nickname`='{0}' WHERE `user_id`={1} LIMIT 1;", nickname, user_id);
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        //Logger.Info("Update profile cmd: " + cmd.ToString());
                        int result = await cmd.ExecuteNonQueryAsync();
                        //Logger.Info("Update profile result: " + result);
                    }
                }

                res.error = 0;
                //res.message = "Cập nhật thông tin thành công.";
            }
            catch (System.Exception ex)
            {
                res.error = 2;
                //res.message = ex.ToString();//"Hệ thống lỗi, xin vui lòng thử lại";
                Logger.Error("Error in update profile: " + ex.ToString());
            }

            return res;
        }

        public static async Task<response_base> UpdateNicknameAvatar(long user_id, string nickname, string avatar)
        {
            response_base res = new response_base() { error = 1, message = "" };
            if (string.IsNullOrEmpty(nickname) && string.IsNullOrEmpty(avatar))
            {
                res.error = 1;
                return res;
            }

            try
            {
                string sql = string.Format("UPDATE `users` SET `nickname`='{0}',`avatar`='{1}' WHERE `user_id`={2} LIMIT 1;", nickname, avatar, user_id);
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();

                        //Logger.Info("Update profile cmd: " + cmd.ToString());
                        int result = await cmd.ExecuteNonQueryAsync();
                        //Logger.Info("Update profile result: " + result);
                    }
                }

                res.error = 0;
                //res.message = "Cập nhật thông tin thành công.";
            }
            catch (System.Exception ex)
            {
                res.error = 2;
                //res.message = ex.ToString();//"Hệ thống lỗi, xin vui lòng thử lại";
                Logger.Error("Error in update profile: " + ex.ToString());
            }

            return res;
        }

        public static bool ValidString(string str)
        {
            string pattern = @"^[a-zA-Z0-9\._\-]{5,25}[^.-]$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(str);
        }

        public static async Task<Dictionary<string, string>> ReadOneData(string table, string select, string select_out, string where = "", string groupby = "")
        {
            Dictionary<string, string> data = new Dictionary<string, string>();

            if (select == "" || select == "*") return data;

            string[] key = select_out.Split(',');

            string sql = "SELECT " + select + " FROM " + table;
            if (where != "")
                sql += " WHERE " + where;
            if (groupby != "")
                sql += " GROUP BY " + groupby;
            sql += " LIMIT 1;";
            int length = key.Length;
            if (length > 0)
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            for (int i = 0; i < length; i++)
                            {
                                string field = key[i].Trim();
                                data[field] = reader[field].ToString();
                            }
                        }

                        reader.Close();
                    }
                    con.Close();
                }
            }

            return data;
        }
        public static async Task IAPLogs(long _user_id, string _signature, string _signeddata, string _platform, string _version)
        {
            using (MySqlConnection con = MySqlConnect.Connection())
            {
                string pay4_sql = "INSERT INTO iap_logs(`user_id`,`signature`,`signeddata`,`platform`,`version`,`time_created`) VALUES(@user_id,@signature,@signeddata,@platform,@version,now())";
                using (MySqlCommand cmd = new MySqlCommand(pay4_sql, con))
                {
                    cmd.CommandType = CommandType.Text;

                    cmd.Parameters.AddWithValue("@user_id", _user_id);
                    cmd.Parameters.AddWithValue("@signature", _signature);
                    cmd.Parameters.AddWithValue("@signeddata", _signeddata);
                    cmd.Parameters.AddWithValue("@platform", _platform);
                    cmd.Parameters.AddWithValue("@version", _version);
                    //string rs = MySqlCommon.ExecuteNonQuery(pay4_sql);               

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }

                con.Close();
            }
        }

        public static async Task<SimpleJSON.JSONArray> GetIapHistories(long userId, int limit = 30)
        {
            var res = new SimpleJSON.JSONArray();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = string.Format("SELECT `product`,`amount`,`create_time` FROM `app_billing` WHERE `user_id`={0} ORDER BY `create_time` DESC LIMIT {1};", userId, limit);
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var data = new SimpleJSON.JSONObject();

                                data["product"] = reader["product"].ToString();
                                data["cash"] = reader.GetInt64("amount");
                                data["use_time"] = reader["create_time"].ToString();

                                res.Add(data);
                            }

                        }
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error GetIapHistories history: " + ex.ToString());
                return res;
            }
        }

        public static async Task<SimpleJSON.JSONArray> GetTranHistories(string nickname, int limit = 30)
        {
            var res = new SimpleJSON.JSONArray();
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    var sql = string.Format("SELECT bc_trans_log.Id, bc_trans_log.UserId, bc_trans_log.Cash, bc_trans_log.CashGain, bc_trans_log.Time, bc_trans_log.Extra, bc_trans_log.Type FROM `bc_trans_log` JOIN users WHERE users.nickname='{0}' AND users.user_id=bc_trans_log.UserId ORDER by bc_trans_log.Id DESC LIMIT {1};", 
                        MySqlHelper.EscapeString(nickname), limit);
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var data = new SimpleJSON.JSONObject();

                                data["Id"] = reader.GetInt64("Id");
                                data["UserId"] = reader.GetInt64("UserId");
                                data["Cash"] = reader.GetInt64("Cash");
                                data["CashGain"] = reader.GetInt64("CashGain");
                                data["Time"] = reader["Time"].ToString();
                                data["Extra"] = reader["Extra"].ToString();
                                data["Type"] = reader.GetInt16("Type");

                                res.Add(data);
                            }

                        }
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error GetTranHistories history: " + ex.ToString());
                return res;
            }
        }

        public static async Task<long> GetUserTotalCashIn(long userId)
        {
            try
            {
                using (MySqlConnection con = MySqlConnect.Connection())
                {
                    string sql = "SELECT SUM(`total_price_in`) as x FROM `users_top` WHERE `user_id`=" + userId;
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            var res = reader["x"].ToString();
                            long result = string.IsNullOrEmpty(res) ? 0L : long.Parse(res.ToString());
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var ms = ex.ToString();
                Logger.Error("Error GetUserTotalCashIn " + userId + ": " + ms);
            }
            return 0;
        }
    }
}
