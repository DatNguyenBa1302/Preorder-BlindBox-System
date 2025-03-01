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

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<BlindBox> BlindBoxes { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<PreorderCampaign> PreorderCampaigns { get; set; }

    public virtual DbSet<PreorderMilestone> PreorderMilestones { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TempCampaignBulkOrder> TempCampaignBulkOrders { get; set; }

    public virtual DbSet<TempCampaignBulkOrderDetail> TempCampaignBulkOrderDetails { get; set; }

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
        => optionsBuilder.UseSqlServer("Data Source=NGUYENDUCHUNG\\SQLEXPRESS;Initial Catalog=Preorder_BlindBox;Persist Security Info=True;User ID=sa;Password=12345;Encrypt=True");*/

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK__Banners__32E86A3194EEE24E");

            entity.Property(e => e.BannerId).HasColumnName("BannerID");
            entity.Property(e => e.CallToActionUrl)
                .HasMaxLength(500)
                .HasColumnName("CallToActionURL");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<BlindBox>(entity =>
        {
            entity.HasKey(e => e.BlindBoxId).HasName("PK__BlindBox__4FDFECB2A1788A79");

            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ListedPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("listedPrice");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Size).HasMaxLength(50);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__51BCD797783F6701");

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.Carts)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__Carts__PreorderC__59063A47");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Carts__UserID__5812160E");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__Images__7516F4ECBA4E325D");

            entity.Property(e => e.ImageId).HasColumnName("ImageID");
            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Url).IsRequired();

            entity.HasOne(d => d.BlindBox).WithMany(p => p.Images)
                .HasForeignKey(d => d.BlindBoxId)
                .HasConstraintName("FK__Images__BlindBox__47DBAE45");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E3297F6DD5D");

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
                .HasConstraintName("FK__Notificat__Recei__30F848ED");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BAFB5917727");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountMoney)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
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
            entity.Property(e => e.UserVoucherId).HasColumnName("UserVoucherID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__Customer__5CD6CB2B");

            entity.HasOne(d => d.UserVoucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserVoucherId)
                .HasConstraintName("FK__Orders__UserVouc__5DCAEF64");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D30CAB290D9A");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.UnitEndCampaignPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPriceAtTime).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__6383C8BA");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__OrderDeta__Preor__6477ECF3");
        });

        modelBuilder.Entity<PreorderCampaign>(entity =>
        {
            entity.HasKey(e => e.PreorderCampaignId).HasName("PK__Preorder__4FE60AAE911D3C44");

            entity.HasIndex(e => e.Slug, "UQ__Preorder__BC7B5FB672F0BBD5").IsUnique();

            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.BlindBoxId).HasColumnName("BlindBoxID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.PlacedOrderCount).HasDefaultValue(0);
            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(255);
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
                .HasConstraintName("FK__PreorderC__Blind__4D94879B");
        });

        modelBuilder.Entity<PreorderMilestone>(entity =>
        {
            entity.HasKey(e => e.PreorderMilestoneId).HasName("PK__Preorder__EF1156A95CD11D6C");

            entity.Property(e => e.PreorderMilestoneId).HasColumnName("PreorderMilestoneID");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.PreorderMilestones)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__PreorderM__Preor__5441852A");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE3A76C892B7");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.RoleName)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<TempCampaignBulkOrder>(entity =>
        {
            entity.HasKey(e => e.TempCampaignBulkOrderId).HasName("PK__TempCamp__6DEB814E76BE5C00");

            entity.Property(e => e.TempCampaignBulkOrderId).HasColumnName("TempCampaignBulkOrderID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DiscountMoney)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
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
            entity.Property(e => e.UserVoucherId).HasColumnName("UserVoucherID");

            entity.HasOne(d => d.Customer).WithMany(p => p.TempCampaignBulkOrders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__TempCampa__Custo__6C190EBB");

            entity.HasOne(d => d.UserVoucher).WithMany(p => p.TempCampaignBulkOrders)
                .HasForeignKey(d => d.UserVoucherId)
                .HasConstraintName("FK__TempCampa__UserV__6D0D32F4");
        });

        modelBuilder.Entity<TempCampaignBulkOrderDetail>(entity =>
        {
            entity.HasKey(e => e.TempCampaignBulkOrderDetailId).HasName("PK__TempCamp__F507B4FACACEA170");

            entity.Property(e => e.TempCampaignBulkOrderDetailId).HasColumnName("TempCampaignBulkOrderDetailID");
            entity.Property(e => e.PreorderCampaignId).HasColumnName("PreorderCampaignID");
            entity.Property(e => e.TempCampaignBulkOrderId).HasColumnName("TempCampaignBulkOrderID");
            entity.Property(e => e.UnitEndCampaignPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPriceAtTime).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.PreorderCampaign).WithMany(p => p.TempCampaignBulkOrderDetails)
                .HasForeignKey(d => d.PreorderCampaignId)
                .HasConstraintName("FK__TempCampa__Preor__73BA3083");

            entity.HasOne(d => d.TempCampaignBulkOrder).WithMany(p => p.TempCampaignBulkOrderDetails)
                .HasForeignKey(d => d.TempCampaignBulkOrderId)
                .HasConstraintName("FK__TempCampa__TempC__72C60C4A");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A4BED83F4D7");

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
                .HasConstraintName("FK__Transacti__Walle__37A5467C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC1F914168");

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
            entity.Property(e => e.EmailConfirmToken)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ForgotPasswordToken).HasMaxLength(200);
            entity.Property(e => e.ForgotPasswordTokenExpiry).HasColumnType("datetime");
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
                .HasConstraintName("FK__Users__RoleID__29572725");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Users)
                .HasForeignKey(d => d.WalletId)
                .HasConstraintName("FK__Users__WalletID__2A4B4B5E");
        });

        modelBuilder.Entity<UserVoucher>(entity =>
        {
            entity.HasKey(e => e.UserVoucherId).HasName("PK__UserVouc__8017D4B99DF2B3E6");

            entity.Property(e => e.UserVoucherId).HasColumnName("UserVoucherID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.VoucherCampaignId).HasColumnName("VoucherCampaignID");

            entity.HasOne(d => d.User).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserVouch__UserI__3F466844");

            entity.HasOne(d => d.VoucherCampaign).WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.VoucherCampaignId)
                .HasConstraintName("FK__UserVouch__Vouch__403A8C7D");
        });

        modelBuilder.Entity<VoucherCampaign>(entity =>
        {
            entity.HasKey(e => e.VoucherCampaignId).HasName("PK__VoucherC__0E161B2F07F308F5");

            entity.Property(e => e.VoucherCampaignId).HasColumnName("VoucherCampaignID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ExpiredDate).HasColumnType("datetime");
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
            entity.HasKey(e => e.WalletId).HasName("PK__Wallets__84D4F92E14676F0A");

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