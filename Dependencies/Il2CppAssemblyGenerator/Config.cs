﻿using System.Collections.Generic;
using System.IO;
using RedLoader.Preferences;

namespace RedLoader.Il2CppAssemblyGenerator
{
    internal class Config
    {
        private static string FilePath;
        private static ReflectiveConfigCategory Category;
        internal static AssemblyGeneratorConfiguration Values;

        internal static void Initialize()
        {
            FilePath = Path.Combine(Core.BasePath, "Config.cfg");

            Category = ConfigSystem.CreateCategory<AssemblyGeneratorConfiguration>("Il2CppAssemblyGenerator");
            Category.SetFilePath(FilePath, printmsg: false);
            Category.DestroyFileWatcher();

            Values = Category.GetValue<AssemblyGeneratorConfiguration>();

            if (!File.Exists(FilePath))
                Save();
        }

        internal static void Save() => Category.SaveToFile(false);

        public class AssemblyGeneratorConfiguration
        {
            public string GameAssemblyHash = null;
            public string DeobfuscationRegex = null;
            public string UnityVersion = "0.0.0.0";
            public string DumperVersion = "0.0.0.0";
            public bool UseInterop = true;
            public List<string> OldFiles = new List<string>();
        }
    }
}