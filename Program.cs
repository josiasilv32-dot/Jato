using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace JarvisJato {
    class Program {
        // ===== CONFIGURAÇÃO - EDITA AQUI =====
        static string TELEGRAM_TOKEN = "8889136036:AAETf7Ow95YDkfUkGyKX1tgO8n4HW2hjLTc";
        static string CHAT_ID = "7892530498";
        
        // CIDADES SCHENGEN - ADICIONA/REMOVE AS QUE QUISER
        static string[] ORIGENS = { "LIS", "OPO", "FAO" }; // Portugal
        static string[] DESTINOS = { 
            "MAD", "BCN", "AGP", "PMI", "IBZ", // Espanha
            "PAR", "NCE", "LYS", "MRS", // França 
            "ROM", "MIL", "VCE", "NAP", // Itália
            "AMS", "BRU", "VIE", "ZRH", // Holanda/Bélgica/Áustria/Suíça
            "BER", "MUC", "FRA", "HAM", // Alemanha
            "PRG", "BUD", "WAW", "ATH" // Leste Europa/Grécia
        };
        
        static string DATA_IDA = "2026-09-10"; // YYYY-MM-DD
        static string DATA_VOLTA = "2026-09-17"; // YYYY-MM-DD
        static int PRECO_MAXIMO = 79; // Total ida+volta
        static int INTERVALO_SEGUNDOS = 120; // 2min = seguro pra não bloquear
        // =====================================

        static HttpClient http = new HttpClient();
        static int alertasEnviados = 0;
        static List<string> voosJaAlertados = new List<string>();

        static async Task Main() {
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            http.Timeout = TimeSpan.FromSeconds(20);

            var totalRotas = ORIGENS.Length * DESTINOS.Length;
            Console.WriteLine($"JARVIS V4.0 SCHENGEN ATIVO");
            Console.WriteLine($"Varrendo {totalRotas} rotas | {DATA_IDA} → {DATA_VOLTA} | <= {PRECO_MAXIMO}€");
            
            await EnviarTelegram($"⚡ JARVIS SCHENGEN V4.0 ONLINE\n🌍 {totalRotas} rotas\n📅 {DATA_IDA} → {DATA_VOLTA}\n💰 Até {PRECO_MAXIMO}€\n🔍 15 companhias");

            while(true) {
                foreach(var origem in ORIGENS) {
                    foreach(var destino in DESTINOS) {
                        if(origem == destino) continue;
                        
                        var tasks = new List<Task> {
                            Ryanair(origem, destino),
                            EasyJet(origem, destino),
                            Vueling(origem, destino),
                            WizzAir(origem, destino),
                            TAP(origem, destino),
                            Transavia(origem, destino),
                            Volotea(origem, destino),
                            Eurowings(origem, destino),
                            Iberia(origem, destino),
                            AirEuropa(origem, destino),
                            Smartwings(origem, destino),
                            Norwegian(origem, destino),
                            RyanairBuzz(origem, destino),
                            LaudaEurope(origem, destino),
                            Skyscanner(origem, destino)
                        };
                        
                        await Task.WhenAll(tasks);
                        await Task.Delay(3000); // 3s entre rotas pra não floodar
                    }
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Ciclo completo. Alertas: {alertasEnviados}. Dormindo {INTERVALO_SEGUNDOS}s...");
                await Task.Delay(INTERVALO_SEGUNDOS * 1000);
            }
        }

        static async Task Ryanair(string org, string dst) {
            try {
                var url = $"https://www.ryanair.com/api/booking/v4/pt-pt/availability?ADT=1&DateIn={DATA_VOLTA}&DateOut={DATA_IDA}&Destination={dst}&Origin={org}&RoundTrip=true";
                var json = await http.GetStringAsync(url);
                var preco = ExtrairPreco(json, @"""total"":{""value"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst)) await Alerta("RYANAIR", preco, org, dst, url);
            } catch { }
        }

        static async Task EasyJet(string org, string dst) {
            try {
                var url = $"https://www.easyjet.com/en/buy/flights?isOneWay=off&origin={org}&destination={dst}&departureDay={DATA_IDA.Split('-')[2]}&departureMonth={DATA_IDA.Split('-')[1]}&departureYear={DATA_IDA.Split('-')[0]}&returnDay={DATA_VOLTA.Split('-')[2]}&returnMonth={DATA_VOLTA.Split('-')[1]}&returnYear={DATA_VOLTA.Split('-')[0]}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"totalPrice"":""€(\d+\.?\d*)""");
                if (ValidaPreco(preco, org, dst)) await Alerta("EASYJET", preco, org, dst, url);
            } catch { }
        }

        static async Task Vueling(string org, string dst) {
            try {
                var url = $"https://www.vueling.com/pt/voos-baratos?origin={org}&destination={dst}&outboundDate={DATA_IDA}&inboundDate={DATA_VOLTA}&adt=1";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"data-price=""(\d+\.?\d*)""");
                if (ValidaPreco(preco, org, dst)) await Alerta("VUELING", preco, org, dst, url);
            } catch { }
        }

        static async Task WizzAir(string org, string dst) {
            try {
                var url = $"https://wizzair.com/pt-pt/booking/select-flight/{org}/{dst}/{DATA_IDA}/{DATA_VOLTA}/1/0/0";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"amount"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst)) await Alerta("WIZZ AIR", preco, org, dst, url);
            } catch { }
        }

        static async Task TAP(string org, string dst) {
            try {
                var url = $"https://www.flytap.com/pt-pt/voos?outboundDate={DATA_IDA}&inboundDate={DATA_VOLTA}&origin={org}&destination={dst}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price-amount"">(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("TAP", preco, org, dst, url);
            } catch { }
        }

        static async Task Transavia(string org, string dst) {
            try {
                var url = $"https://www.transavia.com/pt-PT/reservar-um-voo/voos/pesquisar/?origin={org}&destination={dst}&departureDate={DATA_IDA}&returnDate={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price"":(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("TRANSAVIA", preco, org, dst, url);
            } catch { }
        }

        static async Task Volotea(string org, string dst) {
            try {
                var url = $"https://www.volotea.com/pt/pesquisa-voos/?origin={org}&destination={dst}&departure={DATA_IDA}&return={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst)) await Alerta("VOLOTEA", preco, org, dst, url);
            } catch { }
        }

        static async Task Eurowings(string org, string dst) {
            try {
                var url = $"https://www.eurowings.com/pt/pesquisar-voos.html?origin={org}&destination={dst}&departureDate={DATA_IDA}&returnDate={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"EUR\s*(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("EUROWINGS", preco, org, dst, url);
            } catch { }
        }

        static async Task Iberia(string org, string dst) {
            try {
                var url = $"https://www.iberia.com/pt/voos/{org}/{dst}/?dateOut={DATA_IDA.Replace("-","")}&dateIn={DATA_VOLTA.Replace("-","")}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"totalPrice"":(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("IBERIA", preco, org, dst, url);
            } catch { }
        }

        static async Task AirEuropa(string org, string dst) {
            try {
                var url = $"https://www.aireuropa.com/pt/voos?origin={org}&destination={dst}&departure={DATA_IDA}&return={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"(\d+),00\s*€");
                if (ValidaPreco(preco, org, dst)) await Alerta("AIR EUROPA", preco, org, dst, url);
            } catch { }
        }

        static async Task Smartwings(string org, string dst) {
            try {
                var url = $"https://www.smartwings.com/pt/voos?from={org}&to={dst}&dep={DATA_IDA}&ret={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price"":(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("SMARTWINGS", preco, org, dst, url);
            } catch { }
        }

        static async Task Norwegian(string org, string dst) {
            try {
                var url = $"https://www.norwegian.com/pt/ipc/availability/roundtrip?D_City={org}&A_City={dst}&D_Day={DATA_IDA.Split('-')[2]}&D_Month={DATA_IDA.Split('-')[1]}{DATA_IDA.Split('-')[0]}&R_Day={DATA_VOLTA.Split('-')[2]}&R_Month={DATA_VOLTA.Split('-')[1]}{DATA_VOLTA.Split('-')[0]}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"TotalPrice"":(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("NORWEGIAN", preco, org, dst, url);
            } catch { }
        }

        static async Task RyanairBuzz(string org, string dst) {
            try {
                var url = $"https://www.buzzair.com/booking/v4/availability?Origin={org}&Destination={dst}&DateOut={DATA_IDA}&DateIn={DATA_VOLTA}";
                var json = await http.GetStringAsync(url);
                var preco = ExtrairPreco(json, @"""value"":(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("BUZZ", preco, org, dst, url);
            } catch { }
        }

        static async Task LaudaEurope(string org, string dst) {
            try {
                var url = $"https://www.lauda.com/pt-pt/booking?org={org}&dst={dst}&out={DATA_IDA}&in={DATA_VOLTA}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"€(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("LAUDA", preco, org, dst, url);
            } catch { }
        }

        static async Task Skyscanner(string org, string dst) {
            try {
                var ida = DATA_IDA.Replace("-", "").Substring(2);
                var volta = DATA_VOLTA.Replace("-", "").Substring(2);
                var url = $"https://www.skyscanner.pt/transporte/voos/{org}/{dst}/{ida}/{volta}/";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"Price[^€]*€(\d+)");
                if (ValidaPreco(preco, org, dst)) await Alerta("SKYSCANNER", preco, org, dst, url);
            } catch { }
        }

        static double ExtrairPreco(string texto, string regex) {
            try {
                var match = Regex.Match(texto, regex, RegexOptions.IgnoreCase);
                return match.Success? double.Parse(match.Groups[1].Value.Replace(",", ".")) : 0;
            } catch { return 0; }
        }

        static bool ValidaPreco(double preco, string org, string dst) {
            if (preco <= 0 || preco > PRECO_MAXIMO) return false;
            var chave = $"{org}{dst}{preco}";
            if (voosJaAlertados.Contains(chave)) return false;
            voosJaAlertados.Add(chave);
            if (voosJaAlertados.Count > 100) voosJaAlertados.RemoveAt(0);
            return true;
        }

        static async Task Alerta(string cia, double preco, string org, string dst, string url) {
            alertasEnviados++;
            var msg = $"🚨 JATO {preco:F0}€ SCHENGEN 🚨\n✈️ {cia}\n🌍 {org}⇄{dst}\n📅 {DATA_IDA} → {DATA_VOLTA}\n\n🔗 RESERVAR:\n{url}";
            await EnviarTelegram(msg);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ {cia} {org}⇄{dst} {preco}€");
        }

        static async Task EnviarTelegram(string msg) {
            try {
                var url = $"https://api.telegram.org/bot{TELEGRAM_TOKEN}/sendMessage?chat_id={CHAT_ID}&text={Uri.EscapeDataString(msg)}&disable_web_page_preview=true";
                await http.GetAsync(url);
            } catch { }
        }
    }
}
