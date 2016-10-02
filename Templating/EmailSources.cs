using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using nucs.Emailing.Helpers;

namespace nucs.Emailing.Templating {
    public static class EmailSources {


        public static EmailTemplate Fetch(EmailSource src, string identifier) {
            switch (src) {
                case EmailSource.EmbeddedResource:
                    return Resource(identifier);
                case EmailSource.File:
                    return File(identifier);
                default:
                    throw new ArgumentOutOfRangeException(nameof(src), src, null);
            }
        }

        /// <summary>
        ///     Finds the resource in All assemblies that contains the given string 'name'.
        ///     Be precise as much as possible to avoid multiple detections
        /// </summary>
        public static EmailTemplate Resource(string name) {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            Assembly asmy = null;
            string target = null;
            foreach (var asm in asms.OrderByDescending(asm=>asm.ToString().EndsWith("null"))) { //check local first
                asmy = asm;
                target = asm.GetManifestResourceNames().FirstOrDefault(mrn => mrn.Contains(name));
                if (target != null)
                    break;
            }

            if (target==null)
                throw new FileNotFoundException($"Could not find a resource that contains the name '{name}'");

            using (var stream = asmy.GetManifestResourceStream(target)) {
                if (stream == null)
                    throw new FileNotFoundException($"Could not find a resource that contains the name '{name}'");
                using (var reader = new StreamReader(stream)) {
                    return new EmailTemplate(reader.ReadToEnd());
                }
            }
        }

        /// <summary>
        ///     Loads a string content from a file.
        ///     e.g.: 'dir/wow.email.html' will add before the executing directory.
        ///     e.g.: 'C:/..../wow.email.html' will simply read it
        /// </summary>
        /// <param name="fileandpath"></param>
        /// <returns></returns>
        public static EmailTemplate File(string fileandpath) {
            return new EmailTemplate(Files.Read(fileandpath, Encoding.UTF8));
        }
    }

    public enum EmailSource {
        EmbeddedResource,
        File
    }
}