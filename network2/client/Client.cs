
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Client başladılır...");

            var tcpClient = new TcpClient();
            var ip = IPAddress.Parse("192.168.56.1");
            var port = 50001;

            var endpoint = new IPEndPoint(ip, port);

            try
            {
                tcpClient.Connect(endpoint);

                if (tcpClient.Connected)
                {
                    var networkStream = tcpClient.GetStream();
                    var writer = new BinaryWriter(networkStream, Encoding.UTF8);
                    var reader = new BinaryReader(networkStream, Encoding.UTF8);

                    Console.Write("Username daxil edin: ");
                    string? username = Console.ReadLine();

                    Console.Write("Password daxil edin: ");
                    string? password = Console.ReadLine();

                    writer.Write(username);
                    writer.Write(password);

                    string serverResponse = reader.ReadString();

                    if (serverResponse.StartsWith("REJECTED"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Giriş alınmadı: " + serverResponse);
                        Console.ResetColor();
                        Thread.Sleep(3000);
                        return;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Sistemə giriş uğurludur! Çıxmaq üçün EXIT yazın.");
                    Console.ResetColor();

                    var sendTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            var message = Console.ReadLine();
                            writer.Write(message ?? "no message");

                            if (message?.ToUpper() == "EXIT")
                            {
                                break;
                            }
                        }
                    });

                    var receiveTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                var receivedMessage = reader.ReadString();

                                if (!string.IsNullOrEmpty(receivedMessage))
                                {
                                    Console.WriteLine($"SERVERDƏN: {receivedMessage}");
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
                    });

                    Task.WaitAll(sendTask, receiveTask);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("Bağlantı xətası: " + error.Message);
            }
            finally
            {
                tcpClient.Close();
            }
        }
    }
}
