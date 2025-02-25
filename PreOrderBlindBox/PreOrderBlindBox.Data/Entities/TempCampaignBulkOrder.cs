﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace PreOrderBlindBox.Data.Entities;

public partial class TempCampaignBulkOrder
{
    public int TempCampaignBulkOrderId { get; set; }

    public int? CustomerId { get; set; }

    public int? UserVoucherId { get; set; }

    public decimal Amount { get; set; }

    public string ReceiverName { get; set; }

    public string ReceiverPhone { get; set; }

    public string ReceiverAddress { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string Status { get; set; }

    public virtual User Customer { get; set; }

    public virtual ICollection<TempCampaignBulkOrderDetail> TempCampaignBulkOrderDetails { get; set; } = new List<TempCampaignBulkOrderDetail>();

    public virtual UserVoucher UserVoucher { get; set; }
}