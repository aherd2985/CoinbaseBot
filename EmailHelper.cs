using NLog;
using System;
using System.Net;
using System.Net.Mail;

namespace CoinbaseBot
{
  public class EmailHelper
  {
    public static Logger logger = LogManager.GetCurrentClassLogger();
    public static bool SendEmail(string emailStmp, string passwordSmtp, string emailReceiver, string subject, string messageBody, string smtpHost, int smtpPort)
    {
      bool didSend = false;
      try
      {
        using (MailMessage message = new MailMessage())
        {
          message.From = new MailAddress(emailStmp);
          message.To.Add(emailStmp);
          message.Subject = subject;
          message.IsBodyHtml = true;
          message.Body = messageBody;

          using (SmtpClient smtpClient = new SmtpClient())
          {
            smtpClient.UseDefaultCredentials = true;

            smtpClient.Host = smtpHost;
            smtpClient.Port = smtpPort;
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new System.Net.NetworkCredential(emailStmp, passwordSmtp);
            smtpClient.Send(message);
          }
        }

        didSend = true;
      }
      catch(Exception ex)
      {
        logger.Error(ex.Message);

        if(ex.InnerException != null)
          logger.Error(ex.InnerException.ToString());

        if (ex.StackTrace != null)
          logger.Error(ex.StackTrace);

        if (ex.Source != null)
          logger.Error(ex.Source);
      }

      return didSend;
    }
  }
}
