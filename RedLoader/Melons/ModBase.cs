﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using RedLoader.InternalUtils;

#pragma warning disable 0618

namespace RedLoader
{
    public abstract class ModBase
    {
        #region Static

        /// <summary>
        /// Called once a Melon is fully registered.
        /// </summary>
        public static readonly MelonEvent<ModBase> OnMelonRegistered = new();

        /// <summary>
        /// Called when a Melon unregisters.
        /// </summary>
        public static readonly MelonEvent<ModBase> OnMelonUnregistered = new();

        /// <summary>
        /// Called before a Melon starts initializing.
        /// </summary>
        public static readonly MelonEvent<ModBase> OnMelonInitializing = new();

        public static ReadOnlyCollection<ModBase> RegisteredMelons => _registeredMelons.AsReadOnly();
        internal static List<ModBase> _registeredMelons = new();

        /// <summary>
        /// Creates a new Melon instance for a Wrapper.
        /// </summary>
        public static T CreateWrapper<T>(string name, string author, string version, MelonGameAttribute[] games = null, MelonProcessAttribute[] processes = null, int priority = 0, Color? color = null, Color? authorColor = null, string id = null) where T : ModBase, new()
        {
            var melon = new T
            {
                Info = new MelonInfoAttribute(typeof(T), name, version, author),
                MelonAssembly = MelonAssembly.LoadMelonAssembly(null, typeof(T).Assembly),
                Priority = priority,
                ConsoleColor = color ?? RLog.DefaultMelonColor,
                AuthorConsoleColor = authorColor ?? RLog.DefaultTextColor,
                //SupportedProcesses = processes,
                //Games = games,
                OptionalDependencies = null,
                ID = id
            };

            return melon;
        }

        /// <summary>
        /// Registers a List of Melons in the right order.
        /// </summary>
        public static void RegisterSorted<T>(IEnumerable<T> melons) where T : ModBase
        {
            if (melons == null)
                return;

            var collection = melons.ToList();
            SortMelons(ref collection);

            foreach (var m in melons)
                m.Register();
        }

        private static void SortMelons<T>(ref List<T> melons) where T : ModBase
        {
            DependencyGraph<T>.TopologicalSort(melons);
            melons = melons.OrderBy(x => x.Priority).ToList();
        }

        #endregion

        #region Instance

        private MelonGameAttribute[] _games = new MelonGameAttribute[0];
        private MelonProcessAttribute[] _processes = new MelonProcessAttribute[0];
        private MelonGameVersionAttribute[] _gameVersions = new MelonGameVersionAttribute[0];

        public readonly MelonEvent OnRegister = new();
        public readonly MelonEvent OnUnregister = new();

        /// <summary>
        /// MelonAssembly of the Melon.
        /// </summary>
        public MelonAssembly MelonAssembly { get; internal set; }

        /// <summary>
        /// Priority of the Melon.
        /// </summary>
        public int Priority { get; internal set; }

        /// <summary>
        /// Console Color of the Melon.
        /// </summary>
        public Color ConsoleColor { get; internal set; }

        /// <summary>
        /// Console Color of the Author that made this melon.
        /// </summary>
        public Color AuthorConsoleColor { get; internal set; }

        /// <summary>
        /// Info Attribute of the Melon.
        /// </summary>
        internal MelonInfoAttribute Info { get; set; }

        /// <summary>
        /// AdditionalCredits Attribute of the Melon
        /// </summary>
        public MelonAdditionalCreditsAttribute AdditionalCredits { get; internal set; }

        /// <summary>
        /// Game Version Attributes of the Melon.
        /// </summary>
        public string SupportedGameVersion { get; internal set; }

        /// <summary>
        /// Optional Dependencies Attribute of the Melon.
        /// </summary>
        public string[] OptionalDependencies { get; internal set; }

        /// <summary>
        /// Auto-Created Harmony Instance of the Melon.
        /// </summary>
        public HarmonyLib.Harmony HarmonyInstance { get; internal set; }

        /// <summary>
        /// Auto-Created MelonLogger Instance of the Melon.
        /// </summary>
        public RLog.Instance LoggerInstance { get; internal set; }

        /// <summary>
        /// Optional ID of the Melon.
        /// </summary>
        public string ID { get; internal set; }
        
        /// <summary>
        /// Description of the Mod.
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// <see langword="true"/> if the Melon is registered.
        /// </summary>
        public bool Registered { get; private set; }

        /// <summary>
        /// Name of the current Melon Type.
        /// </summary>
        public abstract string MelonTypeName { get; }

