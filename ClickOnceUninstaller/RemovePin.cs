using System;
using System.Collections.Generic;
using System.IO;

namespace Wunder.ClickOnceUninstaller
{
	// This is an attempt to removed pinned shortcuts to the program from the task bar and start 
	// menu. It's imperfect - it does remove the shortcuts, but the pinned icons remain in
	// place. The icons will be nonfunctional, so clicking on them will not reinstall the 
	// ClickOnce app, but the error message may confuse users.
	public class RemovePin : IUninstallStep
    {
        private readonly UninstallInfo _uninstallInfo;
		private List<string> _filesToRemove;

		public RemovePin(UninstallInfo uninstallInfo)
        {
            _uninstallInfo = uninstallInfo;
        }

        public void Prepare(List<string> componentsToRemove)
        {
			var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var folder = Path.Combine(programsFolder, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
			var shortcut = Path.Combine(folder, _uninstallInfo.ShortcutFileName + ".appref-ms");

			_filesToRemove = new List<string>();
			if (File.Exists(shortcut)) _filesToRemove.Add(shortcut);

			folder = Path.Combine(programsFolder, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\StartMenu");
			shortcut = Path.Combine(folder, _uninstallInfo.ShortcutFileName + ".appref-ms");
			if (File.Exists(shortcut)) _filesToRemove.Add(shortcut);
		}

		public void PrintDebugInformation()
        {
			if (_filesToRemove == null)
				throw new InvalidOperationException("Call Prepare() first.");

			Console.WriteLine("Remove pinned shortcuts");

			foreach (var file in _filesToRemove)
			{
				Console.WriteLine("Delete file " + file);
			}

			Console.WriteLine();
		}

        public void Execute()
        {
			if (_filesToRemove == null)
				throw new InvalidOperationException("Call Prepare() first.");

			try
			{
				foreach (var file in _filesToRemove)
				{
					File.Delete(file);
				}
			}
			catch (IOException)
			{
			}
		}

        public void Dispose()
        {
        }
    }
}
