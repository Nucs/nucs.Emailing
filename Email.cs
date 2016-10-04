using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using B2BFamily.SmtpValidation.Lib;
using nucs.Emailing.Helpers;
using nucs.Emailing.Templating;

namespace nucs.Emailing {
    public class Email {
        /// <summary>
        ///     The exe that has started this process
        /// </summary>
        private static FileInfo ExecutingExe => new FileInfo(Assembly.GetEntryAssembly().Location);

        /// <summary>
        ///     The directory that the executing exe is inside
        /// </summary>
        private static DirectoryInfo ExecutingDirectory => ExecutingExe.Directory;

        public static Email LoadConfiguration(string filename = "email.credentials.settings") {
            return LoadConfiguration(new FileInfo(Files.Normalize(filename)));
        }

        public static Email LoadConfiguration(FileInfo file) {
            var s = Settings.AppSettings<Configuration>.Load(file.FullName);
            var e = new Email() {DefaultSender = s.DefaultSender, EnableSSL = s.EnableSSL, HostIp = s.HostIp, LogDirectoryName = s.LogDirectoryName, Password = ConvertToSecureString(s.Password), Port = s.Port, LogLocally = s.LogLocally, UseDefaultCredentials = s.UseDefaultCredentials, Username = s.Username, DefaultSenderDisplayName = s.DefaultSenderDisplayName};
            return e;
        }

        private static SecureString ConvertToSecureString(string password) {
            var securePassword = new SecureString();

            foreach (char c in password ?? "")
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }

        /// <summary>
        ///     The default email address the emails will be sent through when not specified.
        ///     e.g.: address@yourdomain.com
        /// </summary>
        public string DefaultSender;

        /// <summary>
        ///     The display name that will be shown - sent from...
        /// </summary>
        public string DefaultSenderDisplayName;

        public string Username;

        public SecureString Password;

        public string HostIp;

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

        private readonly object logdir_sync = new object();
        private DirectoryInfo _logdir;

        private DirectoryInfo logdir {
            get {
                lock (logdir_sync) {
                    if (_logdir == null) {
                        _logdir = new DirectoryInfo(Path.Combine(ExecutingDirectory.FullName, LogDirectoryName));
                        if (!Directory.Exists(_logdir.FullName))
                            _logdir.Create();
                    }
                    return _logdir;
                }
            }
        }

        private MailMessage _prepareNewMail => new MailMessage() {
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };


        /// <summary>
        ///     Gets a smtp client, with valid credetials - make sure to dispose at the end!
        /// </summary>
        public SmtpClient GetClient {
            get {
                var client = new SmtpClient(HostIp, Port);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = EnableSSL;
                client.UseDefaultCredentials = UseDefaultCredentials;
                return client;
            }
        }

        #region Templated Send

