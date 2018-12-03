using System;
using System.Runtime.InteropServices;

namespace Folder_Icon_Creator
{
	/// <summary>
	/// Wrapper class for WritePrivateProfileString Win32 API function.
	/// </summary>
	public class IniWriter
	{
        // For convenience's sake, I'm using the WritePrivateProfileString
        // Win32 API function here. Feel free to write your own .ini file
        // writing function if you wish.
        [DllImport("kernel32")] 
        private static extern int WritePrivateProfileString(
                string iniSection, 
                string iniKey, 
                string iniValue, 
                string iniFilePath);		
        
        /// <summary>
        /// Adds to (or modifies) a value to an .ini file. If the file does not exist,
        /// it will be created.
        /// </summary>
        /// <param name="iniSection">The section to which to add or modify a value.If the section does not exist,
        /// it will be created.</param>
        /// <param name="iniKey">The key to which to add or modify a value.If the key does not exist,
        /// it will be created.</param>
        /// <param name="iniValue">The value to write to the .ini file</param>
        /// <param name="iniFilePath">The path to the .ini file to modify.</param>
        /// <returns></returns>
        public static void WriteValue(string iniSection, 
                                     string iniKey, 
                                     string iniValue,
                                     string iniFilePath)
        {
            WritePrivateProfileString(iniSection, iniKey, iniValue, iniFilePath);
        }
        
	}
}
