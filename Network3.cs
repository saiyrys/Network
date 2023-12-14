using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace task_1
{
    internal class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public static async Task ServerAsync(object cancellationTokenObj)
        {
            CancellationToken cancellationToken = (CancellationToken)cancellationTokenObj;

            UdpClient udpServer = new UdpClient(12345);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Enter)
                        {
                            break;
                        }
                    }

                    if (udpServer.Available > 0)
                    {
                        UdpReceiveResult receiveResult = await udpServer.ReceiveAsync();
                        byte[] buffer = receiveResult.Buffer;
                        string data = Encoding.ASCII.GetString(buffer);

                        var messageReception = Message.MessageFromJson(data);

                        Console.WriteLine($"Получено сообщение от {messageReception.NickName}," +
                            $" время получения {messageReception.DateMessage}, ");

                        Console.WriteLine(messageReception.TextMessage);

                        if (messageReception.TextMessage.ToLower() == "exit")
                        {
                            Console.WriteLine("Сервер завершает работу по запросу клиента...");
                            cancellationTokenSource.Cancel();
                            break;
                        }

                        var responseMessage = new Message()
                        {
                            DateMessage = DateTime.Now,
                            NickName = "Сервер",
                            TextMessage = "Сообщение получено"
                        };
                        var responseData = responseMessage.MessageToJson();
                        byte[] responseBytes = Encoding.ASCII.GetBytes(responseData);
                        await udpServer.SendAsync(responseBytes, responseBytes.Length, remoteEndPoint);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                udpServer.Close();
            }
        }

        public static async Task ClientAsync(string name, string ip)
        {
            UdpClient udpClient = new UdpClient();
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), 12345);
            string message = "Привет!";
            var mess = new Message()
            {
                DateMessage = DateTime.Now,
                NickName = name,
                TextMessage = message
            };

            try
            {
                while (true)
                {
                    Console.Write("Введите сообщение (или 'Exit' для выхода): ");
                    string userInput = Console.ReadLine();

                    if (userInput.ToLower() == "exit")
                    {
                        Console.WriteLine("Завершение работы клиента...");
                        break;
                    }

                    mess.TextMessage = userInput;
                    var data = mess.MessageToJson();
                    byte[] bytes = Encoding.ASCII.GetBytes(data);
                    await udpClient.SendAsync(bytes, bytes.Length, remoteEndPoint);

                    Console.WriteLine("Сообщение отправлено!");

                    try
                    {
                        UdpReceiveResult receiveResult = await udpClient.ReceiveAsync();
                        var messageReception = Message.MessageFromJson(Encoding.ASCII.GetString(receiveResult.Buffer));
                        Console.WriteLine($"Получено сообщение от {messageReception.NickName}," +
                            $" время получения {messageReception.DateMessage}, ");
                        Console.WriteLine(messageReception.TextMessage);
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                udpClient.Close();
            }
        }

        static async Task Main()
        {
            var cancellationToken = cancellationTokenSource.Token;
            var serverTask = ServerAsync(cancellationToken);

            Console.Write("Введите ваше имя: ");
            string name = Console.ReadLine();
            Console.Write("Введите IP сервера: ");
            string ip = Console.ReadLine();

            await ClientAsync(name, ip);

            Console.WriteLine("Нажмите Enter, чтобы завершить программу...");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            await serverTask;

            cancellationTokenSource.Dispose();
        }
    }
}
