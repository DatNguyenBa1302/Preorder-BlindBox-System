﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace PreOrderBlindBox.Data.Entities;

public partial class BlindBox
{
    public int BlindBoxId { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool IsDeleted { get; set; }

    public string Size { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual ICollection<PreorderCampaign> PreorderCampaigns { get; set; } = new List<PreorderCampaign>();
}