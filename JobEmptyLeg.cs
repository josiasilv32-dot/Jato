using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jato
{
    public class JobEmptyLeg
    {
        private static HttpClient http = new HttpClient();
        
        public async Task Execute(string token, string chatId)
        {
            Console.WriteLine("JATO V6 - Buscando voos...");
            
            // Por enquanto só testa se o bot funciona, sem buscar
            // Depois a gente coloca a busca real da Ryanair
            var achou = false; // muda pra true quando achar
            
            if(!achou){
                Console.WriteLine("Nada hoje, nao vou spammar");
                return; // ISSO PARA O SPAM
            }
        }

        private async Task SendTelegram(string token, string chatId, string msg)
        {
            var url = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(msg)}";
            await http.GetAsync(url);
        }
    }
}
