namespace PDPWebsite.Services;

public class Database : DbContext
{
    public Database(DbContextOptions<Database> options) : base(options)
    {

    }

    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<SignUp> Signups { get; set; }
    public DbSet<AboutInfo> AboutInfos { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Expansion> Expansions { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ResourceFile> ResourceFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Schedule>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasMany(e => e.Signups)
                .WithOne(e => e.Schedule)
                .HasForeignKey(e => e.ScheduleId);
        });

        modelBuilder.Entity<SignUp>(options =>
        {
            options.HasKey(e => e.Id);
        });

        modelBuilder.Entity<AboutInfo>(options =>
        {
            options.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Category>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasMany(e => e.Resources)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId);
        });

        modelBuilder.Entity<Expansion>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasMany(e => e.Resources)
                .WithOne(e => e.Expansion)
                .HasForeignKey(e => e.ExpansionId);
        });

        modelBuilder.Entity<Resource>(options =>
        {
            options.HasKey(e => e.Id);
        });

        modelBuilder.Entity<ResourceFile>(options =>
        {
            options.HasKey(e => e.Id);

            options.HasOne(e => e.Resource)
                .WithMany(e => e.Files)
                .HasForeignKey(e => e.ResourceId);
        });
    }
}
