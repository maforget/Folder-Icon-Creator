using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace Folder_Icon_Creator
{
    public class FileHideInfo
    {
        enum ConfigSection
        {
            VideoFiles,
            OtherExtensionToKeep
        }

        public string File { get; set; }
        public string FileWithoutExtension { get; set; }
        public bool Hidden { get; set; }
        public bool DeleteFolder { get; set; }
        public bool DeleteFile { get; set; }

        List<string> VideoFiles = new List<string>();
        List<string> OtherExtensionToKeep = new List<string>();
        List<string> RelatedMetadataFiles = new List<string>();
        List<string> ExtensionsToKeep = new List<string>();

        public FileHideInfo(string file)
        {
            if (LoadConfig() == true)
            {
                File = file.ToLowerInvariant();
                FileWithoutExtension = Path.GetFileNameWithoutExtension(File);
                FileWithoutExtension = FileWithoutExtension.Replace("-thumb", string.Empty);
                ExtensionsToKeep.AddRange(VideoFiles);
                ExtensionsToKeep.AddRange(OtherExtensionToKeep);

                Hidden = true;
                DeleteFolder = true;
                DeleteFile = true;

                foreach (var item in VideoFiles)
                {
                    if (file.EndsWith(item))
                    {
                        DeleteFolder = false;
                        DeleteFile = false;
                        break;
                    }
                }

                foreach (var item in ExtensionsToKeep)
                {
                    if (file.EndsWith(item))
                    {
                        Hidden = false;
                        break;
                    }
                }
            }
        }

        private bool LoadConfig()
        {
            var parser = new FileIniDataParser();
            string configLocation = Path.Combine(GetAppLocation(), "Config.ini");

            if (System.IO.File.Exists(configLocation))
            {
                var data = parser.ReadFile(configLocation);
                var generalSection = data.Sections.ContainsSection("General") ? data.Sections["General"] : null;

                if (generalSection != null)
                {
                    VideoFiles = GetConfig(generalSection, ConfigSection.VideoFiles);
                    OtherExtensionToKeep = GetConfig(generalSection, ConfigSection.OtherExtensionToKeep);
                }
                else
                {
                    Logger.Out("No General Section inside the Config.ini file");
                    return false;
                }
            }
            else
            {
                Logger.Out("No Config.ini found in the program root folder");
                return false;

            }

            if (VideoFiles.Count == 0)
                return false;

            return true;
        }

        private List<string> GetConfig(KeyDataCollection data, ConfigSection type)
        {
            string result = string.Empty;

            if (data != null)
            {
                switch (type)
                {
                    case ConfigSection.VideoFiles:
                        result = data["VideoFiles"];
                        break;
                    case ConfigSection.OtherExtensionToKeep:
                        result = data["OtherExtensionToKeep"];
                        break;
                    default:
                        result = string.Empty;
                        break;
                }
            }

            List<string> list = new List<string>();
            foreach (var item in result.Split(','))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    list.Add(string.Format(".{0}", item.Trim()));
                }
            }

            return list;
        }

        private string GetAppLocation()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static List<FileHideInfo> GetFilesList(IEnumerable<string> filesList)
        {
            List<FileHideInfo> List = new List<FileHideInfo>();

            foreach (var item in filesList)
            {
                FileHideInfo fi = new FileHideInfo(item);
                List.Add(fi);
            }

            List = CheckRelatedFiles(List);

            return List;
        }

        private static List<FileHideInfo> CheckRelatedFiles(List<FileHideInfo> list)
        {
            List<FileHideInfo> returnList = new List<FileHideInfo>();
            var VideoFiles = list.Where(x => x.FileWithoutExtension != Program.PosterFileName && x.DeleteFile == false).Select(x => x.FileWithoutExtension).ToList();//Check For Video Files

            for (int i = 0; i < list.Count; i++)
            {
                FileHideInfo item = list[i];
                item.RelatedMetadataFiles = list.Where(x => x.File != item.File && x.FileWithoutExtension == item.FileWithoutExtension).Select(x => x.File).ToList();

                if (item.FileWithoutExtension == Program.PosterFileName || VideoFiles.Contains(item.FileWithoutExtension))
                    item.DeleteFile = false;//Set the file To Not be Deleted, Because a related Video File Exsits

                if (item.RelatedMetadataFiles.Count == 0)
                    item.DeleteFile = false;

                returnList.Add(item);
            }
            return returnList;
        }
    }
}
