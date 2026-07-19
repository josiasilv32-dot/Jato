using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string token = Environment.GetEnvironmentVariable("BOT_TOKEN");
        string chatId = Environment.GetEnvironmentVariable("CHAT_ID");
        
        if(string.IsNullOrEmpty(token)){
            Console.WriteLine("ERRO: BOT_TOKEN nao configurado nos Secrets");
            return;
        }
        
        var job = new JobEmptyLeg();
        await job.Execute(token, chatId);
    }
}
