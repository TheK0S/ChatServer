using ChatClient.Model;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    internal class Program
    {
        static int ID = 1;
        private static ConcurrentDictionary<int, Socket> clients = new ConcurrentDictionary<int, Socket>();

        static async Task Main()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8080;

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(ipAddress, port));
            listener.Listen(10);
            Console.WriteLine($"Сервер запущен. Ожидание подключений на порту {port}...");

            while (true)
            {
                Socket client = await listener.AcceptAsync();

                int clientId = ID++;

                clients.TryAdd(clientId, client);

                _ = HandleClientMessagesAsync(clientId);
            }
        }

        static async Task HandleClientMessagesAsync(int clientId)
        {
            Socket client = clients[clientId];

            using NetworkStream stream = new NetworkStream(client);

            byte[] buffer = new byte[1024];
            int bytesRead;
            string ClientName = "undefined user";

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Сообщение от клиента {clientId}: {message}");

                    Message? msg = JsonConvert.DeserializeObject<Message>(message);

                    if (msg == null) continue;

                    if (msg.Text == "подключился к чату")
                    {
                        ClientName = msg.Ouner;
                        BroadcastMessageAddUser(message , client);                        
                    }                        
                    else                        
                        BroadcastMessage(message);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка при обработке сообщений от клиента. Клиент {clientId} отключен: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке сообщений от клиента {clientId}: {ex.Message}", "Server error");
            }
            finally
            {
                clients.TryRemove(clientId, out _);

                client.Close();

                Message exitMsg = new Message { Ouner = $"Пользователь {ClientName} покинул чат.", Text = string.Empty, Date = DateTime.Now.ToLongTimeString() };
                string jsonMsg = JsonConvert.SerializeObject(exitMsg);

                BroadcastMessage(jsonMsg);
            }
        }

        static void BroadcastMessage(string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients.Values)
            {
                if (client.Connected)
                {
                    client.Send(messageBytes);
                }
            }
        }
        static void BroadcastMessageAddUser(string message, Socket ounerSocket)
        {
            Message? msg = JsonConvert.DeserializeObject<Message>(message);

            if (msg == null) return;

            msg.Ouner = $"Пользователь {msg.Ouner} {msg.Text}";
            msg.Text = string.Empty;

            message = JsonConvert.SerializeObject(msg);

            if(message == null) return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients.Values)
            {
                if (client.Connected && client != ounerSocket)
                {
                    client.Send(messageBytes);
                }
            }
        }
    }
}