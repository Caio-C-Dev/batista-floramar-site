using BatistaFloramar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task InicializarAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BatistaFloramarDbContext>();

            await SeedCredenciaisAsync(db);
            await SeedCelulasAsync(db);
            await SeedMinisteriosAsync(db);
            await SeedPodcastsAsync(db);
            await SeedMinisterioFotosAsync(db);
            await SeedEventosSemanaisAsync(db);
        }

        private static async Task SeedCredenciaisAsync(BatistaFloramarDbContext db)
        {
            if (await db.AdminCredenciais.AnyAsync()) return;

            db.AdminCredenciais.Add(new AdminCredencial
            {
                Usuario = "pastor",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("Floramar@2026")
            });
            await db.SaveChangesAsync();
        }

        private static async Task SeedCelulasAsync(BatistaFloramarDbContext db)
        {
            if (await db.Celulas.AnyAsync()) return;

            db.Celulas.AddRange(
                new Celula { Nome = "Redenção", DiaSemana = "Terça", Lideres = "Leonardo", Endereco = "Rua José Lima de Almeida, 59 - Na igreja", Contato = "(31) 99807-3970", Horario = "20h", ImagemUrl = "Celulas/CelulaRedencao.jpeg", Ordem = 1 },
                new Celula { Nome = "Online", DiaSemana = "Terça", Lideres = "Elaine", Contato = "(31) 99264-6428", Horario = "20h", Descricao = "Visa atender os que não podem estar presencialmente.", ImagemUrl = "Celulas/CelulaPeregrinos.jpeg", Ordem = 2 },
                new Celula { Nome = "Deus Restaura", DiaSemana = "Terça", Lideres = "Joelson", Endereco = "Rua José Lima de Almeida, 59 - Na igreja", Contato = "(31) 98646-5568", Horario = "20h", ImagemUrl = "Celulas/CelulaPeregrinos.jpeg", Ordem = 3 },
                new Celula { Nome = "Atos 2", DiaSemana = "Sexta", Lideres = "Luciano", Endereco = "Rua Laura Soares Guimarães, 158 - Jardim Guanabara", Contato = "(31) 98334-6389", Horario = "20h", ImagemUrl = "Celulas/CelulaAtos.jpeg", Ordem = 1 },
                new Celula { Nome = "Peregrinos", DiaSemana = "Sexta", Lideres = "Cleiton e Josi", Endereco = "Rua Bastão do Imperador, 146 - Jaqueline", Contato = "(31) 97117-7636", Horario = "20h", ImagemUrl = "Celulas/CelulaPeregrinos.jpeg", Ordem = 2 },
                new Celula { Nome = "Shekinah", DiaSemana = "Sexta", Lideres = "Wanderson", Endereco = "Rua Roberto Carlos de Almeida Cunha, 239 - Floramar", Contato = "(31) 98735-5907", Horario = "20h", ImagemUrl = "Celulas/CelulaPeregrinos.jpeg", Ordem = 3 },
                new Celula { Nome = "Reviver", DiaSemana = "Sexta", Lideres = "Claudiney e Marcela", Endereco = "Rua José Lima de Almeida, 70B - Floramar", Contato = "(31) 98829-4674", Horario = "20h", ImagemUrl = "Celulas/CelulaReviver.jpeg", Ordem = 4 },
                new Celula { Nome = "Sedentos", DiaSemana = "Sexta", Lideres = "Samuel e Débora", Endereco = "Rua Jaci Nogueira, 15 - Floramar", Contato = "(31) 98479-4221", Horario = "20h30", ImagemUrl = "Celulas/CelulaPeregrinos.jpeg", Ordem = 5 },
                new Celula { Nome = "Culto de Jovens e Adolescentes", DiaSemana = "Sexta", Lideres = "Wellington", Endereco = "Rua José Lima de Almeida, 59 - Na igreja", Contato = "(31) 98533-9161", Horario = "20h", ImagemUrl = "Celulas/CultoDosJovens.jpeg", Ordem = 6 },
                new Celula { Nome = "Futebol", DiaSemana = "Sábado", Endereco = "Rua Gastão da Costa Pinheiro, 257 — Floramar", Horario = "17h30", ImagemUrl = "Celulas/Futebol.jpeg", Ordem = 1 }
            );
            await db.SaveChangesAsync();
        }

        private static async Task SeedMinisteriosAsync(BatistaFloramarDbContext db)
        {
            // Remove duplicatas criadas via admin (slug diferente do canônico)
            var canonicalJovens = await db.Ministerios.FirstOrDefaultAsync(m => m.Slug == "jovens");
            if (canonicalJovens != null)
            {
                var duplicatas = await db.Ministerios
                    .Where(m => m.Id != canonicalJovens.Id && m.Nome.Contains("Jovens"))
                    .ToListAsync();
                if (duplicatas.Any()) { db.Ministerios.RemoveRange(duplicatas); await db.SaveChangesAsync(); }
            }

            var canonicalLibras = await db.Ministerios.FirstOrDefaultAsync(m => m.Slug == "libras");
            if (canonicalLibras != null)
            {
                var duplicatas = await db.Ministerios
                    .Where(m => m.Id != canonicalLibras.Id && (m.Nome.Contains("Surdo") || m.Nome.Contains("Libras")))
                    .ToListAsync();
                if (duplicatas.Any()) { db.Ministerios.RemoveRange(duplicatas); await db.SaveChangesAsync(); }
            }

            // Upsert por slug — seguro para re-executar
            var ministeriosData = new[]
            {
                new Ministerio {
                    Nome = "Jovens e Adolescentes",
                    Slug = "jovens",
                    ResumoBreve = "Culto de Jovens e Adolescentes e ações de impacto para a juventude.",
                    Descricao = "O Culto de Jovens e Adolescentes da Comunidade Batista Floramar é um espaço vibrante de crescimento espiritual, conexão e propósito. Toda semana os jovens e adolescentes se reúnem para louvar, estudar a Palavra e viver ações de impacto social na comunidade. Este ministério existe para que cada jovem descubra seu chamado e sirva com seus dons.",
                    Lideranca = "Wellington e Mônica",
                    WhatsApp = "5531985339161",
                    Icone = "fas fa-bolt",
                    Ordem = 1
                },
                new Ministerio { Nome = "Kids", Slug = "kids", ResumoBreve = "Cuidando das crianças e bebês com amor enquanto os pais participam do culto.", Descricao = "O Ministério Kids cuida das crianças e bebês da comunidade durante os cultos dominicais, para que os pais possam participar plenamente do momento de adoração. Nossas professoras voluntárias são comprometidas, treinadas e cheias de amor — garantindo um ambiente seguro, divertido e repleto da Palavra de Deus para os pequenos.", Lideranca = "Poliana", Icone = "fas fa-child", Ordem = 2 },
                new Ministerio { Nome = "Louvor", Slug = "louvor", ResumoBreve = "Liderando a congregação na adoração através da música.", Descricao = "O Ministério de Louvor da Comunidade Batista Floramar existe para conduzir a congregação à presença de Deus através da música e da adoração. Em todos os cultos e eventos da igreja, nossa equipe de músicos e vocalistas se dedica a exaltar o nome do Senhor com excelência e unção, criando um ambiente de encontro genuíno com Deus.", Lideranca = "Débora e Samuel", FotoLider = "/images/ministerios/lideres/LiderLouvor.jpeg", Icone = "fas fa-music", Ordem = 3 },
                new Ministerio { Nome = "Batismo", Slug = "batismo", ResumoBreve = "Preparando e celebrando o passo público da fé.", Descricao = "O Ministério de Batismo acompanha e prepara cada crente que decide dar o passo público da fé, seguindo o ensinamento bíblico de Cristo. Por meio de encontros de instrução, estudo da Palavra e acolhimento pessoal, nosso objetivo é que cada batizando compreenda o significado deste ato e inicie sua caminhada cristã com fundamento sólido.", Lideranca = "Lucas César e Cleide", Icone = "fas fa-water", Ordem = 4 },
                new Ministerio { Nome = "Ação Social", Slug = "acao-social", ResumoBreve = "Expressando o amor de Cristo em ações concretas na comunidade.", Descricao = "O Ministério de Ação Social expressa o amor de Cristo em ações concretas: campanhas solidárias, distribuição de alimentos, visitas hospitalares e apoio a famílias em situação de vulnerabilidade na região do Floramar e entorno. Acreditamos que a fé sem obras é morta — e que o Evangelho transforma vidas de forma integral.", Lideranca = "Luciano", Icone = "fas fa-hands-helping", Ordem = 5 },
                new Ministerio { Nome = "Missões", Slug = "missoes", ResumoBreve = "Levando o Evangelho ao Brasil e às nações.", Descricao = "O Ministério de Missões da Comunidade Batista Floramar é movido pela Grande Comissão: fazer discípulos de todas as nações. No Brasil, apoiamos missionários em regiões de difícil acesso, em periferias urbanas e em comunidades quilombolas e indígenas. Mobilizamos nossa congregação para orar, enviar e ir — porque o campo missionário começa aqui, em BH, e vai até os confins da terra.", Lideranca = "Lella", Icone = "fas fa-globe", Link = "/Missao", Ordem = 6 },
                new Ministerio { Nome = "Família", Slug = "familia", ResumoBreve = "Fortalecendo lares e relacionamentos com base Bíblica.", Descricao = "O Ministério de Família cuida e fortalece os lares da congregação através de estudos bíblicos, aconselhamento pastoral e eventos voltados para casais, pais e filhos. Acreditamos que a família é a célula básica da sociedade e que um lar transformado pelo Evangelho reflete o amor de Cristo ao mundo.", Lideranca = "Pr. Delmo Gonçalves e Pra. Dinéia", Icone = "fas fa-home", Ordem = 7 },
                new Ministerio { Nome = "Mídia", Slug = "midia", ResumoBreve = "Comunicando a mensagem da igreja com criatividade e alcance.", Descricao = "O Ministério de Mídia é responsável pela identidade visual, redes sociais, transmissões ao vivo e comunicação interna e externa da Comunidade Batista Floramar. Com criatividade e cuidado, nossa equipe garante que a mensagem da igreja chegue longe — nas telas, nas redes e no coração das pessoas.", Lideranca = "Nívia e Wallace", Icone = "fas fa-camera", Ordem = 8 },
                new Ministerio { Nome = "Na Palavra", Slug = "na-palavra", ResumoBreve = "Vídeos, podcasts, gravações e entrevistas da igreja.", Descricao = "O Ministério Na Palavra reúne e produz os conteúdos audiovisuais da Comunidade Batista Floramar: vídeos de pregações, podcasts devocionais, gravações de cultos e entrevistas com pastores e líderes. Nosso objetivo é que a Palavra de Deus alcance cada vez mais pessoas — onde quer que estejam.", Lideranca = "Ildson e Renara", FotoLider = "/images/ministerios/lideres/LiderNaPalavra.jpeg", Icone = "fas fa-podcast", Ordem = 9 },
                new Ministerio { Nome = "Diaconato", Slug = "diaconato", ResumoBreve = "Cuidando da Igreja e recebendo cada visitante com boas-vindas.", Descricao = "O Diaconato serve à Comunidade Batista Floramar cuidando das necessidades práticas da congregação, da organização dos cultos e eventos, e do acolhimento de cada pessoa que entra pela porta da igreja. Nossa equipe do Receptivo é responsável pelas Boas-Vindas — garantindo que nenhum visitante se sinta invisível. Servir é nossa vocação.", Lideranca = "Josi e Cleiton", FotoLider = "/images/ministerios/lideres/LiderDiaconato.jpeg", Icone = "fas fa-hand-holding-heart", Ordem = 10 },
                new Ministerio { Nome = "Dança", Slug = "danca", ResumoBreve = "Adorando a Deus no palco através da dança profética.", Descricao = "O Ministério de Dança da Comunidade Batista Floramar leva a adoração a outro nível — através do movimento, da expressão corporal e da dança profética no palco, especialmente durante os momentos de louvor. Nosso grupo de dança complementa a adoração coletiva, glorificando a Deus com todo o corpo como instrumento.", Lideranca = "Thaty", Icone = "fas fa-star", Ordem = 11 },
                new Ministerio {
                    Nome = "Libras",
                    Slug = "libras",
                    ResumoBreve = "Garantindo acessibilidade com intérprete de Libras todo domingo à noite.",
                    Descricao = "O Ministério com surdos da Comunidade Batista Floramar garante que nenhuma pessoa surda fique sem acesso ao Evangelho. Todo domingo à noite, nosso intérprete está presente no culto, traduzindo em Língua Brasileira de Sinais para que os deficientes auditivos possam participar plenamente do momento de adoração e da pregação da Palavra. Inclusão é um valor do Reino.",
                    Lideranca = "Vanessa",
                    Icone = "fas fa-hands",
                    Ordem = 12
                },
                new Ministerio { Nome = "Intercessão", Slug = "intercessao", ResumoBreve = "Um ministério dedicado à oração que sustenta toda a obra de Deus na igreja.", Descricao = "O Ministério de Intercessão da Comunidade Batista Floramar é o alicerce espiritual de toda a obra da igreja. São homens e mulheres comprometidos com a oração constante — intercedendo pela congregação, pelas famílias, pelos líderes, pelos perdidos e pelas nações. Cremos que toda grande obra de Deus começa e é sustentada pelo joelho dobrado diante do Pai.", Lideranca = "Maria Helena", Icone = "fas fa-pray", Ordem = 13 },
            };

            foreach (var m in ministeriosData)
            {
                var existing = await db.Ministerios.FirstOrDefaultAsync(x => x.Slug == m.Slug);
                if (existing == null)
                {
                    db.Ministerios.Add(m);
                }
                else
                {
                    existing.Nome = m.Nome;
                    existing.ResumoBreve = m.ResumoBreve;
                    existing.Descricao = m.Descricao;
                    existing.Lideranca = m.Lideranca;
                    existing.FotoLider = m.FotoLider;
                    existing.WhatsApp = m.WhatsApp;
                    existing.Icone = m.Icone;
                    existing.Link = m.Link;
                    existing.Ordem = m.Ordem;
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedPodcastsAsync(BatistaFloramarDbContext db)
        {
            if (await db.PodcastVideos.AnyAsync()) return;

            db.PodcastVideos.AddRange(
                new PodcastVideo { Titulo = "Podcast 1", Descricao = "Mensagem completa para edificar a fé e fortalecer sua caminhada com Cristo.", YoutubeVideoId = "KwNGBInZFHI", StartSeconds = 3240, Ordem = 1 },
                new PodcastVideo { Titulo = "Podcast 2", Descricao = "Mais um episódio com ensino, direção espiritual e encorajamento para a igreja.", YoutubeVideoId = "qvDkqcE8Yf4", Ordem = 2 }
            );
            await db.SaveChangesAsync();
        }

        private static async Task SeedMinisterioFotosAsync(BatistaFloramarDbContext db)
        {
            // Registra as fotos colocadas manualmente nas pastas se ainda não existirem
            var fotosParaSeed = new[]
            {
                new { Slug = "diaconato",   Caminho = "/images/ministerios/diaconato/receptivo.jpeg",     Legenda = "Equipe do Receptivo — Boas-Vindas" },
                new { Slug = "intercessao", Caminho = "/images/ministerios/intercessao/intercessao.jpeg", Legenda = "Ministério de Intercessão" },
            };

            foreach (var item in fotosParaSeed)
            {
                var ministerio = await db.Ministerios.FirstOrDefaultAsync(m => m.Slug == item.Slug);
                if (ministerio == null) continue;

                var jaExiste = await db.MinisterioFotos
                    .AnyAsync(f => f.MinisterioId == ministerio.Id && f.CaminhoArquivo == item.Caminho);

                if (!jaExiste)
                {
                    db.MinisterioFotos.Add(new MinisterioFoto
                    {
                        MinisterioId = ministerio.Id,
                        CaminhoArquivo = item.Caminho,
                        Legenda = item.Legenda,
                        DataUpload = DateTime.UtcNow
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedEventosSemanaisAsync(BatistaFloramarDbContext db)
        {
            // Versão do seed — incrementar para forçar re-seed com dados atualizados
            const int seedVersion = 2;
            const string chave = "seed_eventos_semanais_v";

            var versaoAtual = await db.EventosSemanais.CountAsync();
            // Verifica se já está na versão 3 (Terça com 19h)
            var jaAtualizado = await db.EventosSemanais.AnyAsync(e => e.DiaSemana == "Terça" && e.Horario == "19h");
            if (jaAtualizado) return;

            // Remove dados antigos e re-insere com dados corretos
            db.EventosSemanais.RemoveRange(db.EventosSemanais);
            await db.SaveChangesAsync();

            db.EventosSemanais.AddRange(
                new EventoSemanal { DiaSemana = "Segunda",  Titulo = "Seminário Teológico",   Horario = "19h30", Descricao = "Estudo aprofundado das Escrituras para os leigos, vocacionados, chamado pastoral e ministros.",  Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Terça",    Titulo = "Terça da Oração",       Horario = "19h",   Descricao = "Momento de intercessão coletiva na presença de Deus.",                                            Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Quarta",   Titulo = "Culto de Família",      Horario = "20h",   Descricao = "Culto de oração em família e pregação temática das Escrituras.",                                   Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Sexta",    Titulo = "Culto de Jovens e Adolescentes", Horario = "20h",   Descricao = "Espaço vibrante de crescimento espiritual e conexão para os jovens e adolescentes.",                              Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Sábado",   Titulo = "Bazar Solidário",       Horario = "09h",   Descricao = "Todo primeiro sábado do mês.",                                                                     Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Sábado",   Titulo = "Curso de Libras",       Horario = "17h",   Descricao = "Curso de Língua Brasileira de Sinais para membros e amigos da comunidade.",                        Ativo = true, Ordem = 2 },
                new EventoSemanal { DiaSemana = "Sábado",   Titulo = "Futebol",               Horario = "17h30", Descricao = "Confraternização e integração através do esporte.",                                                 Ativo = true, Ordem = 3 },
                new EventoSemanal { DiaSemana = "Domingo",  Titulo = "Culto da Manhã",        Horario = "10h",   Descricao = "Culto de adoração e aprofundamento na pregação expositiva.",                                        Ativo = true, Ordem = 1 },
                new EventoSemanal { DiaSemana = "Domingo",  Titulo = "Culto da Noite",        Horario = "18h",   Descricao = "Culto de adoração e aprofundamento na pregação expositiva — com intérprete de libras presencial.", Ativo = true, Ordem = 2 }
            );
            await db.SaveChangesAsync();
        }
    }
}
