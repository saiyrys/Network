using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;




// Почитал про паттерны и нашел асинхронную модель TAР, которую и использовал.
// Так же столкнулся с проблемой, что удаленный хост разрывает соединение(независимо какой код использую,исходный или свой. Поэтому должным образом не могу протестировать код.
namespace task_1
{
    internal class Program
    {
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private static async Task ServerAsync(CancellationToken cancellationToken)
        {
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

        private static async Task ClientAsync(string name, string ip, CancellationToken cancellationToken)
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
                while (!cancellationToken.IsCancellationRequested)
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

            var serverTask = Task.Run(() => ServerAsync(cancellationToken));
            Console.Write("Введите ваше имя: ");
            string name = Console.ReadLine();
            Console.Write("Введите IP сервера: ");
            string ip = Console.ReadLine();

            var clientTask = Task.Run(() => ClientAsync(name, ip, cancellationToken));

            await Task.WhenAny(clientTask, serverTask);

            cancellationTokenSource.Cancel();

            await Task.WhenAll(clientTask, serverTask);

            cancellationTokenSource.Dispose();
        }
    }
}
