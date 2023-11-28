using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Folder_Icon_Creator
{
    sealed class Options
    {
        #region Standard Option Attribute
        [OptionArray('f', "folders", Required = true, HelpText = "Folders to process")]
        public string[] InputFolders { get; set; }

        [Option('p', "poster", HelpText = "Filename for the Image file that will be used to create the Folder Icon", DefaultValue = "folder.jpg")]
        public string PosterFileNameWithExtension { get; set; }

        [Option('d', "delete", HelpText = "Deletes folders that don't have any media files or subfolders")]
        public bool DeleteEmptyFolder { get; set; }

        [Option('c', "cleanup", HelpText = "Deletes the metadata files that don't have a related video file")]
        public bool DeleteNonRelatedMetadata { get; set; }

        [Option('h', "hide", HelpText = "Hides every file that isn't a video file (or subtitle)")]
        public bool HideMetadata { get; set; }

        [Option('l', "log", HelpText = "Creates a log on the Desktop")]
        public bool Log { get; set; }

        [Option('r', "force", HelpText = "Recreates icons from folders that already have a folder.ico")]
        public bool ForceIfAlreadyExists { get; set; }
        #endregion

        #region Help Screen
        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()),
                //Copyright = new CopyrightInfo("<<app author>>", 2014),
                AdditionalNewLineAfterOption = false,
                AddDashesToOption = true,
                MaximumDisplayWidth = 300,
            };
            //help.AddPreOptionsLine("<<license details here.>>");
            help.AddPreOptionsLine(" ");
            help.AddPreOptionsLine("You can modify the list of extensions that are flagged as media files and other filetypes you don't want to hide in the COnfig.ini File");
            help.AddPreOptionsLine("Usage: Folder Icon Creator.exe [-d] [-r] [-c] [-h] [-l] [-p poster.jpg] -f Folder1 [Folder2] [...]");
            help.AddPreOptionsLine("Usage: Folder Icon Creator.exe [-ch] -f Folder1 [Folder2] [...]");

            help.AddOptions(this);

            //HelpText help = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            return help;
        }
        #endregion
    }

    class Program
    {
        static List<string> FolderJPGtoProcess;
        static List<string> FilesToHide;
        static List<string> FilesToDelete;
        static List<string> FolderToDelete;

        static public string PosterFileName = "folder";
        static public string PosterExtension = "jpg";
        static public string PosterFileNameWithExtension
        {
            get { return PosterFileName + "." + PosterExtension; }
            set
            {
                if (!string.IsNullOrEmpty(value.Trim()))
                {
                    string[] filename = value.Trim().Split('.');
                    if (filename.Length == 2)
                    {
                        PosterFileName = filename[0];
                        PosterExtension = filename[1];
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            var options = new Options();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Console.ReadLine();
                Environment.Exit(1);
            }

            DoCoreTask(options);

            Environment.Exit(0);
        }

        private static void DoCoreTask(Options options)
        {
            FilesToHide = new List<string>();
            FolderJPGtoProcess = new List<string>();
            FolderToDelete = new List<string>();
            FilesToDelete = new List<string>();

            //Change the Default folder.jpg Poster FileName to anything that is set has an options
            if (!string.IsNullOrEmpty(options.PosterFileNameWithExtension.Trim()) && options.PosterFileNameWithExtension.Trim() != "folder.jpg")
                PosterFileNameWithExtension = options.PosterFileNameWithExtension.ToLowerInvariant();

            if (options.InputFolders.Count() == 0)
                Logger.Out("No Options Selected, see help\n\n\n" + options.GetUsage());//Will never be it

            Logger.Out("Enumerating Folders");
            List<string> folderList = new List<string>();

            foreach (var item in options.InputFolders)
            {
                folderList.AddRange(Directory.EnumerateDirectories(item, "*.*", System.IO.SearchOption.AllDirectories).ToList());
                folderList.Add(item);//Include the Actual Folder To The list to be checked
            }

            CreateIconFromFolders(folderList, options);

            if (options.HideMetadata)
                HideMetadataFiles();

            if (FolderJPGtoProcess.Count > 0)
            {
                Logger.Out(string.Empty);
                Logger.Out("Refresing Icons");
                RefreshIconCache();
            }

            if (options.Log)
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "IconCreatorLog.txt"), Logger.LogString.ToString());
        }

        private static void CreateIconFromFolders(List<string> folderList, Options options)
        {
            var list = folderList.OrderByDescending(x => x.Length).ToList();//So Deeper Path are Processed before, so we can check if the folder has already been Procced for deletion
            list.ForEach(x => CheckForValidFolder(x, options));

            foreach (var item in FolderJPGtoProcess)
            {
                Logger.Out("Found : {0}", Path.GetDirectoryName(item));
            }

            if (options.DeleteNonRelatedMetadata)
                DeleteFilesWithoutARelatedVideoFile();

            if (options.DeleteEmptyFolder)
                DeleteEmptyFolder();

            Logger.Out(string.Empty);
            if (FolderJPGtoProcess.Count > 0)
            {
                ProcessFolderJPGs();
            }
            else
                Logger.Out("No Suitable Folders Found");

        }

        private static bool CheckForValidFolder(string folder, Options option)
        {
            var filesList = CheckForEmptyFolders(folder);

            var filesToDelete = FilterFilesToDelete(filesList);
            FilesToDelete.AddRange(filesToDelete);

            var file = filesList.Where(x => x.ToLowerInvariant().EndsWith(PosterFileNameWithExtension)).FirstOrDefault();
            var icoFile = filesList.Where(x => x.ToLowerInvariant().EndsWith("folder.ico")).FirstOrDefault();

            var toHide = FilterToHide(filesList);
            FilesToHide.AddRange(toHide);
            bool Force = option.ForceIfAlreadyExists == true ? true : string.IsNullOrEmpty(icoFile);//This so only The Folder that don't already have a ico file get processed

            if (!string.IsNullOrEmpty(file) && Force)
            {
                FolderJPGtoProcess.Add(file);
                return true;
            }

            return false;
        }

        private static IEnumerable<string> CheckForEmptyFolders(string folder)
        {
            var filesList = Directory.EnumerateFiles(folder);
            var folderList = Directory.EnumerateDirectories(folder, "*.*", System.IO.SearchOption.AllDirectories);

            int subFoldersToDelete = 0;
            foreach (var item in folderList)
            {
                if (FolderToDelete.Contains(item))
                    subFoldersToDelete++;
            }

            if (folderList.Count() == 0//doesn't have any subFolder
                || folderList.Count() == subFoldersToDelete)//Allthe subFolders have already been requested to be deleted
            {
                if (FilterFolderToDelete(filesList) & !FolderToDelete.Contains(folder))
                {
                    FolderToDelete.Add(folder);
                }
            }

            return filesList;
        }

        private static void ProcessFolderJPGs()
        {
            Logger.Out(string.Empty);
            if (FolderJPGtoProcess.Count > 0)
                Logger.Out("Processing Folders :");

            foreach (var item in FolderJPGtoProcess)
            {
                if (File.Exists(item))
                {
                    string ini = item.Replace(PosterFileNameWithExtension, "desktop.ini");

                    string ico = ProcessFolderJPGs(item);
                    string infoTip = Directory.GetParent(item).Name;

                    CreateDesktopIniFile(ini, ico, infoTip);//THis needs to be present when SetFolderIcon runs, but we will repace it after
                    SetIniFileAttributes(ini);
                    SetFolderAttributesToReadOnly(Path.GetDirectoryName(item));
                    SetFolderIcon(Directory.GetParent(item).ToString());

                    FilesToHide.Add(ico);
                }
            }
        }

        private static string ProcessFolderJPGs(string path)
        {
            var icoFile = path.Replace(PosterFileNameWithExtension, "folder.ico");

            Logger.Out(string.Empty);
            Logger.Out("Processing File : {0}", path);

            using (MemoryStream memStream = new MemoryStream())
            {
                using (MagickImage image = new MagickImage(path))
                {
                    image.Format = MagickFormat.Png32;
                    MagickGeometry geo = new MagickGeometry(256);
                    geo.IgnoreAspectRatio = false;
                    image.BackgroundColor = MagickColors.None;
                    image.Resize(geo);
                    image.Extent(geo, Gravity.Center);
                    image.Write(memStream, MagickFormat.Png32);
                    //string pngFile = path.Replace(".jpg", ".png"); if (File.Exists(pngFile)) File.Delete(pngFile); image.Write(pngFile);//Save PNG File to the folder
                }
                memStream.Seek(0, SeekOrigin.Begin);

                using (MagickImage image = new MagickImage(memStream))
                {
                    Logger.Out("Creating Icon : {0}", icoFile);
                    //image.Settings.SetDefine(MagickFormat.Icon, "auto-resize", string.Empty);
                    image.Settings.SetDefine(MagickFormat.Icon, "auto-resize", "256,192,128,96,64,48,32,16");

                    if (File.Exists(icoFile))
                        File.Delete(icoFile);

                    image.Write(icoFile);
                    return icoFile;
                }
            }
        }

        private static IEnumerable<string> FilterToHide(IEnumerable<string> filesList)
        {
            List<FileHideInfo> filesHideList = FileHideInfo.GetFilesList(filesList);
            return filesHideList.Where(x => x.Hidden == true).Select(x => x.File);
        }

        private static IEnumerable<string> FilterFilesToDelete(IEnumerable<string> filesList)
        {
            List<FileHideInfo> filesHideList = FileHideInfo.GetFilesList(filesList);
            return filesHideList.Where(x => x.DeleteFile == true).Select(x => x.File);
        }

        private static bool FilterFolderToDelete(IEnumerable<string> filesList)
        {
            List<FileHideInfo> filesDeleteList = FileHideInfo.GetFilesList(filesList);
            var DeleteCandidateList = filesDeleteList.Where(x => x.DeleteFolder == false);
            if (DeleteCandidateList.Count() > 0)//Found a Video File so we don't want to delete the Folder that returned this filesList
                return false;

            return true;//Nothing that we want to keep found so delete
        }

        private static void CreateDesktopIniFile(string iniFilePath, string iconFilePath, string infoTip = "")
        {
            Logger.Out("Creating desktop.ini File");

            // determine whether the icon file exists
            if (!File.Exists(iconFilePath))
                return;

            if (File.Exists(iniFilePath))
                File.Delete(iniFilePath);

            // Write .ini settings to the desktop.ini file+
            IniWriter.WriteValue(".ShellClassInfo", "IconResource", "folder.ico,0", iniFilePath);
            IniWriter.WriteValue(".ShellClassInfo", "IconFile", "folder.ico", iniFilePath);
            IniWriter.WriteValue(".ShellClassInfo", "IconIndex", "0", iniFilePath);
            if (!string.IsNullOrEmpty(infoTip)) IniWriter.WriteValue(".ShellClassInfo", "InfoTip", infoTip, iniFilePath);
            //IniWriter.WriteValue(".ShellClassInfo", "FolderType", "Videos", iniFilePath);
            IniWriter.WriteValue("ViewState", "FolderType", "Videos", iniFilePath);
            //IniWriter.WriteValue("ViewState", "Mode", "5", iniFilePath);
            //IniWriter.WriteValue("ViewState", "Vid", "{8BEBB290-52D0-11D0-B7F4-00C04FD706EC}", iniFilePath);
            //IniWriter.WriteValue("ViewState", "Logo", "Folder.ico", iniFilePath);
            //IniWriter.WriteValue("ExtShellFolderViews", "Default", "{8BEBB290-52D0-11d0-B7F4-00C04FD706EC}", iniFilePath);

        }

        private static void SetIniFileAttributes(string ini)
        {
            Logger.Out("Setting desktop.ini Attributes");

            // Set ini file attribute to "Hidden"
            if ((File.GetAttributes(ini) & FileAttributes.Hidden) != FileAttributes.Hidden)
                File.SetAttributes(ini, File.GetAttributes(ini) | FileAttributes.Hidden);

            ////Set ini file attribute to "Archive"
            //if ((File.GetAttributes(ini) & FileAttributes.Archive) != FileAttributes.Archive)
            //    File.SetAttributes(ini, File.GetAttributes(ini) | FileAttributes.Archive);

            // Set ini file attribute to "System"
            if ((File.GetAttributes(ini) & FileAttributes.System) != FileAttributes.System)
                File.SetAttributes(ini, File.GetAttributes(ini) | FileAttributes.System);
        }

        private static void SetFolderAttributesToReadOnly(string v)
        {
            Logger.Out("Setting Folder Attributes");

            // Set folder attribute to "ReadOnly"
            if ((File.GetAttributes(v) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                File.SetAttributes(v, File.GetAttributes(v) | FileAttributes.ReadOnly);
        }

        private static void SetFolderAttributesToNormal(string v)
        {
            var test = File.GetAttributes(v);
            // Set folder attribute to "Normal"
            if ((File.GetAttributes(v) & FileAttributes.Normal) != FileAttributes.Normal)
            {
                Logger.Out("Fixing Folder Attributes To Delete");
                File.SetAttributes(v, FileAttributes.Normal);
            }
        }

        private static void DeleteFilesWithoutARelatedVideoFile()
        {
            Logger.Out(string.Empty);
            if (FilesToDelete.Count > 0)
                Logger.Out("Deleting Unneeded Metadata Files : ");
            else
                Logger.Out("No Unneeded Metadata Files Found");


            foreach (var item in FilesToDelete)
            {
                if (File.Exists(item))
                {
                    Logger.Out("Deleting File: {0}", item);
                    try
                    {
                        //File.Delete(item);
                        MoveToRecycleBin(item);
                    }
                    catch (Exception e)
                    {
                        Logger.Out("Can't Delete File : {0}", item);
                        Logger.Out("Error : {0}", e.Message);
                    }
                }
            }
        }

        private static void DeleteEmptyFolder()
        {
            Logger.Out(string.Empty);
            if (FolderToDelete.Count > 0)
                Logger.Out("Deleting Folders Without a Video File : ");
            else
                Logger.Out("No Empty Folder Found To Delete");

            foreach (var item in FolderToDelete)
            {
                if (Directory.Exists(item))
                {
                    try
                    {
                        SetFolderAttributesToNormal(item);
                        Logger.Out("Deleting Folder: {0}", item);
                        //Directory.Delete(item, true);
                        MoveToRecycleBin(item);
                    }
                    catch (Exception e)
                    {
                        Logger.Out("Can't Delete Folder : {0}", item);
                        Logger.Out("Error : {0}", e.Message);
                    }
                }
            }
        }

        public static void MoveToRecycleBin(string file)
        {
            FileAttributes attr = File.GetAttributes(file);

            if (attr.HasFlag(FileAttributes.Directory))//So this is a directory
                FileSystem.DeleteDirectory(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            else
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        private static void HideMetadataFiles()
        {
            Logger.Out(string.Empty);

            if (FilesToHide.Count > 0)
                Logger.Out("Hidding Metadata Files");
            else
                Logger.Out("No Files Found To Hide");

            foreach (var item in FilesToHide)
            {
                if (File.Exists(item))
                {
                    // Set file attribute to "Hidden"
                    if ((File.GetAttributes(item) & FileAttributes.Hidden) != FileAttributes.Hidden)
                        File.SetAttributes(item, File.GetAttributes(item) | FileAttributes.Hidden);
                }
            }
        }

        private static void SetFolderIcon(string FolderPath)
        {
            try
            {
                Logger.Out("Setting Icon To Folder: {0}", FolderPath);
                LPSHFOLDERCUSTOMSETTINGS FolderSettings = new LPSHFOLDERCUSTOMSETTINGS();
                FolderSettings.dwMask = 0x10;
                FolderSettings.pszIconFile = "folder.ico";

                UInt32 FCS_READ = 0x00000001;
                UInt32 FCS_FORCEWRITE = 0x00000002;
                UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;

                string pszPath = FolderPath;//@"G:\FreeZone\New folder\New folder";
                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, pszPath, FCS_FORCEWRITE);
            }
            catch (Exception ex)
            {
                // log exception
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        static extern UInt32 SHGetSetFolderCustomSettings(ref LPSHFOLDERCUSTOMSETTINGS pfcs, string pszPath, UInt32 dwReadWrite);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct LPSHFOLDERCUSTOMSETTINGS
        {
            public UInt32 dwSize;
            public UInt32 dwMask;
            public IntPtr pvid;
            public string pszWebViewTemplate;
            public UInt32 cchWebViewTemplate;
            public string pszWebViewTemplateVersion;
            public string pszInfoTip;
            public UInt32 cchInfoTip;
            public IntPtr pclsid;
            public UInt32 dwFlags;
            public string pszIconFile;
            public UInt32 cchIconFile;
            public int iIconIndex;
            public string pszLogo;
            public UInt32 cchLogo;
        }

        static void RefreshIconCache()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "ie4uinit.exe");
            var winVersion = Environment­.OSVersion;

            if (File.Exists(path))
            {
                if (IsWindows10() == true)
                {
                    Process.Start(path, "-show");
                }
                else
                {
                    Process.Start(path, "-ClearIconCache");
                }
            }
            else
            {
                Logger.Out("File {0} doesn't exists", path);
            }
        }

        static bool IsWindows10()
        {
            var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string productName = (string)reg.GetValue("ProductName");

            return productName.StartsWith("Windows 10");
        }

    }
}
