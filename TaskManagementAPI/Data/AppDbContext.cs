// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TbProject> TbProjects { get; set; }
    public DbSet<TbTask> TbTasks { get; set; }
    public DbSet<TbComment> TbComments { get; set; }
    public DbSet<RankSnapshot> RankSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置表名
        modelBuilder.Entity<TbProject>().ToTable("tb_projects");
        modelBuilder.Entity<TbTask>().ToTable("tb_tasks");
        modelBuilder.Entity<TbComment>().ToTable("tb_comments");
        modelBuilder.Entity<RankSnapshot>().ToTable("rank_snapshots");

        // 一对多：Project → Task
        modelBuilder.Entity<TbTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // 一对多：Task → Comment
        modelBuilder.Entity<TbComment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引
        modelBuilder.Entity<TbTask>()
            .HasIndex(t => t.ProjectId);

        modelBuilder.Entity<TbTask>()
            .HasIndex(t => t.Status);

        modelBuilder.Entity<TbComment>()
            .HasIndex(c => c.TaskId);

        modelBuilder.Entity<RankSnapshot>()
             .HasIndex(e => e.SnapshotDate);
    }
}