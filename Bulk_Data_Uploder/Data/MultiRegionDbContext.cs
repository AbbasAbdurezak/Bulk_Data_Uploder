using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

public class MultiRegionDbContext : DbContext
{
    public MultiRegionDbContext(DbContextOptions<MultiRegionDbContext> options) : base(options) { }

    public DbSet<DataRecord> DataRecords { get; set; }
    public DbSet<FileUpload> FileUploads { get; set; }
    public DbSet<AIResponseVersion> AIResponseVersions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Add entity configurations
    }
}