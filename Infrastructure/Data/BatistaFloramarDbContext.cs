using BatistaFloramar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Infrastructure.Data
{
    public class BatistaFloramarDbContext : DbContext
    {
        public BatistaFloramarDbContext(DbContextOptions<BatistaFloramarDbContext> options)
            : base(options)
        {
        }

        public DbSet<PerguntaPastor> PerguntasPastor => Set<PerguntaPastor>();
        public DbSet<Celula> Celulas => Set<Celula>();
        public DbSet<Ministerio> Ministerios => Set<Ministerio>();
        public DbSet<MinisterioFoto> MinisterioFotos => Set<MinisterioFoto>();
        public DbSet<PodcastVideo> PodcastVideos => Set<PodcastVideo>();
        public DbSet<AdminCredencial> AdminCredenciais => Set<AdminCredencial>();
        public DbSet<EntradaFinanceira> EntradasFinanceiras => Set<EntradaFinanceira>();
        public DbSet<SaidaFinanceira> SaidasFinanceiras => Set<SaidaFinanceira>();
        public DbSet<Evento> Eventos => Set<Evento>();
        public DbSet<PalavraDoPastor> PalavrasDoPastor => Set<PalavraDoPastor>();
        public DbSet<SerieMensagem> SeriesMensagens => Set<SerieMensagem>();
        public DbSet<EventoSemanal> EventosSemanais => Set<EventoSemanal>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PerguntaPastor>(e =>
            {
                e.ToTable("PerguntasPastor");
                e.HasKey(x => x.Id);
                e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
                e.Property(x => x.Email).HasMaxLength(180).IsRequired();
                e.Property(x => x.Telefone).HasMaxLength(30).IsRequired();
                e.Property(x => x.Assunto).HasMaxLength(200).IsRequired();
                e.Property(x => x.Pergunta).HasMaxLength(1000).IsRequired();
            });

            modelBuilder.Entity<Celula>(e =>
            {
                e.ToTable("Celulas");
                e.HasKey(x => x.Id);
                e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
                e.Property(x => x.Lideres).HasMaxLength(200);
                e.Property(x => x.Endereco).HasMaxLength(300);
                e.Property(x => x.Contato).HasMaxLength(50);
                e.Property(x => x.Horario).HasMaxLength(30).IsRequired();
                e.Property(x => x.DiaSemana).HasMaxLength(50).IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(500);
                e.Property(x => x.ImagemUrl).HasMaxLength(200);
            });

            modelBuilder.Entity<Ministerio>(e =>
            {
                e.ToTable("Ministerios");
                e.HasKey(x => x.Id);
                e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
                e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
                e.Property(x => x.ResumoBreve).HasMaxLength(300);
                e.Property(x => x.Descricao).HasMaxLength(1000).IsRequired();
                e.Property(x => x.Lideranca).HasMaxLength(200).IsRequired();
                e.Property(x => x.WhatsApp).HasMaxLength(30);
                e.Property(x => x.Icone).HasMaxLength(100);
                e.Property(x => x.Link).HasMaxLength(300);
                e.HasIndex(x => x.Slug).IsUnique();
            });

            modelBuilder.Entity<MinisterioFoto>(e =>
            {
                e.ToTable("MinisterioFotos");
                e.HasKey(x => x.Id);
                e.Property(x => x.CaminhoArquivo).HasMaxLength(500).IsRequired();
                e.Property(x => x.Legenda).HasMaxLength(300);
                e.HasOne(x => x.Ministerio)
                 .WithMany(x => x.Fotos)
                 .HasForeignKey(x => x.MinisterioId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PodcastVideo>(e =>
            {
                e.ToTable("PodcastVideos");
                e.HasKey(x => x.Id);
                e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
                e.Property(x => x.YoutubeVideoId).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<AdminCredencial>(e =>
            {
                e.ToTable("AdminCredenciais");
                e.HasKey(x => x.Id);
                e.Property(x => x.Usuario).HasMaxLength(100).IsRequired();
                e.Property(x => x.SenhaHash).HasMaxLength(256).IsRequired();
                e.HasIndex(x => x.Usuario).IsUnique();
            });

            modelBuilder.Entity<EntradaFinanceira>(e =>
            {
                e.ToTable("EntradasFinanceiras");
                e.HasKey(x => x.Id);
                e.Property(x => x.Valor).HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
                e.Property(x => x.Origem).HasMaxLength(200);
                e.Property(x => x.RegistradoPor).HasMaxLength(100);
                e.HasOne(x => x.Ministerio)
                 .WithMany()
                 .HasForeignKey(x => x.MinisterioId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SaidaFinanceira>(e =>
            {
                e.ToTable("SaidasFinanceiras");
                e.HasKey(x => x.Id);
                e.Property(x => x.Valor).HasColumnType("decimal(18,2)").IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(500).IsRequired();
                e.Property(x => x.RegistradoPor).HasMaxLength(100);
            });

            modelBuilder.Entity<Evento>(e =>
            {
                e.ToTable("Eventos");
                e.HasKey(x => x.Id);
                e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(1000);
                e.Property(x => x.Local).HasMaxLength(300);
                e.Property(x => x.ImagemBanner).HasMaxLength(500);
            });

            modelBuilder.Entity<PalavraDoPastor>(e =>
            {
                e.ToTable("PalavrasDoPastor");
                e.HasKey(x => x.Id);
                e.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
                e.Property(x => x.Conteudo).IsRequired();
                e.Property(x => x.AutorNome).HasMaxLength(150).IsRequired();
                e.Property(x => x.ImagemDestaque).HasMaxLength(500);
            });

            modelBuilder.Entity<SerieMensagem>(e =>
            {
                e.ToTable("SeriesMensagens");
                e.HasKey(x => x.Id);
                e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
                e.Property(x => x.PlaylistId).HasMaxLength(100).IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(1000);
                e.Property(x => x.ImagemCapa).HasMaxLength(500);
            });

            modelBuilder.Entity<EventoSemanal>(e =>
            {
                e.ToTable("EventosSemanais");
                e.HasKey(x => x.Id);
                e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
                e.Property(x => x.DiaSemana).HasMaxLength(20).IsRequired();
                e.Property(x => x.Horario).HasMaxLength(30).IsRequired();
                e.Property(x => x.Descricao).HasMaxLength(500);
            });
        }
    }
}
