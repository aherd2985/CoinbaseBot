using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;

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
      logger.Info("Coinbase exe started - " + String.Format("{0:MMM d, yyyy h:mm tt}", DateTime.Now));

      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Loading...");

      string devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

      bool isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) ||
                          devEnvironmentVariable.ToLower() == "development";
      //Determines the working environment as IHostingEnvironment is unavailable in a console app

      IServiceProvider services = ServiceProviderBuilder.GetServiceProvider(args);
      IOptions<SecretKeys> options = services.GetRequiredService<IOptions<SecretKeys>>();

      Console.WriteLine("OpenOption:   " + options.Value.BTCAddress);
      Console.WriteLine("SecretOption: " + options.Value.CoinbaseApiKey);



      Console.ReadKey();

    }
  }
}
