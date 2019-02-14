using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ef_core_principal_dependent_ids
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var appDbContext = new AppDbContext())
            {
                appDbContext.Database.EnsureDeleted();
                appDbContext.Database.EnsureCreated();
                Console.WriteLine("The database has been reset.");
                appDbContext.Users.Add(new User() {
                    Name = "John Doe",
                    Car = new Car() {
                        Make = "Tesla",
                        Model = "3",
                    },
                });

                appDbContext.SaveChanges();
            }

            using (var appDbContext = new AppDbContext())
            {
                foreach (var user in appDbContext.Users)
                {
                    Console.WriteLine($"User {user.Name} ({user.Id}) has car #{user.CarId}");
                }

                foreach (var car in appDbContext.Cars)
                {
                    Console.WriteLine($"Car {car.Make} {car.Model} ({car.Id}) has user #{car.UserId}");
                }
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
    }

    public class Car
    {
        public int Id { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer($@"Server=(localdb)\{nameof(ef_core_principal_dependent_ids)};Database={nameof(ef_core_principal_dependent_ids)};");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
               .Entity<User>()
               .HasOne(principal => principal.Car)
               .WithOne(dependant => dependant.User)
               .HasForeignKey<Car>(car => car.UserId);
        }
    }
}
