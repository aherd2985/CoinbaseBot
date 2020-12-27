using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoinbaseBot
{
  public class ServiceProviderBuilder
  {
    public static IServiceProvider GetServiceProvider(string[] args)
    {
      IConfigurationRoot configuration = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json", true, true)
          .AddEnvironmentVariables()
          .AddUserSecrets(typeof(Program).Assembly)
          .AddCommandLine(args)
          .Build();
      ServiceCollection services = new ServiceCollection();

      services.Configure<SecretKeys>(configuration.GetSection("SecretKeys"));

      var provider = services.BuildServiceProvider();
      return provider;
    }
  }
}
