using Entites.Cms;
using System;
using System.Collections.Generic;

namespace Entites.General
{
    public class User : response_base
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Platform { get; set; }
        public string DeviceId { get; set; }
        public string ClientId { get; set; }
        public string IP { get; set; }
        public string Nickname { get; set; }
        public long Cash { get; set; }
        public long CashSafe { get; set; }
        public long CashSilver { get; set; }
        public byte VipId { get; set; }
        public int VipPoint { get; set; }
        public string Avatar { get; set; }
        public string PhoneNumber { get; set; }
        public byte IsExChange { get; set; }
        public string TimeLogin { get; set; }
        public int TotalFriend { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Married { get; set; }
        public int Level { get; set; }
        public int BcLevel { get; set; }
        public long BcExp { get; set; }
        public int Like { get; set; }
        public string Games { get; set; }
        public string Description { get; set; }
        public string UrlFacebook { get; set; }
        public string UrlTwitter { get; set; }
        public string Language { get; set; }
        public int PublicProfile { get; set; }
        public int Trust { get; set; }
        public string AppId { get; set; }
        public int VersionCode { get; set; }
        public string Cp { get; set; }
        public long TimestampLogin { get; set; }
        public DateTime TimestampRegister { get; set; }

        public bool Authorized { get; set; } = false;

        public int IapCount { get; set; } = 0;
        public long CardInCount { get; set; } = 0;

        public bool VerifyLogin { get; set; } = false;
        public string LastOtp { get; set; }
        public long LastGetOtp { get; set; }
    }
    public class Users : response_base
    {
        public List<User> data { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public Users()
        {
            data = new List<User>();
        }
    }
    public class UserInfo : response_base
    {
        public long UserID { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public long Cash { get; set; }
        public long CashSafe { get; set; }
        public long CashSilver { get; set; }
        public int VipPoint { get; set; }
        public byte VipId { get; set; }
        public string PhoneNumber { get; set; }
        public string Avatar { get; set; }
        public string TimeLogin { get; set; }
        public int TotalFriend { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Married { get; set; }
        public int Level { get; set; }
        public int Like { get; set; }
        public string Games { get; set; }
        public string Description { get; set; }
        public string Online { get; set; }
        public bool Myself { get; set; }
        public string UrlFacebook { get; set; }
        public string UrlTwitter { get; set; }
        public string Language { get; set; }
        public int Trust { get; set; }
        public string GenderValue { get; set; }
        public string MarriedValue { get; set; }
        public int PublicProfile { get; set; }
    }
}
