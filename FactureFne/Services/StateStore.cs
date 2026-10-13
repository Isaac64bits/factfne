using System;
using System.IO;
using Newtonsoft.Json;
using FactureFne.Models;

namespace FactureFne.Services
{
    public static class StateStore
    {
        private static readonly string Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FactureFne");
        private static readonly string FilePath = Path.Combine(Root, "state.json");

        public static AppState Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new AppState();
                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<AppState>(json) ?? new AppState();
            }
            catch { return new AppState(); }
        }

        public static void Save(AppState state)
        {
            Directory.CreateDirectory(Root);
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
    }
}
