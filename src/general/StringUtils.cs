using System;
using System.Globalization;

public static class StringUtils
{
   /// <summary>
   ///   From https://stackoverflow.com/a/30181106 with slight modification.
   /// </summary>
   public static string FormatNumber(long num)
   {
      if (num <= 0)
         return "0";

      // Ensure number has max 3 significant digits (no rounding up can happen)
      long i = (long)Math.Pow(10, (int)Math.Max(0, Math.Log10(num) - 2));
      num = num / i * i;

      if (num >= 1000000000)
         return (num / 1000000000D).ToString("0.##", CultureInfo.CurrentCulture) + "B";
      if (num >= 1000000)
         return (num / 1000000D).ToString("0.##", CultureInfo.CurrentCulture) + "M";
      if (num >= 1000)
         return (num / 1000D).ToString("0.##", CultureInfo.CurrentCulture) + "K";

      return num.ToString("#,0", CultureInfo.CurrentCulture);
   }
}
