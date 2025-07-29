using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Folder_Icon_Creator
{
	public static class Refresh
	{
		[DllImport("Shell32.dll", CharSet = CharSet.Auto)]
		private static extern int SHChangeNotify(int wEventId, int uFlags, IntPtr item1, IntPtr item2);

		public static void Run()
		{
			SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
		}
	}
}
