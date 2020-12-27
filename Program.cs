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
    public static string[] cryptoCoins = { "BTC", "BCH", "ETH", "LTC", "ATOM", "COMP", "BAT", "ALGO", "KNC", "GRT", "BAND" };
    static void Main(string[] args)
    {
      // Logger
      logger.Info("");
      logger.Info("CoinbaseBot exe started - " + String.Format("{0:MMM d, yyyy h:mm tt}", DateTime.Now));

      logger.Info("Loading...");

      string devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

      bool isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                          devEnvironmentVariable.ToLower() == "development";
      //Determines the working environment as IHostingEnvironment is unavailable in a console app

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      IServiceProvider services = ServiceProviderBuilder.GetServiceProvider(args);
      IOptions<SecretKeys> options = services.GetRequiredService<IOptions<SecretKeys>>();

      logger.Info("BTC Address:   " + options.Value.BTCAddress);
      logger.Info("Coinbase Api Key: " + options.Value.CoinbaseApiKey);

      string strHostName = Dns.GetHostName();
      logger.Info("Host name: " + strHostName);
      IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
      IPAddress[] addr = ipEntry.AddressList;

      for (int i = 0; i < addr.Length; i++)
      {
        logger.Info("IP Address {0}: {1} ", i, addr[i].ToString());
      }

      try
      {
        CheckFiatAccounts(options);
        CheckCryptoAccounts(options);
        ToInfinityAndBeyond(options);
      }
      catch (Exception erEx)
      {
        HandleError(options, erEx);
      }




      Console.ReadKey();

    }

    static void ToInfinityAndBeyond(IOptions<SecretKeys> options)
    {
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
      logger.Info("BTC is at USD $" + spotPrice.Result.Data.Amount.ToString("N2"));


    }
    static void CheckFiatAccounts(IOptions<SecretKeys> options)
    {
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;
      //using API Key + Secret authentication
      CoinbaseClient coinbaseClient = new CoinbaseClient(new ApiKeyConfig
      {
        ApiKey = options.Value.CoinbaseApiKey,
        ApiSecret = options.Value.CoinbaseApiSecret,
        UseTimeApi = true
      });

      Task<PagedResponse<PaymentMethod>> paymentMethod = Task.Run(
        () =>
        {
          return coinbaseClient.PaymentMethods.ListPaymentMethodsAsync();
        }, token
        );

      paymentMethod.Wait();
      logger.Info("Check the fiat accounts");
    }
    static void CheckCryptoAccounts(IOptions<SecretKeys> options)
    {
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;
      //using API Key + Secret authentication
      CoinbaseClient coinbaseClient = new CoinbaseClient(new ApiKeyConfig
      {
        ApiKey = options.Value.CoinbaseApiKey,
        ApiSecret = options.Value.CoinbaseApiSecret,
        UseTimeApi = true
      });

      Array.ForEach(cryptoCoins, element =>
      {
        Task<Response<Account>> act = Task.Run(
        () =>
        {
          return coinbaseClient.Accounts.GetAccountAsync(element);
        }, token
        );

        act.Wait();
        logger.Info(element + " Balance is at --- " + act.Result.Data.Balance.Amount.ToString());
      }
      );
    }


    static void SellCrypto(IOptions<SecretKeys> options, CoinbaseClient coinbaseClient, CancellationToken token, decimal sellPrice, decimal coinageCnt, string fiatAddress, string cryptoAddress, string crypto)
    {
      try
      {
        decimal decTotalAmount = sellPrice * coinageCnt;
        string totalAmount = decTotalAmount.ToString();
        PlaceSell createSell = new PlaceSell
        {
          Total = totalAmount,
          Currency = crypto,
          PaymentMethod = fiatAddress,
          Commit = true
        };

        Task<Response<Sell>> sellCoinage = Task.Run(
          () =>
          {
            return coinbaseClient.Sells.PlaceSellOrderAsync(cryptoAddress, createSell);
          }, token
          );

        sellCoinage.Wait();

        string emailBody = coinageCnt + " of " + crypto + " was sold at the unit price of $" + sellPrice.ToString("N2") + ", netting a grand total of " + decTotalAmount.ToString("N2");

        EmailHelper.SendEmail(options.Value.SmtpEmail, options.Value.SmtpPassword, options.Value.SmtpEmail, "CoinbaseBot Sold " + crypto + " Coins", emailBody, options.Value.SmtpHost, options.Value.SmtpPort);
      }
      catch(Exception erEx)
      {
        HandleError(options, erEx);
      }
    }

    static void BuyCrypto(IOptions<SecretKeys> options, CoinbaseClient coinbaseClient, CancellationToken token, decimal dollarDecimal, string fiatAddress, string cryptoAddress, string crypto)
    {
      try
      {
        PlaceBuy createBuy = new PlaceBuy
        {
          Amount = dollarDecimal,
          Currency = "USD",
          PaymentMethod = fiatAddress,
          Commit = true
        };

        Task<Response<Buy>> buyCoinage = Task.Run(
          () =>
          {
            return coinbaseClient.Buys.PlaceBuyOrderAsync(cryptoAddress, createBuy);
          }, token
          );

        buyCoinage.Wait();

        string emailBody = "$" + dollarDecimal.ToString("N2") + " of " + crypto + " was bought!";

        EmailHelper.SendEmail(options.Value.SmtpEmail, options.Value.SmtpPassword, options.Value.SmtpEmail, "CoinbaseBot Sold " + crypto + " Coins", emailBody, options.Value.SmtpHost, options.Value.SmtpPort);
      }
      catch (Exception erEx)
      {
        HandleError(options, erEx);
      }


    }

    static void HandleError(IOptions<SecretKeys> options, Exception erEx)
    {
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




  }
}
