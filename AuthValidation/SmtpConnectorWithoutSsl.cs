using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using nucs.shared.Network;

namespace B2BFamily.SmtpValidation.Lib.SmtpConnector {
    internal abstract class SmtpConnectorBase {
        protected string SmtpServerAddress { get; set; }
        protected int Port { get; set; }
        public const string EOF = "\r\n";

        protected SmtpConnectorBase(string smtpServerAddress, int port) {
            SmtpServerAddress = smtpServerAddress;
            Port = port;
        }

        public abstract Task<bool> CheckResponse(int expectedCode);
        public abstract Task SendData(string data);
    }
}

namespace B2BFamily.SmtpValidation.Lib.SmtpConnector {
    internal class SmtpConnectorWithoutSsl : SmtpConnectorBase {
        private Socket _socket = null;
        private const int MAX_ATTEMPTS_COUNT = 100;

        public SmtpConnectorWithoutSsl(string smtpServerAddress, int port) : base(smtpServerAddress, port) {
            try {
                IPHostEntry hostEntry = Dns.GetHostEntry(smtpServerAddress);
                IPEndPoint endPoint = new IPEndPoint(hostEntry.AddressList[0], port);
                _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //try to connect and test the rsponse for code 220 = success
                _socket.Connect(endPoint);
            } catch (Exception) {
                _socket = null;
            }
        }

        ~SmtpConnectorWithoutSsl() {
            try {
                if (_socket != null) {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            } catch (Exception) {
                ;
            }
        }

        public override async Task<bool> CheckResponse(int expectedCode) {
            await Task.Yield();
            if (_socket == null) {
                return false;
            }
            var currentAttemptIndex = 1;
            while (_socket.Available == 0) {
                System.Threading.Thread.Sleep(100);
                if (currentAttemptIndex++ > MAX_ATTEMPTS_COUNT) {
                    return false;
                }
            }
            byte[] responseArray = new byte[1024];
            
            _socket.Receive(responseArray, 0, _socket.Available, SocketFlags.None);
            string responseData = Encoding.UTF8.GetString(responseArray);
            int responseCode = Convert.ToInt32(responseData.Substring(0, 3));
            if (responseCode == expectedCode) {
                return true;
            }
            return false;
        }

        public override async Task SendData(string data) {
            await Task.Yield();
            if (_socket == null) {
                return;
            }
            byte[] dataArray = Encoding.UTF8.GetBytes(data);
            _socket.Send(dataArray, 0, dataArray.Length, SocketFlags.None);
        }
    }

    namespace B2BFamily.SmtpValidation.Lib.SmtpConnector {
        internal class SmtpConnectorWithSsl : SmtpConnectorBase {
            private SslStream _sslStream = null;
            private TcpClient _client = null;

            /// <summary>
            /// Таймаут подклчюения в секундах
            /// </summary>
            private const byte CONNECT_TIMEOUT = 2;

            private IAsyncResult _connectionResult = null;

            public SmtpConnectorWithSsl(string smtpServerAddress, int port) : base(smtpServerAddress, port) {
                try {
                    _client = new TcpClient();
                    _connectionResult = _client.BeginConnect(smtpServerAddress, port, null, null);

                    var success = _connectionResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(CONNECT_TIMEOUT));

                    if (!success) {
                        _client = null;
                        _sslStream = null;
                        return;
                    }

                    _sslStream = new SslStream(
                        _client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate),
                        null
                    );
                    // The server name must match the name on the server certificate.
                } catch {
                    //Timeout excepted
                    _client = null;
                    _sslStream = null;
                    return;
                }
                try {
                    _sslStream.AuthenticateAsClient(smtpServerAddress);
                } catch (AuthenticationException e) {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null) {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    _client.Close();

                    _client = null;
                    _sslStream = null;
                }
            }

            ~SmtpConnectorWithSsl() {
                try {
                    if (_sslStream != null) {
                        _sslStream.Close();
                        _sslStream.Dispose();
                        _sslStream = null;
                    }
                } catch (Exception) {
                    ;
                }

                try {
                    if (_client != null) {
                        // we have connected
                        _client.EndConnect(_connectionResult);
                        _client.Close();
                        _client = null;
                    }
                } catch (Exception) {
                    ;
                }
            }

            // The following method is invoked by the RemoteCertificateValidationDelegate.
            private static bool ValidateServerCertificate(
                object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors) {
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

                // Do not allow this client to communicate with unauthenticated servers.
                return false;
            }

            public override async Task<bool> CheckResponse(int expectedCode) {
                await Task.Yield();
                if (_sslStream == null) {
                    return false;
                }
                var message = ReadMessageFromStream(_sslStream);
                int responseCode = Convert.ToInt32(message.Substring(0, 3));
                if (responseCode == expectedCode) {
                    return true;
                }
                return false;
            }

            public override async Task SendData(string data) {
                await Task.Yield();
                if (_client == null || _sslStream == null) {
                    return;
                }
                byte[] messsage = Encoding.UTF8.GetBytes(data);
                // Send hello message to the server. 
                _sslStream.Write(messsage);
                _sslStream.Flush();
            }

            private string ReadMessageFromStream(SslStream stream) {
                byte[] buffer = new byte[2048];
                StringBuilder messageData = new StringBuilder();
                int bytes = -1;
                do {
                    bytes = stream.Read(buffer, 0, buffer.Length);

                    // Use Decoder class to convert from bytes to UTF8
                    // in case a character spans two buffers.
                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);
                    // Check for EOF.
                    if (messageData.ToString().IndexOf(EOF) != -1) {
                        break;
                    }
                } while (bytes != 0);

                return messageData.ToString();
            }
        }
    }
}