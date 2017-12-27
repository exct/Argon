using System;

namespace Argon
{
    public static class ExtensionMethods
    {
        public static long NextSecond(this long i)
        {
            return (long)Math.Ceiling((decimal)i / 10000000) * 10000000;
        }
    }
}
