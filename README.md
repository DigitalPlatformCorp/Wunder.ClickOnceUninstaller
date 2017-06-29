Modified uninstaller for ClickOnce applications with duplicate public keys
===========================

### This is a Fork

See [this thread](https://github.com/6wunderkinder/Wunder.ClickOnceUninstaller/issues/5). I was
attempting to use 6wunderkinder's ClickOnceUninstaller in a situation where multiple editions of
the same app had the same public key but different installer (.application) names and different
executable names. The original code would uninstall all of them and leave broken shortcuts
for some of them. 

This fork is a hacky attempt to fix this by filtering the registry keys using the installer
name and executable name. I believe it does correctly uninstall only the intended edition
of the app now.

I also added a new class, RemovePin, which tries to remove user pins of the app in the task
bar and start menu. The reason for doing this is that with a ClickOnce app, the pins point
to the .application file, so clicking them will reinstall the ClickOnce version. I wanted
to uninstall it because I'm switching to a different installer, so this was a bad thing.
It does break the pins but I couldn't find a fully reliable way to remove them entirely.
In the end I decided to use an ininstall method based on 
[this reinstaller](https://code.google.com/archive/p/clickonce-application-reinstaller-api/)
instead. 

This code is as-is, use at your own risk. I am not a registry expert and cannot promise this
does the right thing. If you're not in the special situation of having multiple editions of
the same program installed simultaneously with the same public key, I refer you instead to
[the origin of this fork](https://github.com/6wunderkinder/Wunder.ClickOnceUninstaller).

## License

The source code is available under the [MIT license](http://opensource.org/licenses/mit-license.php).
