public async Task Execute(string token, string chatId)
{
    Console.WriteLine("Buscando voos reais...");
    var voos = BuscarVoosBaratos(); // tua logica real aqui
    
    if(voos.Count == 0){
        Console.WriteLine("Nada encontrado hoje, nao vou mandar mensagem");
        return; // PARA DE SPAMMAR
    }
    
    // Só manda quando tem voo novo
    foreach(var v in voos){
        await SendTelegram(token, chatId, $"✈️ {v.Origem}->{v.Destino} {v.Preco}€");
    }
}
