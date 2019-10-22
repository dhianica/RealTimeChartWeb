using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
using Eneter.Messaging.Nodes.Broker;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


// Website Eneter Messaging http://www.eneter.net/ProductDownload.htm
// Must install library Install-Package Eneter.Messaging.Framework -Version 8.0.4 from Nuget

namespace RealTimeChartWeb
{
    // Properties for value Chart
    public class DataUsage
    {
        public float values { get; set; }
    }
    class Program
    {
        const string connectionString = "Server=DESKTOP-DMR2IJB;Database=testing;User Id=sa;Password = sz123;";
        // JavaScript uses JSON serializer so set using JSON.
        static ISerializer aSerializer = new DataContractJsonStringSerializer();

        // Create broker.
        static IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
        static IDuplexBroker aBroker = aBrokerFactory.CreateBroker();

        private static DateTime _refDate = DateTime.Now;

        static void Main(string[] args)
        {
            SqlDependency.Start(connectionString);
            // Communicate using WebSockets.
            IMessagingSystemFactory aMessaging = new WebSocketMessagingSystemFactory();
            IDuplexInputChannel anInputChannel =
               aMessaging.CreateDuplexInputChannel("ws://127.0.0.1:8000/RealTimeChartWeb/");

            anInputChannel.ResponseReceiverConnected += (x, y) =>
            {
                Console.WriteLine("Connected client: " + y.ResponseReceiverId);
            };
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
            {
                Console.WriteLine("Disconnected client: " + y.ResponseReceiverId);
            };

            // Attach input channel and start listeing.
            aBroker.AttachDuplexInputChannel(anInputChannel);

            // Start working thread monitoring the CPU usage.
            bool aStopWorkingThreadFlag = false;
            Thread aWorkingThread = new Thread(() =>
            {
                float usage = 0;
                while (!aStopWorkingThreadFlag)
                {
                    getData(ref _refDate, aSerializer, aBroker);
                    Thread.Sleep(100);
                }
            });
            aWorkingThread.Start();

            Console.WriteLine("RealTimeChartWeb is running press ENTER to stop.");
            Console.ReadLine();

            // Wait until the working thread stops.
            aStopWorkingThreadFlag = true;
            aWorkingThread.Join(3000);

            aBroker.DetachDuplexInputChannel();
            SqlDependency.Stop(connectionString);
            // Detach the input channel and stop listening.
        }
        private static void getData(ref DateTime _refDate, ISerializer aSerializer, IDuplexBroker aBroker)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string cmdText = "SELECT id, usage, created_at FROM RealTimeChartWeb WHERE created_at > @created_at";
                using (SqlCommand command = new SqlCommand(cmdText, connection))
                {
                    command.Parameters.Add("@created_at", SqlDbType.DateTime);
                    command.Parameters["@created_at"].Value = _refDate;

                    command.Notification = null;
                    SqlDependency dependency = new SqlDependency(command);
                    dependency.OnChange += new OnChangeEventHandler(dependency_OnChange);
                    if (connection.State == ConnectionState.Closed) connection.Open();
                    var reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //json = reader[0].ToString();
                            var date = Convert.ToDateTime(reader["created_at"]);

                            if (date > _refDate)
                            {
                                _refDate = date;
                                DataUsage aMessage = new DataUsage();
                                aMessage.values = float.Parse(reader["usage"].ToString());
                                // Serialize the message.
                                object aSerializedMessage = aSerializer.Serialize<DataUsage>(aMessage);

                                // Notify subscribers via the broker.
                                // Note: The broker will forward the message to subscribed clients.
                                aBroker.SendMessage("MyUpdateData", aSerializedMessage);
                            }
                        }
                    }
                    reader.Close();
                }
            }
        }
        private static void dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                getData(ref _refDate, aSerializer, aBroker);
            }
            else
            {
                //Do somthing here
                //Console.WriteLine(e.Type);
            }
        }
    }
}
