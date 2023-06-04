using SimpleJSON;
using System.Collections.Generic;

namespace BanCa.WebService
{
    public class LobbyConfig
    {
        static LobbyConfig()
        {
            var json = JSON.Parse("{\"default\":{\"_0\":{\"cin\":1,\"cout\":1,\"agen\":0,\"gico\":1,\"iap\":1,\"vitel\":1,\"vina\":1,\"mobi\":1,\"cir\":1.25,\"cor\":1.5,\"ali\":1,\"arg\":1,\"vic\":[10000,20000,50000,100000,200000,300000,500000],\"vnc\":[10000,20000,50000,100000,200000,300000,500000],\"moc\":[10000,20000,50000,100000,200000,300000,500000],\"vttr\":[1,1,1,1,1,1],\"vnpr\":[1,1,1,1,1,1],\"vmsr\":[1,1,1,1,1,1],\"vttf\":[1,1,1,1,1,1],\"vnpf\":[1,1,1,1,1,1],\"vmsf\":[1,1,1,1,1,1],\"covic\":[75000,150000,300000,750000],\"covnc\":[75000,150000,300000,750000],\"comoc\":[75000,150000,300000,750000],\"covitel\":1,\"covina\":1,\"comobi\":1,\"dc\":500,\"vpsc\":1000,\"vcg\":30000,\"vlc\":1000,\"far\":5,\"fas\":5,\"code\":1,\"ourl\":\"null\",\"aca\":1,\"mista\":600,\"mcita\":100000,\"mitta\":2,\"asl5\":1,\"abc\":1,\"atx\":1,\"adid\":\"\",\"adapp\":\"\",\"copd\":5,\"cotr\":0,\"cogr\":100000,\"cocic\":0,\"cocii\":0,\"cocit\":10000,\"comc\":75000,\"comcs\":2000000,\"mjfb\":1,\"mjsb\":1,\"tfcc\":1000,\"tfcm\":10000,\"tffp\":0.02,\"mmcibco\":1,\"mmcir\":1,\"mmcor\":1,\"bncir\":1,\"bncor\":1,\"bncov\":[500000,1000000,1500000,2000000],\"mmcov\":[500000,1000000,1500000,2000000],\"smsvt\":[],\"smsvi\":[],\"smsmo\":[],\"csa\":10000,\"csm\":100,\"cvp\":false,\"dbtors\":[]}},\"all\":{\"_0\":{\"cin\":1,\"cout\":1,\"agen\":1,\"gico\":1,\"iap\":1,\"vitel\":1,\"vina\":1,\"mobi\":1,\"cir\":1,\"cor\":1.15,\"ali\":1,\"arg\":1,\"vic\":[50000,100000,200000,300000,500000],\"vnc\":[50000,100000,200000,300000,500000],\"moc\":[50000,100000,200000,300000,500000],\"vttr\":[1,1,1,1,1,1],\"vnpr\":[1,1,1,1,1,1],\"vmsr\":[1,1,1,1,1,1],\"vttf\":[1,1,1,1,1,1],\"vnpf\":[1,1,1,1,1,1],\"vmsf\":[1,1,1,1,1,1],\"covic\":[50000,100000,200000,500000],\"covnc\":[50000,100000,200000,500000],\"comoc\":[50000,100000,200000,500000],\"covitel\":1,\"covina\":1,\"comobi\":1,\"dc\":500,\"vpsc\":1000,\"vcg\":30000,\"vlc\":1000,\"far\":5,\"fas\":5,\"code\":1,\"ourl\":\"null\",\"aca\":1,\"mista\":600,\"mcita\":100000,\"mitta\":2,\"asl5\":1,\"abc\":1,\"atx\":1,\"adid\":\"\",\"adapp\":\"\",\"copd\":0,\"cotr\":0,\"cogr\":0,\"cocic\":0,\"cocii\":0,\"cocit\":0,\"comc\":0,\"comcs\":2000000,\"mjfb\":1,\"mjsb\":1,\"tfcc\":1000,\"tfcm\":10000,\"tffp\":0.02,\"mmcibco\":1,\"mmcir\":1,\"mmcor\":1,\"bncir\":1,\"bncor\":1,\"bncov\":[500000,1000000,1500000,2000000],\"mmcov\":[500000,1000000,1500000,2000000],\"smsvt\":[],\"smsvi\":[],\"smsmo\":[],\"csa\":10000,\"csm\":100,\"cvp\":false,\"dbtors\":[]}}}");
            LobbyConfig.ParseJson(json);
        }

