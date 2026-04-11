namespace BatistaFloramar.Application.Services
{
    public class BibleVerseDto
    {
        public string Texto { get; set; } = "";
        public string Livro { get; set; } = "";
        public string Referencia { get; set; } = "";
    }

    public class BibleService
    {
        // Lista curada de versículos — rotaciona 1 por dia pelo número do dia do ano
        private static readonly BibleVerseDto[] Versiculo = new[]
        {
            new BibleVerseDto { Texto = "Confie no Senhor de todo o seu coração e não se apoie em seu próprio entendimento; reconheça o Senhor em todos os seus caminhos, e ele endireitará as suas sendas.", Referencia = "Provérbios 3:5-6 (NVI)" },
            new BibleVerseDto { Texto = "O Senhor é o meu pastor e nada me faltará.", Referencia = "Salmos 23:1 (ARA)" },
            new BibleVerseDto { Texto = "Pois Deus amou o mundo de tal maneira que deu o seu Filho unigênito, para que todo aquele que nele crê não pereça, mas tenha a vida eterna.", Referencia = "João 3:16 (ARA)" },
            new BibleVerseDto { Texto = "Tudo posso naquele que me fortalece.", Referencia = "Filipenses 4:13 (ARA)" },
            new BibleVerseDto { Texto = "Porque sou eu que conheço os planos que tenho para vocês, diz o Senhor, planos de fazê-los prosperar e não de causar dano, planos de dar a vocês esperança e um futuro.", Referencia = "Jeremias 29:11 (NVI)" },
            new BibleVerseDto { Texto = "O Senhor é a minha luz e a minha salvação; a quem temerei? O Senhor é a força da minha vida; de quem me recearei?", Referencia = "Salmos 27:1 (ARA)" },
            new BibleVerseDto { Texto = "Buscai primeiro o reino de Deus e a sua justiça, e todas essas coisas vos serão acrescentadas.", Referencia = "Mateus 6:33 (ARA)" },
            new BibleVerseDto { Texto = "Não andeis ansiosos por coisa alguma; antes em tudo sejam os vossos pedidos conhecidos diante de Deus pela oração e súplica com ações de graças; e a paz de Deus, que excede todo o entendimento, guardará os vossos corações e as vossas mentes em Cristo Jesus.", Referencia = "Filipenses 4:6-7 (ARA)" },
            new BibleVerseDto { Texto = "Deem graças em todas as circunstâncias, pois esta é a vontade de Deus para vocês em Cristo Jesus.", Referencia = "1 Tessalonicenses 5:18 (NVI)" },
            new BibleVerseDto { Texto = "Sede fortes e corajosos. Não temais, nem vos assusteis diante deles; porque o Senhor, vosso Deus, é quem anda convosco; não vos deixará, nem vos desamparará.", Referencia = "Deuteronômio 31:6 (ARA)" },
            new BibleVerseDto { Texto = "O amor é paciente, o amor é bondoso. Não inveja, não se vangloria, não se orgulha.", Referencia = "1 Coríntios 13:4 (NVI)" },
            new BibleVerseDto { Texto = "Lançando sobre ele toda a vossa ansiedade, porque ele tem cuidado de vós.", Referencia = "1 Pedro 5:7 (ARA)" },
            new BibleVerseDto { Texto = "Eu sou o caminho, a verdade e a vida. Ninguém vem ao Pai a não ser por mim.", Referencia = "João 14:6 (NVI)" },
            new BibleVerseDto { Texto = "O Senhor, teu Deus, está contigo; ele é poderoso para salvar. Ele se deleitará em ti com alegria, renovará o teu amor, se regozijará sobre ti com júbilo.", Referencia = "Sofonias 3:17 (NVI)" },
            new BibleVerseDto { Texto = "Porque dele, e por meio dele, e para ele são todas as coisas. Glória, pois, a ele eternamente.", Referencia = "Romanos 11:36 (ARA)" },
            new BibleVerseDto { Texto = "Alegrai-vos sempre no Senhor! Outra vez digo: alegrai-vos.", Referencia = "Filipenses 4:4 (ARA)" },
            new BibleVerseDto { Texto = "Vinde a mim, todos os que estais cansados e sobrecarregados, e eu vos aliviarei.", Referencia = "Mateus 11:28 (ARA)" },
            new BibleVerseDto { Texto = "Porque a minha força se aperfeiçoa na fraqueza. De boa vontade, portanto, me gloriarei nas minhas fraquezas, para que em mim habite o poder de Cristo.", Referencia = "2 Coríntios 12:9 (ARA)" },
            new BibleVerseDto { Texto = "Cria em mim, ó Deus, um coração puro e renova dentro de mim um espírito correto.", Referencia = "Salmos 51:10 (NVI)" },
            new BibleVerseDto { Texto = "Ensina-me, Senhor, o caminho dos teus preceitos, e eu o observarei até ao fim.", Referencia = "Salmos 119:33 (ARA)" },
            new BibleVerseDto { Texto = "Por isso, não temais; sois de mais valor do que muitos pássaros.", Referencia = "Mateus 10:31 (ARA)" },
            new BibleVerseDto { Texto = "Mas os que esperam no Senhor renovarão as suas forças, subirão com asas como águias, correrão e não se cansarão, caminharão e não se fatigarão.", Referencia = "Isaías 40:31 (ARA)" },
            new BibleVerseDto { Texto = "Sede bons uns para com os outros, compassivos, perdoando-vos mutuamente, assim como Deus em Cristo vos perdoou.", Referencia = "Efésios 4:32 (ARA)" },
            new BibleVerseDto { Texto = "Não tenhas medo, porque eu sou contigo; não te assombres, porque eu sou teu Deus; eu te fortaleço, e te ajudo, e te sustento com a minha destra fiel.", Referencia = "Isaías 41:10 (ARA)" },
            new BibleVerseDto { Texto = "A minha graça te basta, porque o poder se aperfeiçoa na fraqueza.", Referencia = "2 Coríntios 12:9 (NVI)" },
            new BibleVerseDto { Texto = "Deus é o nosso refúgio e força, socorro bem presente na angústia.", Referencia = "Salmos 46:1 (ARA)" },
            new BibleVerseDto { Texto = "Mas, em todas estas coisas, somos mais do que vencedores, por meio daquele que nos amou.", Referencia = "Romanos 8:37 (ARA)" },
            new BibleVerseDto { Texto = "A palavra da tua boca é melhor para mim do que milhares de peças de ouro e prata.", Referencia = "Salmos 119:72 (ARA)" },
            new BibleVerseDto { Texto = "Que o Deus da esperança os encha de toda alegria e paz na fé, para que a esperança de vocês transborde pelo poder do Espírito Santo.", Referencia = "Romanos 15:13 (NVI)" },
            new BibleVerseDto { Texto = "O coração do homem pondera o seu caminho, mas do Senhor é que provém a direção dos seus passos.", Referencia = "Provérbios 16:9 (ARA)" },
            new BibleVerseDto { Texto = "O Senhor abençoe você e te guarde; o Senhor faça resplandecer o seu rosto sobre ti e te conceda graça.", Referencia = "Números 6:24-25 (NVI)" },
        };

        public Task<BibleVerseDto> GetVersiculoDoDiaAsync()
        {
            // Seleciona o versículo pelo dia do ano — muda a cada dia, sem API externa
            var index = DateTime.Today.DayOfYear % Versiculo.Length;
            return Task.FromResult(Versiculo[index]);
        }
    }
}
