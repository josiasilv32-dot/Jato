using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

class Program {
    static string TELEGRAM_TOKEN = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN")!;
    static string CHAT_ID = Environment.GetEnvironmentVariable("CHAT_ID")!;
    
    static string[] ORIGENS = { "LIS", "OPO", "FAO" };
    static string[] DESTINOS = { "MAD", "BCN", "AGP", "PMI", "PAR", "ROM", "MIL", "AMS", "BRU", "BER" }; // reduzi pra 10 pra testar
    static int PRECO_MAXIMO = 79;
    static HttpClient http = new HttpClient();
    static HashSet<string> jaEnviados = new HashSet<string>();

    static async Task Main() {
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        Console.WriteLine("JARVIS V6 - MODO GITHUB ACTIONS (sem spam)");
        
        // Carrega cache pra nao repetir
        if(File.Exists("cache_voos.txt")) 
            jaEnviados = new HashSet<string>(File.ReadAllLines("cache_voos.txt"));

        var hoje = DateTime.Now.AddDays(3);
        int achados = 0;

        // Só 1 passagem, sem while(true) - GitHub Actions que vai chamar de novo
        foreach(var org in ORIGENS) {
            foreach(var dst in DESTINOS) {
                if(org==dst) continue;
                // Só testa 7 dias pra frente, nao 60x14
                for(int d=0; d<7; d++) {
                    var ida = hoje.AddDays(d).ToString("yyyy-MM-dd");
                    var volta = hoje.AddDays(d+3).ToString("yyyy-MM-dd");
                    if(await RyanairReal(org, dst, ida, volta)) achados++;
                    await Task.Delay(1200);
                }
            }
        }
        Console.WriteLine($"Fim. Achados: {achados}. Se 0, nao manda nada (sem spam).");
        File.WriteAllLines("cache_voos.txt", jaEnviados);
    }

    static async Task<bool> RyanairReal(string org, string dst, string ida, string volta) {
        try {
            var url = $"https://www.ryanair.com/api/booking/v4/pt-pt/availability?ADT=1&DateIn={volta}&DateOut={ida}&Destination={dst}&Origin={org}&RoundTrip=true&Disc=0&INF=0&TEEN=0&CHD=0&IncludeConnectingFlights=false&FlexDaysBeforeOut=2&FlexDaysOut=2&FlexDaysBeforeIn=2&FlexDaysIn=2&ToUs=AGREED";
            var json = await http.GetStringAsync(url);
            // Regex certo da API Ryanair
            var m = Regex.Match(json, @"""amount"":(\d+\.?\d*)");
            if(!m.Success) return false;
            double preco = double.Parse(m.Groups[1].Value.Replace(",","."));
            if(preco > 0 && preco <= PRECO_MAXIMO) {
                var chave = $"{org}{dst}{ida}{volta}{preco}";
                if(jaEnviados.Contains(chave)) return false;
                jaEnviados.Add(chave);
                await EnviarTelegram($"🚨 JATO REAL {preco:F0}€\n✈ RYANAIR\n🌍 {org} ⇄ {dst}\n📅 {ida} → {volta}\n🔗 https://www.ryanair.com/flights/pt/pt/voos-de-{org.ToLower()}-para-{dst.ToLower()}");
                return true;
            }
        } catch(Exception ex) { Console.WriteLine($"Erro {org}-{dst}: {ex.Message}"); }
        return false;
    }

    static async Task EnviarTelegram(string msg) {
        try {
            var url = $"https://api.telegram.org/bot{TELEGRAM_TOKEN}/sendMessage?chat_id={CHAT_ID}&text={Uri.EscapeDataString(msg)}";
            await http.GetAsync(url);
            Console.WriteLine($"ENVIADO: {msg.Substring(0,30)}");
        } catch {}
    }
}
