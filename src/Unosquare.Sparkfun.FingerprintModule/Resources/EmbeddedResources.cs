namespace Unosquare.Sparkfun.FingerprintModule.Resources
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides access to embedded assembly files.
    /// </summary>
    internal static class EmbeddedResources
    {
        internal const string LibCLibrary = "libc";

        /// <summary>
        /// Initializes static members of the <see cref="EmbeddedResources"/> class.
        /// </summary>
        static EmbeddedResources()
        {
#if !NET452
            EntryAssembly = typeof(EmbeddedResources).GetTypeInfo().Assembly;
            ResourceNames = new ReadOnlyCollection<string>(EntryAssembly.GetManifestResourceNames());
            var uri = new UriBuilder(EntryAssembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            EntryAssemblyDirectory = Path.GetDirectoryName(path);

            ExtractAll();
#endif
        }

        /// <summary>
        /// Gets the resource names.
        /// </summary>
        /// <value>
        /// The resource names.
        /// </value>
        public static ReadOnlyCollection<string> ResourceNames { get; }

        /// <summary>
        /// Gets the entry assembly directory.
        /// </summary>
        /// <value>
        /// The entry assembly directory.
        /// </value>
        public static string EntryAssemblyDirectory { get; }

        public static Assembly EntryAssembly { get; }

        /// <summary>
        /// Changes file permissions on a Unix file system.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The result.</returns>
        [DllImport(LibCLibrary, EntryPoint = "chmod", SetLastError = true)]
        public static extern int Chmod(string filename, uint mode);

        /// <summary>
        /// Converts a string to a 32 bit integer. Use endpointer as IntPtr.Zero.
        /// </summary>
        /// <param name="numberString">The number string.</param>
        /// <param name="endPointer">The end pointer.</param>
        /// <param name="numberBase">The number base.</param>
        /// <returns>The result.</returns>
        [DllImport(LibCLibrary, EntryPoint = "strtol", SetLastError = true)]
        public static extern int StringToInteger(string numberString, IntPtr endPointer, int numberBase);

        /// <summary>
        /// Extracts all the file resources to the specified base path.
        /// </summary>
        public static void ExtractAll()
        {
            var basePath = EntryAssemblyDirectory;
            var executablePermissions = StringToInteger("0777", IntPtr.Zero, 8);

            foreach (var resourceName in ResourceNames)
            {
                var filename = resourceName
                    .Substring($"{typeof(EmbeddedResources).Namespace}.".Length)
                    .Replace(".temp", string.Empty);
                var targetPath = Path.Combine(basePath, filename);
                if (File.Exists(targetPath)) return;

                using (var stream =
                    EntryAssembly.GetManifestResourceStream($"{typeof(EmbeddedResources).Namespace}.{filename}"))
                {
                    using (var outputStream = File.OpenWrite(targetPath))
                    {
                        stream?.CopyTo(outputStream);
                    }

                    try
                    {
                        Chmod(targetPath, (uint)executablePermissions);
                    }
                    catch
                    {
                        /* Ignore */
                    }
                }
            }
        }
    }
}