        #region Callbacks

        /// <summary>
        /// Runs before Support Module Initialization and after Assembly Generation for Il2Cpp Games.
        /// </summary>
        protected virtual void OnPreSupportModule() { }

        /// <summary>
        /// Runs once per frame.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Can run multiple times per frame. Mostly used for Physics.
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// Runs once per frame, after <see cref="OnUpdate"/>.
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// Can run multiple times per frame. Mostly used for Unity's IMGUI.
        /// </summary>
        protected virtual void OnGUI() { }

        /// <summary>
        /// Runs on a quit request. It is possible to abort the request in this callback.
        /// </summary>
        protected virtual void OnApplicationQuit() { }

        /// <summary>
        /// Runs when Melon Preferences get saved.
        /// </summary>
        protected virtual void OnPreferencesSaved() { }

        /// <summary>
        /// Runs when Melon Preferences get saved. Gets passed the Preferences's File Path.
        /// </summary>
        protected virtual void OnPreferencesSaved(string filepath) { }

        /// <summary>
        /// Runs when Melon Preferences get loaded.
        /// </summary>
        protected virtual void OnPreferencesLoaded() { }

        /// <summary>
        /// Runs when Melon Preferences get loaded. Gets passed the Preferences's File Path.
        /// </summary>
        protected virtual void OnPreferencesLoaded(string filepath) { }

        /// <summary>
        /// Runs when the Melon is registered. Executed before the Melon's info is printed to the console. This callback should only be used a constructor for the Melon.
        /// </summary>
        /// <remarks>
        /// Please note that this callback may run before the Support Module is loaded.
        /// <br>As a result, using unhollowed assemblies may not be possible yet and you would have to override <see cref="OnInitializeMod"/> instead.</br>
        /// </remarks>
        protected virtual void OnEarlyInitializeMelon() { }

        /// <summary>
        /// Runs after the Mod has registered. This callback waits until the loader has fully initialized (<see cref="GlobalEvents.OnApplicationStart"/>).
        /// </summary>
        protected virtual void OnInitializeMod() { }

        /// <summary>
        /// Runs after <see cref="OnInitializeMod"/>. This callback waits until Unity has invoked the first 'Start' messages (<see cref="GlobalEvents.OnApplicationLateStart"/>).
        /// </summary>
        protected virtual void OnLateInitializeMod() { }

        /// <summary>
        /// Runs when the mod is unregistered. Also runs before the Application is closed (<see cref="GlobalEvents.OnApplicationDefiniteQuit"/>).
        /// </summary>
        protected virtual void OnDeinitializeMod() { }

        #endregion

        public Incompatibility[] FindIncompatiblities(MelonGameAttribute game, string processName, string gameVersion,
            string mlVersion, string mlBuildHashCode, MelonPlatformAttribute.CompatiblePlatforms platform,
            MelonPlatformDomainAttribute.CompatibleDomains domain)
        {
            // var result = new List<Incompatibility>();
            // if (!(SupportedGameVersion.Length == 0 || SupportedGameVersion.Any(x => x == gameVersion)))
            //     result.Add(Incompatibility.GameVersion);
            //
            // if (!(SupportedDomain == null || SupportedDomain.IsCompatible(domain)))
            //     result.Add(Incompatibility.Domain);
            // if (!(SupportedMLVersion == null || SupportedMLVersion.IsCompatible(mlVersion)))
            //     result.Add(Incompatibility.MLVersion);
            //
            // else
            // {
            //     if (!(SupportedMLBuild == null || SupportedMLBuild.IsCompatible(mlBuildHashCode)))
            //         result.Add(Incompatibility.MLBuild);
            // }
            //
            // return result.ToArray();

            return Array.Empty<Incompatibility>();
        }

        public Incompatibility[] FindIncompatiblitiesFromContext()
        {
            return FindIncompatiblities(LoaderUtils.CurrentGameAttribute, Process.GetCurrentProcess().ProcessName, LoaderUtils.GameVersion, BuildInfo.Version, LoaderUtils.HashCode, LoaderUtils.CurrentPlatform, LoaderUtils.CurrentDomain);
        }

