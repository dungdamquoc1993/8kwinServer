using BanCa.Libs;
using BanCa.Redis;
using BanCa.Sql;
using MySql.Data.MySqlClient;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace OneTwoThree
{
    public enum Choice : byte
    {
        Scissors = 0, Leaf = 1, Hammer = 2, All = 3
    }

    public enum GameResult : byte
    {
        Draw = 0, FirstWin = 1, SecondWin = 2
    }

    public class Match
    {
        public int Blind;

        // 0 = bot
        public long UserId1;
        public string Nickname1;
        public string Avatar1;
        public Choice BetChoice1;

        public long UserId2;
        public string Nickname2;
        public string Avatar2;
        public Choice BetChoice2;

        public GameResult MatchResult;

        public int RetryTime = 0;
        public List<History> Histories = new List<History>();
        public long StartTime;
        public TaskRunner Runner;
        public Random Random;
        public bool IsEnd = false;

        public Action<Match> OnStart;
        public Action<Match, History> OnSolved;
        public Action<Match, long, long, long, long> OnEndGame;

        public Match(TaskRunner runner, Random random, int blind, long userId1, string nickname1, string avatar1, long userId2, string nickname2, string avatar2)
        {
            Runner = runner;
            Random = random;

            Blind = blind;

            UserId1 = userId1;
            Nickname1 = nickname1;
            Avatar1 = avatar1;

            UserId2 = userId2;
            Nickname2 = nickname2;
            Avatar2 = avatar2;
        }

        public void Start()
        {
            Runner.QueueAction(1000, () =>
            {
                IsEnd = false;
                BetChoice1 = BetChoice2 = (Choice)(Random.Next() % (byte)Choice.All);
                StartTime = TimeUtil.TimeStamp;
                Runner.QueueAction(OneTwoThreeBoard.PLAYING_TIME_MS, Solve);

                if (OnStart != null) OnStart(this);
            });
        }

        public async void Solve()
        {
            const int MaxRetry = 3;
            try
            {
                IsEnd = true;
                // bot
                if (UserId1 == 0)
                {
                    BetChoice1 = (Choice)(Random.Next() % (byte)Choice.All);
                }
                if (UserId2 == 0)
                {
                    BetChoice2 = (Choice)(Random.Next() % (byte)Choice.All);
                }

                long changeCash1 = 0, cash1 = 0, changeCash2 = 0, cash2 = 0;
                // calculate
                GameResult Result;
                if (BetChoice1 == BetChoice2) // draw
                {
                    if (RetryTime == MaxRetry - 1)
                    {
                        changeCash1 = Blind;
                        changeCash2 = Blind;
                        cash1 = await RedisManager.IncEpicCash(UserId1, changeCash1, "server", "onetwothree refund", TransType.ONE_TWO_THREE_WIN);
                        cash2 = await RedisManager.IncEpicCash(UserId2, changeCash2, "server", "onetwothree refund", TransType.ONE_TWO_THREE_WIN);
                    }
                    else
                    {
                        cash1 = await RedisManager.GetUserCash(UserId1);
                        cash2 = await RedisManager.GetUserCash(UserId2);
                    }
                    Result = GameResult.Draw;
                }
                else if ((BetChoice1 == Choice.Hammer && BetChoice2 == Choice.Scissors) ||
                    (BetChoice1 == Choice.Leaf && BetChoice2 == Choice.Hammer) ||
                    (BetChoice1 == Choice.Scissors && BetChoice2 == Choice.Leaf)) // 1 win
                {
                    changeCash1 = Blind + Blind * 95 / 100;
                    changeCash2 = 0;
                    cash1 = await RedisManager.IncEpicCash(UserId1, changeCash1, "server", "onetwothree", TransType.ONE_TWO_THREE_WIN);
                    cash2 = await RedisManager.GetUserCash(UserId2);
                    Result = GameResult.FirstWin;
                }
                else // 1 lose
                {
                    changeCash1 = 0;
                    changeCash2 = Blind + Blind * 95 / 100;
                    cash1 = await RedisManager.GetUserCash(UserId1);
                    cash2 = await RedisManager.IncEpicCash(UserId2, changeCash2, "server", "onetwothree", TransType.ONE_TWO_THREE_WIN);
                    Result = GameResult.SecondWin; // TODO: fee
                }
                MatchResult = Result;
                var h = new History() { BetChoice1 = BetChoice1, BetChoice2 = BetChoice2, Result = Result };
                Histories.Add(h);
                if (OnSolved != null) OnSolved(this, h);
                LogGame(changeCash1, cash1, changeCash2, cash2);
                if (Result == GameResult.Draw)
                {
                    RetryTime++;
                    if (RetryTime < MaxRetry)
                    {
                        Start();
                    }
                    else
                    {
                        // end game
                        EndGame(changeCash1, cash1, changeCash2, cash2);
                    }
                }
                else
                {
                    // end game
                    EndGame(changeCash1, cash1, changeCash2, cash2);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("OTT solve error: " + ex.ToString());
            }
        }

        public void EndGame(long changeCash1, long cash1, long changeCash2, long cash2)
        {
            if (OnEndGame != null) OnEndGame(this, changeCash1, cash1, changeCash2, cash2);
        }

        private void LogGame(long changeCash1, long cash1, long changeCash2, long cash2)
        {
            var sql = @"INSERT INTO `one_two_three_history` (`Blind`,`UserId1`,`Nickname1`,`Choice1`,`CashChange1`,`Cash1`,`UserId2`,`Nickname2`,`Choice2`,`CashChange2`,`Cash2`,`Result`) " +
                "VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')";
            sql = string.Format(sql, Blind, UserId1, Nickname1, (int)BetChoice1, changeCash1, cash1,
                UserId2, Nickname2, (int)BetChoice2, changeCash2, cash2,
                (int)MatchResult);
            SqlLogger.ExecuteNonQuery(sql);
        }

        public static async Task<JSONNode> GetGameHistories(long userId)
        {
            var sql = @"SELECT `Id`,`Blind`,`Nickname1`,`Choice1`,`CashChange1`,`Nickname2`,`Choice2`,`CashChange2`,`Result`,`GameTime` FROM `one_two_three_history` " +
                "WHERE `UserId1`={0} OR `UserId2`={0} ORDER BY Id DESC LIMIT 50";
            sql = string.Format(sql, userId);
            var res = new JSONArray();
            try
            {
                using (var con = SqlLogger.getConnection())
                {
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new JSONObject();
                                    item["Id"] = reader["Id"].ToString();
                                    item["Blind"] = reader["Blind"].ToString();
                                    item["Nickname1"] = reader["Nickname1"].ToString();
                                    item["Choice1"] = reader["Choice1"].ToString();
                                    item["CashChange1"] = reader["CashChange1"].ToString();
                                    item["Nickname2"] = reader["Nickname2"].ToString();
                                    item["Choice2"] = reader["Choice2"].ToString();
                                    item["CashChange2"] = reader["CashChange2"].ToString();
                                    item["Result"] = reader["Result"].ToString();
                                    item["GameTime"] = reader["GameTime"].ToString();
                                    res.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("OneTwoThree GetGameHistories fail: \n" + sql + "\n" + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }
            return res;
        }

        public static async Task<JSONNode> GetGameGlories()
        {
            const long MIN_GLORY = 50000;
            var sql = @"SELECT `Id`,`Blind`,`Nickname1`,`Choice1`,`CashChange1`,`Nickname2`,`Choice2`,`CashChange2`,`Result`,`GameTime` FROM `one_two_three_history` " +
                "WHERE `Blind`>={0} ORDER BY Id DESC LIMIT 50";
            sql = string.Format(sql, MIN_GLORY);
            var res = new JSONArray();
            try
            {
                using (var con = SqlLogger.getConnection())
                {
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.Text;
                        await con.OpenAsync();
                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                        if (reader != null && reader.HasRows)
                        {
                            try
                            {
                                while (await reader.ReadAsync())
                                {
                                    var item = new JSONObject();
                                    item["Id"] = reader["Id"].ToString();
                                    item["Blind"] = reader["Blind"].ToString();
                                    item["Nickname1"] = reader["Nickname1"].ToString();
                                    item["Choice1"] = reader["Choice1"].ToString();
                                    item["CashChange1"] = reader["CashChange1"].ToString();
                                    item["Nickname2"] = reader["Nickname2"].ToString();
                                    item["Choice2"] = reader["Choice2"].ToString();
                                    item["CashChange2"] = reader["CashChange2"].ToString();
                                    item["Result"] = reader["Result"].ToString();
                                    item["GameTime"] = reader["GameTime"].ToString();
                                    res.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("OneTwoThree GetGameHistories fail: \n" + sql + "\n" + ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExecuteReader fail: \n" + sql + "\n" + ex.ToString());
            }
            return res;
        }
    }

    public class History
    {
        public Choice BetChoice1;
        public Choice BetChoice2;
        public GameResult Result;
    }
}
