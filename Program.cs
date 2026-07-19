using System;
using System.Threading.Tasks;

namespace Jato
{
    class Program
    {
        static async Task Main()
        {
            string token = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "";
            string chatId = Environment.GetEnvironmentVariable("CHAT_ID") ?? "";
            
            if(string.IsNullOrEmpty(token)){
                Console.WriteLine("ERRO: BOT_TOKEN nao configurado");
                return;
            }
            
            var job = new JobEmptyLeg();
            await job.Execute(token, chatId);
            Console.WriteLine("Fim OK");
        }
    }
}