        public static void PrintIncompatibilities(Incompatibility[] incompatibilities, ModBase mod)
        {
            if (incompatibilities == null || incompatibilities.Length == 0)
                return;

            RLog.WriteLine(Color.Red);
            RLog.MsgDirect(Color.DarkRed, $"'{mod.Info.Name} v{mod.Info.Version}' is incompatible:");
            if (incompatibilities.Contains(Incompatibility.GameVersion))
            {
                RLog.MsgDirect($"- {mod.Info.Name} is only compatible with the following Game Version:");

                RLog.MsgDirect($"    - {mod.SupportedGameVersion}");
            }
            // if (incompatibilities.Contains(Incompatibility.Domain))
            // {
            //     MelonLogger.MsgDirect($"- {melon.Info.Name} is only compatible with the following Domain:");
            //     MelonLogger.MsgDirect($"    - {melon.SupportedDomain.Domain}");
            // }
            // if (incompatibilities.Contains(Incompatibility.MLVersion))
            // {
            //     MelonLogger.MsgDirect($"- {melon.Info.Name}  is only compatible with the following RedLoader Versions:");
            //     MelonLogger.MsgDirect($"    - {melon.SupportedMLVersion.SemVer}{(melon.SupportedMLVersion.IsMinimum ? " or higher" : "")}");
            // }
            // if (incompatibilities.Contains(Incompatibility.MLBuild))
            // {
            //     MelonLogger.MsgDirect($"- {melon.Info.Name} is only compatible with the following RedLoader Build Hash Codes:");
            //     MelonLogger.MsgDirect($"    - {melon.SupportedMLBuild.HashCode}");
            // }

            RLog.WriteLine(Color.Red);
            RLog.WriteSpacer();
        }

        /// <summary>
        /// Registers the Melon.
        /// </summary>
        public bool Register()
        {
            if (Registered)
                return false;

            if (FindMelon(Info.Name, Info.Author) != null)
            {
                RLog.Warning($"Failed to register {MelonTypeName} '{MelonAssembly.Location}': A Melon with the same Name and Author is already registered!");
                return false;
            }

            var comp = FindIncompatiblitiesFromContext();
            if (comp.Length != 0)
            {
                PrintIncompatibilities(comp, this);
                return false;
            }

            OnMelonInitializing.Invoke(this);

            LoggerInstance ??= new RLog.Instance(string.IsNullOrEmpty(Info.Name) ? ID : Info.Name, ConsoleColor);
            HarmonyInstance ??= new HarmonyLib.Harmony($"{MelonAssembly.Assembly.FullName}:{Info.Name}");

            Registered = true; // this has to be true before the melon can subscribe to any events
            RegisterCallbacks();

            try
            {
                OnEarlyInitializeMelon();
            }
            catch (Exception ex)
            {
                RLog.Error($"Failed to register {MelonTypeName} '{MelonAssembly.Location}': Melon failed to initialize!");
                RLog.Error(ex.ToString());
                Registered = false;
                return false;
            }

            if (!RegisterInternal())
                return false;

            _registeredMelons.Add(this);

            PrintLoadInfo();

            OnRegister.Invoke();
            OnMelonRegistered.Invoke(this);

            if (GlobalEvents.OnApplicationStart.Disposed)
                LoaderInitialized();
            else
                GlobalEvents.OnApplicationStart.Subscribe(LoaderInitialized, Priority, true);

            if (GlobalEvents.OnApplicationLateStart.Disposed)
                OnLateInitializeMod();
            else
                GlobalEvents.OnApplicationLateStart.Subscribe(OnLateInitializeMod, Priority, true);

            return true;
        }

        private void HarmonyInit()
        {
            if (!MelonAssembly.HarmonyDontPatchAll)
                HarmonyInstance.PatchAll(MelonAssembly.Assembly);
        }

        private void LoaderInitialized()
        {
            try
            {
                OnInitializeMod();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error(ex);
            }
        }

        protected private virtual bool RegisterInternal()
        {
            return true;
        }

        protected private virtual void UnregisterInternal() { }

        protected private virtual void RegisterCallbacks()
        {
            GlobalEvents.OnApplicationQuit.Subscribe(OnApplicationQuit, Priority);
            GlobalEvents.OnUpdate.Subscribe(OnUpdate, Priority);
            GlobalEvents.OnLateUpdate.Subscribe(OnLateUpdate, Priority);
            GlobalEvents.OnGUI.Subscribe(OnGUI, Priority);
            GlobalEvents.OnFixedUpdate.Subscribe(OnFixedUpdate, Priority);

            ConfigSystem.OnPreferencesLoaded.Subscribe(PrefsLoaded, Priority);
            ConfigSystem.OnPreferencesSaved.Subscribe(PrefsSaved, Priority);
        }

        private void PrefsSaved(string path)
        {
            OnPreferencesSaved(path);
            OnPreferencesSaved();
        }