        // <appId, <version code, config>>
        private static Dictionary<string, Dictionary<string, VersionConfig>> Configs = new Dictionary<string, Dictionary<string, VersionConfig>>();
        private static VersionConfig defaultConfig = new VersionConfig();

        public static VersionConfig OpenAllConfig = new VersionConfig();

        public static VersionConfig GetDefaultConfig()
        {
            return defaultConfig;
        }

        public static VersionConfig GetConfig(string appId, int vcode)
        {
            lock (Configs)
            {
                if (!string.IsNullOrEmpty(appId) && Configs.ContainsKey(appId))
                {
                    var c = Configs[appId];
                    var k = "_" + vcode;
                    if (c.ContainsKey(k))
                    {
                        return c[k];
                    }
                }
            }

            return defaultConfig;
        }

        public static void AddConfig(string appId, int vcode, VersionConfig config)
        {
            if (string.IsNullOrEmpty(appId))
                return;

            lock (Configs)
            {
                if (!Configs.ContainsKey(appId)) Configs[appId] = new Dictionary<string, VersionConfig>();
                Configs[appId]["_" + vcode] = config;
            }
        }

        public static JSONNode ToJson()
        {
            lock (Configs)
            {
                var data = new JSONObject();
                if (Configs.Count == 0)
                {
                    var c = new JSONObject();
                    data["default"] = c;
                    c["_0"] = defaultConfig.ToJson();

                    c = new JSONObject();
                    data["all"] = c;
                    c["_0"] = OpenAllConfig.ToJson();
                }
                else
                {
                    foreach (var pairAppid_code in Configs)
                    {
                        var appId = pairAppid_code.Key;
                        var codeConfigs = pairAppid_code.Value;
                        var json = new JSONObject();
                        data[appId] = json;
                        foreach (var pairCode_config in codeConfigs)
                        {
                            json[pairCode_config.Key.ToString()] = pairCode_config.Value.ToJson();
                        }
                    }
                }
                return data;
            }
        }

        public static string ToJsonString()
        {
            return ToJson().ToString();
        }

        public static void ParseJson(JSONNode data)
        {
            lock (Configs)
            {
                //Logger.Info("Update config: " + data.ToString());
                foreach (var key in data.Keys)
                {
                    if (!Configs.ContainsKey(key))
                    {
                        Configs[key] = new Dictionary<string, VersionConfig>();
                    }
                    var configs = Configs[key];
                    var sub = data[key].AsObject;
                    foreach (var key2 in sub.Keys)
                    {
                        if (key == "default")
                        {
                            defaultConfig.ParseJson(sub[key2]);
                            configs[key2] = defaultConfig;
                            break;
                        }
                        else if (key == "all")
                        {
                            OpenAllConfig.ParseJson(sub[key2]);
                            configs[key2] = OpenAllConfig;
                            break;
                        }
                        else
                        {
                            var c = new VersionConfig();
                            c.ParseJson(sub[key2]);
                            configs[key2] = c;
                        }
                    }
                }
            }
        }
    }

    public class VersionConfig
    {
        public volatile int CodeVersion = 1;
        public volatile string OutDateVersionUrl = "";

        public volatile bool ActiveCashIn, ActiveCashOut, ActiveAgency, ActiveGiftCode, ActiveViettel, ActiveVinaphone, ActiveMobiphone, ActiveIAP,
            ActiveCashOutViettel, ActiveCashOutVinaphone, ActiveCashOutMobiphone;
        public volatile bool ActiveLogin, ActiveRegister;
        public volatile JSONArray ViettelCards, VinaphoneCards, MobiphoneCards;
        public volatile JSONArray CashOutViettelCards, CashOutVinaphoneCards, CashOutMobiphoneCards;
        private List<Distributor> Distributors;

        public volatile float CashInRate;
        public volatile float CashOutRate;

