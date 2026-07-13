using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace JarvisJato {
    class Program {
        // ===== CONFIGURAÇÃO =====
        static string TELEGRAM_TOKEN = "8889136036:AAETf7Ow95YDkfUkGyKX1tgO8n4HW2hjLTc";
        static string CHAT_ID = "7892530498";
        
        static string[] ORIGENS = { "LIS", "OPO", "FAO" };
        static string[] DESTINOS = { 
            "MAD", "BCN", "AGP", "PMI", "IBZ", "PAR", "NCE", "LYS", 
            "ROM", "MIL", "VCE", "AMS", "BRU", "BER", "MUC", "VIE", "ZRH", "ATH"
        };
        
        // DATAS FLEXÍVEIS - VARRE PRÓXIMOS 60 DIAS
        static int DIAS_PRA_FRENTE = 60; // Hoje até 60 dias
        static int ESTADIA_MIN = 3; // Fica mínimo 3 dias
        static int ESTADIA_MAX = 14; // Fica máximo 14 dias
        static int PRECO_MAXIMO = 79; // Total ida+volta
        static int INTERVALO_SEGUNDOS = 180; // 3min = seguro
        // =====================================

        static HttpClient http = new HttpClient();
        static int alertasEnviados = 0;
        static HashSet<string> voosJaAlertados = new HashSet<string>();

        static async Task Main() {
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 Chrome/120");
            http.Timeout = TimeSpan.FromSeconds(20);

            var totalRotas = ORIGENS.Length * DESTINOS.Length;
            Console.WriteLine($"JARVIS V5.0 FLEXÍVEL ATIVO");
            Console.WriteLine($"Varrendo {totalRotas} rotas x {DIAS_PRA_FRENTE} dias");
            
            await EnviarTelegram($"⚡ JARVIS SCHENGEN FLEX ONLINE\n🌍 {totalRotas} rotas\n📅 Próximos {DIAS_PRA_FRENTE} dias\n⏱️ Estadia {ESTADIA_MIN}-{ESTADIA_MAX} dias\n💰 Até {PRECO_MAXIMO}€");

            while(true) {
                // GERA TODAS COMBINAÇÕES DE DATAS
                var datas = GerarDatas();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Testando {datas.Count} combinações de datas...");

                foreach(var origem in ORIGENS) {
                    foreach(var destino in DESTINOS) {
                        if(origem == destino) continue;
                        
                        foreach(var (ida, volta) in datas) {
                            await ChecarTodasCias(origem, destino, ida, volta);
                            await Task.Delay(2000); // 2s entre checks pra não floodar
                        }
                    }
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Ciclo completo. Alertas: {alertasEnviados}. Dormindo...");
                await Task.Delay(INTERVALO_SEGUNDOS * 1000);
            }
        }

        static List<(string ida, string volta)> GerarDatas() {
            var lista = new List<(string, string)>();
            var hoje = DateTime.Now.Date.AddDays(2); // Começa daqui 2 dias
            
            for(int i = 0; i < DIAS_PRA_FRENTE; i++) {
                var dataIda = hoje.AddDays(i);
                for(int estadia = ESTADIA_MIN; estadia <= ESTADIA_MAX; estadia++) {
                    var dataVolta = dataIda.AddDays(estadia);
                    lista.Add((dataIda.ToString("yyyy-MM-dd"), dataVolta.ToString("yyyy-MM-dd")));
                }
            }
            return lista;
        }

        static async Task ChecarTodasCias(string org, string dst, string ida, string volta) {
            var tasks = new List<Task> {
                Ryanair(org, dst, ida, volta),
                EasyJet(org, dst, ida, volta),
                Vueling(org, dst, ida, volta),
                WizzAir(org, dst, ida, volta),
                TAP(org, dst, ida, volta),
                Transavia(org, dst, ida, volta),
                Volotea(org, dst, ida, volta),
                Eurowings(org, dst, ida, volta),
                Iberia(org, dst, ida, volta),
                Skyscanner(org, dst, ida, volta)
            };
            await Task.WhenAll(tasks);
        }

        static async Task Ryanair(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.ryanair.com/api/booking/v4/pt-pt/availability?ADT=1&DateIn={volta}&DateOut={ida}&Destination={dst}&Origin={org}&RoundTrip=true";
                var json = await http.GetStringAsync(url);
                var preco = ExtrairPreco(json, @"""total"":{""value"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst, ida, volta)) 
                    await Alerta("RYANAIR", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task EasyJet(string org, string dst, string ida, string volta) {
            try {
                var i = ida.Split('-'); var v = volta.Split('-');
                var url = $"https://www.easyjet.com/en/buy/flights?origin={org}&destination={dst}&departureDay={i[2]}&departureMonth={i[1]}&departureYear={i[0]}&returnDay={v[2]}&returnMonth={v[1]}&returnYear={v[0]}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"totalPrice"":""€(\d+\.?\d*)""");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("EASYJET", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Vueling(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.vueling.com/pt/voos-baratos?origin={org}&destination={dst}&outboundDate={ida}&inboundDate={volta}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"data-price=""(\d+\.?\d*)""");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("VUELING", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task WizzAir(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://wizzair.com/pt-pt/booking/select-flight/{org}/{dst}/{ida}/{volta}/1/0/0";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"amount"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("WIZZ", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task TAP(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.flytap.com/pt-pt/voos?outboundDate={ida}&inboundDate={volta}&origin={org}&destination={dst}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price-amount"">(\d+)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("TAP", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Transavia(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.transavia.com/pt-PT/reservar-um-voo/voos/pesquisar/?origin={org}&destination={dst}&departureDate={ida}&returnDate={volta}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price"":(\d+)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("TRANSAVIA", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Volotea(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.volotea.com/pt/pesquisa-voos/?origin={org}&destination={dst}&departure={ida}&return={volta}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"price"":(\d+\.?\d*)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("VOLOTEA", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Eurowings(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.eurowings.com/pt/pesquisar-voos.html?origin={org}&destination={dst}&departureDate={ida}&returnDate={volta}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"EUR\s*(\d+)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("EUROWINGS", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Iberia(string org, string dst, string ida, string volta) {
            try {
                var url = $"https://www.iberia.com/pt/voos/{org}/{dst}/?dateOut={ida.Replace("-","")}&dateIn={volta.Replace("-","")}";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"totalPrice"":(\d+)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("IBERIA", preco, org, dst, ida, volta);
            } catch { }
        }

        static async Task Skyscanner(string org, string dst, string ida, string volta) {
            try {
                var i = ida.Replace("-", "").Substring(2);
                var v = volta.Replace("-", "").Substring(2);
                var url = $"https://www.skyscanner.pt/transporte/voos/{org}/{dst}/{i}/{v}/";
                var html = await http.GetStringAsync(url);
                var preco = ExtrairPreco(html, @"Price[^€]*€(\d+)");
                if (ValidaPreco(preco, org, dst, ida, volta)) await Alerta("SKYSCANNER", preco, org, dst, ida, volta);
            } catch { }
        }

        static double ExtrairPreco(string texto, string regex) {
            try {
                var match = Regex.Match(texto, regex, RegexOptions.IgnoreCase);
                return match.Success? double.Parse(match.Groups[1].Value.Replace(",", ".")) : 0;
            } catch { return 0; }
        }

        static bool ValidaPreco(double preco, string org, string dst, string ida, string volta) {
            if (preco <= 0 || preco > PRECO_MAXIMO) return false;
            var chave = $"{org}{dst}{ida}{volta}{preco}";
            if (voosJaAlertados.Contains(chave)) return false;
            voosJaAlertados.Add(chave);
            if (voosJaAlertados.Count > 500) voosJaAlertados.Clear();
            return true;
        }

        static async Task Alerta(string cia, double preco, string org, string dst, string ida, string volta) {
            alertasEnviados++;
            var msg = $"🚨 JATO {preco:F0}€ FLEX 🚨\n✈️ {cia}\n🌍 {org}⇄{dst}\n📅 {ida} → {volta}\n\n🔗 RESERVAR AGORA";
            await EnviarTelegram(msg);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ {cia} {org}⇄{dst} {ida}->{volta} {preco}€");
        }

        static async Task EnviarTelegram(string msg) {
            try {
                var url = $"https://api.telegram.org/bot{TELEGRAM_TOKEN}/sendMessage?chat_id={CHAT_ID}&text={Uri.EscapeDataString(msg)}";
                await http.GetAsync(url);
            } catch { }
        }
    }
}
