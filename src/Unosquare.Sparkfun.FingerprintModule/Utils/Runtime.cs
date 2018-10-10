#if !NET452
namespace Unosquare.Sparkfun.FingerprintModule.Utils
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides utility methods to retrieve system information.
    /// </summary>
    internal static class Runtime
    {
        private static OperatingSystem? _oS;

        /// <summary>
        /// Gets the current Operating System.
        /// </summary>
        /// <value>
        /// The os.
        /// </value>
        public static OperatingSystem OS
        {
            get
            {
                if (_oS.HasValue == false)
                {
                    var windowsDirectory = Environment.GetEnvironmentVariable("windir");
                    if (string.IsNullOrEmpty(windowsDirectory) == false
                        && windowsDirectory.Contains(@"\")
                        && Directory.Exists(windowsDirectory))
                    {
                        _oS = OperatingSystem.Windows;
                    }
                    else
                    {
                        _oS = File.Exists(@"/proc/sys/kernel/ostype") ? OperatingSystem.Unix : OperatingSystem.Osx;
                    }
                }

                return _oS ?? OperatingSystem.Unknown;
            }
        }
    }
}
#endif