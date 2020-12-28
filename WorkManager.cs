using System;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace worker.demo
{
    public class WorkManager: IWorkManager
    {
        public async Task AssignWork() { 
            
            try 
            { 
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = "(LocalDb)\\MSSQLLocalDB"; 
                builder.IntegratedSecurity = true;
                builder.InitialCatalog = "WorkerDemo";
         
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();

                    String sql = $"UPDATE Work SET AssignedWorker = 'WORKER1_MMAAPPDEV01' WHERE SystemName = 'NDS' AND AssignedWorker IS null";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        await command.ExecuteNonQueryAsync();
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