        public volatile int DailyCash = 500;
        public volatile int VerifyPhoneSmsCost = 1000;
        public volatile int VerifyGiftCash = 30000;
        public volatile int VerifySmsLoginCost = 1000;
        public volatile int FirstAccRapidFireGift = 5;
        public volatile int FirstAccSnipeGift = 5;
        public volatile bool AllowClientActive = true;
        public volatile float MinIdleSecondToActive = 10 * 60f;
        public volatile int MinCardInToActive = 100000;
        public volatile int MinIapTimeToActive = 2;

        public volatile int CashOutPerDay = 5;
        public volatile int CashOutTimeFromRegisterMS = 60 * 60 * 1000;
        public volatile int CashOutGoldRemain = 20;
        public volatile int CashOutCashInCard = 20000;
        public volatile int CashOutCashInIAP = 0;
        public volatile int CashOutCashInTotal = 20000;
        public volatile int CashOutMaxCashPerDay = 100000;
        public volatile int CashOutMaxCashServerPerDay = 2000000;

        public volatile bool ActiveSlot5 = true;
        public volatile bool ActiveBanCa = true;
        public volatile bool ActiveTaiXiu = true;

        public volatile string AdsAppId = "";
        public volatile string AdsId = "";

        public volatile float MinJoinFreeBc = 100;
        public volatile float MinJoinSoloBc = 100;

        public volatile JSONArray VTTR, VNPR, VMSR;
        public volatile JSONArray VTTF, VNPF, VMSF;

        public volatile SmsInItem[] SmsViettel, SmsVina, SmsMobi;

        public volatile int TransferCashCost = 1000;
        public volatile int TransferCashMin = 10000;
        public volatile float TransferCostFeePercent = 0.02f;

        public volatile float MomoCashInBeforeCashOut = 1;
        public volatile float MomoCashInRate = 1;
        public volatile float MomoCashOutRate = 1;
        public volatile float BankCashInRate = 1;
        public volatile float BankCashOutRate = 1;

        public volatile JSONArray BankCashOutValues, MoMoCashOutValues;

        // add cash when user cash < min
        public volatile int CashSaveAmount = 0;
        public volatile int CashSaveMin = 0;

        public volatile bool CheckVerifyPhone = false;

        public VersionConfig()
        {
            ActiveCashIn = true;
            ActiveCashOut = true;
            ActiveAgency = true;
            ActiveGiftCode = true;
            ActiveIAP = true;
            ActiveViettel = true;
            ActiveVinaphone = true;
            ActiveMobiphone = true;

            ActiveCashOutViettel = true;
            ActiveCashOutVinaphone = true;
            ActiveCashOutMobiphone = true;

            ActiveLogin = true;
            ActiveRegister = true;

            CashInRate = 1;
            CashOutRate = 1.15f;

            ViettelCards = JSON.Parse("[10000, 50000]").AsArray;
            VinaphoneCards = JSON.Parse("[20000, 50000]").AsArray;
            MobiphoneCards = JSON.Parse("[10000, 100000]").AsArray;

            CashOutViettelCards = JSON.Parse("[10000, 20000, 50000]").AsArray;
            CashOutVinaphoneCards = JSON.Parse("[20000, 50000]").AsArray;
            CashOutMobiphoneCards = JSON.Parse("[10000, 50000, 100000]").AsArray;

            // 10, 20, 30, 50, 100, 200, 300, 500
            VTTR = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;
            VNPR = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;
            VMSR = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;

            VTTF = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;
            VNPF = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;
            VMSF = JSON.Parse("[1, 1, 1, 1, 1, 1]").AsArray;

            BankCashOutValues = JSON.Parse("[500000, 1000000, 1500000, 2000000]").AsArray;
            MoMoCashOutValues = JSON.Parse("[500000, 1000000, 1500000, 2000000]").AsArray;

            SmsMobi = new SmsInItem[0];
            SmsViettel = new SmsInItem[0];
            SmsVina = new SmsInItem[0];

            Distributors = new List<Distributor> { };
        }

