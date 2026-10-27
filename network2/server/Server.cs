
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Network2_Server
{
    public class User
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? RemoteEndpoint { get; set; }
        public bool IsOnline { get; set; }
    }

    public class Program
    {
        static TcpListener serverListener = null;
        static List<TcpClient> clients = new List<TcpClient>();
        static string filePath = "users.json";
        static object lockObject = new object();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Server başladılır...");

            var ip = IPAddress.Parse("192.168.56.1");
            var port = 50001;

            var endpoint = new IPEndPoint(ip, port);
            serverListener = new TcpListener(endpoint);
            serverListener.Start();

            Console.WriteLine($"Server aktivdir: {serverListener.LocalEndpoint}");

            while (true)
            {
                var client = serverListener.AcceptTcpClient();
                clients.Add(client);

                Console.WriteLine($"{client.Client.RemoteEndPoint} serverə qoşuldu.");

                Task.Run(() =>
                {
                    string currentUsername = null;
                    string remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "";

                    try
                    {
                        var networkStream = client.GetStream();
                        var reader = new BinaryReader(networkStream, Encoding.UTF8);
                        var writer = new BinaryWriter(networkStream, Encoding.UTF8);

                        string username = reader.ReadString();
                        string password = reader.ReadString();

                        lock (lockObject)
                        {
                            var userList = LoadUsers();
                            var foundUser = userList.FirstOrDefault(
                                u => u.Username == username && u.Password == password
                            );

                            if (foundUser == null)
                            {
                                writer.Write("REJECTED: Yanlış istifadəçi adı və ya şifrə!");
                                client.Close();
                                clients.Remove(client);
                                return;
                            }

                            if (foundUser.IsOnline)
                            {
                                writer.Write($"REJECTED: Bu istifadəçi artıq online-dır! (IP: {foundUser.RemoteEndpoint})");
                                client.Close();
                                clients.Remove(client);
                                return;
                            }

                            foundUser.IsOnline = true;
                            foundUser.RemoteEndpoint = remoteEndpoint;
                            SaveUsers(userList);
                            currentUsername = username;

                            writer.Write("SUCCESS");

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[LOGIN] {currentUsername} serverə daxil oldu. IP: {remoteEndpoint}");
                            Console.ResetColor();
                        }

                        while (true)
                        {
                            if (networkStream.DataAvailable)
                            {
                                var message = reader.ReadString();

                                if (message == "EXIT")
                                {
                                    break;
                                }

                                Console.WriteLine($"İstifadəçi {currentUsername}: {message}");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                    }
                    finally
                    {
                        if (currentUsername != null)
                        {
                            lock (lockObject)
                            {
                                var userList = LoadUsers();
                                var foundUser = userList.FirstOrDefault(
                                    u => u.Username == currentUsername
                                );

                                if (foundUser != null)
                                {
                                    foundUser.IsOnline = false;
                                    SaveUsers(userList);
                                }
                            }

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[LOGOUT] {currentUsername} serverdən ayrıldı. IP: {remoteEndpoint}");
                            Console.ResetColor();
                        }

                        clients.Remove(client);
                        client.Close();
                    }
                });
            }
        }

        static List<User> LoadUsers()
        {
            if (!File.Exists(filePath))
                return new List<User>();

            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<List<User>>(json)
                   ?? new List<User>();
        }

        static void SaveUsers(List<User> users)
        {
            string json = JsonSerializer.Serialize(
                users,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(filePath, json);
        }
    }
}
