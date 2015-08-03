using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using InfluxDB.Net.Collector.Entities;
using InfluxDB.Net.Collector.Interface;
using InfluxDB.Net.Models;
using Tharga.Toolkit.Console.Command.Base;

namespace InfluxDB.Net.Collector.Console.Commands.Setup
{
    abstract class SetupCommandBase : ActionCommandBase
    {
        private readonly IInfluxDbAgentLoader _influxDbAgentLoader;
        private readonly IConfigBusiness _configBusiness;

        protected SetupCommandBase(string name, string description, IInfluxDbAgentLoader influxDbAgentLoader, IConfigBusiness configBusiness)
            : base(name, description)
        {
            _influxDbAgentLoader = influxDbAgentLoader;
            _configBusiness = configBusiness;
        }

        protected async Task<string> GetServerUrlAsync(string paramList, int index, string defaultUrl)
        {
            var url = defaultUrl;

            IInfluxDbAgent client = null;
            if (!string.IsNullOrEmpty(url))
                client = _influxDbAgentLoader.GetAgent(new DatabaseConfig(url, "root", "qwerty", "qwerty"));

            var connectionConfirmed = false;
            try
            {
                if (client != null)
                    connectionConfirmed = await client.CanConnect();
            }
            catch (Exception exception)
            {
                OutputError(exception.Message);
            }

            if (!connectionConfirmed)
            {
                OutputInformation("Enter the url to the InfluxDB to use.");
                OutputInformation("Provide the correct port, typically 8086. (Ex. http://tharga.net:8086)");
                while (!connectionConfirmed)
                {
                    try
                    {
                        url = QueryParam<string>("Url", GetParam(paramList, index));
                        client = _influxDbAgentLoader.GetAgent(new DatabaseConfig(url, "root", "qwerty", "qwert"));

                        connectionConfirmed = await client.CanConnect();
                    }
                    catch (CommandEscapeException)
                    {
                        return null;
                    }
                    catch (Exception exception)
                    {
                        OutputError(exception.Message.Split('\n')[0]);
                    }
                }

                _configBusiness.SaveDatabaseUrl(url);
            }
            OutputInformation("Connection to server {0} confirmed.", url);
            return url;
        }

        protected async Task<IDatabaseConfig> GetUsernameAsync(string url, string paramList, int index)
        {
            var serie = new Serie.Builder("InfluxDB.Net.Collector").Columns("Machine").Values(Environment.MachineName).Build();
            var dataChanged = false;

            var config = _configBusiness.OpenDatabaseConfig();

            IInfluxDbAgent client;
            InfluxDbApiResponse response = null;
            try
            {
                if (!string.IsNullOrEmpty(config.Name) && !string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                {
                    client = _influxDbAgentLoader.GetAgent(config);
                    response = await client.WriteAsync(TimeUnit.Milliseconds, serie);
                }
            }
            catch (Exception exception)
            {
                OutputError(exception.Message);
            }

            if (response == null || !response.Success)
                OutputInformation("Enter the database, username and password for the InfluxDB.");

            while (response == null || !response.Success)
            {
                var database = QueryParam<string>("DatabaseName", GetParam(paramList, index++));
                var user = QueryParam<string>("Username", GetParam(paramList, index++));
                var password = QueryParam<string>("Password", GetParam(paramList, index++));
                config = new DatabaseConfig(url, user, password, database);

                try
                {
                    client = _influxDbAgentLoader.GetAgent(config);
                    response = await client.WriteAsync(TimeUnit.Milliseconds, serie);
                    dataChanged = true;
                }
                catch (CommandEscapeException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    OutputError(exception.Message);
                }
            }

            OutputInformation("Access to database {0} confirmed.", config.Name);

            if (dataChanged)
                _configBusiness.SaveDatabaseConfig(config.Name, config.Username, config.Password);

            return config;
        }

        protected void StartService()
        {
            var service = new ServiceController("InfluxDB.Net.Collector");
            var serviceControllerStatus = "not found";
            try
            {
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 15));
                }
                serviceControllerStatus = service.Status.ToString();
            }
            catch (Exception exception)
            {
                OutputError(exception.Message);
            }
            finally
            {
                OutputInformation("Service is " + serviceControllerStatus + ".");
            }
        }
    }
}