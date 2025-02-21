﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PreOrderBlindBox.Data.Entities;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? PreorderCampaignId { get; set; }

    public decimal UnitPriceAtTime { get; set; }

    public int Quantity { get; set; }

    public decimal? UnitEndCampaignPrice { get; set; }
    [JsonIgnore]
    public virtual Order Order { get; set; }
	
	public virtual PreorderCampaign PreorderCampaign { get; set; }
}