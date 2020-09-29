#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Microsoft.EntityFrameworkCore;

namespace BrickScan.Library.Dataset.Model
{
    public class DatasetDbContext : DbContext
    {
        public DbSet<DatasetClass> DatasetClasses { get; set; } = null!;

        public DbSet<DatasetItem> DatasetItems { get; set; } = null!;

        public DbSet<DatasetImage> DatasetImages { get; set; } = null!;

        public DbSet<DatasetColor> DatasetColors { get; set; } = null!;

        public DatasetDbContext(DbContextOptions<DatasetDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DatasetImage>()
                .Property(x => x.Url)
                .HasMaxLength(2083)
                .IsRequired()
                .IsUnicode(false);

            modelBuilder.Entity<DatasetClass>()
                .Property(x => x.CreatedBy)
                .HasMaxLength(32)
                .IsRequired();

            modelBuilder.Entity<DatasetItem>()
                .Property(x => x.Number)
                .HasMaxLength(64)
                .IsRequired()
                .IsUnicode(false);

            modelBuilder.Entity<DatasetItem>()
                .Property(x => x.AdditionalIdentifier)
                .HasMaxLength(20)
                .IsUnicode(false);

            modelBuilder.Entity<DatasetColor>()
                .Property(x => x.BricklinkColorHtmlCode)
                .HasMaxLength(9)
                .IsRequired()
                .IsUnicode(false);

            modelBuilder.Entity<DatasetColor>()
                .Property(x => x.BricklinkColorName)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            modelBuilder.Entity<DatasetColor>()
                .Property(x => x.BricklinkColorType)
                .HasMaxLength(32)
                .IsRequired()
                .IsUnicode(false);

            modelBuilder.Entity<DatasetClass>().HasMany(c => c.DisplayImages)
                .WithOne(d => d.DisplayDatasetClass!)
                .HasForeignKey(d => d.DisplayDatasetClassId);
            modelBuilder.Entity<DatasetClass>().HasMany(c => c.TrainingImages)
                .WithOne(t => t.TrainDatasetClass!)
                .HasForeignKey(t => t.TrainDatasetClassId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
