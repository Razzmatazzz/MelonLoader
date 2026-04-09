using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Semver;

namespace MelonLoader.Il2CppAssemblyGenerator.Packages
{
    internal class Cpp2IL : Models.ExecutablePackage
    {
        internal static SemVersion NetCoreMinVersion = SemVersion.Parse("2022.1.0-pre-release.18");
        internal SemVersion VersionSem;
        private string BaseFolder;

        private static string ReleaseName =>
            MelonUtils.IsWindows ? "Windows" : MelonUtils.IsUnix ? "Linux" : "OSX";
		
        internal Cpp2IL()
        {
            Version = LoaderConfig.Current.UnityEngine.ForceIl2CppDumperVersion;
#if !DEBUG
            if (string.IsNullOrEmpty(Version) || Version.Equals("0.0.0.0"))
                Version = RemoteAPI.Info.ForceDumperVersion;
#endif
            if (string.IsNullOrEmpty(Version) || Version.Equals("0.0.0.0"))
                Version = $"2022.1.0-pre-release.20";
            VersionSem = SemVersion.Parse(Version);

            Name = nameof(Cpp2IL);
            
            var filename = Name;
#if WINDOWS
            filename += ".exe";
#endif

            BaseFolder = Path.Combine(Core.BasePath, Name);
            Directory.CreateDirectory(BaseFolder);

            FilePath =
                ExeFilePath =
                Destination =
                Path.Combine(BaseFolder, filename);

            OutputFolder = Path.Combine(BaseFolder, "cpp2il_out");

            URL = $"https://github.com/SamboyCoding/{Name}/releases/download/{Version}/{Name}-{Version}-{ReleaseName}";
#if WINDOWS
            URL += ".exe";
#endif
        }

        internal override bool ShouldSetup()
        {
            if (!File.Exists(ExeFilePath))
                return true;

            return string.IsNullOrEmpty(Config.Values.DumperVersion)
                || !Config.Values.DumperVersion.Equals(Version);
        }

        internal override void Cleanup() { }

        internal override void Save()
            => Save(ref Config.Values.DumperVersion);

#if OSX
        internal override bool OnProcess()
        {
            bool processed = base.OnProcess();
            if (!processed)
                return false;

            var process = Process.Start("chmod", $"+x \"{FilePath}\"");
            process?.WaitForExit();
            if (process?.ExitCode != 0)
            {
                Core.Logger.Error($"Failed to set the needed permissions on Cpp2IL, please run the following command " +
                                  $"and try again: chmod +x {FilePath}");
                return false;
            }
            return true;
        }
#endif

        internal override bool Execute()
        {
            List<string> Arguments =  [MelonDebug.IsEnabled() ? "--verbose" : string.Empty];

            bool UseCustomMetadataPath = LoaderConfig.Current.UnityEngine.ForceBinaryPath != string.Empty && LoaderConfig.Current.UnityEngine.ForceMetadataPath != string.Empty && LoaderConfig.Current.UnityEngine.ForceUnityVersion != string.Empty;

            if (!UseCustomMetadataPath)
            {
                Arguments.Add("--game-path");
#if OSX
                Arguments.Add("\"" + MelonUtils.GetPathAncestor(Core.GameAssemblyPath, 3) + "\"");
#else
                Arguments.Add("\"" + Path.GetDirectoryName(Core.GameAssemblyPath) + "\"");
#endif
            }
            else
            {
                Arguments.Add("--force-binary-path");
                Arguments.Add("\"" + Path.GetDirectoryName(Core.GameAssemblyPath) + Path.DirectorySeparatorChar + LoaderConfig.Current.UnityEngine.ForceBinaryPath + "\"");
                Arguments.Add("--force-metadata-path");
                Arguments.Add("\"" + Path.GetDirectoryName(Core.GameAssemblyPath) + Path.DirectorySeparatorChar + LoaderConfig.Current.UnityEngine.ForceMetadataPath + "\"");
                Arguments.Add("--force-unity-version");
                Arguments.Add("\"" + LoaderConfig.Current.UnityEngine.ForceUnityVersion + "\"");
            }

            Arguments.Add("--exe-name");
            Arguments.Add("\"" + Process.GetCurrentProcess().ProcessName + "\"");

            Arguments.Add("--output-as");
            Arguments.Add("dummydll");

            Arguments.Add("--use-processor");
            Arguments.Add("attributeanalyzer");
            Arguments.Add("attributeinjector");
            Arguments.Add(LoaderConfig.Current.UnityEngine.EnableCpp2ILCallAnalyzer ? "callanalyzer" : string.Empty);
            Arguments.Add(LoaderConfig.Current.UnityEngine.EnableCpp2ILNativeMethodDetector ? "nativemethoddetector" : string.Empty);
            //Arguments.Add("deobfmap");
            //Arguments.Add("stablenamer");

            return Execute(Arguments.ToArray(), false, new Dictionary<string, string>() {
                {"NO_COLOR", "1"},
            });
        }
    }
}
