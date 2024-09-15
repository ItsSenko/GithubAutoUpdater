using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft;
using Newtonsoft.Json;

namespace AutoUpdater
{
    public class Settings
    {
        public string GithubAPI { get; set;  }
        public string BasePath { get; set;  }
        public string FileName { get; set;  }
        public bool AutoOpen { get; set; }
        public bool DeleteAll { get; set; }
        public int AssetIndex { get; set; }

        private static Settings _instance;

        public static Settings Instance
        {
            get 
            {
                if(_instance == null)
                    _instance = new Settings();

                return _instance; 
            }
            set { _instance = value; }
        }


        public static Settings Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Settings.json");

            if (!File.Exists(path))
            {
                Console.WriteLine("Couldnt Load Settings. Creating new...");
                return Save();
            }

            string json = File.ReadAllText(path);
            Settings settings = JsonConvert.DeserializeObject<Settings>(json);

            if (settings == null)
            {
                Console.WriteLine("Couldnt Load Settings. Creating new...");
                return Save();
            }

            Instance = settings;
            return Instance;
        }

        public static Settings Save()
        {
            var json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Settings.json"), json);
            return Instance;
        }
    }
}