        public int GetCardAmountIndex(long cardAmount)
        {
            switch (cardAmount)
            {
                case 10000:
                    return 0;
                case 20000:
                    return 1;
                case 30000:
                    return 2;
                case 50000:
                    return 3;
                case 100000:
                    return 4;
                case 200000:
                    return 5;
                case 300000:
                    return 6;
                case 500000:
                    return 7;
            }

            return -1;
        }
        public float GetCashInBonus(string card_type, long cardAmount, bool first)
        {
            switch (card_type.ToLower())
            {
                case "viettel":
                    return 1f + GetCashInBonusViettel(cardAmount, first) / 100f;
                case "vina":
                    return 1f + GetCashInBonusVina(cardAmount, first) / 100f;
                case "mobi":
                    return 1f + GetCashInBonusMobi(cardAmount, first) / 100f;
            }

            return 1f;
        }
        public float GetCashInBonusViettel(long cardAmount, bool first)
        {
            var arr = first ? VTTF : VTTR;
            var index = GetCardAmountIndex(cardAmount);
            if (arr != null && arr.Count > index && index > -1)
            {
                if (first)
                    return arr[index].AsFloat + GetCashInBonusViettel(cardAmount, false);
                return arr[index];
            }
            return 0f;
        }
        public float GetCashInBonusMobi(long cardAmount, bool first)
        {
            var arr = first ? VMSF : VMSR;
            var index = GetCardAmountIndex(cardAmount);
            if (arr != null && arr.Count > index && index > -1)
            {
                if (first)
                    return arr[index].AsFloat + GetCashInBonusMobi(cardAmount, false);
                return arr[index];
            }
            return 0f;
        }
        public float GetCashInBonusVina(long cardAmount, bool first)
        {
            var arr = first ? VNPF : VNPR;
            var index = GetCardAmountIndex(cardAmount);
            if (arr != null && arr.Count > index && index > -1)
            {
                if (first)
                    return arr[index].AsFloat + GetCashInBonusVina(cardAmount, false);
                return arr[index];
            }
            return 0f;
        }

        public SmsInItem FindSmsItem(long amount, string telco)
        {
            switch (telco.ToUpper())
            {
                case "VTT":
                    {
                        var items = SmsViettel;
                        foreach (var item in items)
                        {
                            if (item.Money == amount) return item;
                        }
                        return null;
                    }
                case "VMS":
                    {
                        var items = SmsMobi;
                        foreach (var item in items)
                        {
                            if (item.Money == amount) return item;
                        }
                        return null;
                    }
                case "VNP":
                    {
                        var items = SmsVina;
                        foreach (var item in items)
                        {
                            if (item.Money == amount) return item;
                        }
                        return null;
                    }
            }
            return null;
        }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["cin"] = ActiveCashIn ? 1 : 0;
            data["cout"] = ActiveCashOut ? 1 : 0;
            data["agen"] = ActiveAgency ? 1 : 0;
            data["gico"] = ActiveGiftCode ? 1 : 0;
            data["iap"] = ActiveIAP ? 1 : 0;

            data["vitel"] = ActiveViettel ? 1 : 0;
            data["vina"] = ActiveVinaphone ? 1 : 0;
            data["mobi"] = ActiveMobiphone ? 1 : 0;

            data["cir"] = CashInRate;
            data["cor"] = CashOutRate;

            data["ali"] = ActiveLogin ? 1 : 0;
            data["arg"] = ActiveRegister ? 1 : 0;

            data["vic"] = ViettelCards;
            data["vnc"] = VinaphoneCards;
            data["moc"] = MobiphoneCards;

            data["vttr"] = VTTR;
            data["vnpr"] = VNPR;
            data["vmsr"] = VMSR;

            data["vttf"] = VTTF;
            data["vnpf"] = VNPF;
            data["vmsf"] = VMSF;

            data["covic"] = CashOutViettelCards;
            data["covnc"] = CashOutVinaphoneCards;
            data["comoc"] = CashOutMobiphoneCards;

            data["covitel"] = ActiveCashOutViettel ? 1 : 0;
            data["covina"] = ActiveCashOutVinaphone ? 1 : 0;
            data["comobi"] = ActiveCashOutMobiphone ? 1 : 0;

            data["dc"] = DailyCash;
            data["vpsc"] = VerifyPhoneSmsCost;
            data["vcg"] = VerifyGiftCash;
            data["vlc"] = VerifySmsLoginCost;
            data["far"] = FirstAccRapidFireGift;
            data["fas"] = FirstAccSnipeGift;
            data["code"] = CodeVersion;
            data["ourl"] = OutDateVersionUrl;
            data["aca"] = AllowClientActive ? 1 : 0;

