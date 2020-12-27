using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;
using Coinbase;
using Coinbase.Models;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoinbaseBot
{
  class Program
  {
    public static Logger logger = LogManager.GetCurrentClassLogger();
    public static IConfigurationRoot Configuration { get; set; }
    static void Main(string[] args)
    {
      // Logger
      logger.Info("");
      logger.Info("CoinbaseBot exe started - " + String.Format("{0:MMM d, yyyy h:mm tt}", DateTime.Now));

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Loading...");

      string devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

      bool isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                          devEnvironmentVariable.ToLower() == "development";
      //Determines the working environment as IHostingEnvironment is unavailable in a console app

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      IServiceProvider services = ServiceProviderBuilder.GetServiceProvider(args);
      IOptions<SecretKeys> options = services.GetRequiredService<IOptions<SecretKeys>>();

      Console.WriteLine("BTC Address:   " + options.Value.BTCAddress);
      Console.WriteLine("Coinbase Api Key: " + options.Value.CoinbaseApiKey);

      string strHostName = Dns.GetHostName();
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("Host name: " + strHostName);
      IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
      IPAddress[] addr = ipEntry.AddressList;
      Console.ForegroundColor = ConsoleColor.Cyan;

      for (int i = 0; i < addr.Length; i++)
      {
        Console.WriteLine("IP Address {0}: {1} ", i, addr[i].ToString());
      }

      try
      {
        ToInfinityAndBeyond(options);
      }
      catch (Exception erEx)
      {
        //send email about failure
        string emailBody = erEx.Message;
        logger.Error(erEx.Message);

        if (erEx.InnerException != null)
        {
          logger.Error(erEx.InnerException.ToString());
          emailBody += "<br /><br />" + erEx.InnerException.ToString();
        }

        if (erEx.StackTrace != null)
        {
          logger.Error(erEx.StackTrace);
          emailBody += "<br /><br />" + erEx.StackTrace;
        }

        if (erEx.Source != null)
        {
          logger.Error(erEx.Source);
          emailBody += "<br /><br />" + erEx.Source;
        }

        EmailHelper.SendEmail(options.Value.SmtpEmail, options.Value.SmtpPassword, options.Value.SmtpEmail, "CoinbaseBot Error Message", emailBody, options.Value.SmtpHost, options.Value.SmtpPort);
      }




      Console.ReadKey();

    }

    static void ToInfinityAndBeyond(IOptions<SecretKeys> options)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      //using API Key + Secret authentication
      CoinbaseClient coinbaseClient = new CoinbaseClient(new ApiKeyConfig
      {
        ApiKey = options.Value.CoinbaseApiKey,
        ApiSecret = options.Value.CoinbaseApiSecret,
        UseTimeApi = true
      });

      //No authentication
      //  - Useful only for Data Endpoints that don't require authentication.
      //client = new CoinbaseClient();
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;

      Task<Response<Money>> spotPrice = Task.Run(
        () =>
        {
          return coinbaseClient.Data.GetSpotPriceAsync("BTC-USD");
        }, token
        );

      spotPrice.Wait();
      Console.WriteLine("BTC is at USD $" + spotPrice.Result.Data.Amount.ToString("N2"));


    }


  }
}
