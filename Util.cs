using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdater
{
    public static class Util
    {
        public static int LevenshteinDistance(string s1, string s2)
        {
            int[,] dp = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                dp[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            }

            return dp[s1.Length, s2.Length];
        }

        public static bool IsApproxEqual(this string s1, string s2, int threshold = 8)
        {
            int distance = LevenshteinDistance(s1, s2);
            return distance <= threshold;
        }

        public static string FindFile(string directory, string searchName)
        {
            try
            {
                if (!Directory.Exists(directory))
                    throw new Exception($"The directory '{directory}' does not exist");

                foreach(string file in Directory.GetFiles(directory))
                {
                    if (Path.GetFileName(file).IsApproxEqual(searchName))
                        return file;
                }

                foreach(string subFolder in Directory.GetDirectories(directory))
                {
                    string result = FindFile(subFolder, searchName);

                    if (result != null)
                        return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred when attempting to find file: {ex.Message}");
            }

            return null;
        }

        public static bool DeleteAll(string directory)
        {
            try
            {
                foreach (string file in Directory.GetFileSystemEntries(directory))
                {
                    if (Directory.Exists(file))
                        Directory.Delete(file, true);
                    else if (File.Exists(file))
                        File.Delete(file);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured when attempting to delete: {ex.Message}");
                return false;
            }
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
