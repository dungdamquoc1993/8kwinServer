using BanCa.Libs;

namespace BanCa.GiftCode
{
    public class GiftCodeManager
    {
        private static PrimeSearch random = new PrimeSearch(1000000000);

        public static string GetCode(int length = 8)
        {
            return random.NextId(length);
        }
    }
}
