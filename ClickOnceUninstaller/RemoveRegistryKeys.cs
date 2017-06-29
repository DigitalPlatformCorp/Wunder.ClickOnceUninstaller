using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wunder.ClickOnceUninstaller
{
	public class RemoveRegistryKeys : IUninstallStep
    {
        public const string PackageMetadataRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\PackageMetadata";
        public const string ApplicationsRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\StateManager\Applications";
        public const string FamiliesRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\StateManager\Families";
        public const string VisibilityRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Visibility";

        private readonly ClickOnceRegistry _registry;
        private readonly UninstallInfo _uninstallInfo;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private List<RegistryMarker> _keysToRemove;
        private List<RegistryMarker> _valuesToRemove;

        public RemoveRegistryKeys(ClickOnceRegistry registry, UninstallInfo uninstallInfo)
        {
            _registry = registry;
            _uninstallInfo = uninstallInfo;
        }

        public void Prepare(List<string> componentsToRemove)
        {
            _keysToRemove = new List<RegistryMarker>();
            _valuesToRemove = new List<RegistryMarker>();

            var componentsKey = Registry.CurrentUser.OpenSubKey(ClickOnceRegistry.ComponentsRegistryPath, true);
            _disposables.Add(componentsKey);
            foreach (var component in _registry.Components)
            {
                if (componentsToRemove.Contains(component.Key))
                    _keysToRemove.Add(new RegistryMarker(componentsKey, component.Key));
            }

            var marksKey = Registry.CurrentUser.OpenSubKey(ClickOnceRegistry.MarksRegistryPath, true);
            _disposables.Add(marksKey);
            foreach (var mark in _registry.Marks)
            {
                if (componentsToRemove.Contains(mark.Key))
                {
                    _keysToRemove.Add(new RegistryMarker(marksKey, mark.Key));
                }
                else
                {
                    var implications = mark.Implications.Where(i => componentsToRemove.Any(c => c == i.Name)).ToList();
                    if (implications.Any())
                    {
                        var markKey = marksKey.OpenSubKey(mark.Key, true);
                        _disposables.Add(markKey);

                        foreach (var implication in implications)
                        {
                            _valuesToRemove.Add(new RegistryMarker(markKey, implication.Key));
                        }
                    }
                }
            }

            var token = _uninstallInfo.GetPublicKeyToken();
			var installer = _uninstallInfo.GetInstallerName();
			var exeName = _uninstallInfo.GetExecutableName();

			var metadataKeysRemoved = new List<string>();
            var packageMetadata = Registry.CurrentUser.OpenSubKey(PackageMetadataRegistryPath);
            foreach (var keyName in packageMetadata.GetSubKeyNames())
            {
                metadataKeysRemoved.AddRange(DeleteMatchingSubKeys(PackageMetadataRegistryPath + "\\" + keyName, token, "appid", installer));
            }

            DeleteMatchingSubKeys(ApplicationsRegistryPath, token, "identity", installer);
            DeleteMatchingSubKeys(FamiliesRegistryPath, token, null, null, metadataKeysRemoved);
            DeleteMatchingSubKeys(VisibilityRegistryPath, token, "identity", exeName);
			DeleteMatchingSubKeys(VisibilityRegistryPath, token, "identity", installer);
		}

		private IEnumerable<string> DeleteMatchingSubKeys(string registryPath, string token, string valueName, string valueMatch, IEnumerable<string> keyNameFilter = null)
        {
			var result = new List<string>();
            var key = Registry.CurrentUser.OpenSubKey(registryPath, true);
            _disposables.Add(key);
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                if (subKeyName.Contains(token) 
				 && ((keyNameFilter == null) || keyNameFilter.Any(k => subKeyName.Contains(k))))
                {
					var id = string.Empty;

					if (valueName != null)
					{
						var subKey = key.OpenSubKey(subKeyName);
						_disposables.Add(subKey);

						if (subKey.GetValueNames().Contains(valueName))
						{
							var val = subKey.GetValue(valueName) as byte[];
							try
							{
								id = Encoding.ASCII.GetString(val);
							}
							catch { }
						}
					}

					if ((valueName == null) || id.Contains(valueMatch))
					{
						_keysToRemove.Add(new RegistryMarker(key, subKeyName));
						result.Add(subKeyName);
					}
                }
            }

			return result;
        }

        public void PrintDebugInformation()
        {
            if (_keysToRemove == null)
                throw new InvalidOperationException("Call Prepare() first.");

            foreach (var key in _keysToRemove)
            {
                Console.WriteLine("Delete key {0} in {1}", key.Parent, key.ItemName);
            }

            foreach (var value in _valuesToRemove)
            {
                Console.WriteLine("Delete value {0} in {1}", value.Parent, value.ItemName);
            }

            Console.WriteLine();
        }

        public void Execute()
        {
            if (_keysToRemove == null)
                throw new InvalidOperationException("Call Prepare() first.");

            foreach (var key in _keysToRemove)
            {
                key.Parent.DeleteSubKeyTree(key.ItemName);
            }

            foreach (var value in _valuesToRemove)
            {
                value.Parent.DeleteValue(value.ItemName);
            }
        }

        public void Dispose()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            _keysToRemove = null;
            _valuesToRemove = null;
        }

        private class RegistryMarker
        {
            public RegistryMarker(RegistryKey key, string name)
            {
                Parent = key;
                ItemName = name;
            }

            public RegistryKey Parent { get; private set; }

            public string ItemName { get; private set; }
        }
    }
}
