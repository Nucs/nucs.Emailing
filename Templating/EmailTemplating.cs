using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;

namespace nucs.Emailing {
    /// <summary>
    ///     Two functions that handle templating.
    /// </summary>
    public class EmailTemplate {
        /// <summary>
        ///     Is it actually a template and it requires translating
        /// </summary>
        public bool RequireTranslating { get; }

        public string Template { get; }

        public EmailTemplate(string template) {
            Template = template.Trim().TrimStart('\n', '\r'); //clean and set
            RequireTranslating = IsTemplate(template);
        }

        /// <summary>
        ///     Stamps all available stamps in the <remarks>Stamps</remarks> into the string template. if not template - returns the string.
        /// </summary>
        public string Translate(MailMessage msg) {
            if (!RequireTranslating) //plain text!
                return Template;

            return msg.Translate(this);
        }


        /// <summary>
        ///     Is this email string a template and not plain text
        /// </summary>
        /// <param name="emailstring"></param>
        /// <returns></returns>
        public static bool IsTemplate(EmailTemplate etemp) {
            return etemp.RequireTranslating;
        }

        /// <summary>
        ///     Is this email string a template and not plain text
        /// </summary>
        /// <param name="emailstring"></param>
        /// <returns></returns>
        public static bool IsTemplate(string emailstring) {
            return emailstring.Trim().TrimStart('\n', '\r').StartsWith("@Template", true, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Stamps the value into the template sign, for example (template, val)=>template.Replace("@{lol}", value);
        /// </summary>
        public delegate string StampTemplate(string template, string stamp, MailMessage value);

        /// <summary>
        ///     Contains all the Stamps that stamps the value into the template sign, for example (template, stamp, msg)=>template.Replace(stamp, msg.Subject);
        /// </summary>
        public static Dictionary<string, StampTemplate> Stamps { get; } = new Dictionary<string, StampTemplate>() {
            {"@{body}", (template, stamp, msg) => template.Replace(stamp, msg.Body ?? "")},
            {"@{title}", (template, stamp, msg) => template.Replace(stamp, msg.Subject)},
            {"@{subject}", (template, stamp, msg) => template.Replace(stamp, msg.Subject)},
            {"@{sender}", (template, stamp, msg) => template.Replace(stamp, msg.From?.Address?.ToString())},
            {"@{from}", (template, stamp, msg) => template.Replace(stamp, msg.From?.Address?.ToString())},
            {"@{receiver}", (template, stamp, msg) => template.Replace(stamp, msg.To[0]?.Address?.ToString())},
            {"@{to}", (template, stamp, msg) => template.Replace(stamp, msg.To[0]?.Address?.ToString())},
        };

        /// <summary>
        ///     Stamps all available stamps in the <remarks>Stamps</remarks> into the string template. if not template - returns the string.
        /// </summary>
        public static string Translate(MailMessage message, EmailTemplate etemp) {
            if (!etemp.RequireTranslating) //plain text
                return etemp.Template;

            var template = etemp.Template
                .Trim()
                .TrimStart('\n', '\r')
                .Remove(0, "@Template".Length);

            foreach (var stampkv in Stamps) {
                var stamp = stampkv.Key;
                var act = stampkv.Value;
                template = act(template, stamp, message);
            }
            return template;
        }
    }

    public static class EmailTemplateEx {
        /// <summary>
        ///     Stamps all available stamps in the <remarks>Stamps</remarks> into the string template. if not template - returns the string.
        /// </summary>
        public static string Translate(this MailMessage message, EmailTemplate temp) {
            return EmailTemplate.Translate(message, temp);
        }


    }
}