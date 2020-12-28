using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace worker.demo
{
    class Program
    {
        // simulate getting this value from configuration.
        const bool CONFIG_IS_MGR = true;
        const string CONFIG_CLIENT_ID = "NDS";

        static void Main(string[] args)
        {
            var isManager = args.Length >= 1 ? Convert.ToBoolean(args[0]) : CONFIG_IS_MGR;
            var system = args.Length >= 2 ? args[1] : CONFIG_CLIENT_ID;
            Task program = MainAsync(isManager, system);
            program.Wait();
        }

        static async Task MainAsync(bool isManager, string system) {
            // bring in the manager as an injected dependency so that if/when we move
            // that logic into its own manager application, we can inject it there.
            if (isManager) {
                var serviceProvider = new ServiceCollection()
                .AddSingleton<IWorkManager, WorkManager>()
                .BuildServiceProvider();

                // Assign the work
                var manager = serviceProvider.GetService<IWorkManager>();
                await manager.AssignWork();
            }
            // Do the work assigned to me, and is the client I am configged to work.
            await DoWork(system);
        }

        static async Task DoWork(string system) {
            
            List<int> records = new List<int>();
            try 
            { 
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = "(LocalDb)\\MSSQLLocalDB"; 
                builder.IntegratedSecurity = true;
                builder.InitialCatalog = "WorkerDemo";

                // get the work assigned to this worker
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    string sql = $"SELECT WorkId FROM Work WHERE SystemName = '{system}' AND StartTime IS null";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (await reader.ReadAsync())
                            {
                                records.Add(reader.GetInt32(0));
                            }
                        }
                    }  
                }
         
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();
                    foreach(var record in records) {                        
                        string sql = $"UPDATE Work SET StartTime = '{DateTime.Now}' WHERE SystemName = '{system}' AND WorkId = {record}";

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        // Make 3rd party HTTP call / SMS Work here.

                        // simulate work
                        await Task.Delay(new Random().Next(800, 4500));

                        string sqlEnd = $"UPDATE Work SET EndTime = '{DateTime.Now}', Result = '200' WHERE SystemName = '{system}' AND WorkId = {record}";

                        using (SqlCommand command = new SqlCommand(sqlEnd, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        // here you can add a delay to keep the HTTP traffic moderated.
                        // await Task.Delay(5000);
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