            data["mista"] = MinIdleSecondToActive;
            data["mcita"] = MinCardInToActive;
            data["mitta"] = MinIapTimeToActive;

            data["asl5"] = ActiveSlot5 ? 1 : 0;
            data["abc"] = ActiveBanCa ? 1 : 0;
            data["atx"] = ActiveTaiXiu ? 1 : 0;

            data["adid"] = AdsId;
            data["adapp"] = AdsAppId;

            data["copd"] = CashOutPerDay;
            data["cotr"] = CashOutTimeFromRegisterMS;
            data["cogr"] = CashOutGoldRemain;
            data["cocic"] = CashOutCashInCard;
            data["cocii"] = CashOutCashInIAP;
            data["cocit"] = CashOutCashInTotal;
            data["comc"] = CashOutMaxCashPerDay;
            data["comcs"] = CashOutMaxCashServerPerDay;

            data["mjfb"] = MinJoinFreeBc;
            data["mjsb"] = MinJoinSoloBc;

            data["tfcc"] = TransferCashCost;
            data["tfcm"] = TransferCashMin;
            data["tffp"] = TransferCostFeePercent;

            data["mmcibco"] = MomoCashInBeforeCashOut;
            data["mmcir"] = MomoCashInRate;
            data["mmcor"] = MomoCashOutRate;
            data["bncir"] = BankCashInRate;
            data["bncor"] = BankCashOutRate;

            data["bncov"] = BankCashOutValues;
            data["mmcov"] = MoMoCashOutValues;

            data["smsvt"] = JSON.ListObjToJson(SmsViettel);
            data["smsvi"] = JSON.ListObjToJson(SmsVina);
            data["smsmo"] = JSON.ListObjToJson(SmsMobi);

            data["csa"] = CashSaveAmount;
            data["csm"] = CashSaveMin;

            data["cvp"] = CheckVerifyPhone;

            var dbtors = new JSONArray();
            lock (Distributors)
            {
                for (int i = 0; i < Distributors.Count; i++)
                {
                    dbtors.Add(Distributors[i].ToJson());
                }
            }
            data["dbtors"] = dbtors;

            return data;
        }

        public string ToJsonString()
        {
            return ToJson().ToString();
        }

