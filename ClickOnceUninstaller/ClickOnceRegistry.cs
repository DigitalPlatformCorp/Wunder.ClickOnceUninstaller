using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wunder.ClickOnceUninstaller
{
	public class ClickOnceRegistry
    {
        public const string ComponentsRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Components";
        public const string MarksRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Marks";
        
        public ClickOnceRegistry()
        {
            ReadComponents();
            ReadMarks();
        }

        private void ReadComponents()
        {
            Components = new List<Component>();

            var components = Registry.CurrentUser.OpenSubKey(ComponentsRegistryPath);
            if (components == null) return;

            foreach (var keyName in components.GetSubKeyNames())
            {
                var componentKey = components.OpenSubKey(keyName);
                if (componentKey == null) continue;

                var component = new Component { Key = keyName };
                Components.Add(component);
            }
        }

        private void ReadMarks()
        {
            Marks = new List<Mark>();

            var marks = Registry.CurrentUser.OpenSubKey(MarksRegistryPath);
            if (marks == null) return;

            foreach (var keyName in marks.GetSubKeyNames())
            {
                var markKey = marks.OpenSubKey(keyName);
                if (markKey == null) continue;

                var mark = new Mark { Key = keyName };
                Marks.Add(mark);

                var appid = markKey.GetValue("appid") as byte[];
                if (appid != null) mark.AppId = Encoding.ASCII.GetString(appid);

                var identity = markKey.GetValue("identity") as byte[];
                if (identity != null) mark.Identity = Encoding.ASCII.GetString(identity);

                mark.Implications = new List<Implication>();
                var implications = markKey.GetValueNames().Where(n => n.StartsWith("implication"));
                foreach (var implicationName in implications)
                {
                    var implication = markKey.GetValue(implicationName) as byte[];
                    if (implication != null)
                        mark.Implications.Add(new Implication
                                                  {
                                                      Key = implicationName,
                                                      Name = implicationName.Substring(12),
                                                      Value = Encoding.ASCII.GetString(implication)
                                                  });
                }
            }
        }

        public class RegistryKey
        {
            public string Key { get; set; }

			public string Identity
			{
				get
				{
					if (_identity == null)
					{
						_identity = string.Empty;
						var key = Registry.CurrentUser.OpenSubKey(ComponentsRegistryPath + "\\" + Key);
						if (key.GetValueNames().Contains("identity"))
						{
							var value = key.GetValue("identity") as byte[];
							try
							{
								_identity = Encoding.ASCII.GetString(value);
							}
							catch { }
						}
					}

					return _identity;
				}
				set
				{
					_identity = value;
				}
			}

			public override string ToString()
            {
                return Key ?? base.ToString();
            }

			private string _identity = null;
        }

        public class Component : RegistryKey
        {
            public IEnumerable<Component> Dependencies
			{
				get
				{
					if (_deps == null)
					{
						_deps = new List<Component>();
						var key = Registry.CurrentUser.OpenSubKey(ComponentsRegistryPath + "\\" + Key);

						foreach (var dependencyName in key.GetSubKeyNames().Where(n => n != "Files"))
						{
							_deps.Add(new Component() { Key = dependencyName });
						}
					}

					return _deps;
				}
			}

			private List<Component> _deps = null;
        }

        public class Mark : RegistryKey
        {
            public string AppId { get; set; }

            public List<Implication> Implications { get; set; }
        }

        public class Implication : RegistryKey
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }

        public List<Component> Components { get; set; }

        public List<Mark> Marks { get; set; }
    }
}
