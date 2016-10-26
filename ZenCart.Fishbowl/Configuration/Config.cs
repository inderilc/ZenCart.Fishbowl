﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZenCart.Fishbowl.Configuration
{
    public class Config
    {
        public FishbowlConfig FB { get; set; }
        public StoreConfig Store { get; set; }
        public EmailConfig Email { get; set; }

        public static Config Load()
        {
            var cfg = LoadFromDisk(AppDomain.CurrentDomain.BaseDirectory + "config.json");
            Save(cfg);
            return cfg;
        }

        public static void Save(Config cfg)
        {
            SaveToDisk(AppDomain.CurrentDomain.BaseDirectory + "config.json", cfg);
        }

        public static Config LoadFromDisk(String filename)
        {
            if (File.Exists(filename))
            {
                String json = File.ReadAllText(filename);
                var cc = JsonConvert.DeserializeObject<Config>(json);
                return cc;
            }
            else
            {
                return new Config()
                {
                    FB = new FishbowlConfig(),
                    Store = new StoreConfig(),

                };
            }
        }
        public static void SaveToDisk(String filename, Config config)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Include;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(filename))
            {
                using (JsonWriter jw = new JsonTextWriter(sw))
                {
                    serializer.Serialize(sw, config);
                }
            }
        }
    }

    public class FishbowlConfig
    {
        public Int32 FBIAKey { get; set; }
        public string FBIAName { get; set; }
        public string FBIADesc { get; set; }
        public string ServerAddress { get; set; }
        public Int32 ServerPort { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Persistent { get; set; }

        public string DBPath { get; set; }
        public Int32 DBPort { get; set; }

        public string DBUser { get; set; }
        public string DBPass { get; set; }
    }

    public class StoreConfig
    {
        public string ApiKey { get; set; }
        public string StoreUrl { get; set; }
        public string UploadScriptURL { get; set; }
        public OrderSettings OrderSettings { get; set; }
    }

    public class OrderSettings
    {
        public string DefaultCarrier { get; set; }
        public Dictionary<String, String> CarrierSearchNames { get; set; }

    }

    public class EmailConfig
    {
        public String Host { get; set; }
        public int Port { get; set; }
        public String Pass { get; set; }
        public String User { get; set; }
        public Boolean SSL { get; set; }
        public String MailFromName { get; set; }
        public String MailFrom { get; set; }
        public string LogEmail { get; set; }
    }
}