        #region Overloads

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(MailAddress receiver, string subject, Body body, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(DefaultSender, DefaultSenderDisplayName), receiver, subject, body.Content, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string receiver, string subject, Body body, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(DefaultSender, DefaultSenderDisplayName), new MailAddress(receiver), subject, body.Content, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(MailAddress receiver, string subject, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(DefaultSender, DefaultSenderDisplayName), receiver, subject, null, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string receiver, string subject, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(DefaultSender, DefaultSenderDisplayName), new MailAddress(receiver), subject, null, source, identifier);
        }


        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string sender, MailAddress receiver, string subject, string body, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(sender), receiver, subject, body, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string sender, string receiver, string subject, Body body, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(sender), new MailAddress(receiver), subject, body.Content, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(MailAddress sender, string receiver, string subject, Body body, EmailSource source, string identifier) {
            await SendTemplate(sender, new MailAddress(receiver), subject, body.Content, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string sender, MailAddress receiver, string subject, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(sender), receiver, subject, null, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(string sender, string receiver, string subject, EmailSource source, string identifier) {
            await SendTemplate(new MailAddress(sender), new MailAddress(receiver), subject, null, source, identifier);
        }

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(MailAddress sender, string receiver, string subject, EmailSource source, string identifier) {
            await SendTemplate(sender, new MailAddress(receiver), subject, null, source, identifier);
        }

        #endregion

        /// <summary>
        ///     Sends a templated email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        /// <param name="source">Which source refer to</param>
        /// <param name="identifier">Depends on the EmailSource, if File then a path, if Resource then the resource name.</param>
        public async Task SendTemplate(MailAddress sender, MailAddress receiver, string subject, string body, EmailSource source, string identifier) {
            var template = EmailSources.Fetch(source, identifier);
            if (template == null)
                throw new FileNotFoundException($"Template could not have been found!\n Source: {source}, Identifier: {identifier}");

            var msg = _prepareNewMail;
            msg.Sender = sender;
            msg.From = sender;
            msg.To.Add(receiver);
            msg.Subject = subject ?? "No Subject";
            msg.Body = body ?? "";

            //new translated body
            msg.Body = msg.Translate(template);
            await _internal_sendasyncmail(msg);
        }

        #endregion

        #region Regular Send

        #region Overloads

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(MailAddress receiver, string subject, Body body) {
            await Send(new MailAddress(DefaultSender, DefaultSenderDisplayName), receiver, subject, body.Content);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string receiver, string subject, Body body) {
            await Send(new MailAddress(DefaultSender, DefaultSenderDisplayName), new MailAddress(receiver), subject, body.Content);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(MailAddress receiver, string subject) {
            await Send(new MailAddress(DefaultSender, DefaultSenderDisplayName), receiver, subject, null);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string receiver, string subject) {
            await Send(new MailAddress(DefaultSender, DefaultSenderDisplayName), new MailAddress(receiver), subject, null);
        }


        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string sender, MailAddress receiver, string subject, string body) {
            await Send(new MailAddress(sender), receiver, subject, body);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string sender, string receiver, string subject, Body body) {
            await Send(new MailAddress(sender), new MailAddress(receiver), subject, body.Content);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(MailAddress sender, string receiver, string subject, Body body) {
            await Send(sender, new MailAddress(receiver), subject, body.Content);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string sender, MailAddress receiver, string subject) {
            await Send(new MailAddress(sender), receiver, subject, null);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(string sender, string receiver, string subject) {
            await Send(new MailAddress(sender), new MailAddress(receiver), subject, null);
        }

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(MailAddress sender, string receiver, string subject) {
            await Send(sender, new MailAddress(receiver), subject, null);
        }

        #endregion

        /// <summary>
        ///     Sends an email.
        /// </summary>
        /// <param name="sender">Which email address sends this email</param>
        /// <param name="receiver">The receiver of the email</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Content of the email</param>
        public async Task Send(MailAddress sender, MailAddress receiver, string subject, string body) {
            var msg = _prepareNewMail;
            msg.Sender = sender;
            msg.From = sender;
            msg.To.Add(receiver);
            msg.Subject = subject ?? "No Subject";
            msg.Body = body ?? "";
            await _internal_sendasyncmail(msg);
        }

        #endregion

        private async Task _internal_sendasyncmail(MailMessage msg) {
            if (string.IsNullOrEmpty(msg.Sender?.Address))
                throw new ArgumentNullException(nameof(msg.Sender), "Sender specified is null!");
            if (msg.To.Count == 0 || string.IsNullOrEmpty(msg.To[0]?.Address))
                throw new ArgumentNullException(nameof(msg.Sender), "Receiver specified is null!");
            if (LogLocally)
                LogMessage(msg);

            using (var cli = GetClient) {
                cli.Credentials = new NetworkCredential(Username, Password);
                await cli.SendMailAsync(msg);
            }
        }
        /// <summary>
        ///     Tests the connection and authentication
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TestConnection() {
            return await SmtpHelper.ValidateCredentials(Username, SecureStringToString(Password), HostIp, Port, EnableSSL);
        }


        private void LogMessage(MailMessage msg) {
            var @out = Path.Combine(logdir.FullName, $"{DateTime.Now.Ticks}.{CleanForFileName(msg.Subject)}.email.txt");
            var cont =
                $@"To: {msg.To}
From: {msg.From}
Sender: {msg.Sender}
Title: {msg.Subject}

{msg.Body}";
            File.WriteAllText(@out, cont);
        }


        private static string CleanForFileName(string fileName) {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }

    public class Body {
        public string Content { get; set; }

        public Body(string content) {
            Content = content;
        }
    }
}