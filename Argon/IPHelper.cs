using System;
using System.Net;

using Whois.NET;

namespace Argon
{
    public static class IPHelper
    {
        public static string GetHostname(this string IP)
        {
            try {
                return Dns.GetHostEntry(IP).HostName;
            }
            catch { return null; }
        }

        public static string GetWhois(this string IpOrHostname)
        {
            WhoisResponse res = WhoisClient.Query(IpOrHostname);
            return res.OrganizationName + Environment.NewLine + res.AddressRange.ToString();
        }
    }
}
