using Microsoft.Win32;
using System;
using System.Linq;

namespace Wunder.ClickOnceUninstaller
{
	public class UninstallInfo
    {
        public const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        private UninstallInfo()
        {
        }

        public static UninstallInfo Find(string appName)
        {
            var uninstall = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath);
            if (uninstall != null)
            {
                foreach (var app in uninstall.GetSubKeyNames())
                {
                    var sub = uninstall.OpenSubKey(app);
                    if (sub != null && sub.GetValue("DisplayName") as string == appName)
                    {
                        return new UninstallInfo
                                   {
                                       Key = app,
									   UninstallString = sub.GetValue("UninstallString") as string,
                                       DisplayName = sub.GetValue("DisplayName") as string,
                                       ShortcutFolderName = sub.GetValue("ShortcutFolderName") as string,
                                       ShortcutSuiteName = sub.GetValue("ShortcutSuiteName") as string,
                                       ShortcutFileName = sub.GetValue("ShortcutFileName") as string,
                                       SupportShortcutFileName = sub.GetValue("SupportShortcutFileName") as string,
                                       Version = sub.GetValue("DisplayVersion") as string
                                   };
                    }
                }
            }

            return null;
        }

        public string Key { get; set; }

		public string DisplayName { get; set; }

        public string UninstallString { get; private set; }

        public string ShortcutFolderName { get; set; }

        public string ShortcutSuiteName { get; set; }

        public string ShortcutFileName { get; set; }

        public string SupportShortcutFileName { get; set; }

        public string Version { get; set; }

        public string GetPublicKeyToken()
        {
            var token = UninstallString.Split(',').Select(s => s.Trim()).First(s => s.StartsWith("PublicKeyToken="));
			token = token.Substring(token.Length - 16);
            if (token.Length != 16) throw new ArgumentException();
            return token;
        }

		public string GetInstallerName()
		{
			var token = UninstallString.Split(' ', ',').Select(s => s.Trim()).FirstOrDefault(s => s.EndsWith(".application"));
			if (null == token)
			{
				throw new ArgumentException();
			}

			return token;
		}

		public string GetExecutableName()
		{
			// Guess the executable name from the installer name. This assumes the names
			// are the same except the extension. This also assumes different editions
			// of your program have different installer names, even if the keys are the same.
			// If you can't get the exe name this way, you can change the program to pass
			// it in on the command line instead, but that will still require a different
			// name for each edition to prevent them all from being removed.
			return GetInstallerName().Replace(".application", ".exe");
		}
	}
}
