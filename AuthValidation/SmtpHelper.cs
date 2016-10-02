using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using B2BFamily.SmtpValidation.Lib.SmtpConnector;
using B2BFamily.SmtpValidation.Lib.SmtpConnector.B2BFamily.SmtpValidation.Lib.SmtpConnector;

namespace B2BFamily.SmtpValidation.Lib {
    public class SmtpHelper {
        public static async Task<bool> ValidateCredentials(string login, string password, string server, int port, bool enableSsl) {
            SmtpConnectorBase connector;
            if (enableSsl) {
                connector = new SmtpConnectorWithSsl(server, port);
            } else {
                connector = new SmtpConnectorWithoutSsl(server, port);
            }

            if (!await connector.CheckResponse(220)) {
                return false;
            }

            await connector.SendData($"HELO {Dns.GetHostName()}{SmtpConnectorBase.EOF}");
            if (!await connector.CheckResponse(250)) {
                return false;
            }

            await connector.SendData($"AUTH LOGIN{SmtpConnectorBase.EOF}");
            if (!await connector.CheckResponse(334)) {
                return false;
            }

            await connector.SendData(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}")) + SmtpConnectorBase.EOF);
            if (!await connector.CheckResponse(334)) {
                return false;
            }

            await connector.SendData(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{password}")) + SmtpConnectorBase.EOF);
            if (!await connector.CheckResponse(235)) {
                return false;
            }

            return true;
        }
    }
}