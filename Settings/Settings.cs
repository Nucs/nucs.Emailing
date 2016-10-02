using nucs.Emailing.Settings;

namespace nucs.Emailing {
    public class Configuration : AppSettings<Configuration> {
        /// <summary>
        ///     The default email address the emails will be sent through when not specified.
        ///     e.g.: address@yourdomain.com
        ///     
        /// </summary>
        public string DefaultSender;
        /// <summary>
        ///     The display name that will be shown - sent from...
        /// </summary>
        public string DefaultSenderDisplayName;

        public string Username;

        public string Password;

        public string HostIp = "127.0.0.1";

        public ushort Port = 587;

        public bool EnableSSL = false;

        public bool UseDefaultCredentials = true;

        /// <summary>
        ///     Log all emails sent to a local directory specified in <paramref name="LogDirectoryName"/>
        /// </summary>
        public bool LogLocally = false;

        /// <summary>
        ///     The directory name for logging, plain word.
        ///     e.g.: log
        /// </summary>
        public string LogDirectoryName = "log";



        public override void Save() {
            base.Save();
        }
    }
}