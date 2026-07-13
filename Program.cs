using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Jato
{
    public class Program
    {
        private static readonly string TOKEN_BOT = "8889136036:AAF5m0LwxmXj6lwgi2WT25WI0f3Ody4zcUA";
        private static readonly long CHAT_ID = 7892530498;
        private static ITelegramBotClient? _bot;
        private static Timer? _timer;

        public static async Task Main(string[] args)
        {
            _bot = new TelegramBotClient(TOKEN_BOT);
            
            // Manda online quando liga
            await _bot.SendTextMessageAsync(
                chatId: CHAT_ID, 
                text: "⚡ *JARVIS JATO V1.2 NUVEM ONLINE*\n\nRadar 24/7 ativo. Te aviso quando cair jato 79€.\n\n_Sem API. Sem PC ligado. Só scrape._", 
                parseMode: ParseMode.Markdown
            );
            
            Console.WriteLine("JARVIS: Radar 24/7 ativo. Stark mode on.");
            
            // RODA AUTOMÁTICO A CADA 30 MINUTOS
            _timer = new Timer(async _ => 
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm}] JARVIS: Varrendo céu...");
                await new JobEmptyLeg().CacarEmptyLeg(_bot!, CHAT_ID);
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            
            // Mantém vivo pra sempre
            await Task.Delay(Timeout.Infinite);
        }
    }
}