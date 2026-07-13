using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Jato
{
    public class JobEmptyLeg
    {
        private static readonly HttpClient _http = new() { 
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0" } }
        };
        private static string ultimoVoo = "";

        public async Task CacarEmptyLeg(ITelegramBotClient bot, long chatId)
        {
            try
            {
                var emptyLegs = new List<JObject>();
                emptyLegs.AddRange(await BuscarFlapperPT());
                
                if (emptyLegs.Any())
                {
                    var voo = emptyLegs.First();
                    var idVoo = $"{voo["origem"]}{voo["destino"]}{voo["data"]}{voo["hora"]}";
                    
                    // SÓ MANDA SE FOR VOO NOVO
                    if (idVoo != ultimoVoo)
                    {
                        ultimoVoo = idVoo;
                        await bot.SendTextMessageAsync(chatId, $"⚡ JARVIS: VOO FANTASMA NOVO DETECTADO");
                        await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: MontarMsgEmptyLeg(voo),
                            parseMode: ParseMode.Markdown
                        );
                    }
                }
            }
            catch { }
        }

        private async Task<List<JObject>> BuscarFlapperPT()
        {
            var legs = new List<JObject>();
            try
            {
                var url = "https://flyflapper.com/pt-BR";
                var html = await _http.GetStringAsync(url);
                
                // Regex pra pegar preço na home: "R$ 79" ou "79 €"
                var match = Regex.Match(html, @"(Cascais|Lisboa|Porto|Faro).{0,50}(Faro|Porto|Lisboa|Nice).{0,100}(€|R\$)\s*(\d{2,3})");
                
                if (match.Success)
                {
                    var preco = int.Parse(match.Groups[4].Value);
                    if (preco <= 200) // Só manda se for preço fantasma
                    {
                        legs.Add(new JObject
                        {
                            ["origem"] = match.Groups[1].Value + " CAT",
                            ["destino"] = match.Groups[2].Value + " FAO", 
                            ["data"] = DateTime.Now.AddHours(4).ToString("dd/MM"),
                            ["hora"] = DateTime.Now.AddHours(4).ToString("HH:mm"),
                            ["preco"] = preco,
                            ["aviao"] = "Jato Executivo",
                            ["link"] = "https://flyflapper.com/pt-BR/voos-compartilhados",
                            ["fonte"] = "Flapper PT"
                        });
                    }
                }
            }
            catch { }
            return legs;
        }

        private string MontarMsgEmptyLeg(JObject leg)
        {
            var origem = leg["origem"]!.ToString();
            var destino = leg["destino"]!.ToString();
            var data = leg["data"]!.ToString();
            var hora = leg["hora"]!.ToString();
            var aviao = leg["aviao"]!.ToString();
            var link = leg["link"]!.ToString();
            var preco = leg["preco"]!.Value<int>();
            var fonte = leg["fonte"]!.ToString();

            return $"⚡ **EMPTY LEG DETECTADO** ⚡\n\n" +
                   $"**Fonte: {fonte}**\n" +
                   $"🛩️ **{origem} → {destino}**\n" +
                   $"✈️ **Aeronave:** {aviao}\n" +
                   $"💶 **Preço: {preco}€ por assento**\n" +
                   $"📅 **Saída: {data} às {hora}**\n" +
                   $"⚠️ **URGENTE: Vende em minutos**\n\n" +
                   $"[🎫 RESERVAR AGORA]({link})\n\n" +
                   $"_Liga +351 300 509 990 se o link não abrir_";
        }
    }
}