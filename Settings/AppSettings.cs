using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using nucs.Emailing.Helpers;
using Newtonsoft.Json;

namespace nucs.Emailing.Settings {
    public abstract class AppSettings<T> : ISaveable where T : ISaveable, new() {
        public const string DEFAULT_FILENAME = "email.credentials.settings";

        // ReSharper disable once StaticFieldInGenericType
        static AppSettings() {}

        /// <summary>
        /// The filename that was originally loaded from. saving to other file does not change this field!
        /// </summary>
        public virtual void Save(string filename = DEFAULT_FILENAME) {
            File.WriteAllText(filename, JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// The filename that was originally loaded from. saving to other file does not change this field!
        /// </summary>
        public virtual void Save() {
            object o = this;
            Save((T)o, DEFAULT_FILENAME);
        }

        public static void Save(T pSettings, string filename = DEFAULT_FILENAME) {
            filename = filename.NormalizePath();
            File.WriteAllText(filename, JsonConvert.SerializeObject(pSettings, Formatting.Indented));
        }

        /// <summary>
        /// Loads or creates a settings file.
        /// </summary>
        /// <param name="fileName">File name, for example "settings.jsn". no path required, just a file name.</param>
        /// <returns>The loaded or freshly new saved object</returns>
        public static T Load(string fileName = DEFAULT_FILENAME) {
            fileName = fileName.NormalizePath();
            if (File.Exists(fileName))
                try {
                    var fc = File.ReadAllText(fileName);
                    if (string.IsNullOrEmpty((fc ?? "").Trim()))
                        goto _save;
                    return JsonConvert.DeserializeObject<T>(fc, new JsonSerializerSettings() {Formatting = Formatting.Indented});
                } catch (InvalidOperationException e) {
                    if (e.Message.Contains("Cannot convert"))
                        throw new Exception("Unable to deserialize settings file, value<->type mismatch. see inner exception", e);
                    throw e;
                } catch (System.ArgumentException e) {
                    if (e.Message.StartsWith("Invalid"))
                        throw new Exception("Settings file is corrupt.");
                    throw e;
                }
            _save:
            var t = new T();
            Save(t, fileName);
            return t;
        }

        /// <summary>
        /// Gives you all the types that has <param name="attribute"></param> attached to it in the entire <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="attribute">the type of the attribute.</param>
        /// <returns></returns>
        private static IEnumerable<Type> GetAllAttributeHolders(Type attribute) {
            return from assmb in AppDomain.CurrentDomain.GetAssemblies() from type in gettypes(assmb) where type.GetCustomAttributes(attribute, true).Length > 0 select type;
        }

        private static Type[] gettypes(Assembly assmb) {
            return !File.Exists(AssemblyDirectory(assmb)) ? new Type[0] : assmb.GetTypes();
        }

        private static string AssemblyDirectory(Assembly asm) {
            string codeBase = asm.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
#pragma warning disable 693
        private static T CreateInstance<T>(Type @this) {
            return (T) Activator.CreateInstance(@this);
        }
    }
#pragma warning restore 693

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class SettingsConverterAttribute : Attribute {
        public SettingsConverterAttribute() {}
    }
}