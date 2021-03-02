using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Amqp.Sasl;

namespace example {
    class BasicScdsAmqpConsumer
    {
        static int Main(string[] args) {
            const int ERROR_SUCCESS = 0;
            const int ERROR_NO_MESSAGE = 1;
            const int ERROR_OTHER = 2;

            int exitCode = ERROR_SUCCESS;
            Connection connection = null;
            try
            {                
                //connection information from SCDS Subscription Details Page
                string queue = "SCDS_QUEUE_NAME";
                string username = "SCDS_USERNAME";
                string password = "SCDS_PASSWORD";
                string addressString = "SCDS_CONNECTION_URL_HOST";

                bool forever = true;
                int clientTimeout = 3000;
                int initialCredit = 5;
                bool quiet = false;

                Address address = new Address(addressString,5668,username,password,"/","amqps");     

                ConnectionFactory factory = new ConnectionFactory();
                factory.SSL.ClientCertificates.Add(new X509Certificate(@"scds.cert"));  
                factory.SASL.Profile = SaslProfile.External;           
                factory.SSL.RemoteCertificateValidationCallback = ValidateServerCertificate; 
                connection = factory.CreateAsync(address).Result;                
                
                Session session = new Session(connection);                
                ReceiverLink receiver = new ReceiverLink(session, "amqpConsumer", queue);
                
                TimeSpan timeout = TimeSpan.MaxValue;
                if (!forever)
                    timeout = TimeSpan.FromSeconds(clientTimeout);

                Message message = new Message();
                int nReceived = 0;
                receiver.SetCredit(initialCredit);
                while ((message = receiver.Receive(timeout)) != null)
                {
                    nReceived++;
                    if (!quiet)
                    {
                        Console.WriteLine("Message(Properties={0}, ApplicationProperties={1}",
                                      message.Properties, message.ApplicationProperties);
                    }
                    receiver.Accept(message);
                    message.Dispose();
                }
                if (message == null)
                {
                    exitCode = ERROR_NO_MESSAGE;
                }
                receiver.Close();
                session.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception {0}.", e);
                if (null != connection)
                    connection.Close();
                exitCode = ERROR_OTHER;
            }
            return exitCode;
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                return true;
            else
                return false; 
        }
    }
}
