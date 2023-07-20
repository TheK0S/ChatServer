using ChatClient.Model;
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
        static void Connect()
        {

        }

        static void Disconnect()
        {

        }

        static async Task Main()
        {
            // Задайте IP-адрес и порт, на котором сервер будет прослушивать подключения
            IPAddress ipAddress = IPAddress.Any;
            int port = 8080;

            // Создайте объект Socket и свяжите его с IP-адресом и портом
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(ipAddress, port));
            listener.Listen(10);
            Console.WriteLine($"Сервер запущен. Ожидание подключений на порту {port}...");

            while (true)
            {
                // Принимайте входящие подключения асинхронно
                Socket client = await listener.AcceptAsync();

                // Создайте уникальный идентификатор клиента
                int clientId = ID++;

                // Добавьте клиента в словарь клиентов
                clients.TryAdd(clientId, client);

                // Запустите обработку сообщений от клиента в отдельном потоке
                _ = HandleClientMessagesAsync(clientId);
            }
        }

        static async Task HandleClientMessagesAsync(int clientId)
        {
            Socket client = clients[clientId];

            // Получите поток для чтения и записи данных
            using NetworkStream stream = new NetworkStream(client);

            // Создайте буфер для чтения данных
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    // Преобразуйте полученные данные в строку
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Сообщение от клиента {clientId}: {message}");

                    // Отправьте полученное сообщение всем остальным клиентам
                    BroadcastMessage(message, client);
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
                // Удалите клиента из словаря клиентов
                clients.TryRemove(clientId, out _);

                // Закройте клиентское подключение
                client.Close();

                // Отправьте сообщение о выходе клиента всем остальным клиентам
                string exitMessage = $"Клиент {clientId} покинул чат.";
                BroadcastMessage(exitMessage, client);
            }
        }

        static void BroadcastMessage(string message, Socket clientSocket)
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


        //static async Task Main()
        //{
        //    ServerObject server = new ServerObject();// создаем сервер
        //    await server.ListenAsync(); // запускаем сервер
        //}

        //class ServerObject
        //{
        //    TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
        //    List<ClientObject> clients = new List<ClientObject>(); // все подключения

        //    protected internal void RemoveConnection(string id)
        //    {

        //        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);

        //        if (client != null) clients.Remove(client);
        //        client?.Close();
        //    }
        //    // прослушивание входящих подключений
        //    protected internal async Task ListenAsync()
        //    {
        //        try
        //        {
        //            tcpListener.Start();
        //            Console.WriteLine("Сервер запущен. Ожидание подключений...");

        //            while (true)
        //            {
        //                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

        //                ClientObject clientObject = new ClientObject(tcpClient, this);
        //                clients.Add(clientObject);
        //                Task.Run(clientObject.ProcessAsync);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //        }
        //        finally
        //        {
        //            Disconnect();
        //        }
        //    }

        //    // трансляция сообщения подключенным клиентам
        //    protected internal async Task BroadcastMessageAsync(string message, string id)
        //    {
        //        foreach (var client in clients)
        //        {
        //            if (client.Id != id) // если id клиента не равно id отправителя
        //            {
        //                await client.Writer.WriteLineAsync(message); //передача данных
        //                await client.Writer.FlushAsync();
        //            }
        //        }
        //    }
        //    // отключение всех клиентов
        //    protected internal void Disconnect()
        //    {
        //        foreach (var client in clients)
        //        {
        //            client.Close(); //отключение клиента
        //        }
        //        tcpListener.Stop(); //остановка сервера
        //    }
        //}
        //class ClientObject
        //{
        //    protected internal string Id { get; } = Guid.NewGuid().ToString();
        //    protected internal StreamWriter Writer { get; }
        //    protected internal StreamReader Reader { get; }

        //    TcpClient client;
        //    ServerObject server;

        //    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        //    {
        //        client = tcpClient;
        //        server = serverObject;
        //        // получаем NetworkStream для взаимодействия с сервером
        //        var stream = client.GetStream();
        //        // создаем StreamReader для чтения данных
        //        Reader = new StreamReader(stream);
        //        // создаем StreamWriter для отправки данных
        //        Writer = new StreamWriter(stream);
        //    }

        //    public async Task ProcessAsync()
        //    {
        //        try
        //        {
        //            // получаем имя пользователя
        //            string? userName = await Reader.ReadLineAsync();
        //            string? message = $"{userName} вошел в чат";
        //            // посылаем сообщение о входе в чат всем подключенным пользователям
        //            await server.BroadcastMessageAsync(message, Id);
        //            Console.WriteLine(message);
        //            // в бесконечном цикле получаем сообщения от клиента
        //            while (true)
        //            {
        //                try
        //                {
        //                    message = await Reader.ReadLineAsync();
        //                    if (message == null) continue;
        //                    message = $"{userName}: {message}";
        //                    Console.WriteLine(message);
        //                    await server.BroadcastMessageAsync(message, Id);
        //                }
        //                catch
        //                {
        //                    message = $"{userName} покинул чат";
        //                    Console.WriteLine(message);
        //                    await server.BroadcastMessageAsync(message, Id);
        //                    break;
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine(e.Message);
        //        }
        //        finally
        //        {
        //            // в случае выхода из цикла закрываем ресурсы
        //            server.RemoveConnection(Id);
        //        }
        //    }
        //    // закрытие подключения
        //    protected internal void Close()
        //    {
        //        Writer.Close();
        //        Reader.Close();
        //        client.Close();
        //    }
        //}
    }

}