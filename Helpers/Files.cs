using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace nucs.Emailing.Helpers {
    public static class Files {
        /// <summary>
        ///     e.g.: 'dir/wow.email.html' will add before the executing directory.
        ///     e.g.: 'C:/..../wow.email.html' will simply read it
        ///     e.g.: '/' will return executing directory.
        /// </summary>
        /// <returns></returns>
        public static string NormalizePath(this string path) {
            return Normalize(path);
        }
        /// <summary>
        ///     e.g.: 'dir/wow.email.html' will add before the executing directory.
        ///     e.g.: 'C:/..../wow.email.html' will simply read it
        ///     e.g.: '/' will return executing directory.
        /// </summary>
        /// <returns></returns>
        public static string Normalize(string path) {
            path = path?.Trim();
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 1 && (path[0] == '\\' || path[0] == '/'))
                return ExecutingDirectory.FullName;

            if (path.Length > 2 && char.IsLetter(path[0]) && (path[1] == '/' || path[1] == '\\')) 
                return path;
            
            return Path.Combine(ExecutingDirectory.FullName, path);
        }

        /// <summary>
        ///     Loads a string content from a file.
        ///     e.g.: 'dir/wow.email.html' will add before the executing directory.
        ///     e.g.: 'C:/..../wow.email.html' will simply read it
        /// </summary>
        /// <returns></returns>
        public static string Read(string fileandpath) {
            return Read(fileandpath, Encoding.UTF8);
        }

        /// <summary>
        ///     Loads a string content from a file.
        ///     e.g.: 'dir/wow.email.html' will add before the executing directory.
        ///     e.g.: 'C:/..../wow.email.html' will simply read it
        /// </summary>
        /// <param name="fileandpath"></param>
        /// <param name="encoding">Default: UTF8</param>
        /// <returns></returns>
        public static string Read(string fileandpath, Encoding encoding) {
            return File.ReadAllText(Normalize(fileandpath), encoding);
        }

        /// <summary>
        ///     The exe that has started this process
        /// </summary>
        private static FileInfo ExecutingExe => new FileInfo(Assembly.GetEntryAssembly().Location);

        /// <summary>
        ///     The directory that the executing exe is inside
        /// </summary>
        private static DirectoryInfo ExecutingDirectory => ExecutingExe.Directory;

    }
}