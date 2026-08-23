using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ReservationSystem.Utils
{
    public class EmailController
    {
        public EmailController()
        {
        }

        readonly ILog logger = LogManager.GetLogger(typeof(EmailController));

        public void SendReservationConfirmation(string emailTo, List<KeyValuePair<string, TimeSpan>> data, string date)
        {
            var tables = new Dictionary<string,KeyValuePair<TimeSpan, TimeSpan>>();
            foreach (var d in data)
            {
                if(!tables.ContainsKey(d.Key))
                    tables.Add(d.Key, new KeyValuePair<TimeSpan, TimeSpan>(d.Value, new TimeSpan(0,0,0,0)));
                else
                {
                    var times = tables[d.Key];
                    if (times.Key > d.Value)
                    {
                        var finishTime = times.Key < times.Value ? times.Value : times.Key;

                        tables[d.Key] = new KeyValuePair<TimeSpan, TimeSpan>(d.Value, finishTime);
                    }
                    else if(times.Value < d.Value)
                    {
                        tables[d.Key] = new KeyValuePair<TimeSpan, TimeSpan>(times.Key, d.Value);
                    }
                }
               
            }

            var sb = new StringBuilder();
            foreach (var table in tables)
            {
              sb.Append(string.Format(Resource.ReservationEmailConfirmation, date, table.Value.Key.ToString() + "-" + table.Value.Value.Add(new TimeSpan(0,0,30,0)).ToString(), table.Key));
            }

            var body = sb.ToString();
            var subject = Resource.ReservationEmailConfirmationSubject;

            SendEmail(emailTo, body, subject);
        }

        public void SendResetPasswordEmail(string url, string email)
        {
            var body = string.Format(Resource.ResetPassword, url);
            var subject = Resource.ResetPasswordSubject;

            SendEmail(email, body, subject);
        }

        public void SendRegisterEmail(string url, string email)
        {
            var body = string.Format(Resource.RegistrationEmail, url);
            var subject = Resource.RegistrationEmailSubject;

            SendEmail(email, body, subject);
        }

        private void SendEmail(string mailTo, string body, string subject)
        {
            // SMTP settings default to the original Gmail account but can be overridden in
            // Web.config <appSettings> without a code change - handy for dropping in a valid
            // Gmail App Password (Google no longer accepts a plain account password over SMTP).
            var host = ConfigurationManager.AppSettings["smtpHost"] ?? "smtp.gmail.com";
            var fromAddress = ConfigurationManager.AppSettings["smtpFrom"] ?? "sparta.rezervace@gmail.com";
            var userName = ConfigurationManager.AppSettings["smtpUser"] ?? fromAddress;
            var password = ConfigurationManager.AppSettings["smtpPassword"] ?? string.Empty;

            int port;
            if (!int.TryParse(ConfigurationManager.AppSettings["smtpPort"], out port))
                port = 587;

            bool enableSsl;
            if (!bool.TryParse(ConfigurationManager.AppSettings["smtpEnableSsl"], out enableSsl))
                enableSsl = true;

            if (string.IsNullOrEmpty(password))
                logger.Warn("SMTP password is not configured - set 'smtpPassword' in Web.config <appSettings> (a Gmail App Password).");

            try
            {
                using (var mail = new MailMessage(fromAddress, mailTo)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                using (var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Timeout = 10000,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(userName, password)
                })
                {
                    client.Send(mail);
                }

                logger.Info("Email sent to " + mailTo + " (subject: " + subject + ")");
            }
            catch (Exception ex)
            {
                // Deliberately swallowed: a failed confirmation email must not roll back a
                // reservation or break the reset flow. Delivery problems are visible in the log.
                logger.Error("Email was not sent to " + mailTo + " due to error: " + ex);
            }
        }
    }
}