﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PreOrderBlindBox.Data.Entities;

namespace PreOrderBlindBox.Data.DBContext;

public partial class Preorder_BlindBoxContext : DbContext
{
    public Preorder_BlindBoxContext()
    {
    }

    public Preorder_BlindBoxContext(DbContextOptions<Preorder_BlindBoxContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BlindBox> BlindBoxes { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<PreorderCampaign> PreorderCampaigns { get; set; }

    public virtual DbSet<PreorderMilestone> PreorderMilestones { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserVoucher> UserVouchers { get; set; }

    public virtual DbSet<VoucherCampaign> VoucherCampaigns { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

        if (!optionsBuilder.IsConfigured)
        {
            
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            string ConnectionStr = config.GetConnectionString("DefaultConnectionStringDB");

            optionsBuilder.UseSqlServer(ConnectionStr);
        }

    }
    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=MSI;Initial Catalog=Preorder_BlindBox;Persist Security Info=True;User ID=sa;Password=12345");
*/
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlindBox>(entity =>
        {
            entity.HasKey(e => e.BlindBoxId).HasName("PK__BlindBox__4FDFECB291C71803");

            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasColumnName("ImageURL");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Size).HasMaxLength(50);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__51BCD79782A9AAE1");

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.Carts)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__Carts__PreorderC__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Carts__UserID__693CA210");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Images__7516F4EC20F85887");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired();

            entity.HasOne(d => d.BlindBox).WithMany(p => p.Images)
                .HasForeignKey(d => d.BlindBoxId)
                .HasConstraintName("FK__Images__BlindBox__59FA5E80");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E326AF7AEB3");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(d => d.Receiver).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ReceiverId)
                .HasConstraintName("FK__Notificat__Recei__4316F928");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAF3F1631DA");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ReceiverAddress)
                .IsRequired()
                .HasMaxLength(300);
            entity.Property(e => e.ReceiverName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ReceiverPhone)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__Customer__6E01572D");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30C10152D0E");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.UnitEndCampaignPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPriceAtTime).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__72C60C4A");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__OrderDeta__Preor__73BA3083");
        });

        modelBuilder.Entity<PreorderCampaign>(entity =>
        {
            entity.HasKey(e => e.PreorderCampaignId).HasName("PK__Preorder__4FE60AAE0422710C");

            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.BlindBox).WithMany(p => p.PreorderCampaigns)
                .HasForeignKey(d => d.BlindBoxId)
                .HasConstraintName("FK__PreorderC__Blind__5FB337D6");
        });

        modelBuilder.Entity<PreorderMilestone>(entity =>
        {
            entity.HasKey(e => e.PreorderMilestoneId).HasName("PK__Preorder__EF1156A94656C3D0");

            entity.Property(e => e.PreorderMilestoneId).HasColumnName("PreorderMilestoneID");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.PreorderMilestones)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__PreorderM__Preor__656C112C");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A7DEAA924");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A4B3F8CAC6B");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.BalanceAtTime).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Money).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.WalletId)
                .HasConstraintName("FK__Transacti__Walle__49C3F6B7");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC3A68722B");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.WalletId).HasColumnName("WalletID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleID__3C69FB99");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Users)
                .HasForeignKey(d => d.WalletId)
                .HasConstraintName("FK__Users__WalletID__3D5E1FD2");
        });

        modelBuilder.Entity<UserVoucher>(entity =>
        {
            entity.HasKey(e => e.UserVoucherId).HasName("PK__UserVouc__8017D4B91D5599EB");

            entity.Property(e => e.UserVoucherId).HasColumnName("UserVoucherID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiringDate).HasColumnType("datetime");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.UsedAt).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VoucherCampaignId).HasColumnName("VoucherCampaignID");

            entity.HasOne(d => d.User).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserVouch__UserI__5165187F");

            entity.HasOne(d => d.VoucherCampaign).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.VoucherCampaignId)
                .HasConstraintName("FK__UserVouch__Vouch__52593CB8");
        });

        modelBuilder.Entity<VoucherCampaign>(entity =>
        {
            entity.HasKey(e => e.VoucherCampaignId).HasName("PK__VoucherC__0E161B2FA21AF985");

            entity.Property(e => e.VoucherCampaignId).HasColumnName("VoucherCampaignID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.MaximumDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaximumMoneyDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.PercentDiscount).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92E643E5A99");

            entity.Property(e => e.WalletId).HasColumnName("WalletID");
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}