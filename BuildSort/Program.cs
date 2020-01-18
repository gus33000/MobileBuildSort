using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BuildSort
{
    class Program
    {
        public class MobileBuildDetails
        {
            public string BuildNumber;
            public string WindowsPhoneBuild;
        }

        static void Main(string[] args)
        {
            MainCBS(args);
        }

        static void MainCBS(string[] args)
        {
            string directoryToScan = @"02";
            string outputDirectory = @"02\out";
            var productionCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mainos.production.cbs_*", SearchOption.AllDirectories);

            Console.WriteLine("Gathering all possible NT build numbers.");

            List<string> NTBuilds = new List<string>();

            var efiespCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.efiesp.cbs_*", SearchOption.AllDirectories);
            var mainosCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.mainos.cbs_*", SearchOption.AllDirectories);
            var updateosCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.updateos.cbs_*", SearchOption.AllDirectories);

            foreach (var cabinet in efiespCabinets)
            {
                var buildstr = GetBuildNumberFromEFIESPCbs(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            foreach (var cabinet in mainosCabinets)
            {
                var buildstr = GetBuildNumberFromMainOSCbs(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            foreach (var cabinet in updateosCabinets)
            {
                var buildstr = GetBuildNumberFromMainOSCbs(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            Console.WriteLine("Possible NT builds:");

            foreach (var build in NTBuilds)
            {
                Console.WriteLine(build);
            }

            Console.WriteLine("Gathering information from production cabinets.");

            List<MobileBuildDetails> mobilebuilds = new List<MobileBuildDetails>();

            foreach (var productionCabinet in productionCabinets)
            {
                string version = GetVersionInfoCbs(productionCabinet);
                var buildInfo = GetBuildInfoFromMainOSProdCbs(productionCabinet);

                string wp_version = version.Split('.')[2] + "." + version.Split('.')[3];
                string os_version = "";

                if (ulong.Parse(version.Split('.')[2]) >= 14251)
                {
                    os_version = "10.0." + version.Split('.')[2] + "." + version.Split('.')[3] + " (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")";
                }
                else
                {
                    if (!string.IsNullOrEmpty(buildInfo.Ntrazzlebuildnumber))
                        os_version = buildInfo.Ntrazzlemajorversion + "." + buildInfo.Ntrazzleminorversion + "." + buildInfo.Ntrazzlebuildnumber + "." + buildInfo.Ntrazzlerevisionnumber + " (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")";

                    if (string.IsNullOrEmpty(os_version))
                    {
                        Console.WriteLine("Unable to detected OS version from BuildInfo. Checking additional files.");

                        bool match = NTBuilds.Any(x => x.Contains(" (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")"));
                        if (match)
                        {
                            Console.WriteLine("Found a matching build.");
                            os_version = NTBuilds.First(x => x.Contains(" (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")"));
                        }
                    }
                }

                Console.WriteLine("Detected WP version: " + wp_version);
                Console.WriteLine("Detected OS version: " + os_version);

                if (string.IsNullOrEmpty(os_version))
                {
                    Console.WriteLine("Unable to proceed with this build. Skipping.");
                }
                else
                {
                    mobilebuilds.Add(new MobileBuildDetails() { BuildNumber = os_version, WindowsPhoneBuild = wp_version });
                }
            }

            Console.WriteLine("Moving cabinets.");

            var cabinets = Directory.EnumerateFiles(directoryToScan, "*.cbs_*", SearchOption.AllDirectories);

            foreach (var cabinet in cabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var version = GetVersionInfoCbs(cabinet);
                var versionmin = version.Split('.')[2] + "." + version.Split('.')[3];

                bool match = mobilebuilds.Any(x => x.WindowsPhoneBuild == versionmin);
                if (match)
                {
                    string NTBuild = mobilebuilds.First(x => x.WindowsPhoneBuild == versionmin).BuildNumber;

                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                    if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                    {
                        Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                    }

                    Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                    File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
                }
            }

            foreach (var cabinet in efiespCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromEFIESPCbs(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }

            foreach (var cabinet in mainosCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromMainOSCbs(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }

            foreach (var cabinet in updateosCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromMainOSCbs(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }
        }

        static void MainSPKG(string[] args)
        {
            string directoryToScan = @"02";
            string outputDirectory = @"02\out";
            var productionCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mainos.production.spkg*", SearchOption.AllDirectories);

            Console.WriteLine("Gathering all possible NT build numbers.");

            List<string> NTBuilds = new List<string>();

            var efiespCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.efiesp.spkg*", SearchOption.AllDirectories);
            var mainosCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.mainos.spkg*", SearchOption.AllDirectories);
            var updateosCabinets = Directory.EnumerateFiles(directoryToScan, "microsoft.mobilecore.*.updateos.spkg*", SearchOption.AllDirectories);

            foreach (var cabinet in efiespCabinets)
            {
                var buildstr = GetBuildNumberFromEFIESPSpkg(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            foreach (var cabinet in mainosCabinets)
            {
                var buildstr = GetBuildNumberFromMainOSSpkg(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            foreach (var cabinet in updateosCabinets)
            {
                var buildstr = GetBuildNumberFromMainOSSpkg(cabinet);
                if (!string.IsNullOrEmpty(buildstr) && !NTBuilds.Any(x => x == buildstr))
                {
                    NTBuilds.Add(buildstr);
                }
            }

            Console.WriteLine("Possible NT builds:");

            foreach (var build in NTBuilds)
            {
                Console.WriteLine(build);
            }

            Console.WriteLine("Gathering information from production cabinets.");

            List<MobileBuildDetails> mobilebuilds = new List<MobileBuildDetails>();

            foreach (var productionCabinet in productionCabinets)
            {
                var version = GetVersionInfoSpkg(productionCabinet);
                var buildInfo = GetBuildInfoFromMainOSProdSpkg(productionCabinet);

                string wp_version = version.QFE + "." + version.Build;
                string os_version = "";

                if (ulong.Parse(version.QFE) >= 14251)
                {
                    os_version = "10.0." + version.QFE + "." + version.Build + " (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")";
                }
                else
                {
                    if (!string.IsNullOrEmpty(buildInfo.Ntrazzlebuildnumber))
                        os_version = buildInfo.Ntrazzlemajorversion + "." + buildInfo.Ntrazzleminorversion + "." + buildInfo.Ntrazzlebuildnumber + "." + buildInfo.Ntrazzlerevisionnumber + " (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")";

                    if (string.IsNullOrEmpty(os_version))
                    {
                        Console.WriteLine("Unable to detected OS version from BuildInfo. Checking additional files.");

                        bool match = NTBuilds.Any(x => x.Contains(" (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")"));
                        if (match)
                        {
                            Console.WriteLine("Found a matching build.");
                            os_version = NTBuilds.First(x => x.Contains(" (" + buildInfo.Releaselabel.ToLower() + "." + string.Join("", buildInfo.Buildtime.Skip(2)) + ")"));
                        }
                    }
                }

                Console.WriteLine("Detected WP version: " + wp_version);
                Console.WriteLine("Detected OS version: " + os_version);

                if (string.IsNullOrEmpty(os_version))
                {
                    Console.WriteLine("Unable to proceed with this build. Skipping.");
                }
                else
                {
                    mobilebuilds.Add(new MobileBuildDetails() { BuildNumber = os_version, WindowsPhoneBuild = wp_version });
                }
            }

            Console.WriteLine("Moving cabinets.");

            var cabinets = Directory.EnumerateFiles(directoryToScan, "*.spkg*", SearchOption.AllDirectories);

            foreach (var cabinet in cabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var version = GetVersionInfoSpkg(cabinet);
                var versionmin = version.QFE + "." + version.Build;

                bool match = mobilebuilds.Any(x => x.WindowsPhoneBuild == versionmin);
                if (match)
                {
                    string NTBuild = mobilebuilds.First(x => x.WindowsPhoneBuild == versionmin).BuildNumber;

                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                    if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                    {
                        Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                    }

                    Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                    File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
                }
            }

            foreach (var cabinet in efiespCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromEFIESPSpkg(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }

            foreach (var cabinet in mainosCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromMainOSSpkg(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }

            foreach (var cabinet in updateosCabinets)
            {
                if (cabinet.Contains(outputDirectory))
                    continue;

                var NTBuild = GetBuildNumberFromMainOSSpkg(cabinet);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                if (!Directory.Exists(outputDirectory + "\\" + NTBuild))
                {
                    Directory.CreateDirectory(outputDirectory + "\\" + NTBuild);
                }

                Console.WriteLine("Moving " + cabinet.Split('\\').Last() + " -> " + NTBuild);
                File.Move(cabinet, outputDirectory + "\\" + NTBuild + "\\" + cabinet.Split('\\').Last());
            }
        }

        static string GetBuildNumberFromEFIESPSpkg(string filepath)
        {
            if (File.Exists("man.dsm.xml"))
            {
                File.Delete("man.dsm.xml");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" man.dsm.xml") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("man.dsm.xml"))
            {
                string buildstr = null;

                Stream stream = File.OpenRead("man.dsm.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlDsm.Package));
                XmlDsm.Package package = (XmlDsm.Package)serializer.Deserialize(stream);

                if (package.Files.FileEntry.Any(x => x.DevicePath.Contains(@"\bootmgr.efi")))
                {
                    var entry = package.Files.FileEntry.First(x => x.DevicePath.Contains(@"\bootmgr.efi"));

                    if (File.Exists(entry.CabPath))
                    {
                        File.Delete(entry.CabPath);
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.CabPath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    buildstr = FileVersionInfo.GetVersionInfo(entry.CabPath).FileVersion;

                    File.Delete(entry.CabPath);
                }

                stream.Close();

                File.Delete("man.dsm.xml");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static string GetBuildNumberFromEFIESPCbs(string filepath)
        {
            if (File.Exists("update.mum"))
            {
                File.Delete("update.mum");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" update.mum") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("update.mum"))
            {
                string buildstr = null;

                Stream stream = File.OpenRead("update.mum");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlMum.Assembly));
                XmlMum.Assembly package = (XmlMum.Assembly)serializer.Deserialize(stream);

                if (package.Package.CustomInformation.File.Any(x => x.Name.Contains(@"\bootmgr.efi")))
                {
                    var entry = package.Package.CustomInformation.File.First(x => x.Name.Contains(@"\bootmgr.efi"));

                    if (File.Exists(entry.Cabpath))
                    {
                        File.Delete(entry.Cabpath);
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.Cabpath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    buildstr = FileVersionInfo.GetVersionInfo(entry.Cabpath).FileVersion;

                    File.Delete(entry.Cabpath);
                }

                stream.Close();

                File.Delete("update.mum");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static string GetBuildNumberFromMainOSSpkg(string filepath)
        {
            if (File.Exists("man.dsm.xml"))
            {
                File.Delete("man.dsm.xml");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" man.dsm.xml") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("man.dsm.xml"))
            {
                string buildstr = null;

                Stream stream = File.OpenRead("man.dsm.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlDsm.Package));
                XmlDsm.Package package = (XmlDsm.Package)serializer.Deserialize(stream);

                if (package.Files.FileEntry.Any(x => x.DevicePath.Contains(@"\ntoskrnl.exe")))
                {
                    var entry = package.Files.FileEntry.First(x => x.DevicePath.Contains(@"\ntoskrnl.exe"));

                    if (File.Exists(entry.CabPath))
                    {

                        try
                        {
                            File.Delete(entry.CabPath);
                        }
                        catch
                        {
                            Task.Delay(1000).Wait();
                            File.Delete(entry.CabPath);
                        }
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.CabPath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    buildstr = FileVersionInfo.GetVersionInfo(entry.CabPath).FileVersion;

                    try
                    {
                        File.Delete(entry.CabPath);
                    }
                    catch
                    {

                    }
                }

                stream.Close();

                File.Delete("man.dsm.xml");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static string GetBuildNumberFromMainOSCbs(string filepath)
        {
            if (File.Exists("update.mum"))
            {
                File.Delete("update.mum");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" update.mum") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("update.mum"))
            {
                string buildstr = null;

                Stream stream = File.OpenRead("update.mum");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlMum.Assembly));
                XmlMum.Assembly package = (XmlMum.Assembly)serializer.Deserialize(stream);

                if (package.Package.CustomInformation.File.Any(x => x.Name.Contains(@"\ntoskrnl.exe")))
                {
                    var entry = package.Package.CustomInformation.File.First(x => x.Name.Contains(@"\ntoskrnl.exe"));

                    if (File.Exists(entry.Cabpath))
                    {
                        File.Delete(entry.Cabpath);
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.Cabpath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    buildstr = FileVersionInfo.GetVersionInfo(entry.Cabpath).FileVersion;

                    File.Delete(entry.Cabpath);
                }

                stream.Close();

                File.Delete("update.mum");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static BuildInfo.Buildinformation GetBuildInfoFromMainOSProdSpkg(string filepath)
        {
            if (File.Exists("man.dsm.xml"))
            {
                File.Delete("man.dsm.xml");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" man.dsm.xml") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("man.dsm.xml"))
            {
                BuildInfo.Buildinformation buildstr = null;

                Stream stream = File.OpenRead("man.dsm.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlDsm.Package));
                XmlDsm.Package package = (XmlDsm.Package)serializer.Deserialize(stream);

                if (package.Files.FileEntry.Any(x => x.DevicePath.Contains(@"\buildinfo.xml")))
                {
                    var entry = package.Files.FileEntry.First(x => x.DevicePath.Contains(@"\buildinfo.xml"));

                    if (File.Exists(entry.CabPath))
                    {
                        try
                        {
                            File.Delete(entry.CabPath);
                        }
                        catch
                        {
                            File.Move(entry.CabPath, Path.GetTempFileName());
                            //File.Delete(entry.CabPath);
                        }
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.CabPath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    Stream stream2 = File.OpenRead(entry.CabPath);

                    XmlSerializer serializer2 = new XmlSerializer(typeof(BuildInfo.Buildinformation));
                    BuildInfo.Buildinformation package2 = (BuildInfo.Buildinformation)serializer2.Deserialize(stream2);

                    buildstr = package2;

                    stream2.Close();

                    try
                    {
                        File.Delete(entry.CabPath);
                    }
                    catch
                    {

                    }
                }

                stream.Close();

                File.Delete("man.dsm.xml");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static BuildInfo.Buildinformation GetBuildInfoFromMainOSProdCbs(string filepath)
        {
            if (File.Exists("update.mum"))
            {
                File.Delete("update.mum");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" update.mum") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("update.mum"))
            {
                BuildInfo.Buildinformation buildstr = null;

                Stream stream = File.OpenRead("update.mum");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlMum.Assembly));
                XmlMum.Assembly package = (XmlMum.Assembly)serializer.Deserialize(stream);

                if (package.Package.CustomInformation.File.Any(x => x.Name.Contains(@"\buildinfo.xml")))
                {
                    var entry = package.Package.CustomInformation.File.First(x => x.Name.Contains(@"\buildinfo.xml"));

                    if (File.Exists(entry.Cabpath))
                    {
                        File.Delete(entry.Cabpath);
                    }

                    proc = new Process();
                    proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" " + entry.Cabpath) { WindowStyle = ProcessWindowStyle.Hidden };
                    proc.Start();
                    proc.WaitForExit();

                    Stream stream2 = File.OpenRead(entry.Cabpath);

                    XmlSerializer serializer2 = new XmlSerializer(typeof(BuildInfo.Buildinformation));
                    BuildInfo.Buildinformation package2 = (BuildInfo.Buildinformation)serializer2.Deserialize(stream2);

                    buildstr = package2;

                    stream2.Close();

                    try
                    {
                        File.Delete(entry.Cabpath);
                    }
                    catch
                    {

                    }
                }

                stream.Close();

                File.Delete("update.mum");

                return buildstr;
            }
            else
            {
                return null;
            }
        }

        static XmlDsm.Version GetVersionInfoSpkg(string filepath)
        {
            if (File.Exists("man.dsm.xml"))
            {
                File.Delete("man.dsm.xml");
            }
            
            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" man.dsm.xml") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("man.dsm.xml"))
            {
                Stream stream = File.OpenRead("man.dsm.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlDsm.Package));
                XmlDsm.Package package = (XmlDsm.Package)serializer.Deserialize(stream);

                XmlDsm.Version ver = package.Identity.Version;

                stream.Close();

                File.Delete("man.dsm.xml");

                return ver;
            }
            else
            {
                return null;
            }
        }

        static string GetVersionInfoCbs(string filepath)
        {
            if (File.Exists("update.mum"))
            {
                File.Delete("update.mum");
            }

            var proc = new Process();
            proc.StartInfo = new ProcessStartInfo("7za.exe", "x \"" + filepath + "\" update.mum") { WindowStyle = ProcessWindowStyle.Hidden };
            proc.Start();
            proc.WaitForExit();

            if (File.Exists("update.mum"))
            {
                Stream stream = File.OpenRead("update.mum");

                XmlSerializer serializer = new XmlSerializer(typeof(XmlMum.Assembly));
                XmlMum.Assembly package = (XmlMum.Assembly)serializer.Deserialize(stream);

                string ver = package.AssemblyIdentity.Version;

                stream.Close();

                File.Delete("update.mum");

                return ver;
            }
            else
            {
                return null;
            }
        }
    }
}
