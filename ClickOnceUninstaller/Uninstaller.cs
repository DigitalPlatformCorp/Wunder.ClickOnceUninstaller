using System;
using System.Collections.Generic;
using System.Linq;

namespace Wunder.ClickOnceUninstaller
{
	public class Uninstaller
    {
        private readonly ClickOnceRegistry _registry;

        public Uninstaller()
        : this(new ClickOnceRegistry())
        {
        }

        public Uninstaller(ClickOnceRegistry registry)
        {
            _registry = registry;
        }

		public void Uninstall(UninstallInfo uninstallInfo)
        {
			var toRemove = FindComponentsToRemove(uninstallInfo).ToList();

			Console.WriteLine("Components to remove:");
			toRemove.ForEach(Console.WriteLine);
			Console.WriteLine();

			var steps = new List<IUninstallStep>
							{
								new RemoveFiles(),
								new RemoveStartMenuEntry(uninstallInfo),
								new RemovePin(uninstallInfo),
								new RemoveRegistryKeys(_registry, uninstallInfo),
								new RemoveUninstallEntry(uninstallInfo)
							};

			steps.ForEach(s => s.Prepare(toRemove));
			steps.ForEach(s => s.PrintDebugInformation());
			steps.ForEach(s => s.Execute());

			steps.ForEach(s => s.Dispose());
		}

        private IEnumerable<string> FindComponentsToRemove(UninstallInfo info)
        {
			var token = info.GetPublicKeyToken();
			var installer = info.GetInstallerName();
			var exeName = info.GetExecutableName();

            var candidates = _registry.Components.Where(c => c.Key.Contains(token));
			var components = new List<ClickOnceRegistry.Component>();

			// There should be a lone node for the installer itself.
			components.AddRange(candidates.Where(c => c.Identity.Contains(installer)));

			// Find the component that matches the public key and executable name.
			// Note this assumes different editions of your program have different names,
			// and that there will be only one match for a given key and exe name.
			var exeComponent = candidates.Where(c => c.Identity.Contains(exeName)).FirstOrDefault();
			if (exeComponent != null)
			{
				// Find the nodes that depend on the exe and add them - they will
				// be for the installer .application file and their dependencies should
				// cover everything that needs to be removed.
				components.AddRange(candidates
					.Where(c => c.Dependencies.Any(d => d.Key == exeComponent.Key)));
			}

            var toRemove = new List<ClickOnceRegistry.Component>();
            foreach (var component in components)
            {
                toRemove.Add(component);

                foreach (var dependency in component.Dependencies)
                {
                    if (toRemove.Any(d => d.Key == dependency.Key)) continue; // already in the list
                    if (_registry.Components.All(c => c.Key != dependency.Key)) continue; // not a public component

                    var mark = _registry.Marks.FirstOrDefault(m => m.Key == dependency.Key);
                    if (mark != null && mark.Implications.Any(i => components.All(c => c.Key != i.Name)))
                    {
                        // don't remove because other apps depend on this
                        continue;
                    }

                    toRemove.Add(dependency);
                }
            }

            return toRemove.Select(c => c.Key);
        }
    }
}