        private void PrefsLoaded(string path)
        {
            OnPreferencesLoaded(path);
            OnPreferencesLoaded();
        }

        /// <summary>
        /// Tries to find a registered Melon that matches the given Info.
        /// </summary>
        public static ModBase FindMelon(string melonName, string melonAuthor)
        {
            return _registeredMelons.Find(x => x.Info.Name == melonName && x.Info.Author == melonAuthor);
        }

        /// <summary>
        /// Unregisters the Melon and all other Melons located in the same Assembly.
        /// <para>This only unsubscribes the Melons from all Callbacks/<see cref="MelonEvent"/>s and unpatches all Methods that were patched by Harmony, but doesn't actually unload the whole Assembly.</para>
        /// </summary>
        public void Unregister(string reason = null, bool silent = false)
        {
            if (!Registered)
                return;

            MelonAssembly.UnregisterMelons(reason, silent);
        }

        internal void UnregisterInstance(string reason, bool silent)
        {
            if (!Registered)
                return;

            try
            {
                OnDeinitializeMod();
            }
            catch (Exception ex)
            {
                RLog.Error($"Failed to properly unregister {MelonTypeName} '{MelonAssembly.Location}': Melon failed to deinitialize!");
                RLog.Error(ex.ToString());
            }

            UnregisterInternal();

            _registeredMelons.Remove(this);
            HarmonyInstance.UnpatchSelf();
            Registered = false;

            if (!silent)
                PrintUnloadInfo(reason);

            OnUnregister.Invoke();
            OnMelonUnregistered.Invoke(this);
        }

        private void PrintLoadInfo()
        {
            RLog.WriteLine(Color.DarkGreen);
            
            RLog.Internal_PrintModName(ConsoleColor, AuthorConsoleColor, Info.Name, Info.Author, AdditionalCredits?.Credits, Info.Version, ID);
            RLog.MsgDirect(Color.DarkGray, $"Assembly: {Path.GetFileName(MelonAssembly.Location)}");

            RLog.WriteLine(Color.DarkGreen);
        }

        private void PrintUnloadInfo(string reason)
        {
            RLog.WriteLine(Color.DarkRed);

            RLog.MsgDirect(Color.DarkGray, MelonTypeName + " deinitialized:");
            RLog.Internal_PrintModName(ConsoleColor, AuthorConsoleColor, Info.Name, Info.Author, AdditionalCredits?.Credits, Info.Version, ID);

            if (!string.IsNullOrEmpty(reason))
            {
                RLog.MsgDirect(string.Empty);
                RLog.MsgDirect($"Reason: '{reason}'");
            }

            RLog.WriteLine(Color.DarkRed);
        }

        public static void ExecuteAll(LemonAction<ModBase> func, bool unregisterOnFail = false, string unregistrationReason = null)
        {
            ExecuteList(func, _registeredMelons, unregisterOnFail, unregistrationReason);
        }

        public static void ExecuteList<T>(LemonAction<T> func, List<T> melons, bool unregisterOnFail = false, string unregistrationReason = null) where T : ModBase
        {
            var failedMelons = (unregisterOnFail ? new List<T>() : null);

            LemonEnumerator<T> enumerator = new(melons.ToArray());
            while (enumerator.MoveNext())
            {
                var melon = enumerator.Current;
                if (!melon.Registered)
                    continue;

                try { func(melon); }
                catch (Exception ex)
                {
                    melon.LoggerInstance.Error(ex.ToString());
                    if (unregisterOnFail)
                        failedMelons.Add(melon);
                }
            }

            if (unregisterOnFail)
            {
                foreach (var m in failedMelons)
                    m.Unregister(unregistrationReason);
            }
        }

        public static void SendMessageAll(string name, params object[] arguments)
        {
            LemonEnumerator<ModBase> enumerator = new(_registeredMelons.ToArray());
            while (enumerator.MoveNext())
            {
                var melon = enumerator.Current;
                if (!melon.Registered)
                    continue;

                try { melon.SendMessage(name, arguments); }
                catch (Exception ex) { melon.LoggerInstance.Error(ex.ToString()); }
            }
        }

        public object SendMessage(string name, params object[] arguments)
        {
            var msg = Info.SystemType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (msg == null)
                return null;

            return msg.Invoke(msg.IsStatic ? null : this, arguments);
        }
        #endregion

        public enum Incompatibility
        {
            MLVersion,
            MLBuild,
            Game,
            GameVersion,
            ProcessName,
            Domain,
            Platform
        }
    }
}