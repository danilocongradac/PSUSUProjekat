using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConcentrator
{
    public class ContextClass : DbContext
    {
        public ContextClass() : base("name=SCADA_DB")
        {
            Database.SetInitializer(
                new DropCreateDatabaseIfModelChanges<ContextClass>());
        }

        public DbSet<Tag> Tags { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<ActivatedAlarm> ActivatedAlarms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>()
                .HasMany(t => t.Alarms)
                .WithRequired(a => a.Tag)  
                .HasForeignKey(a => a.TagId)
                .WillCascadeOnDelete(false); 

            base.OnModelCreating(modelBuilder);
        }

    }

}