        public void ParseJson(JSONNode data)
        {
            //Logger.Info("Current config: " + this.ToJsonString());
            //Logger.Info("Parjson: " + data.ToString());
            ActiveCashIn = data.HasKey("cin") ? data["cin"].AsInt != 0 : ActiveCashIn;
            ActiveCashOut = data.HasKey("cout") ? data["cout"].AsInt != 0 : ActiveCashOut;
            ActiveAgency = data.HasKey("agen") ? data["agen"].AsInt != 0 : ActiveAgency;
            ActiveGiftCode = data.HasKey("gico") ? data["gico"].AsInt != 0 : ActiveGiftCode;
            ActiveIAP = data.HasKey("iap") ? data["iap"].AsInt != 0 : ActiveIAP;

            ActiveViettel = data.HasKey("vitel") ? data["vitel"].AsInt != 0 : ActiveViettel;
            ActiveVinaphone = data.HasKey("vina") ? data["vina"].AsInt != 0 : ActiveVinaphone;
            ActiveMobiphone = data.HasKey("mobi") ? data["mobi"].AsInt != 0 : ActiveMobiphone;

            ViettelCards = data.HasKey("vic") ? data["vic"].AsArray : ViettelCards;
            VinaphoneCards = data.HasKey("vnc") ? data["vnc"].AsArray : VinaphoneCards;
            MobiphoneCards = data.HasKey("moc") ? data["moc"].AsArray : MobiphoneCards;

            VTTR = data.HasKey("vttr") ? data["vttr"].AsArray : VTTR;
            VNPR = data.HasKey("vnpr") ? data["vnpr"].AsArray : VNPR;
            VMSR = data.HasKey("vmsr") ? data["vmsr"].AsArray : VMSR;

            VTTF = data.HasKey("vttf") ? data["vttf"].AsArray : VTTF;
            VNPF = data.HasKey("vnpf") ? data["vnpf"].AsArray : VNPF;
            VMSF = data.HasKey("vmsf") ? data["vmsf"].AsArray : VMSF;

            CashInRate = data.HasKey("cir") ? data["cir"].AsFloat : CashInRate;
            CashOutRate = data.HasKey("cor") ? data["cor"].AsFloat : CashOutRate;

            ActiveLogin = data.HasKey("ali") ? data["ali"].AsInt != 0 : ActiveLogin;
            ActiveRegister = data.HasKey("arg") ? data["arg"].AsInt != 0 : ActiveRegister;

            ActiveCashOutViettel = data.HasKey("covitel") ? data["covitel"].AsInt != 0 : ActiveCashOutViettel;
            ActiveCashOutVinaphone = data.HasKey("covina") ? data["covina"].AsInt != 0 : ActiveCashOutVinaphone;
            ActiveCashOutMobiphone = data.HasKey("comobi") ? data["comobi"].AsInt != 0 : ActiveCashOutMobiphone;
            CashOutViettelCards = data.HasKey("covic") ? data["covic"].AsArray : CashOutViettelCards;
            CashOutVinaphoneCards = data.HasKey("covnc") ? data["covnc"].AsArray : CashOutVinaphoneCards;
            CashOutMobiphoneCards = data.HasKey("comoc") ? data["comoc"].AsArray : CashOutMobiphoneCards;

            //Logger.Info("dc before " + DailyCash);
            DailyCash = data.HasKey("dc") ? data["dc"].AsInt : DailyCash;
            //Logger.Info("dc after " + DailyCash);

            VerifyPhoneSmsCost = data.HasKey("vpsc") ? data["vpsc"].AsInt : VerifyPhoneSmsCost;
            VerifyGiftCash = data.HasKey("vcg") ? data["vcg"].AsInt : VerifyGiftCash;
            VerifySmsLoginCost = data.HasKey("vlc") ? data["vlc"].AsInt : VerifySmsLoginCost;
            FirstAccRapidFireGift = data.HasKey("far") ? data["far"].AsInt : FirstAccRapidFireGift;
            FirstAccSnipeGift = data.HasKey("fas") ? data["fas"].AsInt : FirstAccSnipeGift;
            CodeVersion = data.HasKey("code") ? data["code"].AsInt : CodeVersion;
            OutDateVersionUrl = data.HasKey("ourl") ? (string)data["ourl"] : OutDateVersionUrl;
            AllowClientActive = data.HasKey("aca") ? data["aca"].AsInt != 0 : AllowClientActive;

            MinIdleSecondToActive = data.HasKey("mista") ? data["mista"].AsFloat : MinIdleSecondToActive;
            MinCardInToActive = data.HasKey("mcita") ? data["mcita"].AsInt : MinCardInToActive;
            MinIapTimeToActive = data.HasKey("mitta") ? data["mitta"].AsInt : MinIapTimeToActive;

            ActiveSlot5 = data.HasKey("asl5") ? data["asl5"].AsInt != 0 : ActiveSlot5;
            ActiveBanCa = data.HasKey("abc") ? data["abc"].AsInt != 0 : ActiveBanCa;
            ActiveTaiXiu = data.HasKey("atx") ? data["atx"].AsInt != 0 : ActiveTaiXiu;

            AdsId = data.HasKey("adid") ? (string)data["adid"] : AdsId;
            AdsAppId = data.HasKey("adapp") ? (string)data["adapp"] : AdsAppId;

            CashOutPerDay = data.HasKey("copd") ? data["copd"].AsInt : 0;
            CashOutTimeFromRegisterMS = data.HasKey("cotr") ? data["cotr"].AsInt : 0;
            CashOutGoldRemain = data.HasKey("cogr") ? data["cogr"].AsInt : 0;
            CashOutCashInCard = data.HasKey("cocic") ? data["cocic"].AsInt : 0;
            CashOutCashInIAP = data.HasKey("cocii") ? data["cocii"].AsInt : 0;
            CashOutCashInTotal = data.HasKey("cocit") ? data["cocit"].AsInt : 0;

            CashOutMaxCashPerDay = data.HasKey("comc") ? data["comc"].AsInt : 0;

            MinJoinFreeBc = data.HasKey("mjfb") ? data["mjfb"].AsFloat : MinJoinFreeBc;
            MinJoinSoloBc = data.HasKey("mjsb") ? data["mjsb"].AsFloat : MinJoinSoloBc;

            TransferCashCost = data.HasKey("tfcc") ? data["tfcc"].AsInt : TransferCashCost;
            TransferCashMin = data.HasKey("tfcm") ? data["tfcm"].AsInt : TransferCashMin;
            TransferCostFeePercent = data.HasKey("tffp") ? data["tffp"].AsFloat : TransferCostFeePercent;

            MomoCashInBeforeCashOut = data.HasKey("mmcibco") ? data["mmcibco"].AsInt : MomoCashInBeforeCashOut;
            MomoCashInRate = data.HasKey("mmcir") ? data["mmcir"].AsFloat : MomoCashInRate;
            MomoCashOutRate = data.HasKey("mmcor") ? data["mmcor"].AsFloat : MomoCashOutRate;

            //Logger.Info("bncir before " + BankCashInRate);
            BankCashInRate = data.HasKey("bncir") ? data["bncir"].AsFloat : BankCashInRate;
            //Logger.Info("bncir after " + BankCashInRate);
            BankCashOutRate = data.HasKey("bncor") ? data["bncor"].AsFloat : BankCashOutRate;

            BankCashOutValues = data.HasKey("bncov") ? data["bncov"].AsArray : BankCashOutValues;
            MoMoCashOutValues = data.HasKey("mmcov") ? data["mmcov"].AsArray : MoMoCashOutValues;

            if (data.HasKey("smsvt"))
                SmsViettel = JSON.JsonArrayToArray<SmsInItem>(data["smsvt"].AsArray);
            if (data.HasKey("smsvi"))
                SmsVina = JSON.JsonArrayToArray<SmsInItem>(data["smsvi"].AsArray);
            if (data.HasKey("smsmo"))
                SmsMobi = JSON.JsonArrayToArray<SmsInItem>(data["smsmo"].AsArray);

            CashSaveAmount = data.HasKey("csa") ? data["csa"].AsInt : CashSaveAmount;
            CashSaveMin = data.HasKey("csm") ? data["csm"].AsInt : CashSaveMin;

            CheckVerifyPhone = data.HasKey("cvp") ? data["cvp"].AsBool : CheckVerifyPhone;

            if (data.HasKey("comcs"))
            {
                CashOutMaxCashServerPerDay = data["comcs"].AsInt;
                //Redis.RedisManager.ClearCashoutFilterData();
            }

            if (data.HasKey("dbtors"))
            {
                var dbtors = data["dbtors"].AsArray;
                lock (Distributors)
                {
                    Distributors.Clear();
                    for (int i = 0; i < dbtors.Count; i++)
                    {
                        var dtor = new Distributor();
                        dtor.ParseJson(dbtors[i]);
                        Distributors.Add(dtor);
                    }
                }
            }

            //Logger.Info("New config: " + this.ToJsonString());
        }
    }

    public class Distributor : IJsonSerializable
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string FbUrl { get; set; }
        public byte Level { get; set; }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["id"] = Id;
            data["name"] = Name;
            data["phone"] = PhoneNumber;
            data["add"] = Address;
            data["fb"] = FbUrl;
            data["lv"] = Level;
            return data;
        }

        public void ParseJson(JSONNode data)
        {
            Id = data["id"].AsLong;
            Name = data["name"];
            PhoneNumber = data["phone"];
            Address = data["add"];
            FbUrl = data["fb"];
            Level = data["lv"].AsByte;
        }
    }

    public class SmsInItem : IJsonSerializable
    {
        public long Money; // tien tru vao tai khoan sms
        public long Cash; // tien cong them vao game
        public string Syntax; // cu phap sms
        public string Number; // dau so

        public void ParseJson(JSONNode data)
        {
            Money = data["money"];
            Cash = data["cash"];
            Syntax = data["syntax"];
            Number = data["number"];
        }

        public JSONNode ToJson()
        {
            var data = new JSONObject();
            data["money"] = Money;
            data["cash"] = Cash;
            data["syntax"] = Syntax;
            data["number"] = Number;
            return data;
        }
    }
}
