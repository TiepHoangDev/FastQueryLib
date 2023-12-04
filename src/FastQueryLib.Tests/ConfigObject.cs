using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FastQueryLib.Tests
{
    public class ConfigObject
    {
        public ConfigConnectionObject? Source { get; set; }
        public ConfigConnectionObject? Destination { get; set; }

        public static ConfigObject ReadFromConfig()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config.json");
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ConfigObject>(json);
            return data ?? throw new Exception("can not convert json to ConfigConnectionObject");
        }
    }

    public record ConfigConnectionObject(string ServerName, string Database)
    {
        public int SqlPort { get; set; } = 1433;
    }
}
